using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PuppeteerSharp;
using Svg;

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

            DateTime globalFromDate = dtpFromDate.Value.Date;
            DateTime globalToDate = dtpToDate.Value.Date;

            if (globalFromDate > globalToDate)
            {
                MessageBox.Show("Ngày bắt đầu không thể lớn hơn ngày kết thúc.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // --- QUẢN LÝ UI ---
            btnTaiHDGoc.Enabled = false;
            downloadProgressBar.Visible = true;
            lblDownloadStatus.Visible = true;

            // Thư mục tạm chính để chứa MỌI THỨ
            string baseTempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-Export_{Guid.NewGuid()}");
            // Thư mục con cho báo cáo
            string reportSubFolder = Path.Combine(baseTempDirectory, "0_BaoCaoExcel");
            // Thư mục con để chứa các file ZIP gốc
            string zipSubFolder = Path.Combine(baseTempDirectory, "1_HoaDonGoc_ZIP");
            // Thư mục con để chứa các file PDF đã convert (nếu có)
            string pdfSubFolder = Path.Combine(baseTempDirectory, "2_HoaDonPDF");
            // Thư mục con để chứa các file XML (đã giải nén)
            string xmlSubFolder = Path.Combine(baseTempDirectory, "3_HoaDonXML");

            List<SearchResult> invoicesToDownload = new List<SearchResult>();
            List<string> failedChunks = new List<string>();

            // Biến cho PuppeteerSharp
            bool chromiumDownloaded = false;
            string chromiumExecutablePath = null;

            try
            {
                // === GIAI ĐOẠN 1.5: TÌM KIẾM HÓA ĐƠN (LOGIC VÒNG LẶP) ===
                DateTime loopStartDate = globalFromDate;
                const int chunkSizeInDays = 30;
                double totalDays = (globalToDate - globalFromDate).TotalDays + 1;
                int totalLoops = (int)Math.Ceiling(totalDays / chunkSizeInDays);
                int apiCallsPerLoop = rbAllInvoices.Checked ? 2 : 1;

                lblDownloadStatus.Text = "Bước 1: Đang tìm kiếm hóa đơn...";
                downloadProgressBar.Style = ProgressBarStyle.Blocks;
                downloadProgressBar.Value = 0;
                downloadProgressBar.Maximum = totalLoops * apiCallsPerLoop; // Chỉ tính bước tìm kiếm trước

                int currentLoop = 0;
                while (loopStartDate <= globalToDate)
                {
                    currentLoop++;
                    DateTime loopEndDate = loopStartDate.AddDays(chunkSizeInDays - 1);
                    if (loopEndDate > globalToDate) loopEndDate = globalToDate;
                    lblDownloadStatus.Text = $"Bước 1: Tìm kiếm... ({currentLoop}/{totalLoops})";
                    AppendLog($"🔍 (Tải gốc) Lát cắt {currentLoop}/{totalLoops}: {loopStartDate:dd/MM/yyyy} - {loopEndDate:dd/MM/yyyy}");
                    DateTime preciseLoopEndDate = loopEndDate.Date.AddDays(1).AddTicks(-1);
                    string querySold = Timef(loopStartDate, preciseLoopEndDate, InvoiceType.Sold);
                    string queryBought = Timef(loopStartDate, preciseLoopEndDate, InvoiceType.Bought);

                    // Tìm HĐ Bán ra
                    if (rbSold.Checked || rbAllInvoices.Checked)
                    {
                        string resultSold = await GetProductsAsync("query/invoices/sold", querySold);
                        if (resultSold.StartsWith("❌")) { failedChunks.Add($"Bán ra [{loopStartDate:dd/MM} - {loopEndDate:dd/MM}]"); }
                        else
                        {
                            var responseSold = JsonSerializer.Deserialize<SearchResponse>(resultSold, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (responseSold?.Datas != null) invoicesToDownload.AddRange(responseSold.Datas);
                        }
                        if (downloadProgressBar.Value < downloadProgressBar.Maximum) downloadProgressBar.PerformStep();
                    }
                    // Tìm HĐ Mua vào
                    if (rbBought.Checked || rbAllInvoices.Checked)
                    {
                        string resultBought = await GetProductsAsync("query/invoices/purchase", queryBought);
                        if (resultBought.StartsWith("❌")) { failedChunks.Add($"Mua vào [{loopStartDate:dd/MM} - {loopEndDate:dd/MM}]"); }
                        else
                        {
                            var responseBought = JsonSerializer.Deserialize<SearchResponse>(resultBought, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (responseBought?.Datas != null) invoicesToDownload.AddRange(responseBought.Datas);
                        }
                        if (downloadProgressBar.Value < downloadProgressBar.Maximum) downloadProgressBar.PerformStep();
                    }
                    loopStartDate = loopEndDate.AddDays(1);
                    await Task.Delay(200);
                }
                // --- KẾT THÚC VÒNG LẶP TÌM KIẾM ---

                invoicesToDownload = invoicesToDownload
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderByDescending(inv => inv.Ngay_lap)
                .ThenBy(inv => inv.So_hoa_don)
                .ToList();

                if (!invoicesToDownload.Any())
                {
                    MessageBox.Show("Không tìm thấy hóa đơn nào trong khoảng thời gian đã chọn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // === GIAI ĐOẠN 2: TẢI VỀ VÀ XỬ LÝ (Sử dụng invoicesToDownload) ===
                ShowStatusMessage($"Tìm thấy {invoicesToDownload.Count} hóa đơn. Bắt đầu tải và xử lý...", Color.Green);

                // Đặt lại ProgressBar: 2 Excel + (Tải ZIP + Giải nén + Convert/Copy) cho mỗi HĐ + 1 bước Nén cuối
                downloadProgressBar.Maximum = 2 + (invoicesToDownload.Count * 3) + 1;
                downloadProgressBar.Value = 0;

                // Tạo các thư mục con trong thư mục tạm chính
                Directory.CreateDirectory(baseTempDirectory);
                Directory.CreateDirectory(reportSubFolder); // Thư mục cho Excel
                Directory.CreateDirectory(zipSubFolder);
                Directory.CreateDirectory(pdfSubFolder);
                Directory.CreateDirectory(xmlSubFolder);

                lblDownloadStatus.Text = "Bước 2: Đang tạo file Excel danh sách...";
                string listExcelPath = Path.Combine(reportSubFolder, "0_DanhSachHoaDon.xlsx"); // Lưu vào thư mục con
                DateTime preciseGlobalToDate = globalToDate.AddDays(1).AddTicks(-1);
                string queryForListExcel = Timef(globalFromDate, preciseGlobalToDate, rbBought.Checked ? InvoiceType.Bought : InvoiceType.Sold);
                if (rbAllInvoices.Checked) queryForListExcel = Timef(globalFromDate, preciseGlobalToDate, InvoiceType.Sold);
                await DownloadInvoiceListExcelAsync(queryForListExcel, listExcelPath);
                downloadProgressBar.PerformStep(); // +1

                lblDownloadStatus.Text = "Bước 3: Đang tạo file Excel chi tiết...";
                await ExportInvoiceDetailsToExcelAsync(invoicesToDownload, Path.Combine(reportSubFolder, "0_ChiTietHoaDon.xlsx")); // Lưu vào thư mục con
                downloadProgressBar.PerformStep(); // +1

                int successCount = 0;
                int convertSuccessCount = 0;

                // === BƯỚC 4: TẢI, GIẢI NÉN, CONVERT (NÂNG CẤP) ===
                for (int i = 0; i < invoicesToDownload.Count; i++)
                {
                    var invoice = invoicesToDownload[i];
                    string invoiceIdentifier = $"{invoice.Ky_hieu_hoa_don?.Replace('/', '_') ?? "KH_NA"}_{invoice.So_hoa_don?.ToString() ?? "SHD_NA"}";
                    lblDownloadStatus.Text = $"Bước 4: Đang xử lý HĐ ({i + 1}/{invoicesToDownload.Count})...";
                    string zipFilePath = Path.Combine(zipSubFolder, $"HD_{invoiceIdentifier}.zip");
                    string tempExtractPath = Path.Combine(baseTempDirectory, $"_temp_extract_{i}"); // Thư mục tạm để giải nén
                    int currentStepBase = 2 + (i * 3); // 2 (Excel) + (i * 3 bước)

                    try
                    {
                        // --- 4.1. Tải ZIP ---
                        if (await DownloadSingleInvoiceZipAsync(invoice, zipSubFolder))
                        {
                            successCount++;
                        }
                        else
                        {
                            failedChunks.Add($"{invoiceIdentifier} (Lỗi tải ZIP)");
                            downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum); // Bỏ qua 3 bước
                            continue;
                        }
                        downloadProgressBar.Value = Math.Min(currentStepBase + 1, downloadProgressBar.Maximum);

                        // --- 4.2. Giải nén ZIP ---
                        Directory.CreateDirectory(tempExtractPath);
                        await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath, true));
                        downloadProgressBar.Value = Math.Min(currentStepBase + 2, downloadProgressBar.Maximum);

                        // --- 4.3. Tìm và Convert/Copy ---
                        string[] pdfFiles = Directory.GetFiles(tempExtractPath, "*.pdf", SearchOption.AllDirectories);
                        string[] htmlFiles = Directory.GetFiles(tempExtractPath, "*.html", SearchOption.AllDirectories);
                        string[] xmlFiles = Directory.GetFiles(tempExtractPath, "*.xml", SearchOption.AllDirectories);
                        string outputPdfPath = Path.Combine(pdfSubFolder, $"HD_{invoiceIdentifier}.pdf");

                        if (pdfFiles.Length > 0)
                        {
                            File.Copy(pdfFiles[0], outputPdfPath, true);
                            AppendLog($"✅ (Tải gốc) Đã sao chép PDF gốc: {invoiceIdentifier}");
                            convertSuccessCount++;
                        }
                        else if (htmlFiles.Length > 0)
                        {
                            lblDownloadStatus.Text = $"Convert HTML... ({i + 1}/{invoicesToDownload.Count})";
                            AppendLog($"🔄 (Tải gốc) Convert HTML: {invoiceIdentifier}");

                            if (!chromiumDownloaded)
                            {
                                lblDownloadStatus.Text = "Tải Chromium (lần đầu)...";
                                AppendLog("   -> Đang tải Chromium...");
                                var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { });
                                var installedBrowser = await browserFetcher.DownloadAsync();
                                AppendLog("   -> Tải Chromium hoàn tất.");
                                if (installedBrowser != null && !string.IsNullOrEmpty(installedBrowser.BuildId))
                                {
                                    chromiumExecutablePath = browserFetcher.GetExecutablePath(installedBrowser.BuildId);
                                }
                                if (string.IsNullOrEmpty(chromiumExecutablePath)) throw new Exception("Không thể lấy đường dẫn Chromium.");
                                chromiumDownloaded = true;
                                lblDownloadStatus.Text = $"Convert HTML... ({i + 1}/{invoicesToDownload.Count})";
                            }

                            var launchOptions = new LaunchOptions { Headless = true, ExecutablePath = chromiumExecutablePath };
                            await using var browser = await Puppeteer.LaunchAsync(launchOptions);
                            await using var page = await browser.NewPageAsync();
                            await page.GoToAsync("file:///" + htmlFiles[0].Replace('\\', '/'), new NavigationOptions { Timeout = 60000, WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });
                            await page.PdfAsync(outputPdfPath, new PdfOptions { Format = PuppeteerSharp.Media.PaperFormat.A4 });
                            AppendLog($"  -> Convert thành công: {invoiceIdentifier}");
                            convertSuccessCount++;
                        }
                        else
                        {
                            AppendLog($"⚠️ (Tải gốc) Không tìm thấy PDF/HTML cho HĐ: {invoiceIdentifier}");
                            failedChunks.Add($"{invoiceIdentifier} (Không tìm thấy PDF/HTML)");
                        }

                        // Sao chép file XML
                        if (xmlFiles.Length > 0)
                        {
                            string outputXmlPath = Path.Combine(xmlSubFolder, $"HD_{invoiceIdentifier}.xml");
                            File.Copy(xmlFiles[0], outputXmlPath, true);
                        }

                        downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum); // Hoàn thành bước 3
                    }
                    catch (Exception exConvert)
                    {
                        AppendLog($"❌ (Tải gốc) Lỗi Giải nén/Convert HĐ {invoiceIdentifier}: {exConvert.Message}");
                        failedChunks.Add($"{invoiceIdentifier} (Lỗi giải nén/convert)");
                        downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum); // Hoàn thành 3 bước (dù lỗi)
                    }
                    finally
                    {
                        // Dọn dẹp thư mục giải nén tạm
                        try { if (Directory.Exists(tempExtractPath)) Directory.Delete(tempExtractPath, true); }
                        catch (Exception exCleanTemp) { AppendLog($"⚠️ Lỗi dọn dẹp thư mục tạm {tempExtractPath}: {exCleanTemp.Message}"); }
                    }
                } // Kết thúc vòng lặp For

                // --- GIAI ĐOẠN 3: NÉN THƯ MỤC KẾT QUẢ VÀ THÔNG BÁO ---
                lblDownloadStatus.Text = "Bước 5: Đang nén kết quả...";
                downloadProgressBar.Value = Math.Min(downloadProgressBar.Maximum - 1, downloadProgressBar.Maximum); // Gần xong

                using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục để lưu file ZIP kết quả" };

                if (fbd.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    AppendLog("ℹ️ Người dùng đã hủy chọn thư mục lưu.");
                }
                else
                {
                    string finalZipName = $"E-Tax_Export_{globalFromDate:yyyyMMdd}_den_{globalToDate:yyyyMMdd}.zip";
                    string finalZipPath = Path.Combine(fbd.SelectedPath, finalZipName);
                    int counter = 1;
                    while (File.Exists(finalZipPath))
                    {
                        finalZipName = $"E-Tax_Export_{globalFromDate:yyyyMMdd}_den_{globalToDate:yyyyMMdd}_{counter}.zip";
                        finalZipPath = Path.Combine(fbd.SelectedPath, finalZipName);
                        counter++;
                    }

                    AppendLog($"📦 Đang nén thư mục kết quả: {baseTempDirectory} -> {finalZipPath}");
                    await Task.Run(() => ZipFile.CreateFromDirectory(baseTempDirectory, finalZipPath)); // Chạy nén trên Task
                    AppendLog($"✅ Nén xong.");
                    downloadProgressBar.Value = downloadProgressBar.Maximum; // Hoàn thành

                    // *** BỎ QUA VIỆC GIẢI NÉN LẠI VÀ GỌI UnzipInnerArchives ***
                    // AppendLog($"🚀 Đang giải nén file tổng hợp vào thư mục đích...");
                    // ZipFile.ExtractToDirectory(tempZipPath, finalExtractionPath, true);
                    // AppendLog($"🔍 Bắt đầu giải nén các file ZIP hóa đơn con...");
                    // await Task.Run(() => UnzipInnerArchives(finalExtractionPath));

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
            }
            catch (JsonException jsonEx)
            {
                AppendLog($"❌ Lỗi phân tích JSON trong SaveOriginalInvoicesAsync: {jsonEx.ToString()}");
                MessageBox.Show($"Lỗi xử lý dữ liệu trả về từ API: {jsonEx.Message}", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ioEx)
            {
                AppendLog($"❌ Lỗi IO trong SaveOriginalInvoicesAsync: {ioEx.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi khi xử lý file hoặc thư mục: {ioEx.Message}\n\nThường do đường dẫn quá dài, không có quyền ghi, hoặc file đang được sử dụng.", "Lỗi File/Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AppendLog($"🐞 LỖI NGHIÊM TRỌNG trong SaveOriginalInvoicesAsync: {ex.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
               try
                {
                    AppendLog($"🧹 Đang dọn dẹp thư mục tạm: {baseTempDirectory}");
                    if (Directory.Exists(baseTempDirectory))
                    {
                        Directory.Delete(baseTempDirectory, true);
                    }
                    AppendLog($"🧹 Đang dọn dẹp các file ZIP tạm...");
                    foreach (var tempZip in Directory.GetFiles(Path.GetTempPath(), "Temp_HoaDon_TongHop_*.zip"))
                    {
                        File.Delete(tempZip);
                        AppendLog($"   -> Đã xóa: {Path.GetFileName(tempZip)}");
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
                AppendLog("🏁 Kết thúc tiến trình Tải HĐ Gốc.");
            }
        }

        private async void btnTaiHDGoc_Click(object sender, EventArgs e)
        {
            // === BỔ SUNG: THAY ĐỔI TEXT VÀ REFRESH ===
            btnTaiHDGoc.Text = "Đang xử lý...";
            btnTaiHDGoc.Refresh(); // Buộc nút vẽ lại text mới ngay lập tức
            // =========================================

            // === VÔ HIỆU HÓA FORM ĐỂ TẠO HIỆU ỨNG "MODAL" ===
            this.Enabled = false;
            this.Cursor = Cursors.WaitCursor; // Thêm con trỏ chờ

            try
            {
                // Chạy hàm tải chính
                await SaveOriginalInvoicesAsync();
            }
            catch (Exception ex)
            {
                // Bắt các lỗi không mong muốn (nếu SaveOriginalInvoicesAsync ném ra)
                AppendLog($"FATAL ERROR in btnTaiHDGoc_Click (captured from SaveOriginalInvoicesAsync): {ex.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi nghiêm trọng không xác định: {ex.Message}", "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // === KÍCH HOẠT LẠI FORM ===
                this.Enabled = true;
                this.Cursor = Cursors.Default;

                // === BỔ SUNG: KHÔI PHỤC TEXT NÚT ===
                btnTaiHDGoc.Text = "Tải tất cả HĐ Gốc";
                // ====================================
            }
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
            DateTime globalFromDate = dtpFromDate.Value.Date; // Lấy ngày bắt đầu tổng
            DateTime globalToDate = dtpToDate.Value.Date;    // Lấy ngày kết thúc tổng

            if (globalFromDate > globalToDate)
            {
                MessageBox.Show("Ngày bắt đầu không thể lớn hơn ngày kết thúc.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // --- QUẢN LÝ TRẠNG THÁI UI (Bắt đầu) ---
            btnLeftSearch.Enabled = false;
            btnLeftSearch.Text = "Đang tìm...";
            this.Cursor = Cursors.WaitCursor;
            dgvMain.DataSource = null;
            dgvDetails.DataSource = null;
            dgvMua.DataSource = null;
            dgvBan.DataSource = null;
            dgvVatNop.DataSource = null;
            dgvGiamThue.DataSource = null;
            _latestResults.Clear();
            _lastSuccessfulQueryString = ""; // Xóa query string cũ

            // --- LOGIC VÒNG LẶP TÌM KIẾM MỚI ---
            List<SearchResult> allSearchResults = new List<SearchResult>();
            List<string> failedChunks = new List<string>(); // Lưu lại các khoảng thời gian bị lỗi
            DateTime loopStartDate = globalFromDate;

            // Tính toán số lần gọi API cho thanh tiến trình
            const int chunkSizeInDays = 30;
            double totalDays = (globalToDate - globalFromDate).TotalDays + 1; // +1 để bao gồm cả ngày cuối
            int totalLoops = (int)Math.Ceiling(totalDays / chunkSizeInDays);

            int apiCallsPerLoop = 0;
            if (rbSold.Checked) apiCallsPerLoop = 1;
            else if (rbBought.Checked) apiCallsPerLoop = 1;
            else if (rbAllInvoices.Checked) apiCallsPerLoop = 2;

            downloadProgressBar.Value = 0;
            downloadProgressBar.Maximum = totalLoops * apiCallsPerLoop; // SỐ vòng lặp * số API (Bán/Mua)
            downloadProgressBar.Visible = true;
            downloadProgressBar.Style = ProgressBarStyle.Blocks; // Chuyển sang kiểu khối
            lblDownloadStatus.Visible = true;

            try
            {
                int currentLoop = 0;
                while (loopStartDate <= globalToDate)
                {
                    currentLoop++;
                    // Tính ngày kết thúc của "lát cắt" này (tối đa 30 ngày tính cả ngày bắt đầu)
                    DateTime loopEndDate = loopStartDate.AddDays(chunkSizeInDays - 1);

                    // Đảm bảo ngày kết thúc không vượt quá ngày kết thúc tổng
                    if (loopEndDate > globalToDate)
                    {
                        loopEndDate = globalToDate;
                    }

                    lblDownloadStatus.Text = $"Đang tìm... ({currentLoop}/{totalLoops}): [{loopStartDate:dd/MM/yy} - {loopEndDate:dd/MM/yy}]";
                    AppendLog($"🔍 Bắt đầu lát cắt {currentLoop}/{totalLoops}: {loopStartDate:dd/MM/yyyy} - {loopEndDate:dd/MM/yyyy}");

                    // Lấy thời điểm cuối cùng trong ngày (23:59:59)
                    DateTime preciseLoopEndDate = loopEndDate.Date.AddDays(1).AddTicks(-1);

                    string currentQuerySold = Timef(loopStartDate, preciseLoopEndDate, InvoiceType.Sold);
                    string currentQueryBought = Timef(loopStartDate, preciseLoopEndDate, InvoiceType.Bought);

                    // --- Tìm hóa đơn bán ra (trong lát cắt) ---
                    if (rbSold.Checked || rbAllInvoices.Checked)
                    {
                        lblDownloadStatus.Text = $"Đang tìm HĐ Bán ra... ({currentLoop}/{totalLoops})";
                        string resultSold = await GetProductsAsync("query/invoices/sold", currentQuerySold);
                        if (resultSold.StartsWith("❌"))
                        {
                            AppendLog($"❌ Lỗi lát cắt (Bán ra) {currentLoop}: {resultSold}");
                            failedChunks.Add($"Bán ra [{loopStartDate:dd/MM} - {loopEndDate:dd/MM}]");
                        }
                        else
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var responseSold = JsonSerializer.Deserialize<SearchResponse>(resultSold, options);
                            if (responseSold?.Datas != null)
                            {
                                responseSold.Datas.ForEach(item => item.Thong_tin_lien_quan = "Bán ra");
                                allSearchResults.AddRange(responseSold.Datas);
                                AppendLog($"✅ Lát cắt (Bán ra) {currentLoop} tìm thấy {responseSold.Datas.Count} HĐ.");
                            }
                        }
                        if (downloadProgressBar.Value < downloadProgressBar.Maximum)
                            downloadProgressBar.PerformStep();
                    }

                    // --- Tìm hóa đơn mua vào (trong lát cắt) ---
                    if (rbBought.Checked || rbAllInvoices.Checked)
                    {
                        lblDownloadStatus.Text = $"Đang tìm HĐ Mua vào... ({currentLoop}/{totalLoops})";
                        string resultBought = await GetProductsAsync("query/invoices/purchase", currentQueryBought);
                        if (resultBought.StartsWith("❌"))
                        {
                            AppendLog($"❌ Lỗi lát cắt (Mua vào) {currentLoop}: {resultBought}");
                            failedChunks.Add($"Mua vào [{loopStartDate:dd/MM} - {loopEndDate:dd/MM}]");
                        }
                        else
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var responseBought = JsonSerializer.Deserialize<SearchResponse>(resultBought, options);
                            if (responseBought?.Datas != null)
                            {
                                responseBought.Datas.ForEach(item => item.Thong_tin_lien_quan = "Mua vào");
                                allSearchResults.AddRange(responseBought.Datas);
                                AppendLog($"✅ Lát cắt (Mua vào) {currentLoop} tìm thấy {responseBought.Datas.Count} HĐ.");
                            }
                        }
                        if (downloadProgressBar.Value < downloadProgressBar.Maximum)
                            downloadProgressBar.PerformStep();
                    }

                    // Chuyển sang ngày bắt đầu của lát cắt tiếp theo
                    loopStartDate = loopEndDate.AddDays(1);
                    await Task.Delay(200); // Thêm 1 chút delay nhỏ giữa các lần gọi API để tránh bị chặn
                }
                // --- KẾT THÚC VÒNG LẶP ---

                // --- XỬ LÝ KẾT QUẢ TÌM KIẾM TỔNG HỢP ---
                lblDownloadStatus.Text = "Đang tổng hợp kết quả...";
                _latestResults = allSearchResults
                                        .GroupBy(r => r.Id) // Nhóm theo ID để loại bỏ trùng lặp (nếu có)
                                        .Select(g => g.First())
                                        .OrderByDescending(r => r.Ngay_lap)
                                        .ThenBy(r => r.So_hoa_don)
                                        .ToList();

                // Vẫn lưu 1 query string (ví dụ: của lát cắt đầu tiên) để dùng cho "Tải danh sách HĐ"
                // (Lưu ý: Chức năng "Tải danh sách" có thể cần sửa lại để hỗ trợ nhiều khoảng thời gian)
                if (rbSold.Checked) { _lastSuccessfulQueryString = Timef(globalFromDate, globalToDate.AddDays(1).AddTicks(-1), InvoiceType.Sold); }
                else if (rbBought.Checked) { _lastSuccessfulQueryString = Timef(globalFromDate, globalToDate.AddDays(1).AddTicks(-1), InvoiceType.Bought); }
                else { _lastSuccessfulQueryString = Timef(globalFromDate, globalToDate.AddDays(1).AddTicks(-1), InvoiceType.Sold); }


                if (_latestResults.Any())
                {
                    AppendLog($"📊 Tìm thấy tổng cộng {_latestResults.Count} hóa đơn (sau khi loại bỏ trùng lặp).");
                    lblDownloadStatus.Text = $"Đang hiển thị {_latestResults.Count} hóa đơn...";

                    // Gán DataSource cho lưới chính
                    dgvMain.DataSource = _latestResults;

                    // Cập nhật STT và định dạng ngày
                    UpdateGridRowNumbers(); // Gọi hàm cập nhật STT (đã có sẵn)
                    dgvMain.SuspendLayout();
                    try
                    {
                        foreach (DataGridViewRow row in dgvMain.Rows)
                        {
                            // Định dạng ngày (nếu cần, vì UpdateGridRowNumbers có thể chưa làm)
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

                    AppendLog("✅ Hiển thị xong lưới tổng hợp.");
                    AppendLog($"📊 Gán dữ liệu cho Bảng kê giảm thuế...");
                    dgvGiamThue.DataSource = null;
                    dgvGiamThue.DataSource = _latestResults;
                    AppendLog($"✅ Hiển thị xong Bảng kê giảm thuế.");

                    // Tải dữ liệu chi tiết cho các tab khác
                    await _detailGridManager.PopulateDetailGridAsync(_latestResults);
                }
                else
                {
                    lblDownloadStatus.Text = "Không tìm thấy hóa đơn nào.";
                    MessageBox.Show("Không tìm thấy hóa đơn nào phù hợp với điều kiện tìm kiếm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppendLog("ℹ️ Không tìm thấy hóa đơn nào.");
                }

                // Thông báo nếu có lỗi trong các lát cắt
                if (failedChunks.Any())
                {
                    string errorList = string.Join("\n - ", failedChunks);
                    MessageBox.Show($"Đã hoàn tất tìm kiếm, tuy nhiên đã xảy ra lỗi ở các khoảng thời gian sau:\n - {errorList}\n\nKết quả từ các khoảng thời gian này có thể bị thiếu.", "Lỗi trong quá trình tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (JsonException jsonEx)
            {
                AppendLog($"❌ Lỗi phân tích JSON kết quả tìm kiếm: {jsonEx.ToString()}");
                MessageBox.Show($"Lỗi xử lý dữ liệu trả về: {jsonEx.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi không mong muốn khi tìm kiếm: {ex.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // --- KHÔI PHỤC TRẠNG THÁI UI ---
                btnLeftSearch.Enabled = true;
                btnLeftSearch.Text = "Tìm kiếm";
                this.Cursor = Cursors.Default;
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
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
            // Kiểm tra xem đã CÓ KẾT QUẢ TÌM KIẾM (_latestResults) chưa
            if (_latestResults == null || !_latestResults.Any())
            {
                MessageBox.Show("Chưa có dữ liệu hóa đơn nào để xuất. Vui lòng nhấn 'Tìm kiếm' trước.",
                        "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hỏi người dùng nơi lưu file
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"DanhSachHoaDon_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Lưu danh sách hóa đơn"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                // Cập nhật UI
                btnExportDS.Enabled = false;
                btnExportDS.Text = "Đang tạo...";
                downloadProgressBar.Visible = true;
                downloadProgressBar.Style = ProgressBarStyle.Marquee;
                lblDownloadStatus.Text = "Đang tạo file Excel danh sách...";
                lblDownloadStatus.Visible = true;

                try
                {
                    // === THAY ĐỔI QUAN TRỌNG ===
                    // Gọi hàm TỰ TẠO Excel từ danh sách _latestResults (đã đầy đủ)
                    // thay vì gọi DownloadInvoiceListExcelAsync (gọi API)
                    await ExportSearchResultsToExcelAsync(_latestResults, sfd.FileName);
                    // === KẾT THÚC THAY ĐỔI ===

                    MessageBox.Show($"Đã tạo và lưu thành công file Excel danh sách vào:\n{sfd.FileName}",
                  "Tạo file thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tạo file Excel danh sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    AppendLog($"❌ Lỗi btnExportDS_Click (Tự tạo Excel): {ex.ToString()}");
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
            AddTextColumnToGrid(dgvGiamThue, "colGiamThueSoHD", "Số hóa đơn", nameof(SearchResult.So_hoa_don), 100, frozen: true, alignment: DataGridViewContentAlignment.MiddleCenter);
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

            e.ThrowException = false;
        }

        private async void btnCnKoPdf_Click(object sender, EventArgs e)
        {
            // === BƯỚC 1: LẤY DANH SÁCH HÓA ĐƠN ĐÃ CHỌN (ĐÃ SỬA LOGIC) ===
            List<SearchResult> selectedInvoices = new List<SearchResult>();

            if (dgvMain.SelectedRows.Count > 0)
            {
                AppendLog($"Tìm thấy {dgvMain.SelectedRows.Count} dòng được chọn (SelectedRows).");
                selectedInvoices = dgvMain.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(row => row.DataBoundItem as SearchResult)
                    .Where(invoice => invoice != null)
                    .ToList();
            }
            else if (dgvMain.CurrentRow != null)
            {
                AppendLog("Không tìm thấy SelectedRows. Lấy CurrentRow.");
                var currentInvoice = dgvMain.CurrentRow.DataBoundItem as SearchResult;
                if (currentInvoice != null)
                {
                    selectedInvoices.Add(currentInvoice);
                }
            }

            List<SearchResult> validInvoices = selectedInvoices
                .Where(invoice => !string.IsNullOrEmpty(invoice.Ma_so_thue) &&
                                 !string.IsNullOrEmpty(invoice.Ky_hieu_hoa_don) &&
                                 invoice.So_hoa_don.HasValue &&
                                 invoice.Ky_hieu_ma_so.HasValue)
                .OrderBy(inv => inv.Ky_hieu_hoa_don).ThenBy(inv => inv.So_hoa_don)
                .ToList();

            if (!validInvoices.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một hóa đơn hợp lệ từ danh sách để in/lưu PDF.",
                                "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (selectedInvoices.Any() && !validInvoices.Any())
                {
                    AppendLog("⚠️ Đã chọn hóa đơn nhưng thiếu thông tin (MST/KHHĐ/SHĐ/KHMHS).");
                }
                return;
            }

            AppendLog($"🖨️ Bắt đầu xử lý {validInvoices.Count} hóa đơn hợp lệ để In/Lưu PDF...");

            // --- QUẢN LÝ UI ---
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

            bool chromiumDownloaded = false;
            string chromiumExecutablePath = null;

            try
            {
                Directory.CreateDirectory(baseTempDirectory);

                // --- BƯỚC 2: TẢI, GIẢI NÉN VÀ CHUẨN BỊ FILE PDF CHO TỪNG HÓA ĐƠN ---
                for (int i = 0; i < validInvoices.Count; i++)
                {
                    SearchResult invoice = validInvoices[i];
                    string invoiceIdentifier = $"{invoice.Ky_hieu_hoa_don?.Replace('/', '_') ?? "KH_NA"}_{invoice.So_hoa_don?.ToString() ?? "SHD_NA"}";
                    string invoiceTempDir = Path.Combine(baseTempDirectory, $"Invoice_{i}_{invoiceIdentifier}");
                    string extractPath = Path.Combine(invoiceTempDir, "extracted");
                    string zipFilePath = Path.Combine(invoiceTempDir, $"HD_{invoiceIdentifier}.zip");
                    string outputPdfPath = Path.Combine(invoiceTempDir, $"HD_{invoiceIdentifier}.pdf");
                    string fileToProcess = null;
                    string fileTypeFound = "";
                    int currentStepBase = i * 4;

                    try
                    {
                        Directory.CreateDirectory(invoiceTempDir);
                        Directory.CreateDirectory(extractPath);

                        // --- Tải ZIP ---
                        lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang tải HĐ {invoice.So_hoa_don}...";
                        AppendLog($" Tải ZIP cho HĐ: {invoiceIdentifier}");
                        bool downloadSuccess = await DownloadSingleInvoiceZipAsync(invoice, invoiceTempDir);
                        downloadProgressBar.Value = Math.Min(currentStepBase + 1, downloadProgressBar.Maximum);

                        if (!downloadSuccess || !File.Exists(zipFilePath))
                        {
                            AppendLog($"  -> Lỗi tải ZIP HĐ {invoiceIdentifier}");
                            failedInvoicesInfo.Add($"{invoiceIdentifier} (Lỗi tải file ZIP)");
                            downloadProgressBar.Value = Math.Min(currentStepBase + 4, downloadProgressBar.Maximum);
                            continue;
                        }

                        // --- Giải nén ---
                        lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang giải nén HĐ {invoice.So_hoa_don}...";
                        AppendLog($"  -> Giải nén ZIP HĐ: {invoiceIdentifier}");
                        await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, extractPath, true));
                        downloadProgressBar.Value = Math.Min(currentStepBase + 2, downloadProgressBar.Maximum);

                        // --- Tìm file PDF hoặc HTML ---
                        lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang tìm file HĐ {invoice.So_hoa_don}...";
                        string[] pdfFiles = Directory.GetFiles(extractPath, "*.pdf", SearchOption.AllDirectories);
                        string[] htmlFiles = Directory.GetFiles(extractPath, "*.html", SearchOption.AllDirectories);

                        if (pdfFiles.Length > 0)
                        {
                            fileToProcess = pdfFiles[0];
                            fileTypeFound = "PDF";
                            AppendLog($"  -> Tìm thấy PDF: {Path.GetFileName(fileToProcess)}");
                            File.Move(fileToProcess, outputPdfPath);
                            finalPdfPaths.Add(outputPdfPath);
                            downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum);
                        }
                        else if (htmlFiles.Length > 0)
                        {
                            fileToProcess = htmlFiles[0];
                            fileTypeFound = "HTML";
                            AppendLog($"  -> Tìm thấy HTML: {Path.GetFileName(fileToProcess)}");

                            // --- Convert HTML sang PDF ---
                            lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang convert HTML sang PDF...";
                            AppendLog($"  -> Chuyển đổi HTML sang PDF...");

                            try
                            {
                                // Tải/Kiểm tra Chromium nếu cần
                                if (!chromiumDownloaded)
                                {
                                    lblDownloadStatus.Text = "Đang kiểm tra/tải Chromium (chỉ lần đầu)...";
                                    AppendLog("   -> Đang kiểm tra/tải trình duyệt Chromium...");
                                    var browserFetcherOptions = new BrowserFetcherOptions { /* Path = ... */ };
                                    var browserFetcher = new BrowserFetcher(browserFetcherOptions);

                                    var installedBrowser = await browserFetcher.DownloadAsync();
                                    AppendLog("   -> Kiểm tra/Tải Chromium hoàn tất.");

                                    if (installedBrowser != null && !string.IsNullOrEmpty(installedBrowser.BuildId))
                                    {
                                        AppendLog($"   -> Lấy được BuildId từ DownloadAsync: {installedBrowser.BuildId}");
                                        try
                                        {
                                            chromiumExecutablePath = browserFetcher.GetExecutablePath(installedBrowser.BuildId);
                                            AppendLog($"   -> Đường dẫn Chromium: {chromiumExecutablePath}");
                                        }
                                        catch (Exception pathEx)
                                        {
                                            AppendLog($"   -> Lỗi khi gọi GetExecutablePath với BuildId {installedBrowser.BuildId}: {pathEx.Message}");
                                            chromiumExecutablePath = null;
                                        }
                                    }
                                    else
                                    {
                                        AppendLog("   -> !!! Lỗi: Không lấy được BuildId hợp lệ từ DownloadAsync.");
                                        chromiumExecutablePath = null;
                                    }

                                    if (string.IsNullOrEmpty(chromiumExecutablePath))
                                    {
                                        AppendLog("   -> !!! Lỗi nghiêm trọng: Không tìm thấy đường dẫn Chromium.");
                                        throw new Exception("Không thể xác định đường dẫn thực thi Chromium. Quá trình convert PDF không thể tiếp tục.");
                                    }

                                    chromiumDownloaded = true;
                                    lblDownloadStatus.Text = $"({i + 1}/{validInvoices.Count}) Đang convert HTML sang PDF...";
                                }

                                if (string.IsNullOrEmpty(chromiumExecutablePath))
                                {
                                    throw new Exception("Đường dẫn thực thi Chromium không hợp lệ.");
                                }

                                // Convert
                                var launchOptions = new LaunchOptions { Headless = true, ExecutablePath = chromiumExecutablePath };
                                await using var browser = await Puppeteer.LaunchAsync(launchOptions);
                                await using var page = await browser.NewPageAsync();

                                await page.GoToAsync("file:///" + fileToProcess.Replace('\\', '/'),
                                                    timeout: 60000,
                                                    waitUntil: new[] { WaitUntilNavigation.Networkidle0 });

                                await page.PdfAsync(outputPdfPath, new PdfOptions { Format = PuppeteerSharp.Media.PaperFormat.A4 });
                                finalPdfPaths.Add(outputPdfPath);
                                AppendLog($"  -> Chuyển đổi thành công: {Path.GetFileName(outputPdfPath)}");
                                downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum);
                            }
                            catch (Exception exConvert)
                            {
                                AppendLog($"  -> Lỗi convert HTML->PDF HĐ {invoiceIdentifier}: {exConvert.ToString()}");
                                failedInvoicesInfo.Add($"{invoiceIdentifier} (Lỗi convert HTML sang PDF)");
                                downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum);
                                continue;
                            }
                        }
                        else
                        {
                            AppendLog($"  -> Không tìm thấy PDF/HTML cho HĐ {invoiceIdentifier}");
                            failedInvoicesInfo.Add($"{invoiceIdentifier} (Không tìm thấy file PDF/HTML)");
                            downloadProgressBar.Value = Math.Min(currentStepBase + 3, downloadProgressBar.Maximum);
                            continue;
                        }
                    }
                    catch (Exception exInvoice)
                    {
                        AppendLog($"❌ Lỗi xử lý HĐ {invoiceIdentifier}: {exInvoice.ToString()}");
                        failedInvoicesInfo.Add($"{invoiceIdentifier} (Lỗi chung: {exInvoice.Message})");
                        downloadProgressBar.Value = Math.Min(currentStepBase + 4, downloadProgressBar.Maximum);
                        continue;
                    }
                } // Kết thúc vòng lặp for

                // --- BƯỚC 3: KIỂM TRA MÁY IN VÀ THỰC HIỆN IN HOẶC LƯU ---
                if (!finalPdfPaths.Any())
                {
                    MessageBox.Show("Không có file PDF nào được tạo/tìm thấy để thực hiện.", "Không có kết quả", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    AppendLog("⚠️ Không có file PDF nào sau khi xử lý.");
                    downloadProgressBar.Value = downloadProgressBar.Maximum;
                    return;
                }

                lblDownloadStatus.Text = "Kiểm tra máy in...";
                bool hasPrinters = false;
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
                    // Có máy in -> In từng file
                    AppendLog($"🖨️ Có máy in. Gửi {finalPdfPaths.Count} file PDF đến hộp thoại in...");
                    lblDownloadStatus.Text = $"Đang mở hộp thoại in ({finalPdfPaths.Count} file)...";
                    int printSuccessCount = 0;
                    foreach (string pdfPath in finalPdfPaths)
                    {
                        lblDownloadStatus.Text = $"Đang gửi file {Path.GetFileName(pdfPath)} đến máy in...";
                        try
                        {
                            if (!File.Exists(pdfPath))
                            {
                                AppendLog($"  -> Lỗi: File PDF không tồn tại để in: {pdfPath}");
                                failedInvoicesInfo.Add($"{Path.GetFileNameWithoutExtension(pdfPath)} (File PDF tạm không tồn tại)");
                                downloadProgressBar.PerformStep();
                                continue;
                            }
                            ProcessStartInfo psi = new ProcessStartInfo(pdfPath) { Verb = "print", UseShellExecute = true, CreateNoWindow = true };
                            Process.Start(psi);
                            AppendLog($"  -> Đã gửi lệnh in: {Path.GetFileName(pdfPath)}");
                            printSuccessCount++;
                            await Task.Delay(1000); // Delay
                        }
                        catch (Exception exPrint)
                        {
                            AppendLog($"  -> Lỗi gửi lệnh in cho {Path.GetFileName(pdfPath)}: {exPrint.ToString()}");
                            failedInvoicesInfo.Add($"{Path.GetFileNameWithoutExtension(pdfPath)} (Lỗi gửi lệnh in)");
                        }
                        downloadProgressBar.PerformStep();
                    }
                    MessageBox.Show($"Đã gửi {printSuccessCount}/{finalPdfPaths.Count} hóa đơn đến hộp thoại in.\n" +
                                    $"Vui lòng kiểm tra các hộp thoại in xuất hiện.\n\n" +
                                    (failedInvoicesInfo.Count > 0 ? $"Lưu ý: Có {failedInvoicesInfo.Count} hóa đơn gặp lỗi." : ""),
                                    "Hoàn tất gửi lệnh in", MessageBoxButtons.OK,
                                    failedInvoicesInfo.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }
                else
                {
                    // Không có máy in -> Lưu tất cả PDF
                    AppendLog("🚫 Không có máy in. Lưu PDF...");

                    // === BỔ SUNG THÔNG BÁO CHO NGƯỜI DÙNG ===
                    MessageBox.Show("Không tìm thấy máy in nào được cài đặt.\nChương trình sẽ chuyển sang chế độ Lưu file PDF về máy tính.",
                                    "Không tìm thấy máy in",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                    // === KẾT THÚC BỔ SUNG ===

                    lblDownloadStatus.Text = "Chọn thư mục để lưu PDF...";

                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.Description = $"Không tìm thấy máy in. Chọn thư mục để lưu {finalPdfPaths.Count} file PDF";
                        fbd.UseDescriptionForTitle = true;
                        if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                        {
                            AppendLog($"📁 Thư mục lưu đã chọn: {fbd.SelectedPath}");
                            lblDownloadStatus.Text = $"Đang lưu {finalPdfPaths.Count} file PDF...";
                            int saveSuccessCount = 0;
                            foreach (string pdfPath in finalPdfPaths)
                            {
                                lblDownloadStatus.Text = $"Đang lưu file {Path.GetFileName(pdfPath)}...";
                                try
                                {
                                    if (!File.Exists(pdfPath))
                                    {
                                        AppendLog($"  -> Lỗi: File PDF không tồn tại để lưu: {pdfPath}");
                                        failedInvoicesInfo.Add($"{Path.GetFileNameWithoutExtension(pdfPath)} (File PDF tạm không tồn tại)");
                                        downloadProgressBar.PerformStep();
                                        continue;
                                    }
                                    string originalFileName = Path.GetFileName(pdfPath);
                                    string destFileName = Path.Combine(fbd.SelectedPath, originalFileName);
                                    int counter = 1;
                                    while (File.Exists(destFileName))
                                    {
                                        string tempFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_{counter}{Path.GetExtension(originalFileName)}";
                                        destFileName = Path.Combine(fbd.SelectedPath, tempFileName);
                                        counter++;
                                    }
                                    File.Copy(pdfPath, destFileName);
                                    AppendLog($"  -> Đã lưu: {Path.GetFileName(destFileName)}");
                                    saveSuccessCount++;
                                }
                                catch (Exception exSave)
                                {
                                    AppendLog($"  -> Lỗi lưu file {Path.GetFileName(pdfPath)}: {exSave.ToString()}");
                                    failedInvoicesInfo.Add($"{Path.GetFileNameWithoutExtension(pdfPath)} (Lỗi lưu file)");
                                }
                                downloadProgressBar.PerformStep();
                            }
                            MessageBox.Show($"Đã lưu thành công {saveSuccessCount}/{finalPdfPaths.Count} hóa đơn thành file PDF vào thư mục:\n{fbd.SelectedPath}\n\n" +
                                            (failedInvoicesInfo.Count > 0 ? $"Lưu ý: Có {failedInvoicesInfo.Count} hóa đơn gặp lỗi." : ""),
                                           "Lưu PDF thành công", MessageBoxButtons.OK,
                                           failedInvoicesInfo.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                        }
                        else
                        {
                            AppendLog("⚠️ Người dùng đã hủy chọn thư mục lưu.");
                            MessageBox.Show("Đã hủy thao tác lưu PDF.", "Đã hủy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            downloadProgressBar.Value = downloadProgressBar.Maximum;
                        }
                    }
                }

                if (failedInvoicesInfo.Any())
                {
                    AppendLog($"🔴 Có lỗi xảy ra với {failedInvoicesInfo.Count} hóa đơn trong toàn bộ quá trình.");
                }

            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi nghiêm trọng trong btnCnKoPdf_Click: {ex.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn trong quá trình xử lý: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                downloadProgressBar.Value = downloadProgressBar.Maximum;
            }
            finally
            {
                // --- KHÔI PHỤC UI ---
                btnCnKoPdf.Enabled = true;
                btnCnKoPdf.Text = "In HĐ";
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
                this.Cursor = Cursors.Default;

                // --- DỌN DẸP THƯ MỤC TẠM CHÍNH ---
                try
                {
                    if (Directory.Exists(baseTempDirectory))
                    {
                        Directory.Delete(baseTempDirectory, true);
                        AppendLog($"🧹 Đã dọn dẹp thư mục tạm chính: {baseTempDirectory}");
                    }
                }
                catch (Exception exClean)
                {
                    AppendLog($"⚠️ Không thể dọn dẹp thư mục tạm chính: {exClean.Message}");
                    MessageBox.Show($"Không thể tự động xóa thư mục tạm:\n{baseTempDirectory}\n\nVui lòng xóa thủ công.\nLỗi: {exClean.Message}",
                                    "Lỗi dọn dẹp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void label1_Click(object sender, EventArgs e)
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