using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Svg;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;

namespace E_Tax
{
    public enum InvoiceType { Sold, Bought };
    public partial class Form1 : Form, IDisposable
    {
        private readonly CookieContainer cookieContainer = new CookieContainer();
        private readonly HttpClient client;
        private string jwtToken = "";
        private string captchaKey = "";
        private List<SearchResult> _latestResults = new List<SearchResult>();
        private string _lastSuccessfulQueryString = "";
        private readonly string BrowserUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36";
        private bool disposed = false;
        private readonly DetailGridManager _detailGridManager;
        private Dictionary<string, string> _invoiceNotes = new Dictionary<string, string>();
        private string _notesFilePath = Path.Combine(Application.StartupPath, "notes.json");

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
                BaseAddress = new Uri("https://hoadondientu.gdt.gov.vn:30000/")
            };

            _detailGridManager = new DetailGridManager(
                 client,
                 dgvDetails,
                 dgvMua,
                 dgvBan,
                 dgvVatNop,
                 downloadProgressBar, // ProgressBar dùng chung
                 lblDownloadStatus,   // Label trạng thái dùng chung
                 AppendLog,           // Hàm ghi log của Form1
                 BrowserUserAgent     // UserAgent
             );
            ExcelPackage.License.SetNonCommercialPersonal("Your Name");

            panelLogin.Visible = true;
            panelSearch.Visible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    client?.Dispose();
                }
                disposed = true;
            }
            base.Dispose(disposing);
        }

        private void AppendLog(string message)
        {

        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            CheckForLicense();
            LoadNotesFromFile();
            dgvMain.AutoGenerateColumns = false;
            await LoadCaptchaAsync();

        }

        private async void btnRefreshCaptcha_Click(object sender, EventArgs e)
        {
            await LoadCaptchaAsync();
        }

        private async Task LoadCaptchaAsync()
        {
            try
            {
                AppendLog("👉 Đang tải Captcha...");
                string captchaApi = "captcha";
                string fullUrl = new Uri(client.BaseAddress, captchaApi).ToString();
                AppendLog("URL gọi: " + fullUrl);

                using var req = new HttpRequestMessage(HttpMethod.Get, captchaApi);
                req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                req.Headers.Add("Accept", "application/json, text/plain, */*");
                req.Headers.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8");
                req.Headers.Add("Referer", "https://hoadondientu.gdt.gov.vn/");
                req.Headers.Add("Origin", "https://hoadondientu.gdt.gov.vn");

                var response = await client.SendAsync(req);
                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");

                string raw = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw) || raw == "{}")
                {
                    AppendLog("⚠️ Response JSON rỗng hoặc không hợp lệ.");
                    return;
                }

                AppendLog("Dữ liệu trả về:\r\n" + (raw.Length > 2000 ? raw.Substring(0, 2000) + "..." : raw));

                response.EnsureSuccessStatusCode();

                using (JsonDocument doc = JsonDocument.Parse(raw))
                {
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("key", out JsonElement keyEl) &&
                        !root.TryGetProperty("ckey", out keyEl) &&
                        !root.TryGetProperty("captchaKey", out keyEl))
                    {
                        throw new Exception("Thiếu trường 'key' / 'ckey' trong response JSON.");
                    }

                    JsonElement contentEl;
                    if (root.TryGetProperty("content", out contentEl) ||
                        root.TryGetProperty("image", out contentEl) ||
                        root.TryGetProperty("cvalue", out contentEl))
                    {
                        // ok
                    }
                    else
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
                        var bmp = svgDoc.Draw();

                        if (picCaptcha.Image != null)
                        {
                            var old = picCaptcha.Image;
                            picCaptcha.Image = null;
                            old.Dispose();
                        }

                        picCaptcha.Image = new Bitmap(bmp);
                        AppendLog("✅ Captcha (SVG) hiển thị thành công.");
                    }
                    catch (Exception exSvg)
                    {
                        string tmp = Path.Combine(Path.GetTempPath(), $"captcha_{DateTime.Now.Ticks}.svg");
                        File.WriteAllText(tmp, svgText, Encoding.UTF8);
                        AppendLog("⚠️ Không convert trực tiếp SVG -> Bitmap: " + exSvg.Message);
                        AppendLog("🔸 Đã lưu SVG tạm: " + tmp);

                        var bmp = new Bitmap(220, 80);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.Clear(Color.White);
                            g.DrawString("Captcha (SVG) lưu vào file", new Font("Segoe UI", 9), Brushes.Black, new PointF(6, 10));
                            g.DrawString(Path.GetFileName(tmp), new Font("Segoe UI", 8), Brushes.Gray, new PointF(6, 30));
                        }

                        if (picCaptcha.Image != null)
                        {
                            var old = picCaptcha.Image;
                            picCaptcha.Image = null;
                            old.Dispose();
                        }
                        picCaptcha.Image = bmp;
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                AppendLog("❌ Lỗi HTTP khi gọi Captcha: " + httpEx.Message);
            }
            catch (JsonException jsonEx)
            {
                AppendLog("❌ Lỗi parse JSON Captcha: " + jsonEx.Message);
            }
            catch (FormatException fmtEx)
            {
                AppendLog("❌ Lỗi Format (Base64?) Captcha: " + fmtEx.Message);
            }
            catch (Exception ex)
            {
                AppendLog("❌ Lỗi khác khi load Captcha: " + ex.Message);
            }
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Text.Trim();
            string captchaValue = txtCaptcha.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(captchaValue))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!");
                return;
            }

            // Tắt nút để tránh nhấn nhiều lần
            btnLogin.Enabled = false;
            btnLogin.Text = "Đang đăng nhập...";

            try
            {
                bool success = await LoginAsync(username, password, captchaValue);

                if (success)
                {
                    MessageBox.Show("✅ Đăng nhập thành công!");

                    // Ẩn panel đăng nhập, hiển thị panel tìm kiếm
                    panelLogin.Visible = false;
                    panelSearch.Visible = true;

                    _detailGridManager.SetJwtToken(jwtToken);

                    // (Tùy chọn) Xóa các giá trị đăng nhập để tránh lưu lại
                    txtUser.Clear();
                    txtPass.Clear();
                    txtCaptcha.Clear();
                }
                else
                {
                    MessageBox.Show("❌ Đăng nhập thất bại! Vui lòng thử lại.");
                    await LoadCaptchaAsync(); // Làm mới captcha
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

                string jsonBody = JsonSerializer.Serialize(payload);

                using var req = new HttpRequestMessage(HttpMethod.Post,
                    "https://hoadondientu.gdt.gov.vn:30000/security-taxpayer/authenticate")
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                req.Headers.Add("Accept", "application/json, text/plain, */*");
                req.Headers.Add("Referer", "https://hoadondientu.gdt.gov.vn/");
                req.Headers.Add("Origin", "https://hoadondientu.gdt.gov.vn");

                var response = await client.SendAsync(req);
                string respText = await response.Content.ReadAsStringAsync();

                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                AppendLog("Response body:\r\n" + (respText.Length > 2000 ? respText.Substring(0, 2000) + "..." : respText));

                if (!response.IsSuccessStatusCode)
                {
                    AppendLog("❌ Đăng nhập trả mã lỗi.");
                    return false;
                }

                var cookies = cookieContainer.GetCookies(new Uri("https://hoadondientu.gdt.gov.vn/"));
                foreach (Cookie ck in cookies)
                {
                    AppendLog($"🍪 Cookie nhận được: {ck.Name}={ck.Value}");
                    if (ck.Name == "jwt")
                    {
                        jwtToken = ck.Value;
                    }
                }

                if (string.IsNullOrEmpty(jwtToken))
                {
                    using (JsonDocument doc = JsonDocument.Parse(respText))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("token", out JsonElement tokenEl))
                        {
                            jwtToken = tokenEl.GetString();
                            AppendLog("✅ Nhận token (prefix): " + (jwtToken?.Length > 30 ? jwtToken.Substring(0, 30) + "..." : jwtToken));
                        }
                        else
                        {
                            AppendLog("⚠️ Không tìm thấy 'token' trong response.");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(jwtToken))
                {
                    if (cookies["jwt"] == null)
                    {
                        cookieContainer.Add(new Uri("https://hoadondientu.gdt.gov.vn/"), new Cookie("jwt", jwtToken));
                        AppendLog("🍪 Thêm cookie jwt thủ công.");
                    }

                    try
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                        AppendLog("🔐 Đã set Authorization header cho HttpClient.");
                    }
                    catch (Exception exAuth)
                    {
                        AppendLog("⚠️ Không thể set DefaultRequestHeaders.Authorization: " + exAuth.Message);
                    }

                    AppendLog("🔐 Lưu jwtToken vào biến toàn cục thành công.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                AppendLog("❌ Lỗi khi đăng nhập: " + ex.Message);
                return false;
            }
        }

        private async Task<string> GetProductsAsync(string endpoint, string queryString)
        {
            try
            {
                string fullUrl = $"{endpoint}?{queryString}";
                AppendLog($"👉 Đang gọi API: {fullUrl}");

                using var req = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                req.Headers.Add("Accept", "application/json");

                if (!string.IsNullOrEmpty(jwtToken))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                }

                var response = await client.SendAsync(req);
                string text = await response.Content.ReadAsStringAsync();
                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    // Trả về chuỗi lỗi có chứa cả nội dung JSON
                    return $"❌ Lỗi khi gọi API: {text}";
                }

                return text;
            }
            catch (Exception ex)
            {
                return $"❌ Lỗi hệ thống: {ex.Message}";
            }
        }

        private string Timef(DateTime from, DateTime to, InvoiceType type)
        {
            // Tạo chuỗi tìm kiếm cơ bản với khoảng thời gian
            string baseSearch = $"tdlap=ge={from:dd/MM/yyyyTHH:mm:ss};tdlap=le={to:dd/MM/yyyyTHH:mm:ss}";

            // Nếu là hóa đơn mua vào, thêm điều kiện ttxly==5
            if (type == InvoiceType.Bought)
            {
                baseSearch += ";ttxly==5";
            }

            // Trả về chuỗi query hoàn chỉnh, bổ sung tham số 'size' để lấy nhiều kết quả hơn
            return $"size=50&sort=tdlap:desc,khmshdon:asc,shdon:desc&search={baseSearch}";
        }



        public async Task ExportSearchResultsToExcelAsync(List<SearchResult> results, string filePath)
        {
            if (results == null || !results.Any())
            {
                AppendLog("⚠️ Không có dữ liệu danh sách để xuất Excel.");
                return;
            }

            try
            {
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("DanhSachHoaDon");

                    // --- PHẦN CODE ĐƯỢC BỔ SUNG ---
                    // Thêm tiêu đề chung
                    ws.Cells[1, 1].Value = "DANH SÁCH HÓA ĐƠN";
                    ws.Cells[1, 1, 1, 16].Merge = true;
                    ws.Cells[1, 1].Style.Font.Size = 16;
                    ws.Cells[1, 1].Style.Font.Bold = true;
                    ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[2, 1].Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    ws.Cells[2, 1, 2, 16].Merge = true;
                    ws.Cells[2, 1].Style.Font.Italic = true;
                    // -----------------------------

                    // Tiêu đề cột
                    string[] headers = {
                "Mã số thuế", "Ký hiệu mã số", "Ký hiệu hóa đơn", "Số hóa đơn", "Ngày lập",
                "Thông tin hóa đơn", "Tổng tiền chưa thuế", "Tổng tiền thuế", "Tổng tiền chiết khấu",
                "Tổng tiền phí", "Tổng tiền thanh toán", "Đơn vị tiền tệ",
                "Trạng thái hóa đơn", "Kết quả kiểm tra", "Hóa đơn liên quan", "Thông tin liên quan"
            };
                    // --- PHẦN CODE ĐƯỢC BỔ SUNG ---
                    // Dời dòng tiêu đề xuống để có khoảng trống cho tiêu đề chung
                    for (int i = 0; i < headers.Length; i++)
                        ws.Cells[4, i + 1].Value = headers[i].ToUpper();

                    // Định dạng tiêu đề
                    using (var headerRange = ws.Cells[4, 1, 4, headers.Length])
                    {
                        headerRange.Style.Font.Name = "Calibri";
                        headerRange.Style.Font.Size = 12;
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }
                    // -----------------------------

                    // Dữ liệu
                    int row = 5; // Bắt đầu từ dòng 5
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
                        ws.Cells[row, 16].Value = item.Thong_tin_lien_quan;

                        // --- PHẦN CODE ĐƯỢC BỔ SUNG ---
                        // Định dạng số tiền
                        ws.Cells[row, 7, row, 11].Style.Numberformat.Format = "#,##0";

                        // Màu xen kẽ
                        if (i % 2 == 0)
                        {
                            ws.Cells[row, 1, row, headers.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[row, 1, row, headers.Length].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 240));
                        }
                        // -----------------------------
                        row++;
                    }

                    // --- PHẦN CODE ĐƯỢC BỔ SUNG ---
                    // Thêm viền cho toàn bộ bảng dữ liệu
                    using (var dataRange = ws.Cells[4, 1, row - 1, headers.Length])
                    {
                        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }
                    // -----------------------------

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    await package.SaveAsAsync(new FileInfo(filePath));
                }
                AppendLog($"✅ Đã tạo file Excel danh sách tại: {filePath}");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi khi tạo file Excel danh sách: {ex.Message}");
                throw;
            }
        }

        // *** THAY ĐỔI: Thêm tham số 'List<SearchResult> invoices' ***
        private async Task ExportInvoiceDetailsToExcelAsync(List<SearchResult> invoices, string filePath)
        {
            // *** THAY ĐỔI: Kiểm tra tham số 'invoices' thay vì '_latestResults' ***
            if (invoices == null || !invoices.Any())
            {
                AppendLog("⚠️ (Excel Chi Tiết) Không có dữ liệu hóa đơn để lấy chi tiết.");
                // Quyết định: Có thể tạo file Excel trống hoặc báo lỗi. Hiện tại sẽ return.
                // Nếu muốn tạo file trống báo lỗi, có thể thêm code tạo Excel đơn giản ở đây.
                return;
            }

            try
            {
                AppendLog($"👉 (Excel Chi Tiết) Bắt đầu lấy chi tiết {invoices.Count} hóa đơn cho file Excel...");
                var detailsList = new List<Dictionary<string, string>>();

                // --- Cấu hình thứ tự và tên cột xuất ra --- (Giữ nguyên)
                var exportColumns = new (string key, string header)[]
                {
                ("stt", "STT"), ("khhdon", "Ký hiệu"), ("shdon", "Số hóa đơn"), ("tdlap", "Ngày hóa đơn"),
                ("nbten", "Tên người bán"), ("nbmst", "Mã số thuế"), ("ten", "Tên sản phẩm"),
                ("dvtinh", "Đơn vị tính"), ("sluong", "Số lượng"), ("dgia", "Đơn giá"),
                ("thtien", "Tổng tiền hàng"), ("stckhau", "Tiền chiết khấu"), ("thtcthue", "Doanh số bán chưa thuế"),
                ("tsuat", "Thuế suất"), ("tthue", "Thuế GTGT"), ("tgtttbso", "Tổng tiền thanh toán"),
                ("solo", "Số lô"), ("hsd", "Hạn sử dụng"), ("tthai", "Trạng thái hóa đơn"), ("ghichu", "GHI CHÚ")
                };

                int globalStt = 1; // Biến đếm STT tổng cho tất cả sản phẩm từ các hóa đơn

                // --- Lấy dữ liệu chi tiết từng hóa đơn ---
                // *** THAY ĐỔI: Dùng vòng lặp trên 'invoices' thay vì '_latestResults' ***
                foreach (var invoice in invoices)
                {
                    // Kiểm tra thông tin cần thiết của từng hóa đơn
                    if (invoice == null || string.IsNullOrEmpty(invoice.Ma_so_thue) ||
                        string.IsNullOrEmpty(invoice.Ky_hieu_hoa_don) || // Thêm kiểm tra ký hiệu
                        !invoice.So_hoa_don.HasValue ||
                        !invoice.Ky_hieu_ma_so.HasValue)
                    {
                        AppendLog($"⚠️ (Excel Chi Tiết) Bỏ qua hóa đơn ID {invoice?.Id} vì thiếu thông tin MST/KHHĐ/SHĐ/KHMHS.");
                        continue; // Bỏ qua hóa đơn này
                    }

                    try
                    {
                        // Gọi API lấy chi tiết cho hóa đơn hiện tại
                        string jsonDetail = await GetInvoiceDetailAsync(
                            invoice.Ma_so_thue,
                            invoice.Ky_hieu_hoa_don,
                            invoice.So_hoa_don,
                            invoice.Ky_hieu_ma_so
                        );

                        // Kiểm tra kết quả trả về từ GetInvoiceDetailAsync
                        if (string.IsNullOrEmpty(jsonDetail) || jsonDetail.StartsWith("❌"))
                        {
                            AppendLog($"❌ (Excel Chi Tiết) Lỗi khi lấy chi tiết HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}: {jsonDetail}");
                            continue; // Bỏ qua hóa đơn lỗi này
                        }

                        AppendLog($"💾 (Excel Chi Tiết) JSON chi tiết {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don} hợp lệ.");

                        using var doc = JsonDocument.Parse(jsonDetail);
                        JsonElement dataEl = doc.RootElement;
                        if (doc.RootElement.TryGetProperty("data", out var tmp)) dataEl = tmp;

                        // Thông tin chung của hóa đơn này
                        var common = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var prop in dataEl.EnumerateObject())
                            if (prop.Value.ValueKind != JsonValueKind.Object && prop.Value.ValueKind != JsonValueKind.Array)
                                common[prop.Name] = prop.Value.ToString();

                        // Tìm danh sách hàng hóa (mảng 'hdhhdvu')
                        JsonElement? productArray = null;
                        void FindProductArrayLocal(JsonElement element) // Đổi tên để tránh trùng lặp nếu dùng lại hàm gốc
                        {
                            if (productArray.HasValue) return;
                            if (element.ValueKind == JsonValueKind.Object)
                            {
                                if (element.TryGetProperty("hdhhdvu", out var hdhhdvu) && hdhhdvu.ValueKind == JsonValueKind.Array)
                                { productArray = hdhhdvu; return; }
                                foreach (var innerProp in element.EnumerateObject())
                                    if (innerProp.Value.ValueKind == JsonValueKind.Object || innerProp.Value.ValueKind == JsonValueKind.Array)
                                        FindProductArrayLocal(innerProp.Value);
                            }
                            else if (element.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in element.EnumerateArray()) FindProductArrayLocal(item);
                            }
                        }
                        FindProductArrayLocal(dataEl);


                        if (productArray.HasValue && productArray.Value.GetArrayLength() > 0)
                        {
                            AppendLog($"   -> Tìm thấy {productArray.Value.GetArrayLength()} sản phẩm.");
                            // Duyệt qua từng sản phẩm trong hóa đơn
                            foreach (var item in productArray.Value.EnumerateArray())
                            {
                                // Tạo dictionary chứa dữ liệu cho dòng này, bắt đầu bằng thông tin chung
                                var row = new Dictionary<string, string>(common, StringComparer.OrdinalIgnoreCase)
                                {
                                    // Ghi đè STT bằng biến đếm toàn cục
                                    ["stt"] = globalStt.ToString()
                                };

                                // Ghi đè/thêm thông tin cụ thể của sản phẩm
                                if (item.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var itemProp in item.EnumerateObject())
                                        row[itemProp.Name] = itemProp.Value.ToString();
                                }

                                detailsList.Add(row); // Thêm dòng dữ liệu sản phẩm này vào danh sách tổng
                                globalStt++; // Tăng STT toàn cục
                            }
                        }
                        else
                        {
                            AppendLog($"   -> Không tìm thấy mảng 'hdhhdvu' hoặc mảng rỗng.");
                            // Quyết định: Có thể thêm 1 dòng trống đại diện cho HĐ không có sản phẩm, hoặc bỏ qua.
                            // Hiện tại đang bỏ qua. Nếu muốn thêm dòng trống, cần tạo 'row' chỉ từ 'common'.
                        }
                    }
                    catch (JsonException jsonEx) // Lỗi parse JSON chi tiết
                    {
                        AppendLog($"❌ (Excel Chi Tiết) Lỗi parse JSON chi tiết HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}: {jsonEx.Message}");
                        // Bỏ qua hóa đơn này
                    }
                    catch (Exception ex) // Lỗi khác khi xử lý 1 hóa đơn
                    {
                        AppendLog($"❌ (Excel Chi Tiết) Lỗi không mong muốn khi xử lý chi tiết HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}: {ex.Message}");
                        // Bỏ qua hóa đơn này
                    }

                    await Task.Delay(150); // Delay nhỏ giữa các lần gọi API chi tiết
                } // Kết thúc vòng lặp qua các hóa đơn

                // --- Kiểm tra xem có chi tiết nào để xuất không ---
                if (!detailsList.Any())
                {
                    AppendLog("⚠️ (Excel Chi Tiết) Không có chi tiết sản phẩm nào từ các hóa đơn hợp lệ để xuất Excel.");
                    // Tạo file Excel trống báo lỗi? Hoặc chỉ return.
                    // Ví dụ tạo file báo lỗi:
                    using (var package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        var ws = package.Workbook.Worksheets.Add("Lỗi");
                        ws.Cells["A1"].Value = "Không tìm thấy chi tiết hàng hóa/dịch vụ nào từ các hóa đơn đã tìm.";
                        await package.SaveAsync();
                    }
                    return; // Dừng lại
                }

                // --- Xuất Excel --- (Phần này giữ nguyên logic, chỉ cần đảm bảo dùng detailsList)
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Chi Tiết Hóa Đơn");

                    // Tiêu đề chính (Giữ nguyên)
                    ws.Cells["A1"].Value = "CHI TIẾT HÓA ĐƠN";
                    ws.Cells["A1"].Style.Font.Size = 18;
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.Cells["A2"].Value = $"Ngày tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                    ws.Cells["A2"].Style.Font.Italic = true;
                    ws.Cells["A2"].Style.Font.Color.SetColor(System.Drawing.Color.DarkGray);
                    ws.Cells["A2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.Cells[1, 1, 2, exportColumns.Length].Merge = true; // Merge theo số cột đã định nghĩa

                    // Hàng tiêu đề (Giữ nguyên)
                    int headerRow = 4;
                    for (int i = 0; i < exportColumns.Length; i++)
                    {
                        ws.Cells[headerRow, i + 1].Value = exportColumns[i].header;
                        ws.Cells[headerRow, i + 1].Style.Font.Bold = true;
                        ws.Cells[headerRow, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[headerRow, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 204)); // Vàng nhạt
                        ws.Cells[headerRow, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                        ws.Cells[headerRow, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[headerRow, i + 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    }

                    // Dữ liệu (Dùng detailsList)
                    int rowIndex = headerRow + 1;
                    foreach (var detailRowData in detailsList) // Duyệt qua danh sách đã tổng hợp
                    {
                        for (int c = 0; c < exportColumns.Length; c++)
                        {
                            detailRowData.TryGetValue(exportColumns[c].key, out var value);

                            // Xử lý và định dạng giá trị (Giữ nguyên logic cũ, đã sửa định dạng số)
                            if (value != null && (exportColumns[c].key == "sluong" || exportColumns[c].key == "dgia" || exportColumns[c].key.Contains("tien") || exportColumns[c].key == "tthue" || exportColumns[c].key == "tgtttbso" || exportColumns[c].key == "stckhau"))
                            {
                                if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal numValue))
                                {
                                    ws.Cells[rowIndex, c + 1].Value = numValue;
                                    ws.Cells[rowIndex, c + 1].Style.Numberformat.Format = "#,##0"; // Đã sửa
                                    ws.Cells[rowIndex, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                }
                                else { ws.Cells[rowIndex, c + 1].Value = value; }
                            }
                            else { ws.Cells[rowIndex, c + 1].Value = value; }

                            // Thêm viền cho ô dữ liệu
                            ws.Cells[rowIndex, c + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }
                        rowIndex++;
                    }

                    // Autofit và định dạng chung (Giữ nguyên)
                    if (ws.Dimension != null) // Kiểm tra nếu worksheet có dữ liệu
                    {
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    }
                    ws.Cells.Style.Font.Name = "Segoe UI"; // Hoặc font bạn muốn
                    ws.Cells.Style.Font.Size = 10;

                    // Lưu file (Giữ nguyên)
                    await package.SaveAsAsync(new FileInfo(filePath));
                } // Kết thúc using package

                AppendLog($"✅ (Excel Chi Tiết) Đã tạo file Excel chi tiết tổng hợp tại: {filePath}");

            }
            catch (IOException ioEx) // Lỗi ghi file Excel
            {
                AppendLog($"❌ (Excel Chi Tiết) Lỗi IO khi tạo file Excel: {ioEx.Message}");
                // Ném lại lỗi để SaveOriginalInvoicesAsync có thể bắt và thông báo
                throw;
            }
            catch (Exception ex) // Các lỗi khác khi tạo Excel
            {
                AppendLog($"❌ (Excel Chi Tiết) Lỗi không mong muốn khi tạo file Excel chi tiết: {ex.Message}");
                // Ném lại lỗi
                throw;
            }
        }

        private async Task<bool> DownloadSingleInvoiceZipAsync(SearchResult invoice, string savePath)
        {
            // Kiểm tra các thông tin cần thiết trước khi gọi API
            if (invoice == null || string.IsNullOrEmpty(invoice.Ma_so_thue) || string.IsNullOrEmpty(invoice.Ky_hieu_hoa_don) || !invoice.So_hoa_don.HasValue || !invoice.Ky_hieu_ma_so.HasValue)
            {
                AppendLog($"⚠️ Bỏ qua hóa đơn ID {invoice?.Id} vì thiếu thông tin.");
                return false;
            }

            try
            {
                // Xây dựng URL với các tham số của một hóa đơn cụ thể
                string url = $"query/invoices/export-xml?" +
                             $"nbmst={invoice.Ma_so_thue}&" +
                             $"khhdon={invoice.Ky_hieu_hoa_don}&" +
                             $"shdon={invoice.So_hoa_don.Value}&" +
                             $"khmshdon={invoice.Ky_hieu_ma_so.Value}";

                AppendLog($"📥 Đang tải ZIP cho HĐ [{invoice.Ky_hieu_hoa_don} - {invoice.So_hoa_don}]...");
                AppendLog($"   URL: {url}");

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                req.Headers.Add("Accept", "application/zip, */*");
                req.Headers.Add("Referer", "https://hoadondientu.gdt.gov.vn/");
                req.Headers.Add("Origin", "https://hoadondientu.gdt.gov.vn");

                if (!string.IsNullOrEmpty(jwtToken))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                }

                var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    string errorText = await response.Content.ReadAsStringAsync();
                    AppendLog($"❌ Lỗi khi tải HĐ [{invoice.Ky_hieu_hoa_don} - {invoice.So_hoa_don}]: {(int)response.StatusCode} - {errorText}");
                    return false;
                }

                // Tạo tên file duy nhất cho từng hóa đơn
                string fileName = $"HD_{invoice.Ky_hieu_hoa_don}_{invoice.So_hoa_don}.zip";
                string fullPath = Path.Combine(savePath, fileName);

                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                AppendLog($"✅ Đã lưu thành công file: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"⚠️ Lỗi hệ thống khi tải HĐ [{invoice.Ky_hieu_hoa_don} - {invoice.So_hoa_don}]: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Tải file Excel danh sách hóa đơn trực tiếp từ API.
        /// </summary>
        /// <param name="queryString">Chuỗi query đã được tạo bởi hàm Timef</param>
        /// <param name="savePath">Đường dẫn đầy đủ để lưu file Excel (bao gồm cả tên file)</param>
        /// <returns>True nếu tải thành công</returns>
        private async Task<bool> DownloadInvoiceListExcelAsync(string queryString, string savePath)
        {
            try
            {
                string url = $"query/invoices/export-excel?{queryString}";
                AppendLog($"📦 Đang gọi API tải file Excel danh sách: {url}");

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                // Header Accept quan trọng để server biết bạn muốn nhận file Excel
                req.Headers.Add("Accept", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                if (!string.IsNullOrEmpty(jwtToken))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                }

                var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    string errorText = await response.Content.ReadAsStringAsync();
                    AppendLog($"❌ Lỗi khi tải file Excel danh sách: {response.StatusCode} - {errorText}");
                    return false;
                }

                using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                AppendLog($"✅ Đã tải thành công file Excel danh sách.");
                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"⚠️ Lỗi hệ thống khi tải file Excel danh sách: {ex.Message}");
                return false;
            }
        }

        private async Task SaveOriginalInvoicesAsync()
        {
            // === GIAI ĐOẠN 1: KIỂM TRA ĐẦU VÀO ===
            if (string.IsNullOrEmpty(jwtToken))
            {
                MessageBox.Show("Bạn chưa đăng nhập hoặc token không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DateTime fromDate = dtpFromDate.Value.Date;
            DateTime toDate = dtpToDate.Value.Date;

            if (toDate > fromDate.AddMonths(1))
            {
                MessageBox.Show("Khoảng thời gian tìm kiếm không được lớn hơn 1 tháng.", "Giới hạn tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (fromDate > toDate)
            {
                MessageBox.Show("Ngày bắt đầu không thể lớn hơn ngày kết thúc.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // --- QUẢN LÝ UI ---
            btnTaiHDGoc.Enabled = false; // Giả sử nút của bạn tên là btnTaiHDGoc
            downloadProgressBar.Visible = true;
            lblDownloadStatus.Visible = true;
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-Export_{Guid.NewGuid()}");

            // === DANH SÁCH KẾT QUẢ MỚI ĐỂ LƯU ===
            List<SearchResult> invoicesToDownload = new List<SearchResult>();
            bool searchSuccess = true; // Cờ để theo dõi lỗi tìm kiếm

            try
            {
                lblDownloadStatus.Text = "Bước 1: Đang tìm kiếm lại hóa đơn...";
                downloadProgressBar.Style = ProgressBarStyle.Marquee;
                DateTime preciseToDate = toDate.AddDays(1).AddTicks(-1);
                string querySold = Timef(fromDate, preciseToDate, InvoiceType.Sold);
                string queryBought = Timef(fromDate, preciseToDate, InvoiceType.Bought);

                // --- TÌM KIẾM LẠI DỮ LIỆU (XỬ LÝ CẢ 3 TRƯỜNG HỢP) ---

                // Tìm HĐ Bán ra
                if (rbSold.Checked || rbAllInvoices.Checked)
                {
                    AppendLog("🔍 (Tải gốc) Bắt đầu tìm hóa đơn bán ra...");
                    string resultSold = await GetProductsAsync("query/invoices/sold", querySold);
                    if (resultSold.StartsWith("❌"))
                    {
                        AppendLog(resultSold);
                        MessageBox.Show("Lỗi khi tìm lại HĐ Bán ra:\n" + resultSold, "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        searchSuccess = false;
                    }
                    else
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var responseSold = JsonSerializer.Deserialize<SearchResponse>(resultSold, options);
                        if (responseSold?.Datas != null)
                        {
                            invoicesToDownload.AddRange(responseSold.Datas);
                            AppendLog($"✅ (Tải gốc) Tìm thấy {responseSold.Datas.Count} hóa đơn bán ra.");
                        }
                    }
                }

                // Tìm HĐ Mua vào (chỉ tìm nếu lần trước thành công và cần tìm mua/tất cả)
                if (searchSuccess && (rbBought.Checked || rbAllInvoices.Checked))
                {
                    AppendLog("🔍 (Tải gốc) Bắt đầu tìm hóa đơn mua vào...");
                    string resultBought = await GetProductsAsync("query/invoices/purchase", queryBought);
                    if (resultBought.StartsWith("❌"))
                    {
                        AppendLog(resultBought);
                        MessageBox.Show("Lỗi khi tìm lại HĐ Mua vào:\n" + resultBought, "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        searchSuccess = false;
                    }
                    else
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var responseBought = JsonSerializer.Deserialize<SearchResponse>(resultBought, options);
                        if (responseBought?.Datas != null)
                        {
                            invoicesToDownload.AddRange(responseBought.Datas);
                            AppendLog($"✅ (Tải gốc) Tìm thấy {responseBought.Datas.Count} hóa đơn mua vào.");
                        }
                    }
                }

                // Nếu có lỗi ở bất kỳ bước tìm kiếm nào, dừng lại
                if (!searchSuccess)
                {
                    return; // Thoát khỏi hàm, khối finally vẫn chạy để reset UI
                }

                // === KIỂM TRA KẾT QUẢ TÌM KIẾM LẠI ===
                if (!invoicesToDownload.Any())
                {
                    MessageBox.Show("Không tìm thấy hóa đơn nào trong khoảng thời gian đã chọn (khi tìm kiếm lại).",
                                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return; // Dừng hàm
                }

                // === GIAI ĐOẠN 2: TẢI VỀ VÀ XỬ LÝ (Sử dụng invoicesToDownload) ===
                // Sắp xếp lại nếu cần (ví dụ: theo ngày giảm dần)
                invoicesToDownload = invoicesToDownload.OrderByDescending(inv => inv.Ngay_lap).ThenBy(inv => inv.So_hoa_don).ToList();

                ShowStatusMessage($"Tìm thấy {invoicesToDownload.Count} hóa đơn. Bắt đầu tải...", Color.Green);
                downloadProgressBar.Style = ProgressBarStyle.Blocks;
                downloadProgressBar.Maximum = invoicesToDownload.Count + 4; // Cập nhật Maximum
                downloadProgressBar.Value = 0;

                Directory.CreateDirectory(tempDirectory);

                lblDownloadStatus.Text = "Bước 2: Đang tải file Excel danh sách...";
                string listExcelPath = Path.Combine(tempDirectory, "DanhSachHoaDon.xlsx");
                // Xác định query string cho file Excel danh sách API
                string queryForListExcel = (rbSold.Checked || rbAllInvoices.Checked) ? querySold : queryBought;
                await DownloadInvoiceListExcelAsync(queryForListExcel, listExcelPath);
                downloadProgressBar.PerformStep(); // Tăng 1 bước

                lblDownloadStatus.Text = "Bước 3: Đang tạo file Excel chi tiết...";
                // *** GỌI HÀM ĐÃ SỬA ĐỔI ***
                await ExportInvoiceDetailsToExcelAsync(invoicesToDownload, Path.Combine(tempDirectory, "ChiTietHoaDon.xlsx"));
                downloadProgressBar.PerformStep(); // Tăng 1 bước

                int successCount = 0;
                // Tải file ZIP cho từng hóa đơn
                for (int i = 0; i < invoicesToDownload.Count; i++)
                {
                    lblDownloadStatus.Text = $"Bước 4: Đang tải hóa đơn gốc ({i + 1}/{invoicesToDownload.Count})...";
                    if (await DownloadSingleInvoiceZipAsync(invoicesToDownload[i], tempDirectory))
                    {
                        successCount++;
                    }
                    downloadProgressBar.Value = Math.Min(downloadProgressBar.Value + 1, downloadProgressBar.Maximum); // Cập nhật progress
                    await Task.Delay(100); // Giữ delay nhỏ
                }

                // --- Phần còn lại: Nén, giải nén, thông báo ---
                lblDownloadStatus.Text = "Bước 5: Đang tổng hợp và giải nén...";
                using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục để lưu kết quả" };

                // Kiểm tra xem người dùng có chọn thư mục không
                if (fbd.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    AppendLog("ℹ️ Người dùng đã hủy chọn thư mục lưu.");
                    // Không return vội, vẫn cần dọn dẹp ở finally
                }
                else // Chỉ thực hiện nén/giải nén nếu người dùng đã chọn thư mục
                {
                    string tempZipPath = Path.Combine(Path.GetTempPath(), $"Temp_HoaDon_TongHop_{Guid.NewGuid()}.zip");
                    AppendLog($"📦 Đang nén thư mục tạm: {tempDirectory} -> {tempZipPath}");
                    ZipFile.CreateFromDirectory(tempDirectory, tempZipPath);
                    AppendLog($"✅ Nén xong.");

                    // Đảm bảo tên thư mục cuối cùng hợp lệ
                    string finalFolderName = Path.GetFileNameWithoutExtension(tempZipPath).Replace("Temp_", "");
                    // Loại bỏ các ký tự không hợp lệ khỏi tên thư mục nếu cần (tùy HĐH)
                    // finalFolderName = string.Join("_", finalFolderName.Split(Path.GetInvalidFileNameChars()));

                    string finalExtractionPath = Path.Combine(fbd.SelectedPath, finalFolderName);
                    AppendLog($"📂 Tạo thư mục đích: {finalExtractionPath}");
                    Directory.CreateDirectory(finalExtractionPath);

                    AppendLog($"🚀 Đang giải nén file tổng hợp vào thư mục đích...");
                    ZipFile.ExtractToDirectory(tempZipPath, finalExtractionPath, true); // true: ghi đè nếu tồn tại
                    downloadProgressBar.Value = Math.Min(downloadProgressBar.Value + 1, downloadProgressBar.Maximum); // Cập nhật progress
                    AppendLog($"✅ Giải nén file tổng hợp xong.");

                    AppendLog($"🔍 Bắt đầu giải nén các file ZIP hóa đơn con...");
                    await Task.Run(() => UnzipInnerArchives(finalExtractionPath)); // Chạy giải nén con trên thread khác
                    downloadProgressBar.Value = Math.Min(downloadProgressBar.Value + 1, downloadProgressBar.Maximum); // Cập nhật progress
                    AppendLog($"✅ Giải nén file con xong.");


                    MessageBox.Show($"✅ Hoàn tất! \n\nĐã lưu và giải nén thành công {successCount} hóa đơn gốc và 2 file báo cáo Excel vào thư mục:\n\n{finalExtractionPath}", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (JsonException jsonEx) // Bắt lỗi cụ thể khi Deserialize
            {
                AppendLog($"❌ Lỗi phân tích JSON trong SaveOriginalInvoicesAsync: {jsonEx.ToString()}");
                MessageBox.Show($"Lỗi xử lý dữ liệu trả về từ API: {jsonEx.Message}", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                searchSuccess = false; // Đánh dấu là tìm kiếm lỗi
            }
            catch (IOException ioEx) // Bắt lỗi liên quan đến file/thư mục
            {
                AppendLog($"❌ Lỗi IO trong SaveOriginalInvoicesAsync: {ioEx.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi khi xử lý file hoặc thư mục: {ioEx.Message}\n\nThường do đường dẫn quá dài, không có quyền ghi, hoặc file đang được sử dụng.", "Lỗi File/Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex) // Bắt các lỗi chung khác
            {
                AppendLog($"🐞 LỖI NGHIÊM TRỌNG trong SaveOriginalInvoicesAsync: {ex.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // --- Dọn dẹp và Reset UI ---
                try
                {
                    AppendLog($"🧹 Đang dọn dẹp thư mục tạm: {tempDirectory}");
                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, true); // true: xóa cả nội dung bên trong
                    }
                    AppendLog($"🧹 Đang dọn dẹp các file ZIP tạm...");
                    foreach (var tempZip in Directory.GetFiles(Path.GetTempPath(), "Temp_HoaDon_TongHop_*.zip"))
                    {
                        File.Delete(tempZip);
                        AppendLog($"   -> Đã xóa: {Path.GetFileName(tempZip)}");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"⚠️ Không thể dọn dẹp file/thư mục tạm: {ex.Message}");
                    // Có thể thông báo cho người dùng nếu muốn, nhưng thường không cần thiết
                }

                btnTaiHDGoc.Enabled = true; // Đổi tên nút nếu cần
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
                AppendLog("🏁 Kết thúc tiến trình Tải HĐ Gốc.");
            }
        }
        private void UnzipInnerArchives(string directoryPath)
        {
            try
            {
                AppendLog($"⚙️ Bắt đầu tìm và giải nén các file hóa đơn con...");
                string[] zipFiles = Directory.GetFiles(directoryPath, "HD_*.zip"); // Chỉ tìm các file zip hóa đơn

                if (!zipFiles.Any())
                {
                    AppendLog("ℹ️ Không tìm thấy file zip hóa đơn con nào để giải nén.");
                    return;
                }

                foreach (var zipFilePath in zipFiles)
                {
                    try
                    {
                        // Tạo thư mục giải nén từ tên file zip (ví dụ: "HD_C25TQH_28")
                        string destinationPath = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(zipFilePath));
                        Directory.CreateDirectory(destinationPath);

                        // Giải nén và ghi đè nếu tồn tại
                        ZipFile.ExtractToDirectory(zipFilePath, destinationPath, true);
                        AppendLog($"   => Đã giải nén: {Path.GetFileName(zipFilePath)}");

                        // Xóa file .zip con sau khi giải nén xong
                        File.Delete(zipFilePath);
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"❌ Lỗi khi giải nén file con '{Path.GetFileName(zipFilePath)}': {ex.Message}");
                    }
                }
                AppendLog("✅ Hoàn tất giải nén các file hóa đơn con.");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi nghiêm trọng trong quá trình giải nén file con: {ex.Message}");
            }
        }

        private async void btnTaiHDGoc_Click(object sender, EventArgs e)
        {
            await SaveOriginalInvoicesAsync();
        }

        private void CheckForLicense()
        {
            var status = LicenseManager.CheckLicense();

            switch (status)
            {
                case LicenseStatus.Activated:
                    this.Text = "E-Tax (Phiên bản đầy đủ)";
                    panelActivation.Visible = false;
                    panelLogin.Enabled = true;
                    break;

                case LicenseStatus.ValidTrial:
                    // === THAY ĐỔI: Bỏ số ngày còn lại ===
                    this.Text = "E-Tax (Bản dùng thử)";
                    // ==================================
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
                // Sau khi kích hoạt thành công, kiểm tra lại license để mở khóa giao diện
                CheckForLicense();
            }
            else
            {
                MessageBox.Show("❌ Mã kích hoạt không hợp lệ. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        /// Cập nhật DateTimePickers dựa trên tháng và năm nhập vào.
        /// </summary>
        private void UpdateDatePickersFromMonthYear()
        {
            // --- VALIDATE INPUT ---
            bool isMonthValid = int.TryParse(txtThang.Text.Trim(), out int month);
            bool isYearValid = int.TryParse(txtNam.Text.Trim(), out int year);

            // Chỉ cập nhật nếu cả tháng và năm đều là số hợp lệ
            if (!isMonthValid || !isYearValid)
            {
                // Có thể thêm thông báo lỗi ở đây nếu muốn, ví dụ dùng ErrorProvider
                // Hoặc đơn giản là không làm gì cả nếu nhập liệu chưa đủ/đúng
                return;
            }

            // Kiểm tra giá trị tháng (1-12)
            if (month < 1 || month > 12)
            {
                MessageBox.Show("Tháng không hợp lệ. Vui lòng nhập giá trị từ 1 đến 12.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtThang.Focus(); // Đưa con trỏ về ô tháng
                txtThang.SelectAll();
                return;
            }

            // Kiểm tra giá trị năm (ví dụ: > 0 và < 9999)
            if (year <= 0 || year > 9999)
            {
                MessageBox.Show("Năm không hợp lệ.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNam.Focus(); // Đưa con trỏ về ô năm
                txtNam.SelectAll();
                return;
            }

            // --- CALCULATE DATES ---
            try
            {
                // Ngày đầu tiên của tháng
                DateTime firstDayOfMonth = new DateTime(year, month, 1);

                // Ngày cuối cùng của tháng (Lấy ngày đầu tháng sau rồi trừ đi 1 ngày)
                DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // --- UPDATE DATETIMEPICKERS ---
                dtpFromDate.Value = firstDayOfMonth;
                dtpToDate.Value = lastDayOfMonth;

                AppendLog($"🗓️ Đã cập nhật Từ ngày: {firstDayOfMonth:dd/MM/yyyy}, Đến ngày: {lastDayOfMonth:dd/MM/yyyy}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Trường hợp này ít xảy ra do đã validate ở trên, nhưng vẫn nên có phòng ngừa
                MessageBox.Show($"Ngày tháng không hợp lệ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"❌ Lỗi khi tạo ngày từ tháng={month}, năm={year}: {ex.Message}");
            }
            catch (Exception ex) // Bắt các lỗi khác có thể xảy ra
            {
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"❌ Lỗi không mong muốn khi cập nhật ngày: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý sự kiện click nút Tìm kiếm bên trái.
        /// </summary>
        private async void btnLeftSearch_Click(object sender, EventArgs e)
        {
            // --- KIỂM TRA ĐĂNG NHẬP ---
            if (string.IsNullOrEmpty(jwtToken))
            {
                MessageBox.Show("Bạn chưa đăng nhập hoặc token không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // --- LẤY THÔNG TIN TÌM KIẾM & VALIDATE ---
            DateTime fromDate = dtpFromDate.Value.Date;
            DateTime toDate = dtpToDate.Value.Date;
            DateTime preciseToDate = toDate.AddDays(1).AddTicks(-1); // Lấy đến cuối ngày 'Đến ngày'
            if (toDate > fromDate.AddMonths(1))
            {
                MessageBox.Show("Khoảng thời gian tìm kiếm không được lớn hơn 1 tháng.", "Giới hạn tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (fromDate > toDate)
            {
                MessageBox.Show("Ngày bắt đầu không thể lớn hơn ngày kết thúc.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // --- QUẢN LÝ TRẠNG THÁI UI (Bắt đầu) ---
            btnLeftSearch.Enabled = false;
            btnLeftSearch.Text = "Đang tìm...";
            this.Cursor = Cursors.WaitCursor; // Đổi con trỏ chờ cho cả form
            downloadProgressBar.Value = 0; // Reset progress bar
            downloadProgressBar.Maximum = 100; // Đặt tạm Maximum
            downloadProgressBar.Visible = true;
            downloadProgressBar.Style = ProgressBarStyle.Marquee; // Marquee khi tìm kiếm danh sách
            lblDownloadStatus.Text = "Đang tìm kiếm hóa đơn...";
            lblDownloadStatus.Visible = true;
            dgvMain.DataSource = null;
            dgvDetails.DataSource = null; // Xóa lưới chi tiết
            dgvMua.DataSource = null;     // Xóa lưới mua
            dgvBan.DataSource = null;     // Xóa lưới bán
            dgvVatNop.DataSource = null;
            dgvGiamThue.DataSource = null;
            _latestResults.Clear();
            _lastSuccessfulQueryString = "";

            // --- THỰC HIỆN TÌM KIẾM DANH SÁCH ---
            List<SearchResult> searchResults = new List<SearchResult>();
            bool searchSoldSuccess = true;
            bool searchBoughtSuccess = true;
            string currentQuerySold = "";
            string currentQueryBought = "";

            try
            {
                // --- Tìm hóa đơn bán ra ---
                if (rbSold.Checked || rbAllInvoices.Checked)
                {
                    lblDownloadStatus.Text = "Đang tìm hóa đơn bán ra...";
                    AppendLog("🔍 Bắt đầu tìm hóa đơn bán ra...");
                    currentQuerySold = Timef(fromDate, preciseToDate, InvoiceType.Sold);
                    string resultSold = await GetProductsAsync("query/invoices/sold", currentQuerySold);
                    if (resultSold.StartsWith("❌"))
                    {
                        AppendLog(resultSold);
                        MessageBox.Show("Lỗi khi tìm hóa đơn bán ra:\n" + resultSold, "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        searchSoldSuccess = false;
                    }
                    else
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var responseSold = JsonSerializer.Deserialize<SearchResponse>(resultSold, options);
                        if (responseSold?.Datas != null)
                        {
                            responseSold.Datas.ForEach(item => item.Thong_tin_lien_quan = "Bán ra"); // Gán loại hóa đơn
                            searchResults.AddRange(responseSold.Datas);
                            AppendLog($"✅ Tìm thấy {responseSold.Datas.Count} hóa đơn bán ra.");
                        }
                    }
                }

                // --- Tìm hóa đơn mua vào ---
                if (rbBought.Checked || rbAllInvoices.Checked)
                {
                    if (searchSoldSuccess) // Chỉ tìm nếu lần trước thành công
                    {
                        lblDownloadStatus.Text = "Đang tìm hóa đơn mua vào...";
                        AppendLog("🔍 Bắt đầu tìm hóa đơn mua vào...");
                        currentQueryBought = Timef(fromDate, preciseToDate, InvoiceType.Bought);
                        string resultBought = await GetProductsAsync("query/invoices/purchase", currentQueryBought);
                        if (resultBought.StartsWith("❌"))
                        {
                            AppendLog(resultBought);
                            MessageBox.Show("Lỗi khi tìm hóa đơn mua vào:\n" + resultBought, "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            searchBoughtSuccess = false;
                        }
                        else
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var responseBought = JsonSerializer.Deserialize<SearchResponse>(resultBought, options);
                            if (responseBought?.Datas != null)
                            {
                                responseBought.Datas.ForEach(item => item.Thong_tin_lien_quan = "Mua vào"); // Gán loại hóa đơn
                                searchResults.AddRange(responseBought.Datas);
                                AppendLog($"✅ Tìm thấy {responseBought.Datas.Count} hóa đơn mua vào.");
                            }
                        }
                    }
                    else { searchBoughtSuccess = false; } // Đánh dấu lỗi nếu tìm bán bị lỗi
                }

                // --- XỬ LÝ KẾT QUẢ TÌM KIẾM DANH SÁCH ---
                if (searchSoldSuccess && searchBoughtSuccess)
                {
                    // Sắp xếp lại kết quả tổng hợp (ví dụ: theo ngày giảm dần)
                    _latestResults = searchResults.OrderByDescending(r => r.Ngay_lap).ThenBy(r => r.So_hoa_don).ToList();

                    // Lưu query string thành công cuối cùng
                    if (rbSold.Checked) { _lastSuccessfulQueryString = currentQuerySold; }
                    else if (rbBought.Checked) { _lastSuccessfulQueryString = currentQueryBought; }
                    else { _lastSuccessfulQueryString = currentQuerySold; AppendLog("ℹ️ Lưu query HĐ Bán cho export khi chọn 'Tất cả'."); }
                    if (!_latestResults.Any()) { _lastSuccessfulQueryString = ""; } // Xóa nếu không có kết quả
                    else { AppendLog($"💾 Đã lưu query string: {_lastSuccessfulQueryString}"); }


                    if (_latestResults.Any())
                    {
                        AppendLog($"📊 Tìm thấy tổng cộng {_latestResults.Count} hóa đơn. Đang xử lý...");
                        lblDownloadStatus.Text = $"Đang hiển thị {_latestResults.Count} hóa đơn...";
                        downloadProgressBar.Style = ProgressBarStyle.Blocks; // Đổi style

                        // Gán DataSource cho lưới chính
                        dgvMain.DataSource = _latestResults;

                        // Cập nhật STT và định dạng ngày cho lưới chính
                        dgvMain.SuspendLayout(); // Tạm dừng vẽ
                        try
                        {
                            for (int i = 0; i < dgvMain.Rows.Count; i++)
                            {
                                if (dgvMain.Columns.Contains("colDgvSTT")) dgvMain.Rows[i].Cells["colDgvSTT"].Value = i + 1;
                                // Gán Loại HĐ dựa trên Thong_tin_lien_quan đã gán trước đó
                                if (dgvMain.Columns.Contains("colDgvLoaiHD") && i < _latestResults.Count && _latestResults[i] != null)
                                    dgvMain.Rows[i].Cells["colDgvLoaiHD"].Value = _latestResults[i].Thong_tin_lien_quan;

                                // Định dạng ngày
                                var cellNgayLap = dgvMain.Rows[i].Cells["colDgvNgayLap"];
                                if (cellNgayLap.Value is string ngayLapStr && DateTime.TryParse(ngayLapStr, out DateTime ngayLap))
                                {
                                    cellNgayLap.Value = ngayLap.ToString("dd/MM/yyyy");
                                }
                            }
                        }
                        finally
                        {
                            dgvMain.ResumeLayout(); // Bật lại vẽ
                        }
                        AppendLog("✅ Hiển thị xong lưới tổng hợp.");
                        AppendLog($"📊 Gán dữ liệu cho Bảng kê giảm thuế...");
                        dgvGiamThue.DataSource = null;
                        dgvGiamThue.DataSource = _latestResults; // Gán cùng list kết quả                                          
                        AppendLog($"✅ Hiển thị xong Bảng kê giảm thuế.");

                        // !!! GỌI HÀM TẢI DỮ LIỆU CHI TIẾT !!!
                        await _detailGridManager.PopulateDetailGridAsync(_latestResults);

                    }
                    else // Không tìm thấy hóa đơn nào
                    {
                        lblDownloadStatus.Text = "Không tìm thấy hóa đơn nào.";
                        MessageBox.Show("Không tìm thấy hóa đơn nào phù hợp với điều kiện tìm kiếm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        AppendLog("ℹ️ Không tìm thấy hóa đơn nào.");
                        // Đảm bảo các lưới chi tiết trống
                        dgvDetails.DataSource = null;
                        dgvMua.DataSource = null;
                        dgvBan.DataSource = null;
                        dgvVatNop.DataSource = null;
                        dgvGiamThue.DataSource = null;
                    }
                }
                // Trường hợp tìm kiếm lỗi (searchSoldSuccess=false hoặc searchBoughtSuccess=false)
                else
                {
                    // Đã có MessageBox báo lỗi API ở trên
                    dgvMain.DataSource = null;
                    dgvDetails.DataSource = null;
                    dgvMua.DataSource = null;
                    dgvBan.DataSource = null;
                    AppendLog("❌ Tìm kiếm hóa đơn không thành công.");
                }
            }
            catch (JsonException jsonEx) // Lỗi Deserialize danh sách
            {
                AppendLog($"❌ Lỗi phân tích JSON kết quả tìm kiếm: {jsonEx.ToString()}");
                MessageBox.Show($"Lỗi xử lý dữ liệu trả về: {jsonEx.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dgvMain.DataSource = null;
                dgvDetails.DataSource = null;
                dgvMua.DataSource = null;
                dgvBan.DataSource = null;
                dgvVatNop.DataSource = null;
                dgvGiamThue.DataSource = null;
            }
            catch (Exception ex) // Lỗi chung khác
            {
                AppendLog($"❌ Lỗi không mong muốn khi tìm kiếm: {ex.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dgvMain.DataSource = null;
                dgvDetails.DataSource = null;
                dgvMua.DataSource = null;
                dgvBan.DataSource = null;
                dgvVatNop.DataSource = null;
                dgvGiamThue.DataSource = null;
            }
            finally
            {
                // --- KHÔI PHỤC TRẠNG THÁI UI ---
                btnLeftSearch.Enabled = true;
                btnLeftSearch.Text = "Tìm kiếm";
                this.Cursor = Cursors.Default;
            }
        }
        private async Task<string> GetInvoiceDetailAsync(string nbmst, string khhdon, int? shdon, int? khmshdon)
        {
            // --- KIỂM TRA ĐẦU VÀO ---
            if (string.IsNullOrEmpty(nbmst) || string.IsNullOrEmpty(khhdon) || !shdon.HasValue || !khmshdon.HasValue)
            {
                return "❌ Lỗi: Thiếu thông tin cần thiết để lấy chi tiết hóa đơn.";
            }
            if (string.IsNullOrEmpty(jwtToken))
            {
                return "❌ Lỗi: Chưa đăng nhập hoặc token không hợp lệ.";
            }

            try
            {
                // --- TẠO URL ---
                string url = $"query/invoices/detail?" +
                             $"nbmst={Uri.EscapeDataString(nbmst)}&" +
                             $"khhdon={Uri.EscapeDataString(khhdon)}&" +
                             $"shdon={shdon.Value}&" +
                             $"khmshdon={khmshdon.Value}";

                AppendLog($"📄 Đang gọi API lấy chi tiết hóa đơn: {url}");

                // --- TẠO YÊU CẦU ---
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                req.Headers.Add("Accept", "application/json");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken); // Sử dụng token đã lưu

                // --- GỬI YÊU CẦU VÀ NHẬN PHẢN HỒI ---
                var response = await client.SendAsync(req);
                string jsonResponse = await response.Content.ReadAsStringAsync();
                AppendLog($"HTTP Status (Detail): {(int)response.StatusCode} {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    AppendLog($"❌ Lỗi API chi tiết: {jsonResponse}");
                    return $"❌ Lỗi từ API ({response.StatusCode}): {jsonResponse}";
                }

                AppendLog($"✅ Nhận JSON chi tiết thành công:\n{jsonResponse}");
                return jsonResponse; // Trả về chuỗi JSON thành công
            }
            catch (HttpRequestException httpEx)
            {
                AppendLog($"❌ Lỗi HTTP khi lấy chi tiết: {httpEx.Message}");
                return $"❌ Lỗi kết nối: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi không mong muốn khi lấy chi tiết: {ex.ToString()}");
                return $"❌ Lỗi hệ thống: {ex.Message}";
            }
        }

        private async void btnXemHD_Click(object sender, EventArgs e)
        {
            // --- KIỂM TRA XEM CÓ DÒNG NÀO ĐƯỢC CHỌN KHÔNG ---
            if (dgvMain.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn từ danh sách để xem.", "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // --- LẤY DỮ LIỆU TỪ DÒNG ĐƯỢC CHỌN ---
            SearchResult selectedInvoice = dgvMain.CurrentRow.DataBoundItem as SearchResult;

            if (selectedInvoice == null)
            {
                MessageBox.Show("Không thể lấy dữ liệu hóa đơn từ dòng đã chọn.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog("⚠️ Lỗi: Không thể ép kiểu DataBoundItem thành SearchResult tại btnXemHT_Click.");
                return;
            }

            // --- KIỂM TRA THÔNG TIN HÓA ĐƠN CẦN THIẾT ---
            if (string.IsNullOrEmpty(selectedInvoice.Ma_so_thue) ||
                string.IsNullOrEmpty(selectedInvoice.Ky_hieu_hoa_don) ||
                !selectedInvoice.So_hoa_don.HasValue ||
                !selectedInvoice.Ky_hieu_ma_so.HasValue)
            {
                MessageBox.Show("Hóa đơn được chọn thiếu thông tin cần thiết (MST, Ký hiệu, Số HĐ, KH Mẫu số).", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AppendLog($"⚠️ Hóa đơn ID {selectedInvoice.Id} thiếu thông tin để tải ZIP.");
                return;
            }


            // --- QUẢN LÝ UI ---
            btnXemHD.Enabled = false;
            btnXemHD.Text = "Đang tải...";
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-View_{Guid.NewGuid()}"); // Thư mục tạm duy nhất
            string zipFilePath = Path.Combine(tempDirectory, $"HD_{selectedInvoice.Ky_hieu_hoa_don}_{selectedInvoice.So_hoa_don}.zip");
            string extractPath = Path.Combine(tempDirectory, "extracted");

            try
            {
                Directory.CreateDirectory(tempDirectory); // Tạo thư mục tạm
                Directory.CreateDirectory(extractPath);   // Tạo thư mục con để giải nén

                AppendLog($"👁️‍🗨️ Bắt đầu tải ZIP để xem HĐ: {selectedInvoice.Ky_hieu_hoa_don} - {selectedInvoice.So_hoa_don}");

                // --- TẢI FILE ZIP ---
                bool downloadSuccess = await DownloadSingleInvoiceZipAsync(selectedInvoice, tempDirectory);

                if (!downloadSuccess)
                {
                    MessageBox.Show("Không thể tải được file ZIP chứa hóa đơn.", "Lỗi tải file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // Dừng lại nếu tải lỗi
                }

                if (!File.Exists(zipFilePath))
                {
                    MessageBox.Show($"File ZIP dự kiến ({Path.GetFileName(zipFilePath)}) không tồn tại sau khi tải.", "Lỗi tải file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    AppendLog($"⚠️ File ZIP không tồn tại tại đường dẫn: {zipFilePath}");
                    return;
                }


                // --- GIẢI NÉN ---
                AppendLog($"⚙️ Đang giải nén file: {zipFilePath} vào {extractPath}");
                try
                {
                    ZipFile.ExtractToDirectory(zipFilePath, extractPath, true); // Giải nén và ghi đè nếu tồn tại
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi giải nén file ZIP: {ex.Message}", "Lỗi giải nén", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    AppendLog($"❌ Lỗi giải nén ZIP: {ex.ToString()}");
                    return;
                }

                // --- TÌM VÀ MỞ FILE HTML HOẶC PDF ---
                string[] htmlFiles = Directory.GetFiles(extractPath, "*.html", SearchOption.AllDirectories);
                string[] pdfFiles = Directory.GetFiles(extractPath, "*.pdf", SearchOption.AllDirectories);
                string fileToOpen = null;

                if (htmlFiles.Length > 0)
                {
                    fileToOpen = htmlFiles[0]; // Ưu tiên mở file HTML đầu tiên tìm thấy
                    AppendLog($"✅ Tìm thấy file HTML: {fileToOpen}");
                }
                else if (pdfFiles.Length > 0)
                {
                    fileToOpen = pdfFiles[0]; // Nếu không có HTML, mở file PDF đầu tiên
                    AppendLog($"✅ Không có HTML, tìm thấy file PDF: {fileToOpen}");
                }

                if (fileToOpen != null)
                {
                    try
                    {
                        // Mở file bằng ứng dụng mặc định của hệ thống
                        AppendLog($"🚀 Đang mở file: {fileToOpen}");
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fileToOpen) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể tự động mở file hóa đơn: {ex.Message}\n\nBạn có thể tìm file trong thư mục:\n{extractPath}", "Lỗi mở file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        AppendLog($"⚠️ Lỗi khi dùng Process.Start để mở file: {ex.ToString()}");
                    }
                }
                else
                {
                    AppendLog("⚠️ Không tìm thấy file HTML hoặc PDF trong file ZIP.");
                    MessageBox.Show("Không tìm thấy file HTML hoặc PDF thể hiện hóa đơn trong file ZIP tải về. Chỉ có file dữ liệu XML.", "Không có bản thể hiện", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Gợi ý: Có thể mở thư mục giải nén cho người dùng tự xem
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(extractPath) { UseShellExecute = true }); } catch { }
                }
            }
            catch (Exception ex) // Bắt các lỗi tổng quát khác
            {
                AppendLog($"❌ Lỗi không mong muốn trong btnXemHT_Click: {ex.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // --- KHÔI PHỤC UI ---
                btnXemHD.Enabled = true;
                btnXemHD.Text = "Xem HĐ";
            }
        }

        //private async Task ExportSingleInvoiceDetailsToExcelAsync(string invoiceDetailJson, string filePath)
        //{
        //    if (string.IsNullOrEmpty(invoiceDetailJson) || invoiceDetailJson.StartsWith("❌"))
        //    {
        //        AppendLog("⚠️ Dữ liệu chi tiết không hợp lệ để xuất Excel.");
        //        throw new ArgumentException("Dữ liệu chi tiết hóa đơn không hợp lệ.");
        //    }

        //    try
        //    {
        //        using var package = new ExcelPackage();
        //        var ws = package.Workbook.Worksheets.Add("ChiTietHoaDon");

        //        using var doc = JsonDocument.Parse(invoiceDetailJson);
        //        JsonElement dataEl = doc.RootElement;
        //        // Một số API trả về { "data": { ... } }, kiểm tra và lấy phần data nếu có
        //        if (doc.RootElement.TryGetProperty("data", out var tmpData) && tmpData.ValueKind == JsonValueKind.Object)
        //        {
        //            dataEl = tmpData;
        //        }

        //        int currentRow = 1; // Bắt đầu ghi từ dòng 1

        //        // --- Ghi thông tin chung ---
        //        ws.Cells[currentRow, 1].Value = "Thông tin chung hóa đơn";
        //        ws.Cells[currentRow, 1, currentRow, 2].Merge = true;
        //        ws.Cells[currentRow, 1].Style.Font.Bold = true;
        //        currentRow++;

        //        // Lấy các cặp key-value không phải là mảng hoặc đối tượng con
        //        foreach (var prop in dataEl.EnumerateObject().Where(p => p.Value.ValueKind != JsonValueKind.Object && p.Value.ValueKind != JsonValueKind.Array))
        //        {
        //            ws.Cells[currentRow, 1].Value = prop.Name; // Tên trường
        //            ws.Cells[currentRow, 2].Value = prop.Value.ToString(); // Giá trị
        //            currentRow++;
        //        }
        //        currentRow++; // Thêm dòng trống

        //        // --- Ghi chi tiết hàng hóa/dịch vụ (Tìm mảng hdhhdvu) ---
        //        ws.Cells[currentRow, 1].Value = "Chi tiết hàng hóa/dịch vụ";
        //        ws.Cells[currentRow, 1].Style.Font.Bold = true;
        //        currentRow++;

        //        // Hàm đệ quy tìm mảng hdhhdvu
        //        JsonElement? productArray = null;
        //        void FindProductArray(JsonElement element)
        //        {
        //            if (productArray.HasValue) return; // Đã tìm thấy thì dừng

        //            if (element.ValueKind == JsonValueKind.Object)
        //            {
        //                if (element.TryGetProperty("hdhhdvu", out var hdhhdvu) && hdhhdvu.ValueKind == JsonValueKind.Array)
        //                {
        //                    productArray = hdhhdvu;
        //                    return;
        //                }
        //                // Tìm sâu hơn trong các thuộc tính là object hoặc array
        //                foreach (var innerProp in element.EnumerateObject())
        //                {
        //                    if (innerProp.Value.ValueKind == JsonValueKind.Object || innerProp.Value.ValueKind == JsonValueKind.Array)
        //                    {
        //                        FindProductArray(innerProp.Value);
        //                    }
        //                }
        //            }
        //            else if (element.ValueKind == JsonValueKind.Array)
        //            {
        //                foreach (var item in element.EnumerateArray())
        //                {
        //                    FindProductArray(item);
        //                }
        //            }
        //        }

        //        FindProductArray(dataEl); // Bắt đầu tìm kiếm từ gốc

        //        if (productArray.HasValue && productArray.Value.GetArrayLength() > 0)
        //        {
        //            var products = productArray.Value.EnumerateArray().ToList();

        //            // Lấy danh sách tên cột từ item đầu tiên
        //            var headers = products.First().EnumerateObject().Select(p => p.Name).ToList();

        //            // Ghi Header
        //            for (int i = 0; i < headers.Count; i++)
        //            {
        //                ws.Cells[currentRow, i + 1].Value = headers[i];
        //                ws.Cells[currentRow, i + 1].Style.Font.Bold = true;
        //                ws.Cells[currentRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        //                ws.Cells[currentRow, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        //                ws.Cells[currentRow, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        //            }
        //            currentRow++;

        //            // Ghi dữ liệu từng sản phẩm
        //            foreach (var product in products)
        //            {
        //                for (int i = 0; i < headers.Count; i++)
        //                {
        //                    if (product.TryGetProperty(headers[i], out var propValue))
        //                    {
        //                        // Cố gắng chuyển đổi sang số để định dạng
        //                        if (propValue.ValueKind == JsonValueKind.Number)
        //                        {
        //                            ws.Cells[currentRow, i + 1].Value = propValue.GetDecimal();
        //                            // Định dạng các cột có thể là số tiền/số lượng
        //                            if (headers[i].Contains("tien", StringComparison.OrdinalIgnoreCase) ||
        //                               headers[i].Equals("sluong", StringComparison.OrdinalIgnoreCase) ||
        //                               headers[i].Equals("dgia", StringComparison.OrdinalIgnoreCase))
        //                            {
        //                                ws.Cells[currentRow, i + 1].Style.Numberformat.Format = "#,##0";
        //                                ws.Cells[currentRow, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            ws.Cells[currentRow, i + 1].Value = propValue.ToString();
        //                        }
        //                    }
        //                }
        //                currentRow++;
        //            }
        //        }
        //        else
        //        {
        //            ws.Cells[currentRow, 1].Value = "Không tìm thấy chi tiết hàng hóa/dịch vụ (mảng 'hdhhdvu').";
        //        }


        //        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        //        ws.Cells.Style.Font.Name = "Segoe UI";
        //        ws.Cells.Style.Font.Size = 10;

        //        await package.SaveAsAsync(new FileInfo(filePath));
        //        AppendLog($"✅ Đã tạo file Excel chi tiết đơn lẻ tại: {filePath}");
        //    }
        //    catch (JsonException jsonEx)
        //    {
        //        AppendLog($"❌ Lỗi phân tích JSON khi xuất chi tiết đơn lẻ: {jsonEx.Message}");
        //        throw new Exception($"Lỗi đọc dữ liệu chi tiết hóa đơn: {jsonEx.Message}", jsonEx);
        //    }
        //    catch (Exception ex)
        //    {
        //        AppendLog($"❌ Lỗi khi tạo file Excel chi tiết đơn lẻ: {ex.Message}");
        //        throw; // Ném lại lỗi để nơi gọi xử lý
        //    }
        //} 

        /// <summary>
        /// Xử lý sự kiện click nút "Tải danh sách HĐ" (btnExportDS) - Nâng cấp: Tải trực tiếp từ API.
        /// </summary>
        private async void btnExportDS_Click(object sender, EventArgs e)
        {
            // Kiểm tra xem đã có kết quả tìm kiếm và query string chưa
            if (string.IsNullOrEmpty(_lastSuccessfulQueryString))
            {
                MessageBox.Show("Không có dữ liệu hoặc truy vấn tìm kiếm gần nhất để xuất. Vui lòng tìm kiếm trước.",
                                "Chưa có dữ liệu/truy vấn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_latestResults == null || !_latestResults.Any()) // Kiểm tra lại _latestResults để chắc chắn có HĐ để tải
            {
                MessageBox.Show("Không có hóa đơn nào trong kết quả tìm kiếm gần nhất để tải danh sách.",
                                "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            // Hỏi người dùng nơi lưu file
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"DanhSachHoaDon_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx", // Tên file gợi ý
                Title = "Lưu danh sách hóa đơn (Tải từ API)"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                btnExportDS.Enabled = false;
                btnExportDS.Text = "Đang tải...";
                downloadProgressBar.Visible = true;
                downloadProgressBar.Style = ProgressBarStyle.Marquee; // Dùng Marquee vì không biết tiến trình tải file
                lblDownloadStatus.Text = "Đang tải file Excel danh sách từ API...";
                lblDownloadStatus.Visible = true;

                try
                {
                    // Gọi hàm tải Excel trực tiếp từ API
                    bool success = await DownloadInvoiceListExcelAsync(_lastSuccessfulQueryString, sfd.FileName);

                    if (success)
                    {
                        MessageBox.Show($"Đã tải thành công file Excel danh sách hóa đơn vào:\n{sfd.FileName}",
                                        "Tải thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Không thể tải file Excel danh sách từ API. Vui lòng xem log (nếu có) để biết chi tiết.",
                                        "Lỗi tải file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tải file Excel danh sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    AppendLog($"❌ Lỗi btnExportDS_Click (API Download): {ex.ToString()}");
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
        }

        /// <summary>
        /// Xử lý sự kiện click nút "Tải chi tiết HĐ" (btnExportChiTiet).
        /// </summary>
        private async void btnExportChiTiet_Click(object sender, EventArgs e)
        {
            // Kiểm tra xem có đúng một dòng được chọn không
            if (dgvMain.SelectedRows.Count != 1 && dgvMain.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn đúng một hóa đơn từ danh sách để xuất chi tiết.", "Chọn một hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Lấy dòng được chọn
            DataGridViewRow selectedRow = dgvMain.SelectedRows.Count == 1 ? dgvMain.SelectedRows[0] : dgvMain.CurrentRow;
            SearchResult selectedInvoice = selectedRow?.DataBoundItem as SearchResult;

            if (selectedInvoice == null)
            {
                MessageBox.Show("Không thể lấy dữ liệu hóa đơn từ dòng đã chọn.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog("⚠️ Lỗi: Không thể ép kiểu DataBoundItem thành SearchResult tại btnExportChiTiet_Click.");
                return;
            }

            // Kiểm tra thông tin hóa đơn cần thiết
            if (string.IsNullOrEmpty(selectedInvoice.Ma_so_thue) || string.IsNullOrEmpty(selectedInvoice.Ky_hieu_hoa_don) || !selectedInvoice.So_hoa_don.HasValue || !selectedInvoice.Ky_hieu_ma_so.HasValue)
            {
                MessageBox.Show("Hóa đơn được chọn thiếu thông tin cần thiết (MST, Ký hiệu, Số HĐ, KH Mẫu số).", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AppendLog($"⚠️ Hóa đơn ID {selectedInvoice.Id} thiếu thông tin để lấy chi tiết.");
                return;
            }

            // Hỏi nơi lưu file
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"ChiTietHD_{selectedInvoice.Ky_hieu_hoa_don}_{selectedInvoice.So_hoa_don}_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Lưu chi tiết hóa đơn"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                btnExportChiTiet.Enabled = false;
                btnExportChiTiet.Text = "Đang xử lý..."; // Đổi text
                downloadProgressBar.Visible = true;
                downloadProgressBar.Style = ProgressBarStyle.Marquee;
                lblDownloadStatus.Text = "Đang lấy chi tiết và tạo file Excel...";
                lblDownloadStatus.Visible = true;

                string detailJson = null; // Khai báo biến ở ngoài try-catch

                try
                {
                    // =================================================================
                    // !!! PHẦN SỬA LỖI NẰM Ở ĐÂY !!!
                    // =================================================================
                    // 1. Gọi API lấy chi tiết (Sử dụng hàm GetInvoiceDetailAsync bạn đã viết)
                    AppendLog($"⚙️ Đang gọi API lấy chi tiết cho HĐ {selectedInvoice.So_hoa_don}...");

                    detailJson = await GetInvoiceDetailAsync(
                        selectedInvoice.Ma_so_thue,
                        selectedInvoice.Ky_hieu_hoa_don,
                        selectedInvoice.So_hoa_don,
                        selectedInvoice.Ky_hieu_ma_so
                    );

                    // 2. Kiểm tra kết quả API (Rất quan trọng!)
                    if (string.IsNullOrEmpty(detailJson) || detailJson.StartsWith("❌"))
                    {
                        // Ném lỗi để khối catch bên dưới xử lý
                        string errorMessage = string.IsNullOrEmpty(detailJson) ? "API không trả về dữ liệu chi tiết." : detailJson;
                        throw new Exception(errorMessage);
                    }
                    // =================================================================
                    // KẾT THÚC PHẦN SỬA LỖI
                    // =================================================================

                    AppendLog($"✅ Lấy chi tiết API thành công. Bắt đầu tạo file Excel: {sfd.FileName}");

                    // 3. Tạo Excel và Parse JSON
                    using var package = new ExcelPackage();
                    var ws = package.Workbook.Worksheets.Add("ChiTietHoaDon");

                    // Dòng này sẽ KHÔNG LỖI NỮA
                    using var doc = JsonDocument.Parse(detailJson);
                    JsonElement dataEl = doc.RootElement;
                    if (doc.RootElement.TryGetProperty("data", out var tmpData) && tmpData.ValueKind == JsonValueKind.Object)
                    {
                        dataEl = tmpData;
                    }

                    int currentRow = 1;

                    // Tiêu đề chính (Tùy chọn)
                    ws.Cells[currentRow, 1].Value = "Chi tiết hàng hóa/dịch vụ";
                    ws.Cells[currentRow, 1].Style.Font.Bold = true;
                    ws.Cells[currentRow, 1].Style.Font.Size = 14;
                    ws.Cells[currentRow, 1, currentRow, 15].Merge = true; // Ước lượng merge
                    ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    currentRow++;
                    currentRow++; // Dòng trống

                    JsonElement? productArray = null;

                    // Hàm nội tuyến để tìm mảng sản phẩm
                    void FindProductArray(JsonElement element)
                    {
                        if (productArray.HasValue) return; // Đã tìm thấy

                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            // Ưu tiên tìm "hdhhdvu" trước
                            if (element.TryGetProperty("hdhhdvu", out var hdhhdvuArray) && hdhhdvuArray.ValueKind == JsonValueKind.Array)
                            {
                                productArray = hdhhdvuArray;
                                return;
                            }
                            // Nếu không, tìm sâu hơn
                            foreach (var prop in element.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                                {
                                    FindProductArray(prop.Value);
                                }
                            }
                        }
                        else if (element.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in element.EnumerateArray())
                            {
                                FindProductArray(item);
                            }
                        }
                    }

                    FindProductArray(dataEl);

                    // <<< ĐỊNH NGHĨA exportColumns Ở ĐÂY >>>
                    var exportColumns = new (string key, string header)[]
                    {
                ("stt", "STT"), ("khhdon", "Ký hiệu"), ("shdon", "Số hóa đơn"), ("tdlap", "Ngày hóa đơn"),
                ("nbten", "Tên người bán"), ("nbmst", "Mã số thuế"), ("ten", "Tên sản phẩm"),
                ("dvtinh", "Đơn vị tính"), ("sluong", "Số lượng"), ("dgia", "Đơn giá"),
                ("thtien", "Tổng tiền hàng"), ("stckhau", "Tiền chiết khấu"), ("thtcthue", "Doanh số bán chưa thuế"),
                ("tsuat", "Thuế suất"), ("tthue", "Thuế GTGT"), ("tgtttbso", "Tổng tiền thanh toán"),
                ("solo", "Số lô"), ("hsd", "Hạn sử dụng"), ("tthai", "Trạng thái hóa đơn"), ("ghichu", "GHI CHÚ")
                    };
                    // ===================================

                    // Ghi Header dựa trên exportColumns
                    int headerRow = currentRow;
                    for (int i = 0; i < exportColumns.Length; i++)
                    {
                        if (i + 1 > ExcelPackage.MaxColumns) continue;
                        ws.Cells[headerRow, i + 1].Value = exportColumns[i].header;
                        ws.Cells[headerRow, i + 1].Style.Font.Bold = true;
                        ws.Cells[headerRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[headerRow, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        ws.Cells[headerRow, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }
                    currentRow++;

                    // Ghi dữ liệu sản phẩm
                    if (productArray.HasValue && productArray.Value.GetArrayLength() > 0)
                    {
                        var commonInfoDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var prop in dataEl.EnumerateObject().Where(p => p.Value.ValueKind != JsonValueKind.Object && p.Value.ValueKind != JsonValueKind.Array))
                        {
                            commonInfoDict[prop.Name] = prop.Value.ToString();
                        }

                        foreach (var product in productArray.Value.EnumerateArray())
                        {
                            var rowData = new Dictionary<string, string>(commonInfoDict, StringComparer.OrdinalIgnoreCase);
                            if (product.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in product.EnumerateObject())
                                {
                                    rowData[prop.Name] = prop.Value.ToString();
                                }
                            }

                            for (int c = 0; c < exportColumns.Length; c++)
                            {
                                if (c + 1 > ExcelPackage.MaxColumns) continue;

                                rowData.TryGetValue(exportColumns[c].key, out var value);

                                // Cố gắng parse sang kiểu decimal để định dạng số
                                if (value != null && (exportColumns[c].key == "sluong" || exportColumns[c].key == "dgia" || exportColumns[c].key.Contains("tien") || exportColumns[c].key == "tthue" || exportColumns[c].key == "tgtttbso" || exportColumns[c].key == "stckhau"))
                                {
                                    if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal numValue))
                                    {
                                        ws.Cells[currentRow, c + 1].Value = numValue;
                                        ws.Cells[currentRow, c + 1].Style.Numberformat.Format = "#,##0";
                                        ws.Cells[currentRow, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                    }
                                    else { ws.Cells[currentRow, c + 1].Value = value; }
                                }
                                else { ws.Cells[currentRow, c + 1].Value = value; }
                            }
                            currentRow++;
                        }

                        // Thêm viền
                        int lastColumnIndex = Math.Min(exportColumns.Length, ExcelPackage.MaxColumns);
                        if (currentRow > headerRow + 1 && lastColumnIndex >= 1)
                        {
                            using (var dataRange = ws.Cells[headerRow, 1, currentRow - 1, lastColumnIndex])
                            {
                                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            }
                        }
                    }
                    else
                    {
                        ws.Cells[currentRow, 1].Value = "Không tìm thấy chi tiết hàng hóa/dịch vụ (mảng 'hdhhdvu').";
                        int lastColumnIndex = Math.Min(exportColumns.Length, ExcelPackage.MaxColumns);
                        if (lastColumnIndex >= 1) ws.Cells[currentRow, 1, currentRow, lastColumnIndex].Merge = true;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    ws.Cells.Style.Font.Name = "Segoe UI";
                    ws.Cells.Style.Font.Size = 10;

                    await package.SaveAsAsync(new FileInfo(sfd.FileName));
                    // --- KẾT THÚC LOGIC TẠO EXCEL ---

                    MessageBox.Show($"Đã xuất thành công chi tiết hóa đơn vào file:\n{sfd.FileName}", "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppendLog($"✅ Đã xuất chi tiết HĐ (chỉ hàng hóa) về: {sfd.FileName}");
                }
                catch (Exception ex) // Bắt lỗi chung (bao gồm cả lỗi API và lỗi tạo Excel)
                {
                    // Khối catch này đã được CẢI TIẾN để bắt lỗi từ API

                    // Kiểm tra xem lỗi có phải từ API không (dựa vào nội dung exception)
                    if (ex.Message.StartsWith("❌"))
                    {
                        string apiError = ex.Message;
                        AppendLog($"❌ Lỗi API đã được báo cáo, không tạo Excel. Lỗi: {apiError}");
                        MessageBox.Show($"Lỗi khi lấy chi tiết hóa đơn từ API: {apiError.Substring(2)}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else // Lỗi xảy ra trong quá trình tạo Excel (ví dụ: JsonDocument.Parse) hoặc lỗi không mong muốn khác
                    {
                        MessageBox.Show($"Lỗi khi lấy hoặc xuất chi tiết hóa đơn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        AppendLog($"❌ Lỗi btnExportChiTiet_Click: {ex.ToString()}");
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
        }

        private void btnRightSearch_Click(object sender, EventArgs e)
        {
            // 1. Lấy mã số thuế cần lọc từ TextBox, loại bỏ khoảng trắng thừa
            string filterTaxCode = txtTimMST.Text.Trim();

            // 2. Kiểm tra xem có dữ liệu gốc (_latestResults) để lọc không
            if (_latestResults == null || !_latestResults.Any())
            {
                MessageBox.Show("Chưa có dữ liệu hóa đơn để lọc. Vui lòng nhấn nút 'Tìm kiếm' trước.", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 3. Xử lý trường hợp ô lọc trống -> hiển thị lại toàn bộ dữ liệu gốc
            if (string.IsNullOrEmpty(filterTaxCode))
            {
                AppendLog("🔍 Bộ lọc MST trống, hiển thị lại toàn bộ kết quả.");
                dgvMain.DataSource = null; // Xóa nguồn cũ
                dgvMain.DataSource = _latestResults; // Gán lại nguồn gốc
                UpdateGridRowNumbers(); // Cập nhật lại STT
                return; // Kết thúc
            }

            // 4. Áp dụng bộ lọc (dùng LINQ)
            AppendLog($"🔍 Bắt đầu lọc theo MST: '{filterTaxCode}'");
            var filteredResults = _latestResults
                .Where(invoice => invoice.Ma_so_thue != null && // Đảm bảo MST không null
                                  invoice.Ma_so_thue.Contains(filterTaxCode)) // Lọc theo Contains (tìm kiếm gần đúng)
                                                                              // Hoặc dùng Equals nếu muốn tìm chính xác:
                                                                              // invoice.Ma_so_thue.Equals(filterTaxCode, StringComparison.OrdinalIgnoreCase)) 
                .ToList(); // Chuyển kết quả lọc thành List mới

            // 5. Kiểm tra và hiển thị kết quả lọc
            if (filteredResults.Any())
            {
                AppendLog($"✅ Tìm thấy {filteredResults.Count} hóa đơn khớp với MST.");
                dgvMain.DataSource = null; // Xóa nguồn cũ
                dgvMain.DataSource = filteredResults; // Hiển thị kết quả đã lọc
                UpdateGridRowNumbers(); // Cập nhật STT cho kết quả lọc
            }
            else
            {
                AppendLog("ℹ️ Không tìm thấy hóa đơn nào khớp với MST.");
                dgvMain.DataSource = null; // Xóa dữ liệu khỏi lưới
                MessageBox.Show($"Không tìm thấy hóa đơn nào có Mã số thuế chứa '{filterTaxCode}'.", "Không có kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateGridRowNumbers()
        {
            if (dgvMain.DataSource == null) return; // Không làm gì nếu không có dữ liệu

            dgvMain.SuspendLayout(); // Tạm dừng vẽ lại lưới để tăng tốc độ
            for (int i = 0; i < dgvMain.Rows.Count; i++)
            {
                // Kiểm tra xem cột STT có tồn tại không trước khi gán
                if (dgvMain.Columns.Contains("colDgvSTT"))
                {
                    dgvMain.Rows[i].Cells["colDgvSTT"].Value = i + 1;
                }
            }
            dgvMain.ResumeLayout(); // Vẽ lại lưới
        }

        // Gắn sự kiện này vào DataGridView trong InitializeComponent nếu chưa có:
        // this.dgvMain.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvMain_CellContentClick);

        private async void dgvMain_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // 1. Kiểm tra xem có phải click vào cột button "Tra cứu" không
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvMain.Columns["colDgvTraCuu"].Index)
            {
                // 2. Lấy thông tin hóa đơn từ dòng được click
                SearchResult selectedInvoice = dgvMain.Rows[e.RowIndex].DataBoundItem as SearchResult;

                if (selectedInvoice == null)
                {
                    MessageBox.Show("Không thể lấy dữ liệu hóa đơn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3. Kiểm tra các thông tin cần thiết
                if (string.IsNullOrEmpty(selectedInvoice.Ma_so_thue) ||
                    string.IsNullOrEmpty(selectedInvoice.Ky_hieu_hoa_don) ||
                    !selectedInvoice.So_hoa_don.HasValue ||
                    !selectedInvoice.Ky_hieu_ma_so.HasValue)
                {
                    MessageBox.Show("Hóa đơn thiếu thông tin cần thiết để tra cứu (MST/KHHĐ/SHĐ/KHMHS).", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // --- Hiển thị trạng thái đang xử lý ---
                var cellButton = dgvMain.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cellButton != null) cellButton.Value = "Đang..."; // Đổi text tạm thời
                dgvMain.Cursor = Cursors.WaitCursor; // Đổi con trỏ chuột

                try
                {
                    // 4. Gọi API lấy chi tiết hóa đơn
                    AppendLog($"🔍 Đang lấy chi tiết HĐ {selectedInvoice.So_hoa_don} để tra cứu...");
                    string jsonDetail = await GetInvoiceDetailAsync(
                        selectedInvoice.Ma_so_thue,
                        selectedInvoice.Ky_hieu_hoa_don,
                        selectedInvoice.So_hoa_don,
                        selectedInvoice.Ky_hieu_ma_so
                    );

                    if (string.IsNullOrEmpty(jsonDetail) || jsonDetail.StartsWith("❌"))
                    {
                        MessageBox.Show($"Lỗi khi lấy chi tiết hóa đơn:\n{jsonDetail}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 5. Phân tích JSON lấy mã tra cứu và nhà cung cấp
                    string maTraCuu = ""; // Mã tra cứu thực tế
                    string nhaCungCap = ""; // Ví dụ: "tvan_misa"
                    string tenNB = selectedInvoice.Thong_tin_hoa_don; // Lấy từ SearchResult cho nhanh
                    string mstNB = selectedInvoice.Ma_so_thue;
                    string diaChiNB = ""; // Cần lấy từ JSON chi tiết nếu muốn hiển thị

                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(jsonDetail))
                        {
                            JsonElement root = doc.RootElement;
                            // Lấy gốc dữ liệu (có thể nằm trong "data")
                            if (root.TryGetProperty("data", out JsonElement dataEl) && dataEl.ValueKind == JsonValueKind.Object)
                            {
                                root = dataEl;
                            }

                            // --- Lấy Mã Tra Cứu ---
                            // TH1: Ưu tiên trường "mhdon" nếu có
                            if (root.TryGetProperty("mhdon", out JsonElement maElement) && maElement.ValueKind == JsonValueKind.String)
                            {
                                maTraCuu = maElement.GetString();
                            }
                            // TH2: Nếu không có "mhdon", thử lấy trường "id" (giả định)
                            else if (root.TryGetProperty("id", out JsonElement idElement) && idElement.ValueKind == JsonValueKind.String)
                            {
                                maTraCuu = idElement.GetString(); // Hoặc trường nào phù hợp
                            }
                            // TH3: Lấy từ SearchResult nếu không tìm thấy trong chi tiết (ít chính xác hơn)
                            else if (!string.IsNullOrEmpty(selectedInvoice.Id))
                            {
                                maTraCuu = selectedInvoice.Id; // Dùng tạm ID từ danh sách
                                AppendLog("⚠️ Không tìm thấy mã tra cứu trong JSON chi tiết, dùng tạm ID từ danh sách.");
                            }


                            // --- Lấy Nhà Cung Cấp ---
                            if (root.TryGetProperty("ngcnhat", out JsonElement nccElement) && nccElement.ValueKind == JsonValueKind.String)
                            {
                                nhaCungCap = nccElement.GetString()?.ToLower() ?? ""; // Chuyển về chữ thường để dễ so sánh
                            }

                            // --- (Tùy chọn) Lấy Địa Chỉ Người Bán ---
                            if (root.TryGetProperty("nbdchi", out JsonElement dcElement) && dcElement.ValueKind == JsonValueKind.String)
                            {
                                diaChiNB = dcElement.GetString();
                            }

                        }
                        AppendLog($"✅ Lấy chi tiết thành công: Mã TC='{maTraCuu}', NCC='{nhaCungCap}'");

                    }
                    catch (JsonException jsonEx)
                    {
                        AppendLog($"❌ Lỗi parse JSON chi tiết khi tra cứu: {jsonEx.Message}");
                        MessageBox.Show("Lỗi đọc dữ liệu chi tiết hóa đơn.", "Lỗi JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 6. Xây dựng Link tra cứu
                    string linkTraCuu = BuildTraCuuLink(mstNB, nhaCungCap, maTraCuu);

                    if (string.IsNullOrEmpty(linkTraCuu))
                    {
                        AppendLog($"⚠️ Không thể tạo link tra cứu cho NCC: '{nhaCungCap}'");
                        MessageBox.Show($"Không hỗ trợ tạo link tra cứu tự động cho nhà cung cấp '{nhaCungCap}'.\nBạn có thể tự truy cập trang tra cứu của họ.", "Chưa hỗ trợ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // Vẫn có thể hiển thị form chỉ với mã tra cứu
                        linkTraCuu = "(Không thể tạo tự động)";
                    }

                    // 7. Hiển thị Form tra cứu
                    using (var traCuuForm = new TraCuuForm(maTraCuu, linkTraCuu, tenNB, mstNB, diaChiNB))
                    {
                        traCuuForm.ShowDialog(this); // Hiển thị form popup
                    }
                }
                catch (Exception ex) // Bắt lỗi chung
                {
                    AppendLog($"❌ Lỗi không mong muốn khi xử lý tra cứu: {ex.ToString()}");
                    MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // --- Khôi phục trạng thái UI ---
                    if (cellButton != null) cellButton.Value = "Tải"; // Trả lại text gốc
                    dgvMain.Cursor = Cursors.Default; // Trả lại con trỏ chuột
                }
            }
        }

        // Hàm trợ giúp để xây dựng link tra cứu
        private string BuildTraCuuLink(string mst, string nhaCungCap, string maTraCuu)
        {
            if (string.IsNullOrEmpty(mst) || string.IsNullOrEmpty(nhaCungCap)) // Mã tra cứu có thể trống tùy nhà cung cấp
            {
                return null;
            }

            // Chuyển về chữ thường để so sánh không phân biệt hoa thường
            string nccLower = nhaCungCap.ToLower();

            // --- DANH SÁCH CÁC MẪU URL TRA CỨU ---
            // (Cần bổ sung và kiểm tra lại các URL này)
            switch (nccLower)
            {
                case "tvan_misa":
                    // Misa có thể có nhiều tên miền, đây là một ví dụ
                    return $"https://{mst}.meinvoice.vn"; // Thường Misa tra cứu không cần mã trực tiếp trên URL chính
                                                          // Hoặc nếu có trang tra cứu cụ thể:
                                                          // return $"https://tracuu.meinvoice.vn/?mst={mst}&code={maTraCuu}"; // Ví dụ

                case "tvan_bkav":
                    return $"https://{mst}.bkav.com/TraCuu"; // Ví dụ, cần kiểm tra

                case "tvan_vnpt": // Tên này không chuẩn, thường là tvan_buuchinhvt hoặc tương tự
                case "tvan_buuchinhvt":
                    // VNPT cũng có nhiều hệ thống, đây là ví dụ theo ảnh của bạn
                    return $"https://{mst}-tt78.vnpt-invoice.com.vn"; // VNPT thường cũng không cần mã trên URL
                                                                      // Hoặc trang tra cứu chung:
                                                                      // return $"https://portal.vnpt-invoice.com.vn/Invoice/lookup"; // Ví dụ

                case "tvan_viettel":
                    return $"https://{mst}.viettel-einvoice.vn"; // Ví dụ
                                                                 // return $"https://tracuu.viettel-einvoice.vn"; // Trang tra cứu chung

                case "tvan_thaison":
                    return $"https://{mst}.einvoice.vn/tracuu"; // Ví dụ

                default:
                    return null;
            }
        }

        private void ConfigureGiamThueGridColumns()
        {
            dgvGiamThue.AutoGenerateColumns = false;
            dgvGiamThue.Columns.Clear();
            dgvGiamThue.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgvGiamThue.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvGiamThue.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

            // --- DÒNG MỚI THÊM ---
            // Thêm cột Số hóa đơn (bind dữ liệu từ SearchResult.So_hoa_don)
            AddTextColumnToGrid(dgvGiamThue, "colGiamThueKyHieuHoaDon", "Ký hiệu hóa đơn", nameof(SearchResult.Ky_hieu_hoa_don), 100, frozen: true, alignment: DataGridViewContentAlignment.MiddleCenter);
            // --- KẾT THÚC DÒNG MỚI ---

            // Thêm các cột tiền tệ (bind dữ liệu từ SearchResult)
            AddNumericColumnToGrid(dgvGiamThue, "colGiamThueTienChuaThue", "Tổng tiền chưa thuế", nameof(SearchResult.Tong_tien_chua_thue), 150, "#,##0");
            AddNumericColumnToGrid(dgvGiamThue, "colGiamThueTienThue", "Tổng tiền thuế", nameof(SearchResult.Tong_tien_thue), 140, "#,##0");
            AddNumericColumnToGrid(dgvGiamThue, "colGiamThueTienCK", "Tổng tiền chiết khấu", nameof(SearchResult.Tong_tien_chiet_khau), 160, "#,##0");

            // Thêm cột Tổng tiền phí (KHÔNG bind trực tiếp, sẽ dùng CellFormatting)
            AddNumericColumnToGrid(dgvGiamThue, "colGiamThueTongPhi", "Tổng tiền phí", null, 140, "#,##0"); // DataPropertyName = null

            AddNumericColumnToGrid(dgvGiamThue, "colGiamThueTongTT", "Tổng tiền thanh toán", nameof(SearchResult.Tong_tien_thanh_toan), 160, "#,##0");
            AddTextColumnToGrid(dgvGiamThue, "colGiamThueDVT", "Đơn vị tiền tệ", nameof(SearchResult.Don_vi_tien_te), 100);

            // Đăng ký sự kiện CellFormatting để tính tổng phí VÀ TẠO STT
            dgvGiamThue.CellFormatting += DgvGiamThue_CellFormatting;
        }

        // --- Thêm 2 hàm trợ giúp này vào Form1 nếu chưa có ---
        // Hàm trợ giúp thêm cột Text vào DataGridView cụ thể
        private void AddTextColumnToGrid(DataGridView dgv, string name, string header, string dataProperty, int width, bool frozen = false, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProperty, // Có thể null nếu không bind
                Width = width,
                ReadOnly = true,
                Frozen = frozen,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = alignment }
            };
            dgv.Columns.Add(column);
        }

        // Hàm trợ giúp thêm cột Numeric vào DataGridView cụ thể
        private void AddNumericColumnToGrid(DataGridView dgv, string name, string header, string dataProperty, int width, string format)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProperty, // Có thể null nếu không bind
                Width = width,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = format, Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dgv.Columns.Add(column);
        }
        // --------------------------------------------------------

        /// <summary>
        /// Xử lý sự kiện CellFormatting cho dgvGiamThue để tính và hiển thị Tổng tiền phí.
        /// </summary>
        /// <summary>
        /// Xử lý sự kiện CellFormatting cho dgvGiamThue để TẠO STT, 
        /// tính Tổng tiền phí và định dạng các giá trị null.
        /// </summary>
        private void DgvGiamThue_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {

            // Lấy đối tượng SearchResult tương ứng với dòng hiện tại
            if (!(dgvGiamThue.Rows[e.RowIndex].DataBoundItem is SearchResult invoice))
            {
                // Nếu không lấy được dữ liệu (ví dụ: hàng mới cuối lưới),
                // thì dừng lại, không xử lý các cột dữ liệu khác
                return;
            }

            // Lấy tên cột hiện tại
            string colName = dgvGiamThue.Columns[e.ColumnIndex].Name;

            try
            {
                switch (colName)
                {
                    // Xử lý các cột số cần định dạng #,##0 và xử lý giá trị null
                    case "colGiamThueTienChuaThue":
                        // Nếu giá trị là null, hiển thị "0" thay vì lỗi
                        e.Value = invoice.Tong_tien_chua_thue?.ToString("#,##0") ?? "0";
                        e.FormattingApplied = true;
                        break;
                    case "colGiamThueTienThue":
                        e.Value = invoice.Tong_tien_thue?.ToString("#,##0") ?? "0";
                        e.FormattingApplied = true;
                        break;
                    case "colGiamThueTienCK":
                        e.Value = invoice.Tong_tien_chiet_khau?.ToString("#,##0") ?? "0";
                        e.FormattingApplied = true;
                        break;
                    case "colGiamThueTongTT":
                        e.Value = invoice.Tong_tien_thanh_toan?.ToString("#,##0") ?? "0";
                        e.FormattingApplied = true;
                        break;

                    // Xử lý cột Tổng tiền phí (tính toán)
                    case "colGiamThueTongPhi":
                        // Tính tổng các giá trị trong List<decimal?> Tong_tien_phi
                        decimal totalFee = invoice.Tong_tien_phi?.Sum(fee => fee ?? 0) ?? 0;
                        e.Value = totalFee.ToString("#,##0"); // Định dạng luôn ở đây
                        e.FormattingApplied = true;
                        break;

                    // Cột Số hóa đơn (colGiamThueSoHoaDon) đã được bind tự động
                    // nên không cần xử lý ở đây, trừ khi bạn muốn xử lý null
                    case "colGiamThueSoHoaDon":
                        if (e.Value == null)
                        {
                            e.Value = "N/A"; // Hoặc "" (chuỗi rỗng)
                            e.FormattingApplied = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi CellFormatting Grid: {dgvGiamThue.Name}, Col: {colName}, Row: {e.RowIndex}: {ex.Message}");
                e.Value = "[Lỗi]"; // Hiển thị lỗi trên ô
                e.FormattingApplied = true;
            }
        }
        private void DataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Lấy thông tin lưới, cột và dòng gây lỗi
            DataGridView dgv = sender as DataGridView;
            string gridName = dgv?.Name ?? "Unknown Grid";
            string colName = (e.ColumnIndex >= 0 && e.ColumnIndex < dgv?.Columns.Count)
                             ? dgv.Columns[e.ColumnIndex].Name : $"Index {e.ColumnIndex}"; // Lấy tên hoặc index
            string errorValue = (e.RowIndex >= 0 && e.RowIndex < dgv?.Rows.Count &&
                                 e.ColumnIndex >= 0 && e.ColumnIndex < dgv?.Columns.Count)
                                 ? dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "null" : "N/A"; // Lấy giá trị gây lỗi

            string errorMsg = $"Lỗi DataError:\nGrid: {gridName}\nRow: {e.RowIndex}, Col: {e.ColumnIndex} ({colName})\nGiá trị lỗi: '{errorValue}'\nError: {e.Exception.Message}\nContext: {e.Context}";

            // Ghi log lỗi (Quan trọng để debug)
            AppendLog($"‼️ {errorMsg.Replace("\n", " | ")}"); // Ghi log trên 1 dòng

            // Tùy chọn: Hiển thị lỗi chi tiết hơn (có thể comment dòng này đi nếu không muốn popup)
            // MessageBox.Show(errorMsg, "Lỗi DataGridView", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // Quan trọng: Ngăn dialog mặc định hiển thị và giữ chương trình chạy
            e.ThrowException = false; // Bỏ qua lỗi và không hiển thị dialog mặc định
                                      // Bạn có thể đặt giá trị mặc định cho ô nếu muốn, ví dụ:
                                      // if(e.Context == DataGridViewDataErrorContexts.Formatting) {
                                      //    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0; // Hoặc null tùy kiểu cột
                                      // }
        }

        private async void btnCnKoPdf_Click(object sender, EventArgs e)
        {
            if (dgvMain.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn từ danh sách để in PDF.", "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // --- LẤY DỮ LIỆU TỪ DÒNG ĐƯỢC CHỌN ---
            SearchResult selectedInvoice = dgvMain.CurrentRow.DataBoundItem as SearchResult;

            if (selectedInvoice == null)
            {
                MessageBox.Show("Không thể lấy dữ liệu hóa đơn từ dòng đã chọn.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog("⚠️ Lỗi: Không thể ép kiểu DataBoundItem thành SearchResult tại btnCnKoPdf_Click."); // Sửa tên hàm trong log
                return;
            }

            // --- KIỂM TRA THÔNG TIN HÓA ĐƠN CẦN THIẾT ---
            if (string.IsNullOrEmpty(selectedInvoice.Ma_so_thue) ||
                string.IsNullOrEmpty(selectedInvoice.Ky_hieu_hoa_don) ||
                !selectedInvoice.So_hoa_don.HasValue ||
                !selectedInvoice.Ky_hieu_ma_so.HasValue)
            {
                MessageBox.Show("Hóa đơn được chọn thiếu thông tin cần thiết (MST, Ký hiệu, Số HĐ, KH Mẫu số).", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AppendLog($"⚠️ Hóa đơn ID {selectedInvoice.Id} thiếu thông tin để tải ZIP.");
                return;
            }

            // --- QUẢN LÝ UI --- (SỬA TÊN NÚT)
            btnCnKoPdf.Enabled = false;       // <-- Đã sửa
            btnCnKoPdf.Text = "Đang tải...";  // <-- Đã sửa
            downloadProgressBar.Visible = true;
            downloadProgressBar.Style = ProgressBarStyle.Marquee;
            lblDownloadStatus.Text = $"Đang tải HĐ {selectedInvoice.So_hoa_don}...";
            lblDownloadStatus.Visible = true;

            string tempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-Print_{Guid.NewGuid()}"); // Thư mục tạm duy nhất
            string extractPath = Path.Combine(tempDirectory, "extracted");
            string zipFilePath = Path.Combine(tempDirectory, $"HD_{selectedInvoice.Ky_hieu_hoa_don}_{selectedInvoice.So_hoa_don}.zip");

            try
            {
                Directory.CreateDirectory(tempDirectory); // Tạo thư mục tạm
                Directory.CreateDirectory(extractPath);   // Tạo thư mục con để giải nén

                AppendLog($"🖨️ Bắt đầu tải ZIP để IN: {selectedInvoice.Ky_hieu_hoa_don} - {selectedInvoice.So_hoa_don}"); // Cập nhật log

                // --- TẢI FILE ZIP ---
                bool downloadSuccess = await DownloadSingleInvoiceZipAsync(selectedInvoice, tempDirectory);

                if (!downloadSuccess)
                {
                    MessageBox.Show("Không thể tải được file ZIP chứa hóa đơn.", "Lỗi tải file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!File.Exists(zipFilePath))
                {
                    MessageBox.Show($"File ZIP dự kiến ({Path.GetFileName(zipFilePath)}) không tồn tại sau khi tải.", "Lỗi tải file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    AppendLog($"⚠️ File ZIP không tồn tại tại đường dẫn: {zipFilePath}");
                    return;
                }

                // --- GIẢI NÉN ---
                lblDownloadStatus.Text = "Đang giải nén file...";
                await Task.Run(() =>
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(zipFilePath, extractPath, true); // Giải nén và ghi đè
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"❌ Lỗi giải nén ZIP (Task): {ex.ToString()}");
                        throw new Exception($"Lỗi khi giải nén file ZIP: {ex.Message}", ex);
                    }
                });

                AppendLog($"⚙️ Đã giải nén file: {zipFilePath} vào {extractPath}");

                // --- TÌM VÀ IN FILE PDF --- (THAY THẾ CODE LƯU BẰNG CODE IN)
                lblDownloadStatus.Text = "Đang tìm file PDF...";
                string[] pdfFiles = Directory.GetFiles(extractPath, "*.pdf", SearchOption.AllDirectories);
                string pdfPath = null;

                if (pdfFiles.Length > 0)
                {
                    pdfPath = pdfFiles[0]; // Lấy file PDF đầu tiên tìm thấy
                    AppendLog($"✅ Tìm thấy file PDF: {pdfPath}. Đang gửi đến máy in...");
                    lblDownloadStatus.Text = "Đang gửi file đến máy in...";

                    try
                    {
                        // Tạo một tiến trình mới để in file
                        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(pdfPath)
                        {
                            Verb = "print", // Sử dụng động từ "print" của Windows
                            UseShellExecute = true,
                            CreateNoWindow = true // Không hiển thị cửa sổ của phần mềm PDF
                        };
                        System.Diagnostics.Process.Start(psi);

                        AppendLog($"✅ Đã gửi lệnh in cho file: {pdfPath}");
                        MessageBox.Show($"Đã gửi hóa đơn (file PDF) đến máy in mặc định của bạn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception exPrint)
                    {
                        // Bắt lỗi nếu không thể tự động in (ví dụ: không có phần mềm đọc PDF)
                        AppendLog($"⚠️ Lỗi khi tự động in: {exPrint.Message}");
                        MessageBox.Show($"Không thể tự động gửi lệnh in. Lỗi: {exPrint.Message}\n\nFile PDF đã được lưu tạm tại:\n{pdfPath}\n(File này sẽ bị xóa khi đóng chương trình)", "Lỗi In", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    // Không tìm thấy file PDF nào
                    AppendLog("⚠️ Không tìm thấy file PDF trong file ZIP.");
                    MessageBox.Show("Không tìm thấy file PDF nào trong file ZIP của hóa đơn này. Không thể in.", "Không tìm thấy PDF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                // --- KẾT THÚC PHẦN THAY THẾ ---
            }
            catch (Exception ex) // Bắt các lỗi tổng quát (bao gồm cả lỗi giải nén)
            {
                AppendLog($"❌ Lỗi không mong muốn trong btnCnKoPdf_Click: {ex.ToString()}"); // Sửa tên hàm trong log
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // --- KHÔI PHỤC UI --- (SỬA TÊN NÚT)
                btnCnKoPdf.Enabled = true;     // <-- Đã sửa
                btnCnKoPdf.Text = "In PDF"; // <-- Đã sửa (bạn có thể đổi Text này thành "In HĐ" nếu muốn)
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";

                // --- DỌN DẸP THƯ MỤC TẠM ---
                try
                {
                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, true);
                        AppendLog($"🧹 Đã dọn dẹp thư mục tạm: {tempDirectory}");
                    }
                }
                catch (Exception exClean)
                {
                    AppendLog($"⚠️ Không thể dọn dẹp thư mục tạm PDF: {exClean.Message}");
                }
            }
        }

        private void LoadNotesFromFile()
        {
            try
            {
                if (File.Exists(_notesFilePath))
                {
                    AppendLog($"Đang tải ghi chú từ file: {_notesFilePath}");
                    string json = File.ReadAllText(_notesFilePath);
                    _invoiceNotes = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                    AppendLog($"✅ Đã tải {_invoiceNotes.Count} ghi chú.");
                }
                else
                {
                    _invoiceNotes = new Dictionary<string, string>(); // Khởi tạo nếu file không tồn tại
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi khi tải file ghi chú: {ex.Message}");
                _invoiceNotes = new Dictionary<string, string>(); // Dùng bộ nhớ trống nếu lỗi
            }
        }

        // Hàm này dùng để LƯU ghi chú ra file
        private void SaveNotesToFile()
        {
            try
            {
                AppendLog("Đang lưu ghi chú ra file...");
                string json = JsonSerializer.Serialize(_invoiceNotes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_notesFilePath, json);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi khi lưu file ghi chú: {ex.Message}");
                MessageBox.Show($"Không thể lưu file ghi chú: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvMain_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvMain.CurrentRow != null && dgvMain.CurrentRow.DataBoundItem is SearchResult selectedInvoice)
            {
                // 1. Lấy ID của hóa đơn
                string invoiceId = selectedInvoice.Id;

                // 2. Tìm ghi chú cho ID này trong bộ nhớ
                if (_invoiceNotes.TryGetValue(invoiceId, out string note))
                {
                    // 3a. Nếu tìm thấy, hiển thị nó
                    txtGhiChu.Text = note; // <-- THAY TÊN NẾU CẦN
                }
                else
                {
                    // 3b. Nếu không, xóa trắng ô
                    txtGhiChu.Text = ""; // <-- THAY TÊN NẾU CẦN
                }
            }
            else
            {
                txtGhiChu.Text = "";
            }
        }

        private void btnMoGhiChu_Click(object sender, EventArgs e)
        {
            // 1. Lấy hóa đơn đang được chọn
            if (dgvMain.CurrentRow == null || !(dgvMain.CurrentRow.DataBoundItem is SearchResult selectedInvoice))
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn để ghi chú.", "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. Lấy ID của hóa đơn và nội dung ghi chú
            string invoiceId = selectedInvoice.Id;
            string noteText = txtGhiChu.Text; // <-- THAY TÊN NẾU CẦN

            // 3. Lưu vào bộ nhớ (Dictionary)
            // Nếu đã có ghi chú cũ, nó sẽ bị ghi đè. Nếu chưa có, nó sẽ được thêm mới.
            _invoiceNotes[invoiceId] = noteText;
            SaveNotesToFile();

            MessageBox.Show("Đã lưu ghi chú thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AppendLog($"✅ Đã lưu ghi chú cho HĐ ID: {invoiceId}");
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void rbAllInvoices_CheckedChanged(object sender, EventArgs e)
        {

        }
    }

    public class SearchResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("nbmst")]
        public string Ma_so_thue { get; set; }

        [JsonPropertyName("khmshdon")]
        public int? Ky_hieu_ma_so { get; set; }

        [JsonPropertyName("khhdon")]
        public string Ky_hieu_hoa_don { get; set; }

        [JsonPropertyName("shdon")]
        public int? So_hoa_don { get; set; }

        [JsonPropertyName("tdlap")]
        public string Ngay_lap { get; set; }

        [JsonPropertyName("nbten")]
        public string Thong_tin_hoa_don { get; set; }

        [JsonPropertyName("tgtcthue")]
        public decimal? Tong_tien_chua_thue { get; set; }

        [JsonPropertyName("tgtthue")]
        public decimal? Tong_tien_thue { get; set; }

        [JsonPropertyName("ttcktmai")]
        public decimal? Tong_tien_chiet_khau { get; set; }

        [JsonPropertyName("thttlphi")]
        public List<decimal?> Tong_tien_phi { get; set; }

        [JsonPropertyName("tgtttbso")]
        public decimal? Tong_tien_thanh_toan { get; set; }

        [JsonPropertyName("dvtte")]
        public string Don_vi_tien_te { get; set; }

        [JsonPropertyName("tthai")]
        public int? Trang_thai_hoa_don { get; set; }

        [JsonPropertyName("kqcht")]
        public string Ket_qua_kiem_tra_hoa_don { get; set; }

        [JsonPropertyName("shdgoc")]
        public string Hoa_don_lien_quan { get; set; }

        [JsonPropertyName("nmdchi")]
        public string Thong_tin_lien_quan { get; set; }
    }

    public class SearchResponse
    {
        [JsonPropertyName("datas")]
        public List<SearchResult> Datas { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("time")]
        public int Time { get; set; }
    }
}