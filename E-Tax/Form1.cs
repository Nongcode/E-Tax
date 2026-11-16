using System.Diagnostics;
using System.Drawing.Printing;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PuppeteerSharp;
using Svg;

namespace E_Tax
{
    public partial class Form1 : Form
    {
        // Giới hạn của API tìm kiếm: tối đa 30 ngày mỗi lần và 50 hóa đơn mỗi trang
        private const int SearchChunkDays = 30;
        private const int SearchPageSize = 50;

        private const string BlacklistApiUrl = "https://license.metaecom.net/api/blacklist/check";
        private const string BlacklistApiToken = "VpVC1xtjIBha1VZVdBQE3z5uvw6jXmGVoAHbotLA1MDruVg4fKRY4JH1OlilistK";

        private readonly CookieContainer cookieContainer = new CookieContainer();
        private readonly HttpClient client;
        private readonly DetailGridManager _detailGridManager;
        private readonly NotesStore _notes = new NotesStore(NotesStore.DefaultPath, Path.Combine(Application.StartupPath, "notes.json"));

        private string jwtToken = "";
        private string captchaKey = "";
        private List<SearchResult> _latestResults = new List<SearchResult>();
        private string? _chromiumExecutablePath;

        public Form1()
        {
            InitializeComponent();
            ConfigureGiamThueGridColumns();
            dgvVatNop.DataError += DataGridView_DataError;
            dgvMain.DataError += DataGridView_DataError;
            dgvDetails.DataError += DataGridView_DataError;
            dgvMua.DataError += DataGridView_DataError;
            dgvBan.DataError += DataGridView_DataError;
            dgvGiamThue.DataError += DataGridView_DataError;

            var handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };
            client = new HttpClient(handler)
            {
                BaseAddress = new Uri(GdtApi.BaseUrl)
            };

            _detailGridManager = new DetailGridManager(
                client,
                dgvDetails,
                dgvMua,
                dgvBan,
                dgvVatNop,
                downloadProgressBar,
                lblDownloadStatus,
                AppendLog);
            ExcelPackage.License.SetNonCommercialPersonal("Your Name");

            panelLogin.Visible = true;
            panelSearch.Visible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                client?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void AppendLog(string message)
        {
            Logger.Write(message);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            CheckForLicense();
            _notes.Load();
            dgvMain.AutoGenerateColumns = false;
            await LoadCaptchaAsync();
        }

        #region Đăng nhập & bản quyền

        private async void btnRefreshCaptcha_Click(object sender, EventArgs e)
        {
            await LoadCaptchaAsync();
        }

        private async Task LoadCaptchaAsync()
        {
            try
            {
                AppendLog("👉 Đang tải Captcha...");

                using var req = GdtApi.CreateRequest(HttpMethod.Get, "captcha", "application/json, text/plain, */*", withPortalHeaders: true);
                req.Headers.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8");

                var response = await client.SendAsync(req);
                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");

                string raw = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw) || raw == "{}")
                {
                    AppendLog("⚠️ Response JSON rỗng hoặc không hợp lệ.");
                    return;
                }

                response.EnsureSuccessStatusCode();

                using JsonDocument doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (!root.TryGetProperty("key", out JsonElement keyEl) &&
                    !root.TryGetProperty("ckey", out keyEl) &&
                    !root.TryGetProperty("captchaKey", out keyEl))
                {
                    throw new Exception("Thiếu trường 'key' / 'ckey' trong response JSON.");
                }

                if (!root.TryGetProperty("content", out JsonElement contentEl) &&
                    !root.TryGetProperty("image", out contentEl) &&
                    !root.TryGetProperty("cvalue", out contentEl))
                {
                    throw new Exception("Thiếu trường 'content'/'image'/'cvalue' trong response JSON.");
                }

                captchaKey = keyEl.GetString();
                string svgText = contentEl.GetString();
                AppendLog($"📌 Captcha Key: {captchaKey}");

                if (string.IsNullOrEmpty(svgText))
                    throw new Exception("Trường content rỗng.");

                try
                {
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(svgText));
                    var svgDoc = SvgDocument.Open<SvgDocument>(ms);
                    SetCaptchaImage(new Bitmap(svgDoc.Draw()));
                    AppendLog("✅ Captcha (SVG) hiển thị thành công.");
                }
                catch (Exception exSvg)
                {
                    // Không render được SVG: lưu ra file tạm và hiển thị ảnh thông báo
                    string tmp = Path.Combine(Path.GetTempPath(), $"captcha_{DateTime.Now.Ticks}.svg");
                    File.WriteAllText(tmp, svgText, Encoding.UTF8);
                    AppendLog("⚠️ Không convert trực tiếp SVG -> Bitmap: " + exSvg.Message);

                    var bmp = new Bitmap(220, 80);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.DrawString("Captcha (SVG) lưu vào file", new Font("Segoe UI", 9), Brushes.Black, new PointF(6, 10));
                        g.DrawString(Path.GetFileName(tmp), new Font("Segoe UI", 8), Brushes.Gray, new PointF(6, 30));
                    }
                    SetCaptchaImage(bmp);
                }
            }
            catch (Exception ex)
            {
                AppendLog("❌ Lỗi khi load Captcha: " + ex.Message);
            }
        }

        private void SetCaptchaImage(Image image)
        {
            var old = picCaptcha.Image;
            picCaptcha.Image = image;
            old?.Dispose();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Text;
            string captchaValue = txtCaptcha.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(captchaValue))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!");
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Đang đăng nhập...";

            try
            {
                bool success = await LoginAsync(username, password, captchaValue);

                if (success)
                {
                    MessageBox.Show("✅ Đăng nhập thành công!");

                    panelLogin.Visible = false;
                    panelSearch.Visible = true;

                    _detailGridManager.SetJwtToken(jwtToken);

                    txtUser.Clear();
                    txtPass.Clear();
                    txtCaptcha.Clear();
                }
                else
                {
                    MessageBox.Show("❌ Đăng nhập thất bại! Vui lòng thử lại.");
                    await LoadCaptchaAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đăng nhập: {ex.Message}");
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Login";
            }
        }

        private async Task<bool> LoginAsync(string username, string password, string captchaValue)
        {
            try
            {
                AppendLog("👉 Gửi yêu cầu đăng nhập...");

                var payload = new
                {
                    username,
                    password,
                    cvalue = captchaValue,
                    ckey = captchaKey
                };

                using var req = GdtApi.CreateRequest(HttpMethod.Post, GdtApi.BaseUrl + "security-taxpayer/authenticate",
                    "application/json, text/plain, */*", withPortalHeaders: true);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(req);
                string respText = await response.Content.ReadAsStringAsync();
                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    AppendLog("❌ Đăng nhập trả mã lỗi.");
                    return false;
                }

                // Token có thể nằm trong cookie "jwt" hoặc trường "token" của body
                var portalUri = new Uri(GdtApi.PortalUrl);
                var cookies = cookieContainer.GetCookies(portalUri);
                if (cookies["jwt"] != null)
                {
                    jwtToken = cookies["jwt"].Value;
                }

                if (string.IsNullOrEmpty(jwtToken))
                {
                    using JsonDocument doc = JsonDocument.Parse(respText);
                    if (doc.RootElement.TryGetProperty("token", out JsonElement tokenEl))
                    {
                        jwtToken = tokenEl.GetString();
                    }
                    else
                    {
                        AppendLog("⚠️ Không tìm thấy 'token' trong response.");
                    }
                }

                if (string.IsNullOrEmpty(jwtToken))
                    return false;

                if (cookies["jwt"] == null)
                {
                    cookieContainer.Add(portalUri, new Cookie("jwt", jwtToken));
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                return true;
            }
            catch (Exception ex)
            {
                AppendLog("❌ Lỗi khi đăng nhập: " + ex.Message);
                return false;
            }
        }

        private void CheckForLicense()
        {
            switch (LicenseManager.CheckLicense())
            {
                case LicenseStatus.Activated:
                    this.Text = "E-Tax (Phiên bản đầy đủ)";
                    panelActivation.Visible = false;
                    panelLogin.Enabled = true;
                    break;

                case LicenseStatus.ValidTrial:
                    this.Text = "E-Tax (Bản dùng thử)";
                    panelActivation.Visible = false;
                    panelLogin.Enabled = true;
                    break;

                case LicenseStatus.Expired:
                    this.Text = "E-Tax (Bản dùng thử đã hết hạn)";
                    panelActivation.Visible = true;
                    panelLogin.Enabled = false;
                    MessageBox.Show("Thời gian dùng thử của bạn đã kết thúc. Vui lòng kích hoạt sản phẩm.", "Hết hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            string key = txtActivationKey.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Vui lòng nhập mã kích hoạt.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (LicenseManager.Activate(key))
            {
                MessageBox.Show("✅ Kích hoạt thành công! Cảm ơn bạn đã sử dụng sản phẩm.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CheckForLicense();
            }
            else
            {
                MessageBox.Show("❌ Mã kích hoạt không hợp lệ. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Gọi API hóa đơn

        /// <summary>
        /// Gọi API danh sách hóa đơn. Trả về JSON, hoặc chuỗi bắt đầu bằng "❌" nếu lỗi.
        /// </summary>
        private async Task<string> GetInvoiceListAsync(string endpoint, string queryString)
        {
            try
            {
                using var req = GdtApi.CreateRequest(HttpMethod.Get, $"{endpoint}?{queryString}", "application/json", jwtToken);
                var response = await client.SendAsync(req);
                string text = await response.Content.ReadAsStringAsync();
                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Lỗi khi gọi API: {text}";
                }
                return text;
            }
            catch (Exception ex)
            {
                return $"❌ Lỗi hệ thống: {ex.Message}";
            }
        }

        private static string BuildSearchQuery(DateTime from, DateTime to, InvoiceType type, int page, int size)
        {
            string search = $"tdlap=ge={from:dd/MM/yyyyTHH:mm:ss};tdlap=le={to:dd/MM/yyyyTHH:mm:ss}";

            // Hóa đơn mua vào: chỉ lấy trạng thái xử lý 5
            if (type == InvoiceType.Bought)
            {
                search += ";ttxly==5";
            }

            return $"size={size}&page={page}&sort=tdlap:desc,khmshdon:asc,shdon:desc&search={search}";
        }

        /// <summary>
        /// Tìm hóa đơn trong khoảng ngày: chia thành các lát 30 ngày, mỗi lát lấy hết các trang.
        /// Kết quả đã loại trùng theo Id, sắp theo ngày lập giảm dần; Loai_hoa_don = "Bán ra"/"Mua vào".
        /// </summary>
        private async Task<(List<SearchResult> Results, List<string> FailedChunks)> SearchInvoicesAsync(
            DateTime fromDate, DateTime toDate, bool includeSold, bool includeBought, Action<string> reportStatus)
        {
            var allResults = new List<SearchResult>();
            var failedChunks = new List<string>();
            DateTime loopStartDate = fromDate;

            while (loopStartDate <= toDate)
            {
                DateTime loopEndDate = loopStartDate.AddDays(SearchChunkDays - 1);
                if (loopEndDate > toDate) loopEndDate = toDate;
                DateTime preciseLoopEndDate = loopEndDate.Date.AddDays(1).AddTicks(-1);

                AppendLog($"🔍 Bắt đầu lát cắt: {loopStartDate:dd/MM/yyyy} - {loopEndDate:dd/MM/yyyy}");

                int currentPage = 0;
                bool keepFetchingSold = includeSold;
                bool keepFetchingBought = includeBought;

                while (keepFetchingSold || keepFetchingBought)
                {
                    reportStatus($"Đang tìm [{loopStartDate:dd/MM/yy} - {loopEndDate:dd/MM/yy}], Trang {currentPage + 1}...");

                    if (keepFetchingSold)
                        keepFetchingSold = await FetchSearchPageAsync(InvoiceType.Sold, loopStartDate, preciseLoopEndDate, currentPage, allResults, failedChunks);

                    if (keepFetchingBought)
                        keepFetchingBought = await FetchSearchPageAsync(InvoiceType.Bought, loopStartDate, preciseLoopEndDate, currentPage, allResults, failedChunks);

                    if (keepFetchingSold || keepFetchingBought)
                    {
                        currentPage++;
                        await Task.Delay(200);
                    }
                }

                loopStartDate = loopEndDate.AddDays(1);
            }

            var results = allResults
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderByDescending(r => r.Ngay_lap)
                .ThenBy(r => r.So_hoa_don)
                .ToList();
            return (results, failedChunks);
        }

        /// <summary>
        /// Lấy một trang kết quả. Trả về true nếu có thể còn trang tiếp theo.
        /// </summary>
        private async Task<bool> FetchSearchPageAsync(InvoiceType type, DateTime from, DateTime to, int page,
            List<SearchResult> results, List<string> failedChunks)
        {
            string endpoint = type == InvoiceType.Sold ? "query/invoices/sold" : "query/invoices/purchase";
            string typeLabel = type == InvoiceType.Sold ? "Bán ra" : "Mua vào";

            string response = await GetInvoiceListAsync(endpoint, BuildSearchQuery(from, to, type, page, SearchPageSize));
            if (response.StartsWith("❌"))
            {
                failedChunks.Add($"{typeLabel} [{from:dd/MM} - {to:dd/MM}], Trang {page + 1}");
                return false;
            }

            var parsed = JsonSerializer.Deserialize<SearchResponse>(response, InvoiceJson.Options);
            if (parsed?.Datas == null || !parsed.Datas.Any())
                return false;

            parsed.Datas.ForEach(item => item.Loai_hoa_don = typeLabel);
            results.AddRange(parsed.Datas);
            return parsed.Datas.Count >= SearchPageSize;
        }

        /// <summary>
        /// Gọi API chi tiết một hóa đơn. Trả về JSON, hoặc chuỗi bắt đầu bằng "❌" nếu lỗi.
        /// </summary>
        private async Task<string> GetInvoiceDetailAsync(SearchResult invoice)
        {
            if (!invoice.HasLookupKeys())
            {
                return "❌ Lỗi: Thiếu thông tin cần thiết để lấy chi tiết hóa đơn.";
            }
            if (string.IsNullOrEmpty(jwtToken))
            {
                return "❌ Lỗi: Chưa đăng nhập hoặc token không hợp lệ.";
            }

            try
            {
                string url = GdtApi.BuildDetailUrl(invoice.Ma_so_thue, invoice.Ky_hieu_hoa_don,
                    invoice.So_hoa_don!.Value, invoice.Ky_hieu_ma_so!.Value);
                using var req = GdtApi.CreateRequest(HttpMethod.Get, url, "application/json", jwtToken);

                var response = await client.SendAsync(req);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    AppendLog($"❌ Lỗi API chi tiết: {jsonResponse}");
                    return $"❌ Lỗi từ API ({response.StatusCode}): {jsonResponse}";
                }
                return jsonResponse;
            }
            catch (HttpRequestException httpEx)
            {
                AppendLog($"❌ Lỗi HTTP khi lấy chi tiết: {httpEx.Message}");
                return $"❌ Lỗi kết nối: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi không mong muốn khi lấy chi tiết: {ex}");
                return $"❌ Lỗi hệ thống: {ex.Message}";
            }
        }

        /// <summary>
        /// Tải file ZIP gốc (XML + bản thể hiện) của một hóa đơn vào thư mục saveDirectory.
        /// Trả về đường dẫn file ZIP, hoặc null nếu lỗi.
        /// </summary>
        private async Task<string?> DownloadSingleInvoiceZipAsync(SearchResult invoice, string saveDirectory)
        {
            if (invoice == null || !invoice.HasLookupKeys())
            {
                AppendLog($"⚠️ Bỏ qua hóa đơn ID {invoice?.Id} vì thiếu thông tin.");
                return null;
            }

            try
            {
                string url = $"query/invoices/export-xml?" +
                             $"nbmst={invoice.Ma_so_thue}&" +
                             $"khhdon={invoice.Ky_hieu_hoa_don}&" +
                             $"shdon={invoice.So_hoa_don.Value}&" +
                             $"khmshdon={invoice.Ky_hieu_ma_so.Value}";

                AppendLog($"📥 Đang tải ZIP cho HĐ [{invoice.Ky_hieu_hoa_don} - {invoice.So_hoa_don}]...");

                using var req = GdtApi.CreateRequest(HttpMethod.Get, url, "application/zip, */*", jwtToken, withPortalHeaders: true);
                var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    string errorText = await response.Content.ReadAsStringAsync();
                    AppendLog($"❌ Lỗi khi tải HĐ [{invoice.Ky_hieu_hoa_don} - {invoice.So_hoa_don}]: {(int)response.StatusCode} - {errorText}");
                    return null;
                }

                string fullPath = Path.Combine(saveDirectory, $"HD_{invoice.FileIdentifier}.zip");
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                AppendLog($"✅ Đã lưu thành công file: {Path.GetFileName(fullPath)}");
                return fullPath;
            }
            catch (Exception ex)
            {
                AppendLog($"⚠️ Lỗi hệ thống khi tải HĐ [{invoice.Ky_hieu_hoa_don} - {invoice.So_hoa_don}]: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Excel & PDF

        public async Task ExportSearchResultsToExcelAsync(List<SearchResult> results, string filePath)
        {
            if (results == null || !results.Any())
            {
                AppendLog("⚠️ Không có dữ liệu danh sách để xuất Excel.");
                return;
            }

            string[] headers =
            {
                "Mã số thuế", "Ký hiệu mã số", "Ký hiệu hóa đơn", "Số hóa đơn", "Ngày lập",
                "Thông tin hóa đơn", "Tổng tiền chưa thuế", "Tổng tiền thuế", "Tổng tiền chiết khấu",
                "Tổng tiền phí", "Tổng tiền thanh toán", "Đơn vị tiền tệ",
                "Trạng thái hóa đơn", "Kết quả kiểm tra", "Hóa đơn liên quan", "Loại hóa đơn"
            };

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachHoaDon");

            ws.Cells[1, 1].Value = "DANH SÁCH HÓA ĐƠN";
            ws.Cells[1, 1, 1, headers.Length].Merge = true;
            ws.Cells[1, 1].Style.Font.Size = 16;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[2, 1].Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cells[2, 1, 2, headers.Length].Merge = true;
            ws.Cells[2, 1].Style.Font.Italic = true;

            const int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
                ws.Cells[headerRow, i + 1].Value = headers[i].ToUpper();

            using (var headerRange = ws.Cells[headerRow, 1, headerRow, headers.Length])
            {
                headerRange.Style.Font.Name = "Calibri";
                headerRange.Style.Font.Size = 12;
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                SetThinBorders(headerRange);
            }

            int row = headerRow + 1;
            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                ws.Cells[row, 1].Value = item.Ma_so_thue;
                ws.Cells[row, 2].Value = item.Ky_hieu_ma_so;
                ws.Cells[row, 3].Value = item.Ky_hieu_hoa_don;
                ws.Cells[row, 4].Value = item.So_hoa_don;
                ws.Cells[row, 5].Value = item.Ngay_lap;
                ws.Cells[row, 6].Value = item.Thong_tin_hoa_don;
                ws.Cells[row, 7].Value = item.Tong_tien_chua_thue;
                ws.Cells[row, 8].Value = item.Tong_tien_thue;
                ws.Cells[row, 9].Value = item.Tong_tien_chiet_khau;
                ws.Cells[row, 10].Value = item.Tong_tien_phi?.Sum() ?? 0;
                ws.Cells[row, 11].Value = item.Tong_tien_thanh_toan;
                ws.Cells[row, 12].Value = item.Don_vi_tien_te;
                ws.Cells[row, 13].Value = item.Trang_thai_hoa_don;
                ws.Cells[row, 14].Value = item.Ket_qua_kiem_tra_hoa_don;
                ws.Cells[row, 15].Value = item.Hoa_don_lien_quan;
                ws.Cells[row, 16].Value = item.Loai_hoa_don;

                ws.Cells[row, 7, row, 11].Style.Numberformat.Format = "#,##0";

                // Màu xen kẽ
                if (i % 2 == 0)
                {
                    ws.Cells[row, 1, row, headers.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, headers.Length].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 240));
                }
                row++;
            }

            using (var dataRange = ws.Cells[headerRow, 1, row - 1, headers.Length])
            {
                SetThinBorders(dataRange);
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            await package.SaveAsAsync(new FileInfo(filePath));
            AppendLog($"✅ Đã tạo file Excel danh sách tại: {filePath}");
        }

        /// <summary>
        /// Lấy chi tiết hàng hóa của nhiều hóa đơn và xuất ra một file Excel tổng hợp.
        /// </summary>
        private async Task ExportInvoiceDetailsToExcelAsync(List<SearchResult> invoices, string filePath)
        {
            if (invoices == null || !invoices.Any())
            {
                AppendLog("⚠️ (Excel Chi Tiết) Không có dữ liệu hóa đơn để lấy chi tiết.");
                return;
            }

            var detailsList = new List<Dictionary<string, string>>();
            int globalStt = 1;

            foreach (var invoice in invoices)
            {
                if (invoice == null || !invoice.HasLookupKeys())
                {
                    AppendLog($"⚠️ (Excel Chi Tiết) Bỏ qua hóa đơn ID {invoice?.Id} vì thiếu thông tin MST/KHHĐ/SHĐ/KHMHS.");
                    continue;
                }

                try
                {
                    string jsonDetail = await GetInvoiceDetailAsync(invoice);
                    if (string.IsNullOrEmpty(jsonDetail) || jsonDetail.StartsWith("❌"))
                    {
                        AppendLog($"❌ (Excel Chi Tiết) Lỗi khi lấy chi tiết HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}: {jsonDetail}");
                        continue;
                    }

                    using var doc = JsonDocument.Parse(jsonDetail);
                    foreach (var row in InvoiceJson.BuildDetailRows(InvoiceJson.GetDataElement(doc.RootElement)))
                    {
                        row["stt"] = (globalStt++).ToString();
                        detailsList.Add(row);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"❌ (Excel Chi Tiết) Lỗi khi xử lý chi tiết HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}: {ex.Message}");
                }

                await Task.Delay(150);
            }

            if (!detailsList.Any())
            {
                AppendLog("⚠️ (Excel Chi Tiết) Không có chi tiết sản phẩm nào từ các hóa đơn hợp lệ để xuất Excel.");
                using var emptyPackage = new ExcelPackage(new FileInfo(filePath));
                var emptyWs = emptyPackage.Workbook.Worksheets.Add("Lỗi");
                emptyWs.Cells["A1"].Value = "Không tìm thấy chi tiết hàng hóa/dịch vụ nào từ các hóa đơn đã tìm.";
                await emptyPackage.SaveAsync();
                return;
            }

            var columns = InvoiceJson.DetailExportColumns;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Chi Tiết Hóa Đơn");

            ws.Cells["A1"].Value = "CHI TIẾT HÓA ĐƠN";
            ws.Cells["A1"].Style.Font.Size = 18;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells["A2"].Value = $"Ngày tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Cells["A2"].Style.Font.Italic = true;
            ws.Cells["A2"].Style.Font.Color.SetColor(Color.DarkGray);
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1, 2, columns.Length].Merge = true;

            const int headerRow = 4;
            for (int i = 0; i < columns.Length; i++)
            {
                var cell = ws.Cells[headerRow, i + 1];
                cell.Value = columns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            int rowIndex = headerRow + 1;
            foreach (var rowData in detailsList)
            {
                for (int c = 0; c < columns.Length; c++)
                {
                    rowData.TryGetValue(columns[c].Key, out var value);
                    SetDetailCellValue(ws.Cells[rowIndex, c + 1], columns[c].Key, value);
                    ws.Cells[rowIndex, c + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
                rowIndex++;
            }

            if (ws.Dimension != null)
            {
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }
            ws.Cells.Style.Font.Name = "Segoe UI";
            ws.Cells.Style.Font.Size = 10;

            await package.SaveAsAsync(new FileInfo(filePath));
            AppendLog($"✅ (Excel Chi Tiết) Đã tạo file Excel chi tiết tổng hợp tại: {filePath}");
        }

        /// <summary>
        /// Ghi giá trị vào ô Excel; cột tiền/số lượng được chuyển sang số và định dạng #,##0.
        /// </summary>
        private static void SetDetailCellValue(ExcelRange cell, string key, string? value)
        {
            if (value != null && InvoiceJson.IsNumericColumn(key) &&
                decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal numValue))
            {
                cell.Value = numValue;
                cell.Style.Numberformat.Format = "#,##0";
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }
            else
            {
                cell.Value = value;
            }
        }

        private static void SetThinBorders(ExcelRange range)
        {
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        /// <summary>
        /// Tải Chromium cho PuppeteerSharp (chỉ lần đầu) và trả về đường dẫn thực thi.
        /// </summary>
        private async Task<string> EnsureChromiumAsync()
        {
            if (!string.IsNullOrEmpty(_chromiumExecutablePath))
                return _chromiumExecutablePath;

            string previousStatus = lblDownloadStatus.Text;
            lblDownloadStatus.Text = "Đang kiểm tra/tải Chromium (chỉ lần đầu)...";
            AppendLog("   -> Đang kiểm tra/tải trình duyệt Chromium...");

            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions());
            var installedBrowser = await browserFetcher.DownloadAsync();

            string? path = null;
            if (installedBrowser != null && !string.IsNullOrEmpty(installedBrowser.BuildId))
            {
                path = browserFetcher.GetExecutablePath(installedBrowser.BuildId);
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception("Không thể xác định đường dẫn thực thi Chromium. Quá trình convert PDF không thể tiếp tục.");
            }

            lblDownloadStatus.Text = previousStatus;
            _chromiumExecutablePath = path;
            return path;
        }

        private static string? FindFirstFile(string directory, string pattern) =>
            Directory.GetFiles(directory, pattern, SearchOption.AllDirectories).FirstOrDefault();

        #endregion

        #region Tìm kiếm & lọc

        private async void btnLeftSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                MessageBox.Show("Bạn chưa đăng nhập...", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DateTime globalFromDate = dtpFromDate.Value.Date;
            DateTime globalToDate = dtpToDate.Value.Date;
            if (globalFromDate > globalToDate)
            {
                MessageBox.Show("Ngày bắt đầu không thể lớn hơn ngày kết thúc.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLeftSearch.Enabled = false;
            btnLeftSearch.Text = "Đang tìm...";
            this.Cursor = Cursors.WaitCursor;
            dgvMain.DataSource = null;
            _latestResults.Clear();

            var modal = new ProcessingForm();
            modal.Show(this);
            this.Enabled = false;
            bool showCompleted = false;

            try
            {
                var (results, failedChunks) = await SearchInvoicesAsync(
                    globalFromDate, globalToDate,
                    includeSold: rbSold.Checked || rbAllInvoices.Checked,
                    includeBought: rbBought.Checked || rbAllInvoices.Checked,
                    modal.UpdateStatus);

                modal.UpdateStatus("Đang tổng hợp kết quả...");
                _latestResults = results;

                if (_latestResults.Any())
                {
                    modal.UpdateStatus($"Đang hiển thị {_latestResults.Count} hóa đơn...");
                    BindMainGrid();

                    dgvGiamThue.DataSource = null;
                    dgvGiamThue.DataSource = _latestResults;

                    modal.UpdateStatus($"Đang tải chi tiết cho {_latestResults.Count} hóa đơn...");
                    await _detailGridManager.PopulateDetailGridAsync(_latestResults);
                    showCompleted = !failedChunks.Any();
                }
                else
                {
                    lblDownloadStatus.Text = "Không tìm thấy hóa đơn nào.";
                    MessageBox.Show("Không tìm thấy hóa đơn nào phù hợp với điều kiện tìm kiếm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dgvMain.DataSource = null;
                    dgvDetails.DataSource = null;
                    dgvMua.DataSource = null;
                    dgvBan.DataSource = null;
                    dgvVatNop.DataSource = null;
                    dgvGiamThue.DataSource = null;
                }

                if (failedChunks.Any())
                {
                    string errorList = string.Join("\n - ", failedChunks);
                    MessageBox.Show($"Đã hoàn tất tìm kiếm, tuy nhiên đã xảy ra lỗi ở các khoảng thời gian/trang sau:\n - {errorList}\n\nKết quả có thể bị thiếu.", "Lỗi trong quá trình tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (JsonException jsonEx)
            {
                AppendLog($"❌ Lỗi phân tích JSON kết quả tìm kiếm: {jsonEx}");
                MessageBox.Show($"Lỗi xử lý dữ liệu trả về: {jsonEx.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi không mong muốn khi tìm kiếm: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;
                modal.Close();

                btnLeftSearch.Enabled = true;
                btnLeftSearch.Text = "Tìm kiếm";
                this.Cursor = Cursors.Default;
            }

            // Chỉ báo hoàn tất khi có kết quả và không có lỗi (các trường hợp khác đã có thông báo riêng)
            if (showCompleted)
            {
                MessageBox.Show("✅ Hoàn tất tìm kiếm. Dữ liệu đã thêm đầy đủ vào các bảng.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Bind _latestResults vào lưới tổng hợp, điền cột loại HĐ và đổi ngày lập sang dd/MM/yyyy.
        /// </summary>
        private void BindMainGrid()
        {
            dgvMain.DataSource = _latestResults;
            UpdateGridRowNumbers();
            dgvMain.SuspendLayout();
            try
            {
                foreach (DataGridViewRow row in dgvMain.Rows)
                {
                    if (row.DataBoundItem is SearchResult item && dgvMain.Columns.Contains("colDgvLoaiHD"))
                    {
                        row.Cells["colDgvLoaiHD"].Value = item.Loai_hoa_don;
                    }
                    var cellNgayLap = row.Cells["colDgvNgayLap"];
                    if (cellNgayLap.Value is string ngayLapStr && DateTime.TryParse(ngayLapStr, out DateTime ngayLap))
                    {
                        cellNgayLap.Value = ngayLap.ToString("dd/MM/yyyy");
                    }
                }
            }
            finally
            {
                dgvMain.ResumeLayout();
            }
        }

        private void btnRightSearch_Click(object sender, EventArgs e)
        {
            string filterTaxCode = txtTimMST.Text.Trim();

            if (_latestResults == null || !_latestResults.Any())
            {
                MessageBox.Show("Chưa có dữ liệu hóa đơn để lọc. Vui lòng nhấn nút 'Tìm kiếm' trước.", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ô lọc trống -> hiển thị lại toàn bộ
            if (string.IsNullOrEmpty(filterTaxCode))
            {
                dgvMain.DataSource = null;
                dgvMain.DataSource = _latestResults;
                UpdateGridRowNumbers();
                return;
            }

            var filteredResults = _latestResults
                .Where(invoice => invoice.Ma_so_thue != null && invoice.Ma_so_thue.Contains(filterTaxCode))
                .ToList();

            dgvMain.DataSource = null;
            if (filteredResults.Any())
            {
                dgvMain.DataSource = filteredResults;
                UpdateGridRowNumbers();
            }
            else
            {
                MessageBox.Show($"Không tìm thấy hóa đơn nào có Mã số thuế chứa '{filterTaxCode}'.", "Không có kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateGridRowNumbers()
        {
            if (dgvMain.DataSource == null || !dgvMain.Columns.Contains("colDgvSTT")) return;

            dgvMain.SuspendLayout();
            for (int i = 0; i < dgvMain.Rows.Count; i++)
            {
                dgvMain.Rows[i].Cells["colDgvSTT"].Value = i + 1;
            }
            dgvMain.ResumeLayout();
        }

        #endregion

        #region Thao tác trên hóa đơn

        private async void btnTaiHDGoc_Click(object sender, EventArgs e)
        {
            btnTaiHDGoc.Text = "Đang xử lý...";
            btnTaiHDGoc.Refresh();

            this.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                await SaveOriginalInvoicesAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"FATAL ERROR in btnTaiHDGoc_Click: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi nghiêm trọng không xác định: {ex.Message}", "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;
                this.Cursor = Cursors.Default;
                btnTaiHDGoc.Text = "Tải tất cả HĐ Gốc";
            }
        }

        /// <summary>
        /// Tìm lại hóa đơn theo khoảng ngày, tải toàn bộ bản gốc và đóng gói thành một file ZIP:
        /// 0_BaoCaoExcel (DS + chi tiết), 1_HoaDonGoc_ZIP, 2_HoaDonPDF, 3_HoaDonXML.
        /// </summary>
        private async Task SaveOriginalInvoicesAsync()
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                MessageBox.Show("Bạn chưa đăng nhập hoặc token không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DateTime globalFromDate = dtpFromDate.Value.Date;
            DateTime globalToDate = dtpToDate.Value.Date;

            if (globalFromDate > globalToDate)
            {
                MessageBox.Show("Ngày bắt đầu không thể lớn hơn ngày kết thúc.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTaiHDGoc.Enabled = false;
            downloadProgressBar.Visible = true;
            lblDownloadStatus.Visible = true;
            this.Cursor = Cursors.WaitCursor;

            string baseTempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-Export_{Guid.NewGuid()}");
            string reportSubFolder = Path.Combine(baseTempDirectory, "0_BaoCaoExcel");
            string zipSubFolder = Path.Combine(baseTempDirectory, "1_HoaDonGoc_ZIP");
            string pdfSubFolder = Path.Combine(baseTempDirectory, "2_HoaDonPDF");
            string xmlSubFolder = Path.Combine(baseTempDirectory, "3_HoaDonXML");

            try
            {
                // --- Bước 1: Tìm kiếm ---
                lblDownloadStatus.Text = "Bước 1: Đang tìm kiếm hóa đơn...";
                downloadProgressBar.Style = ProgressBarStyle.Marquee;
                downloadProgressBar.Value = 0;

                var (invoicesToDownload, failedChunks) = await SearchInvoicesAsync(
                    globalFromDate, globalToDate,
                    includeSold: rbSold.Checked || rbAllInvoices.Checked,
                    includeBought: rbBought.Checked || rbAllInvoices.Checked,
                    status => lblDownloadStatus.Text = "Bước 1: " + status);

                if (!invoicesToDownload.Any())
                {
                    MessageBox.Show("Không tìm thấy hóa đơn nào trong khoảng thời gian đã chọn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowStatusMessage($"Tìm thấy {invoicesToDownload.Count} hóa đơn. Bắt đầu tải và xử lý...", Color.Green);

                // Tiến trình: 2 Excel + 3 bước (tải ZIP, giải nén, convert/copy) cho mỗi HĐ + 1 nén cuối
                downloadProgressBar.Maximum = 2 + (invoicesToDownload.Count * 3) + 1;
                downloadProgressBar.Value = 0;
                downloadProgressBar.Style = ProgressBarStyle.Blocks;

                Directory.CreateDirectory(reportSubFolder);
                Directory.CreateDirectory(zipSubFolder);
                Directory.CreateDirectory(pdfSubFolder);
                Directory.CreateDirectory(xmlSubFolder);

                // --- Bước 2, 3: Excel danh sách và chi tiết ---
                lblDownloadStatus.Text = "Bước 2: Đang tạo file Excel danh sách...";
                await ExportSearchResultsToExcelAsync(invoicesToDownload, Path.Combine(reportSubFolder, "0_DanhSachHoaDon.xlsx"));
                downloadProgressBar.PerformStep();

                lblDownloadStatus.Text = "Bước 3: Đang tạo file Excel chi tiết...";
                await ExportInvoiceDetailsToExcelAsync(invoicesToDownload, Path.Combine(reportSubFolder, "0_ChiTietHoaDon.xlsx"));
                downloadProgressBar.PerformStep();

                // --- Bước 4: Tải ZIP, giải nén, lấy PDF (hoặc convert HTML) và XML ---
                await using var pdfConverter = new PdfConverter(EnsureChromiumAsync);
                int successCount = 0;
                int convertSuccessCount = 0;

                for (int i = 0; i < invoicesToDownload.Count; i++)
                {
                    var invoice = invoicesToDownload[i];
                    string invoiceIdentifier = invoice.FileIdentifier;
                    lblDownloadStatus.Text = $"Bước 4: Xử lý HĐ ({i + 1}/{invoicesToDownload.Count})...";
                    string tempExtractPath = Path.Combine(baseTempDirectory, $"_temp_extract_{i}");
                    int currentStepBase = 2 + (i * 3);

                    try
                    {
                        string? zipFilePath = await DownloadSingleInvoiceZipAsync(invoice, zipSubFolder);
                        if (zipFilePath == null)
                        {
                            failedChunks.Add($"{invoiceIdentifier} (Lỗi tải ZIP)");
                            downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum);
                            continue;
                        }
                        successCount++;
                        downloadProgressBar.Value = Math.Min(currentStepBase + 1, downloadProgressBar.Maximum);

                        Directory.CreateDirectory(tempExtractPath);
                        await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath, true));
                        downloadProgressBar.Value = Math.Min(currentStepBase + 2, downloadProgressBar.Maximum);

                        string? pdfFile = FindFirstFile(tempExtractPath, "*.pdf");
                        string? htmlFile = FindFirstFile(tempExtractPath, "*.html");
                        string? xmlFile = FindFirstFile(tempExtractPath, "*.xml");
                        string outputPdfPath = Path.Combine(pdfSubFolder, $"HD_{invoiceIdentifier}.pdf");

                        if (pdfFile != null)
                        {
                            File.Copy(pdfFile, outputPdfPath, true);
                            convertSuccessCount++;
                        }
                        else if (htmlFile != null)
                        {
                            lblDownloadStatus.Text = $"Convert HTML... ({i + 1}/{invoicesToDownload.Count})";
                            await pdfConverter.ConvertAsync(htmlFile, outputPdfPath);
                            convertSuccessCount++;
                        }
                        else
                        {
                            AppendLog($"⚠️ (Tải gốc) Không tìm thấy PDF/HTML cho HĐ: {invoiceIdentifier}");
                            failedChunks.Add($"{invoiceIdentifier} (Không tìm thấy PDF/HTML)");
                        }

                        if (xmlFile != null)
                        {
                            File.Copy(xmlFile, Path.Combine(xmlSubFolder, $"HD_{invoiceIdentifier}.xml"), true);
                        }

                        downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum);
                    }
                    catch (Exception exConvert)
                    {
                        AppendLog($"❌ (Tải gốc) Lỗi Giải nén/Convert HĐ {invoiceIdentifier}: {exConvert.Message}");
                        failedChunks.Add($"{invoiceIdentifier} (Lỗi giải nén/convert)");
                        downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum);
                    }
                    finally
                    {
                        try { if (Directory.Exists(tempExtractPath)) Directory.Delete(tempExtractPath, true); }
                        catch (Exception exCleanTemp) { AppendLog($"⚠️ Lỗi dọn dẹp thư mục tạm {tempExtractPath}: {exCleanTemp.Message}"); }
                    }
                }

                // --- Bước 5: Nén kết quả ---
                lblDownloadStatus.Text = "Bước 5: Đang nén kết quả...";
                downloadProgressBar.Value = downloadProgressBar.Maximum - 1;

                using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục để lưu file ZIP kết quả" };

                if (fbd.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    AppendLog("ℹ️ Người dùng đã hủy chọn thư mục lưu.");
                }
                else
                {
                    string baseZipName = $"E-Tax_Export_{globalFromDate:yyyyMMdd}_den_{globalToDate:yyyyMMdd}";
                    string finalZipPath = Path.Combine(fbd.SelectedPath, baseZipName + ".zip");
                    int counter = 1;
                    while (File.Exists(finalZipPath))
                    {
                        finalZipPath = Path.Combine(fbd.SelectedPath, $"{baseZipName}_{counter}.zip");
                        counter++;
                    }

                    await Task.Run(() => ZipFile.CreateFromDirectory(baseTempDirectory, finalZipPath));

                    string successMessage = $"✅ Hoàn tất! \n\nĐã tải {successCount} HĐ gốc, xử lý/convert {convertSuccessCount} HĐ sang PDF.\n" +
                                            $"Đã lưu tất cả file (Excel, PDF, XML, ZIP gốc) vào một file nén duy nhất:\n\n{finalZipPath}";
                    if (failedChunks.Any())
                    {
                        string errorList = string.Join("\n - ", failedChunks);
                        successMessage += $"\n\nLưu ý: Đã xảy ra lỗi khi xử lý các mục sau (kết quả có thể bị thiếu):\n - {errorList}";
                    }
                    MessageBox.Show(successMessage,
                         failedChunks.Any() ? "Hoàn tất (Có lỗi)" : "Thành Công",
                         MessageBoxButtons.OK,
                         failedChunks.Any() ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }
                downloadProgressBar.Value = downloadProgressBar.Maximum;
            }
            catch (JsonException jsonEx)
            {
                AppendLog($"❌ Lỗi phân tích JSON trong SaveOriginalInvoicesAsync: {jsonEx}");
                MessageBox.Show($"Lỗi xử lý dữ liệu trả về từ API: {jsonEx.Message}", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ioEx)
            {
                AppendLog($"❌ Lỗi IO trong SaveOriginalInvoicesAsync: {ioEx}");
                MessageBox.Show($"Đã xảy ra lỗi khi xử lý file hoặc thư mục: {ioEx.Message}\n\nThường do đường dẫn quá dài, không có quyền ghi, hoặc file đang được sử dụng.", "Lỗi File/Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AppendLog($"🐞 LỖI NGHIÊM TRỌNG trong SaveOriginalInvoicesAsync: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(baseTempDirectory))
                    {
                        Directory.Delete(baseTempDirectory, true);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"⚠️ Không thể dọn dẹp file/thư mục tạm: {ex.Message}");
                }

                btnTaiHDGoc.Enabled = true;
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
                this.Cursor = Cursors.Default;
            }
        }

        private void ShowStatusMessage(string message, Color color)
        {
            lblStatusMessage.Text = message;
            lblStatusMessage.ForeColor = color;
            lblStatusMessage.Visible = true;

            statusTimer.Stop();
            statusTimer.Start();
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            lblStatusMessage.Visible = false;
            statusTimer.Stop();
        }

        /// <summary>
        /// Lấy hóa đơn đang chọn trên lưới tổng hợp; hiện thông báo và trả về null nếu không hợp lệ.
        /// </summary>
        private SearchResult? GetSelectedInvoiceWithKeys(DataGridViewRow? row, string missingKeysMessage)
        {
            if (row?.DataBoundItem is not SearchResult invoice)
            {
                MessageBox.Show("Không thể lấy dữ liệu hóa đơn từ dòng đã chọn.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            if (!invoice.HasLookupKeys())
            {
                MessageBox.Show(missingKeysMessage, "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return invoice;
        }

        private async void btnXemHD_Click(object sender, EventArgs e)
        {
            if (dgvMain.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn từ danh sách để xem.", "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SearchResult? selectedInvoice = GetSelectedInvoiceWithKeys(dgvMain.CurrentRow,
                "Hóa đơn được chọn thiếu thông tin cần thiết (MST, Ký hiệu, Số HĐ, KH Mẫu số).");
            if (selectedInvoice == null) return;

            btnXemHD.Enabled = false;
            btnXemHD.Text = "Đang tải...";
            // Thư mục tạm không bị xóa vì file hóa đơn vẫn đang được mở bằng ứng dụng ngoài
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-View_{Guid.NewGuid()}");
            string extractPath = Path.Combine(tempDirectory, "extracted");

            try
            {
                Directory.CreateDirectory(extractPath);

                string? zipFilePath = await DownloadSingleInvoiceZipAsync(selectedInvoice, tempDirectory);
                if (zipFilePath == null)
                {
                    MessageBox.Show("Không thể tải được file ZIP chứa hóa đơn.", "Lỗi tải file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi giải nén file ZIP: {ex.Message}", "Lỗi giải nén", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Ưu tiên mở HTML, nếu không có thì mở PDF
                string? fileToOpen = FindFirstFile(extractPath, "*.html") ?? FindFirstFile(extractPath, "*.pdf");

                if (fileToOpen != null)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(fileToOpen) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể tự động mở file hóa đơn: {ex.Message}\n\nBạn có thể tìm file trong thư mục:\n{extractPath}", "Lỗi mở file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Không tìm thấy file HTML hoặc PDF thể hiện hóa đơn trong file ZIP tải về. Chỉ có file dữ liệu XML.", "Không có bản thể hiện", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    try { Process.Start(new ProcessStartInfo(extractPath) { UseShellExecute = true }); } catch { }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi không mong muốn trong btnXemHD_Click: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnXemHD.Enabled = true;
                btnXemHD.Text = "Xem HĐ";
            }
        }

        private async void btnExportDS_Click(object sender, EventArgs e)
        {
            if (_latestResults == null || !_latestResults.Any())
            {
                MessageBox.Show("Chưa có dữ liệu hóa đơn nào để xuất. Vui lòng nhấn 'Tìm kiếm' trước.",
                        "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"DanhSachHoaDon_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Lưu danh sách hóa đơn"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            btnExportDS.Enabled = false;
            btnExportDS.Text = "Đang tạo...";
            downloadProgressBar.Visible = true;
            downloadProgressBar.Style = ProgressBarStyle.Marquee;
            lblDownloadStatus.Text = "Đang tạo file Excel danh sách...";
            lblDownloadStatus.Visible = true;

            try
            {
                await ExportSearchResultsToExcelAsync(_latestResults, sfd.FileName);
                MessageBox.Show($"Đã tạo và lưu thành công file Excel danh sách vào:\n{sfd.FileName}",
                    "Tạo file thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo file Excel danh sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExportDS.Enabled = true;
                btnExportDS.Text = "Tải danh sách HĐ";
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
            }
        }

        /// <summary>
        /// Xuất Excel chi tiết hàng hóa của một hóa đơn đang chọn.
        /// </summary>
        private async void btnExportChiTiet_Click(object sender, EventArgs e)
        {
            if (dgvMain.SelectedRows.Count != 1 && dgvMain.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn đúng một hóa đơn từ danh sách để xuất chi tiết.", "Chọn một hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow? selectedRow = dgvMain.SelectedRows.Count == 1 ? dgvMain.SelectedRows[0] : dgvMain.CurrentRow;
            SearchResult? selectedInvoice = GetSelectedInvoiceWithKeys(selectedRow,
                "Hóa đơn được chọn thiếu thông tin cần thiết (MST, Ký hiệu, Số HĐ, KH Mẫu số).");
            if (selectedInvoice == null) return;

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"ChiTietHD_{selectedInvoice.Ky_hieu_hoa_don}_{selectedInvoice.So_hoa_don}_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Lưu chi tiết hóa đơn"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            btnExportChiTiet.Enabled = false;
            btnExportChiTiet.Text = "Đang xử lý...";
            downloadProgressBar.Visible = true;
            downloadProgressBar.Style = ProgressBarStyle.Marquee;
            lblDownloadStatus.Text = "Đang lấy chi tiết và tạo file Excel...";
            lblDownloadStatus.Visible = true;

            try
            {
                string detailJson = await GetInvoiceDetailAsync(selectedInvoice);
                if (string.IsNullOrEmpty(detailJson) || detailJson.StartsWith("❌"))
                {
                    throw new Exception(string.IsNullOrEmpty(detailJson) ? "API không trả về dữ liệu chi tiết." : detailJson);
                }

                using var doc = JsonDocument.Parse(detailJson);
                var detailRows = InvoiceJson.BuildDetailRows(InvoiceJson.GetDataElement(doc.RootElement));
                var columns = InvoiceJson.DetailExportColumns;

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("ChiTietHoaDon");

                int currentRow = 1;
                ws.Cells[currentRow, 1].Value = "Chi tiết hàng hóa/dịch vụ";
                ws.Cells[currentRow, 1].Style.Font.Bold = true;
                ws.Cells[currentRow, 1].Style.Font.Size = 14;
                ws.Cells[currentRow, 1, currentRow, 15].Merge = true;
                ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                currentRow += 2;

                int headerRow = currentRow;
                for (int i = 0; i < columns.Length; i++)
                {
                    var cell = ws.Cells[headerRow, i + 1];
                    cell.Value = columns[i].Header;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
                currentRow++;

                if (detailRows.Any())
                {
                    foreach (var rowData in detailRows)
                    {
                        for (int c = 0; c < columns.Length; c++)
                        {
                            rowData.TryGetValue(columns[c].Key, out var value);
                            SetDetailCellValue(ws.Cells[currentRow, c + 1], columns[c].Key, value);
                        }
                        currentRow++;
                    }

                    using var dataRange = ws.Cells[headerRow, 1, currentRow - 1, columns.Length];
                    SetThinBorders(dataRange);
                }
                else
                {
                    ws.Cells[currentRow, 1].Value = "Không tìm thấy chi tiết hàng hóa/dịch vụ (mảng 'hdhhdvu').";
                    ws.Cells[currentRow, 1, currentRow, columns.Length].Merge = true;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                ws.Cells.Style.Font.Name = "Segoe UI";
                ws.Cells.Style.Font.Size = 10;

                await package.SaveAsAsync(new FileInfo(sfd.FileName));

                MessageBox.Show($"Đã xuất thành công chi tiết hóa đơn vào file:\n{sfd.FileName}", "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("❌"))
                {
                    MessageBox.Show($"Lỗi khi lấy chi tiết hóa đơn từ API: {ex.Message.Substring(2)}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Lỗi khi lấy hoặc xuất chi tiết hóa đơn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                btnExportChiTiet.Enabled = true;
                btnExportChiTiet.Text = "Tải chi tiết HĐ";
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
            }
        }

        /// <summary>
        /// In (hoặc lưu PDF nếu không có máy in) các hóa đơn đang chọn.
        /// </summary>
        private async void btnCnKoPdf_Click(object sender, EventArgs e)
        {
            List<SearchResult> selectedInvoices = new List<SearchResult>();

            if (dgvMain.SelectedRows.Count > 0)
            {
                selectedInvoices = dgvMain.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(row => row.DataBoundItem as SearchResult)
                    .Where(invoice => invoice != null)
                    .ToList()!;
            }
            else if (dgvMain.CurrentRow?.DataBoundItem is SearchResult currentInvoice)
            {
                selectedInvoices.Add(currentInvoice);
            }

            List<SearchResult> validInvoices = selectedInvoices
                .Where(invoice => invoice.HasLookupKeys())
                .OrderBy(inv => inv.Ky_hieu_hoa_don).ThenBy(inv => inv.So_hoa_don)
                .ToList();

            if (!validInvoices.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một hóa đơn hợp lệ từ danh sách để in/lưu PDF.",
                                "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnCnKoPdf.Enabled = false;
            btnCnKoPdf.Text = $"Xử lý {validInvoices.Count}...";
            downloadProgressBar.Visible = true;
            downloadProgressBar.Minimum = 0;
            downloadProgressBar.Maximum = validInvoices.Count * 4;
            downloadProgressBar.Value = 0;
            downloadProgressBar.Style = ProgressBarStyle.Blocks;
            lblDownloadStatus.Text = "Bắt đầu xử lý...";
            lblDownloadStatus.Visible = true;
            this.Cursor = Cursors.WaitCursor;

            List<string> finalPdfPaths = new List<string>();
            List<string> failedInvoicesInfo = new List<string>();
            string baseTempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-MultiPrint_{Guid.NewGuid()}");

            try
            {
                await using var pdfConverter = new PdfConverter(EnsureChromiumAsync);

                // --- Tải, giải nén và chuẩn bị file PDF cho từng hóa đơn ---
                for (int i = 0; i < validInvoices.Count; i++)
                {
                    SearchResult invoice = validInvoices[i];
                    string invoiceIdentifier = invoice.FileIdentifier;
                    string invoiceTempDir = Path.Combine(baseTempDirectory, $"Invoice_{i}_{invoiceIdentifier}");
                    string extractPath = Path.Combine(invoiceTempDir, "extracted");
                    string outputPdfPath = Path.Combine(invoiceTempDir, $"HD_{invoiceIdentifier}.pdf");
                    int currentStepBase = i * 4;

                    try
                    {
                        Directory.CreateDirectory(extractPath);

                        lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang tải HĐ {invoice.So_hoa_don}...";
                        string? zipFilePath = await DownloadSingleInvoiceZipAsync(invoice, invoiceTempDir);
                        downloadProgressBar.Value = Math.Min(currentStepBase + 1, downloadProgressBar.Maximum);

                        if (zipFilePath == null)
                        {
                            failedInvoicesInfo.Add($"{invoiceIdentifier} (Lỗi tải file ZIP)");
                            downloadProgressBar.Value = Math.Min(currentStepBase + 4, downloadProgressBar.Maximum);
                            continue;
                        }

                        lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang giải nén HĐ {invoice.So_hoa_don}...";
                        await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, extractPath, true));
                        downloadProgressBar.Value = Math.Min(currentStepBase + 2, downloadProgressBar.Maximum);

                        lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang tìm file HĐ {invoice.So_hoa_don}...";
                        string? pdfFile = FindFirstFile(extractPath, "*.pdf");
                        string? htmlFile = FindFirstFile(extractPath, "*.html");

                        if (pdfFile != null)
                        {
                            File.Move(pdfFile, outputPdfPath);
                            finalPdfPaths.Add(outputPdfPath);
                        }
                        else if (htmlFile != null)
                        {
                            lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang convert HTML sang PDF...";
                            try
                            {
                                await pdfConverter.ConvertAsync(htmlFile, outputPdfPath);
                                finalPdfPaths.Add(outputPdfPath);
                            }
                            catch (Exception exConvert)
                            {
                                AppendLog($"  -> Lỗi convert HTML->PDF HĐ {invoiceIdentifier}: {exConvert}");
                                failedInvoicesInfo.Add($"{invoiceIdentifier} (Lỗi convert HTML sang PDF)");
                            }
                        }
                        else
                        {
                            failedInvoicesInfo.Add($"{invoiceIdentifier} (Không tìm thấy file PDF/HTML)");
                        }
                        downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum);
                    }
                    catch (Exception exInvoice)
                    {
                        AppendLog($"❌ Lỗi xử lý HĐ {invoiceIdentifier}: {exInvoice}");
                        failedInvoicesInfo.Add($"{invoiceIdentifier} (Lỗi chung: {exInvoice.Message})");
                        downloadProgressBar.Value = Math.Min(currentStepBase + 4, downloadProgressBar.Maximum);
                    }
                }

                if (!finalPdfPaths.Any())
                {
                    MessageBox.Show("Không có file PDF nào được tạo/tìm thấy để thực hiện.", "Không có kết quả", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    downloadProgressBar.Value = downloadProgressBar.Maximum;
                    return;
                }

                // --- Có máy in thì in, không thì lưu PDF ---
                lblDownloadStatus.Text = "Kiểm tra máy in...";
                bool hasPrinters;
                try
                {
                    hasPrinters = PrinterSettings.InstalledPrinters.Count > 0;
                }
                catch (Exception exPrinterCheck)
                {
                    AppendLog($"⚠️ Lỗi khi kiểm tra danh sách máy in: {exPrinterCheck.Message}. Giả định không có máy in.");
                    hasPrinters = false;
                }

                downloadProgressBar.Maximum = downloadProgressBar.Value + finalPdfPaths.Count;

                if (hasPrinters)
                {
                    await PrintPdfFilesAsync(finalPdfPaths, failedInvoicesInfo);
                }
                else
                {
                    SavePdfFilesToFolder(finalPdfPaths, failedInvoicesInfo);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi nghiêm trọng trong btnCnKoPdf_Click: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn trong quá trình xử lý: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                downloadProgressBar.Value = downloadProgressBar.Maximum;
            }
            finally
            {
                btnCnKoPdf.Enabled = true;
                btnCnKoPdf.Text = "In HĐ";
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
                this.Cursor = Cursors.Default;

                try
                {
                    if (Directory.Exists(baseTempDirectory))
                    {
                        Directory.Delete(baseTempDirectory, true);
                    }
                }
                catch (Exception exClean)
                {
                    MessageBox.Show($"Không thể tự động xóa thư mục tạm:\n{baseTempDirectory}\n\nVui lòng xóa thủ công.\nLỗi: {exClean.Message}",
                                    "Lỗi dọn dẹp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private async Task PrintPdfFilesAsync(List<string> pdfPaths, List<string> failedInvoicesInfo)
        {
            lblDownloadStatus.Text = $"Đang mở hộp thoại in ({pdfPaths.Count} file)...";
            int sentCount = 0;
            foreach (string pdfPath in pdfPaths)
            {
                lblDownloadStatus.Text = $"Đang gửi file {Path.GetFileName(pdfPath)} đến máy in...";
                try
                {
                    if (!File.Exists(pdfPath))
                    {
                        failedInvoicesInfo.Add($"{Path.GetFileNameWithoutExtension(pdfPath)} (File PDF tạm không tồn tại)");
                        downloadProgressBar.PerformStep();
                        continue;
                    }
                    Process.Start(new ProcessStartInfo(pdfPath) { Verb = "print", UseShellExecute = true, CreateNoWindow = true });
                    sentCount++;
                    await Task.Delay(1000);
                }
                catch (Exception exPrint)
                {
                    AppendLog($"  -> Lỗi gửi lệnh in cho {Path.GetFileName(pdfPath)}: {exPrint}");
                    failedInvoicesInfo.Add($"{Path.GetFileNameWithoutExtension(pdfPath)} (Lỗi gửi lệnh in)");
                }
                downloadProgressBar.PerformStep();
            }

            string message = $"Đã gửi {sentCount}/{pdfPaths.Count} hóa đơn đến máy in.";
            if (failedInvoicesInfo.Any())
            {
                message += $"\n\nCác hóa đơn gặp lỗi:\n - {string.Join("\n - ", failedInvoicesInfo)}";
            }
            MessageBox.Show(message,
                            failedInvoicesInfo.Any() ? "In hóa đơn (Có lỗi)" : "In hóa đơn",
                            MessageBoxButtons.OK,
                            failedInvoicesInfo.Any() ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private void SavePdfFilesToFolder(List<string> pdfPaths, List<string> failedInvoicesInfo)
        {
            MessageBox.Show("Không tìm thấy máy in nào được cài đặt.\nChương trình sẽ chuyển sang chế độ Lưu file PDF về máy tính.",
                            "Không tìm thấy máy in", MessageBoxButtons.OK, MessageBoxIcon.Information);

            lblDownloadStatus.Text = "Chọn thư mục để lưu PDF...";
            using var fbd = new FolderBrowserDialog
            {
                Description = $"Không tìm thấy máy in. Chọn thư mục để lưu {pdfPaths.Count} file PDF",
                UseDescriptionForTitle = true
            };

            if (fbd.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                MessageBox.Show("Đã hủy thao tác lưu PDF.", "Đã hủy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                downloadProgressBar.Value = downloadProgressBar.Maximum;
                return;
            }

            lblDownloadStatus.Text = $"Đang lưu {pdfPaths.Count} file PDF...";
            int saveSuccessCount = 0;
            foreach (string pdfPath in pdfPaths)
            {
                lblDownloadStatus.Text = $"Đang lưu file {Path.GetFileName(pdfPath)}...";
                try
                {
                    if (!File.Exists(pdfPath))
                    {
                        failedInvoicesInfo.Add($"{Path.GetFileNameWithoutExtension(pdfPath)} (File PDF tạm không tồn tại)");
                        downloadProgressBar.PerformStep();
                        continue;
                    }

                    string originalFileName = Path.GetFileName(pdfPath);
                    string destFileName = Path.Combine(fbd.SelectedPath, originalFileName);
                    int counter = 1;
                    while (File.Exists(destFileName))
                    {
                        destFileName = Path.Combine(fbd.SelectedPath,
                            $"{Path.GetFileNameWithoutExtension(originalFileName)}_{counter}{Path.GetExtension(originalFileName)}");
                        counter++;
                    }
                    File.Copy(pdfPath, destFileName);
                    saveSuccessCount++;
                }
                catch (Exception exSave)
                {
                    AppendLog($"  -> Lỗi lưu file {Path.GetFileName(pdfPath)}: {exSave}");
                    failedInvoicesInfo.Add($"{Path.GetFileNameWithoutExtension(pdfPath)} (Lỗi lưu file)");
                }
                downloadProgressBar.PerformStep();
            }

            MessageBox.Show($"Đã lưu thành công {saveSuccessCount}/{pdfPaths.Count} hóa đơn thành file PDF vào thư mục:\n{fbd.SelectedPath}\n\n" +
                            (failedInvoicesInfo.Count > 0 ? $"Lưu ý: Có {failedInvoicesInfo.Count} hóa đơn gặp lỗi." : ""),
                            "Lưu PDF thành công", MessageBoxButtons.OK,
                            failedInvoicesInfo.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        #endregion

        #region Tra cứu hóa đơn

        private async void dgvMain_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Chỉ xử lý click vào cột nút "Tra cứu"
            if (e.RowIndex < 0 || e.ColumnIndex != dgvMain.Columns["colDgvTraCuu"].Index) return;

            if (dgvMain.Rows[e.RowIndex].DataBoundItem is not SearchResult selectedInvoice)
            {
                MessageBox.Show("Không thể lấy dữ liệu hóa đơn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!selectedInvoice.HasLookupKeys())
            {
                MessageBox.Show("Hóa đơn thiếu thông tin cần thiết để tra cứu (MST/KHHĐ/SHĐ/KHMHS).", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cellButton = dgvMain.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
            if (cellButton != null) cellButton.Value = "Đang...";
            dgvMain.Cursor = Cursors.WaitCursor;

            try
            {
                string jsonDetail = await GetInvoiceDetailAsync(selectedInvoice);
                if (string.IsNullOrEmpty(jsonDetail) || jsonDetail.StartsWith("❌"))
                {
                    MessageBox.Show($"Lỗi khi lấy chi tiết hóa đơn:\n{jsonDetail}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string maTraCuu = "";
                string nhaCungCap = "";
                string diaChiNB = "";
                string tenNB = selectedInvoice.Thong_tin_hoa_don;
                string mstNB = selectedInvoice.Ma_so_thue;

                try
                {
                    using JsonDocument doc = JsonDocument.Parse(jsonDetail);
                    JsonElement root = InvoiceJson.GetDataElement(doc.RootElement);

                    // Mã tra cứu: ưu tiên "mhdon", sau đó "id" trong chi tiết, cuối cùng là Id trong danh sách
                    if (root.TryGetProperty("mhdon", out JsonElement maElement) && maElement.ValueKind == JsonValueKind.String)
                    {
                        maTraCuu = maElement.GetString();
                    }
                    else if (root.TryGetProperty("id", out JsonElement idElement) && idElement.ValueKind == JsonValueKind.String)
                    {
                        maTraCuu = idElement.GetString();
                    }
                    else if (!string.IsNullOrEmpty(selectedInvoice.Id))
                    {
                        maTraCuu = selectedInvoice.Id;
                    }

                    if (root.TryGetProperty("ngcnhat", out JsonElement nccElement) && nccElement.ValueKind == JsonValueKind.String)
                    {
                        nhaCungCap = nccElement.GetString()?.ToLower() ?? "";
                    }

                    if (root.TryGetProperty("nbdchi", out JsonElement dcElement) && dcElement.ValueKind == JsonValueKind.String)
                    {
                        diaChiNB = dcElement.GetString();
                    }
                }
                catch (JsonException)
                {
                    MessageBox.Show("Lỗi đọc dữ liệu chi tiết hóa đơn.", "Lỗi JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string? linkTraCuu = BuildTraCuuLink(mstNB, nhaCungCap);
                if (string.IsNullOrEmpty(linkTraCuu))
                {
                    MessageBox.Show($"Không hỗ trợ tạo link tra cứu tự động cho nhà cung cấp '{nhaCungCap}'.\nBạn có thể tự truy cập trang tra cứu của họ.", "Chưa hỗ trợ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    linkTraCuu = "(Không thể tạo tự động)";
                }

                using var traCuuForm = new TraCuuForm(maTraCuu, linkTraCuu, tenNB, mstNB, diaChiNB);
                traCuuForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi không mong muốn khi xử lý tra cứu: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (cellButton != null) cellButton.Value = "Tải";
                dgvMain.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Link trang tra cứu theo nhà cung cấp hóa đơn (trường "ngcnhat"). Trả về null nếu chưa hỗ trợ.
        /// Các URL dưới đây là mẫu, cần kiểm tra lại với từng nhà cung cấp.
        /// </summary>
        private static string? BuildTraCuuLink(string mst, string nhaCungCap)
        {
            if (string.IsNullOrEmpty(mst) || string.IsNullOrEmpty(nhaCungCap))
            {
                return null;
            }

            switch (nhaCungCap.ToLower())
            {
                case "tvan_misa":
                    return $"https://{mst}.meinvoice.vn";

                case "tvan_bkav":
                    return $"https://{mst}.bkav.com/TraCuu";

                case "tvan_vnpt":
                case "tvan_buuchinhvt":
                    return $"https://{mst}-tt78.vnpt-invoice.com.vn";

                case "tvan_viettel":
                    return $"https://{mst}.viettel-einvoice.vn";

                case "tvan_thaison":
                    return $"https://{mst}.einvoice.vn/tracuu";

                default:
                    return null;
            }
        }

        #endregion

        #region Lưới bảng kê giảm thuế

        private void ConfigureGiamThueGridColumns()
        {
            GridColumnHelper.ApplyDefaultStyle(dgvGiamThue);

            GridColumnHelper.AddTextColumn(dgvGiamThue, "colGiamThueKyHieuHoaDon", "Ký hiệu hóa đơn", nameof(SearchResult.Ky_hieu_hoa_don), 100, frozen: true, alignment: DataGridViewContentAlignment.MiddleCenter);
            GridColumnHelper.AddTextColumn(dgvGiamThue, "colGiamThueSoHD", "Số hóa đơn", nameof(SearchResult.So_hoa_don), 100, frozen: true, alignment: DataGridViewContentAlignment.MiddleCenter);
            GridColumnHelper.AddNumericColumn(dgvGiamThue, "colGiamThueTienChuaThue", "Tổng tiền chưa thuế", nameof(SearchResult.Tong_tien_chua_thue), 150, "#,##0");
            GridColumnHelper.AddNumericColumn(dgvGiamThue, "colGiamThueTienThue", "Tổng tiền thuế", nameof(SearchResult.Tong_tien_thue), 140, "#,##0");
            GridColumnHelper.AddNumericColumn(dgvGiamThue, "colGiamThueTienCK", "Tổng tiền chiết khấu", nameof(SearchResult.Tong_tien_chiet_khau), 160, "#,##0");
            // Không bind: giá trị được tính trong CellFormatting
            GridColumnHelper.AddNumericColumn(dgvGiamThue, "colGiamThueTongPhi", "Tổng tiền phí", null, 140, "#,##0");
            GridColumnHelper.AddNumericColumn(dgvGiamThue, "colGiamThueTongTT", "Tổng tiền thanh toán", nameof(SearchResult.Tong_tien_thanh_toan), 160, "#,##0");
            GridColumnHelper.AddTextColumn(dgvGiamThue, "colGiamThueDVT", "Đơn vị tiền tệ", nameof(SearchResult.Don_vi_tien_te), 100);

            dgvGiamThue.CellFormatting += DgvGiamThue_CellFormatting;
        }

        /// <summary>
        /// Hiển thị "0" cho tiền null và tính tổng tiền phí.
        /// </summary>
        private void DgvGiamThue_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvGiamThue.Rows[e.RowIndex].DataBoundItem is not SearchResult invoice) return;

            string? formatted = dgvGiamThue.Columns[e.ColumnIndex].Name switch
            {
                "colGiamThueTienChuaThue" => invoice.Tong_tien_chua_thue?.ToString("#,##0") ?? "0",
                "colGiamThueTienThue" => invoice.Tong_tien_thue?.ToString("#,##0") ?? "0",
                "colGiamThueTienCK" => invoice.Tong_tien_chiet_khau?.ToString("#,##0") ?? "0",
                "colGiamThueTongTT" => invoice.Tong_tien_thanh_toan?.ToString("#,##0") ?? "0",
                "colGiamThueTongPhi" => (invoice.Tong_tien_phi?.Sum(fee => fee ?? 0) ?? 0).ToString("#,##0"),
                _ => null
            };

            if (formatted != null)
            {
                e.Value = formatted;
                e.FormattingApplied = true;
            }
        }

        private void DataGridView_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            var dgv = sender as DataGridView;
            AppendLog($"‼️ Lỗi DataError | Grid: {dgv?.Name} | Row: {e.RowIndex}, Col: {e.ColumnIndex} | Error: {e.Exception?.Message} | Context: {e.Context}");
            e.ThrowException = false;
        }

        #endregion

        #region Ghi chú

        private void dgvMain_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvMain.CurrentRow?.DataBoundItem is SearchResult selectedInvoice)
            {
                txtGhiChu.Text = _notes.Get(selectedInvoice.Id);
                txtKtraMaDoanhNghiep.Text = selectedInvoice.Ma_so_thue;
            }
            else
            {
                txtGhiChu.Text = "";
                txtKtraMaDoanhNghiep.Text = "";
            }
        }

        private void btnMoGhiChu_Click(object sender, EventArgs e)
        {
            if (dgvMain.CurrentRow?.DataBoundItem is not SearchResult selectedInvoice)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn để ghi chú.", "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _notes.Set(selectedInvoice.Id, txtGhiChu.Text);
            try
            {
                _notes.Save();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi khi lưu file ghi chú: {ex.Message}");
                MessageBox.Show($"Không thể lưu file ghi chú: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show("Đã lưu ghi chú thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Kiểm tra doanh nghiệp rủi ro

        private async void btnKtraDNMuaLe_Click(object sender, EventArgs e)
        {
            string taxCode = txtKtraMaDoanhNghiep.Text.Trim();
            if (string.IsNullOrEmpty(taxCode))
            {
                MessageBox.Show("Vui lòng nhập một Mã số thuế vào ô 'Nhập mã doanh nghiệp' (hoặc click vào một hóa đơn trên lưới để chọn).",
                        "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtKtraMaDoanhNghiep.Focus();
                return;
            }

            string apiUrl = $"{BlacklistApiUrl}?tax_code={Uri.EscapeDataString(taxCode)}&token={Uri.EscapeDataString(BlacklistApiToken)}";

            btnKtraDNMuaLe.Enabled = false;
            btnKtraDNMuaLe.Text = "Đang KT...";
            this.Cursor = Cursors.WaitCursor;

            string jsonResponse = "";
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(GdtApi.BrowserUserAgent);

                var response = await httpClient.GetAsync(apiUrl);
                jsonResponse = await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("success", out JsonElement successEl) && successEl.ValueKind == JsonValueKind.False)
                {
                    string errorMessage = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Lỗi không xác định.";
                    throw new Exception($"API báo lỗi: {errorMessage}");
                }

                if (root.TryGetProperty("error", out JsonElement errorEl) && errorEl.ValueKind == JsonValueKind.String)
                {
                    throw new Exception($"API báo lỗi: {errorEl.GetString()}");
                }

                // "blacklisted" có thể nằm trong "data" hoặc ở gốc
                string tenDoanhNghiep = "";
                JsonElement blacklistedElement = default;
                bool foundBlacklistProp = false;

                if (root.TryGetProperty("data", out JsonElement dataEl) && dataEl.ValueKind == JsonValueKind.Object)
                {
                    if (dataEl.TryGetProperty("name", out JsonElement nameEl) && nameEl.ValueKind == JsonValueKind.String)
                    {
                        tenDoanhNghiep = nameEl.GetString();
                    }
                    foundBlacklistProp = dataEl.TryGetProperty("blacklisted", out blacklistedElement);
                }

                if (!foundBlacklistProp)
                {
                    foundBlacklistProp = root.TryGetProperty("blacklisted", out blacklistedElement);
                }

                if (!foundBlacklistProp)
                {
                    throw new Exception($"Phản hồi JSON không hợp lệ. Không tìm thấy 'blacklisted'.\nPhản hồi nhận được: {jsonResponse}");
                }

                // Giá trị có thể là bool, chuỗi "true"/"1" hoặc số 1
                bool isBlacklisted = blacklistedElement.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.String => blacklistedElement.GetString() is string s &&
                                            (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1"),
                    JsonValueKind.Number => blacklistedElement.TryGetInt32(out int intValue) && intValue == 1,
                    _ => false
                };

                string message = $"Mã số thuế: {taxCode}\n" +
                                 (!string.IsNullOrEmpty(tenDoanhNghiep) ? $"(Tên: {tenDoanhNghiep})\n\n" : "\n");

                if (isBlacklisted)
                {
                    MessageBox.Show(message + "Trạng thái: 🔴 Công ty nằm trong danh sách cảnh báo rủi ro!",
                            "Nguy Hiểm", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show(message + "Trạng thái: ✅ An toàn (Công ty không nằm trong danh sách cảnh báo rủi ro).",
                            "An Toàn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (JsonException jsonEx)
            {
                MessageBox.Show($"Lỗi đọc phản hồi từ máy chủ: {jsonEx.Message}\nPhản hồi gốc: {jsonResponse}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"Lỗi kết nối đến máy chủ kiểm tra: {httpEx.Message}", "Lỗi mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnKtraDNMuaLe.Enabled = true;
                btnKtraDNMuaLe.Text = "Ktra DN Mua Lẻ";
                this.Cursor = Cursors.Default;
            }
        }

        #endregion
    }
}
