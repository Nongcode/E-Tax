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
    public partial class Form1 : Form, IDisposable
    {
        private readonly CookieContainer cookieContainer = new CookieContainer();
        private readonly HttpClient client;
        private string jwtToken = "";
        private string captchaKey = "";
        private List<SearchResult> _latestResults = new List<SearchResult>();
        private readonly string BrowserUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36";
        private bool disposed = false;

        public Form1()
        {
            InitializeComponent();
            ExcelPackage.License.SetNonCommercialPersonal("Your Name");
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
            // Kiểm tra xem có cần "nhờ" luồng giao diện viết hộ không
            if (txtResult.InvokeRequired)
            {
                // Nếu có, chúng ta gửi yêu cầu (Invoke) đến luồng giao diện
                // để nó thực hiện hành động viết chữ.
                txtResult.Invoke(new Action(() =>
                {
                    txtResult.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                }));
            }
            else
            {
                // Nếu không cần, nghĩa là chúng ta đang ở trên luồng giao diện rồi,
                // nên có thể tự viết trực tiếp.
                txtResult.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            CheckForLicense();

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

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                MessageBox.Show("Bạn chưa đăng nhập hoặc token không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DateTime fromDate = dtpFromDate.Value.Date;
            DateTime toDate = dtpToDate.Value.Date;

            // === BƯỚC KIỂM TRA MỚI ===
            // Kiểm tra xem "Đến ngày" có vượt quá "Từ ngày" + 1 tháng không.
            if (toDate > fromDate.AddMonths(1))
            {
                MessageBox.Show("Khoảng thời gian tìm kiếm không được lớn hơn 1 tháng. Vui lòng chọn lại.", "Giới hạn tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Dừng lại không thực hiện tìm kiếm
            }
            // ==========================

            if (fromDate > toDate)
            {
                MessageBox.Show("Ngày bắt đầu không thể lớn hơn ngày kết thúc.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtResult.Clear();
            btnSearch.Enabled = false;
            btnSaveOriginal.Enabled = false;
            searchProgressBar.Visible = true;

            try
            {
                AppendLog("🔍 Bắt đầu truy vấn dữ liệu hoá đơn...");
                // Điều chỉnh lại toDate để bao gồm cả ngày cuối cùng
                DateTime preciseToDate = toDate.AddDays(1).AddTicks(-1);
                string query = Timef(fromDate, preciseToDate);
                AppendLog($"➡️ URL Query được tạo: {query}");

                string result = await GetProductsAsync(query);
                AppendLog($"⬅️ Dữ liệu thô nhận từ server:\n{result}");

                if (result.StartsWith("❌"))
                {
                    // Trích xuất thông báo lỗi từ JSON nếu có
                    try
                    {
                        using var doc = JsonDocument.Parse(result.Substring(result.IndexOf('{')));
                        if (doc.RootElement.TryGetProperty("message", out var messageElement))
                        {
                            MessageBox.Show(messageElement.GetString(), "Lỗi từ API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch
                    {
                        MessageBox.Show(result, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var searchResponse = JsonSerializer.Deserialize<SearchResponse>(result, options);

                if (searchResponse?.Datas == null || !searchResponse.Datas.Any())
                {
                    AppendLog("⚠️ Không tìm thấy hóa đơn nào trong khoảng thời gian đã chọn.");
                    MessageBox.Show("Không tìm thấy hóa đơn nào trong khoảng thời gian đã chọn.", "Thông báo");
                    _latestResults.Clear();
                    return;
                }

                _latestResults = searchResponse.Datas;
                AppendLog($"✅ Đã lấy {_latestResults.Count} kết quả.");
                MessageBox.Show($"Tìm thấy {_latestResults.Count} hóa đơn.", "Hoàn tất");
            }
            catch (Exception ex)
            {
                AppendLog($"🐞 LỖI NGHIÊM TRỌNG: {ex.ToString()}");
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi");
            }
            finally
            {
                btnSearch.Enabled = true;
                btnSaveOriginal.Enabled = true;
                searchProgressBar.Visible = false;
            }
        }

        private async Task<string> GetProductsAsync(string queryString)
        {
            try
            {
                string baseUrl = "query/invoices/sold";
                string fullUrl = string.IsNullOrWhiteSpace(queryString) ? baseUrl : $"{baseUrl}?{queryString}";
                AppendLog($"👉 Đang gọi API: {fullUrl}");

                using var req = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                req.Headers.Add("Accept", "application/json");
                req.Headers.Add("Referer", "https://hoadondientu.gdt.gov.vn/");
                req.Headers.Add("Origin", "https://hoadondientu.gdt.gov.vn");

                if (!string.IsNullOrEmpty(jwtToken))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                    AppendLog("🔐 Đã thêm JWT token vào header Authorization");
                }

                var response = await client.SendAsync(req);
                string text = await response.Content.ReadAsStringAsync();

                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                AppendLog("Response:\r\n" + (text.Length > 2000 ? text.Substring(0, 2000) + "..." : text));

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    AppendLog("⚠️ Token hết hạn, vui lòng đăng nhập lại.");
                    await LoadCaptchaAsync();
                    return "❌ Vui lòng đăng nhập lại.";
                }

                response.EnsureSuccessStatusCode();
                return text;
            }
            catch (Exception ex)
            {
                return $"❌ Lỗi khi gọi API sản phẩm: {ex.Message}";
            }
        }

        private string Timef(DateTime from, DateTime to)
        {
            return $"sort=tdlap:desc,khmshdon:asc,shdon:desc" +
                   $"&search=tdlap=ge={from:dd/MM/yyyyTHH:mm:ss};tdlap=le={to:dd/MM/yyyyTHH:mm:ss}";
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

        private async Task ExportInvoiceDetailsToExcelAsync(string filePath)
        {
            if (_latestResults == null || !_latestResults.Any())
            {
                AppendLog("⚠️ Không có dữ liệu chi tiết để xuất Excel.");
                return;
            }

            try
            {
                AppendLog($"👉 Bắt đầu lấy chi tiết {_latestResults.Count} hóa đơn cho file Excel...");
                var detailsList = new List<Dictionary<string, string>>();

                // --- PHẦN CODE ĐƯỢC BỔ SUNG: LẤY DỮ LIỆU CHI TIẾT ---
                foreach (var invoice in _latestResults)
                {
                    if (string.IsNullOrEmpty(invoice?.Ma_so_thue) || invoice.So_hoa_don == null || invoice.Ky_hieu_ma_so == null)
                    {
                        AppendLog($"⚠️ Bỏ qua invoice thiếu thông tin (mst={invoice?.Ma_so_thue})");
                        continue;
                    }

                    try
                    {
                        string url = $"query/invoices/detail?nbmst={Uri.EscapeDataString(invoice.Ma_so_thue)}" +
                                     $"&khhdon={Uri.EscapeDataString(invoice.Ky_hieu_hoa_don)}" +
                                     $"&shdon={invoice.So_hoa_don}&khmshdon={invoice.Ky_hieu_ma_so}";

                        using var req = new HttpRequestMessage(HttpMethod.Get, url);
                        req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                        req.Headers.Add("Accept", "application/json");
                        if (!string.IsNullOrEmpty(jwtToken))
                        {
                            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                        }

                        var response = await client.SendAsync(req);
                        if (!response.IsSuccessStatusCode)
                        {
                            AppendLog($"⚠️ Chi tiết HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don} trả lỗi {(int)response.StatusCode}");
                            continue;
                        }

                        string json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        JsonElement dataEl = doc.RootElement;
                        if (doc.RootElement.TryGetProperty("data", out var tmp)) dataEl = tmp;

                        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        if (dataEl.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in dataEl.EnumerateObject())
                            {
                                map[prop.Name] = prop.Value.ToString();
                            }
                        }

                        if (map.Any()) detailsList.Add(map);
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"❌ Lỗi lấy chi tiết hóa đơn {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}: {ex.Message}");
                    }
                    await Task.Delay(200);
                }
                // ----------------------------------------------------

                if (!detailsList.Any()) return;

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("ChiTietHoaDon");
                    var allKeys = detailsList.SelectMany(d => d.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    // --- PHẦN CODE ĐƯỢC BỔ SUNG: ĐIỀN DỮ LIỆU VÀO EXCEL ---
                    // Tiêu đề
                    for (int i = 0; i < allKeys.Count; i++)
                    {
                        ws.Cells[1, i + 1].Value = allKeys[i].ToUpper();
                        ws.Cells[1, i + 1].Style.Font.Bold = true;
                    }

                    // Dữ liệu
                    for (int r = 0; r < detailsList.Count; r++)
                    {
                        var detail = detailsList[r];
                        for (int c = 0; c < allKeys.Count; c++)
                        {
                            detail.TryGetValue(allKeys[c], out var value);
                            ws.Cells[r + 2, c + 1].Value = value;
                        }
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    // -----------------------------------------------------

                    await package.SaveAsAsync(new FileInfo(filePath));
                }
                AppendLog($"✅ Đã tạo file Excel chi tiết tại: {filePath}");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi khi tạo file Excel chi tiết: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> DownloadExportExcelAsync(DateTime fromDate, DateTime toDate, string savePath)
        {
            try
            {
                string query = Timef(fromDate, toDate); // Sử dụng hàm Timef hiện có
                string url = $"query/invoices/export-excel?{query}";
                AppendLog($"📦 Đang gọi API export Excel: {url}");

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                req.Headers.Add("Accept", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, application/zip, application/json");
                req.Headers.Add("Referer", "https://hoadondientu.gdt.gov.vn/");
                req.Headers.Add("Origin", "https://hoadondientu.gdt.gov.vn");

                if (!string.IsNullOrEmpty(jwtToken))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                }

                var response = await client.SendAsync(req);

                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                AppendLog($"Content-Type: {response.Content.Headers.ContentType?.MediaType ?? "unknown"}");

                if (!response.IsSuccessStatusCode)
                {
                    string errorText = await response.Content.ReadAsStringAsync();
                    AppendLog($"❌ Lỗi export: {response.StatusCode} - {errorText}");
                    return false;
                }

                // Lưu file dựa trên Content-Type
                string contentType = response.Content.Headers.ContentType?.MediaType?.ToLower() ?? "";
                string extension = contentType.Contains("zip") ? ".zip" : (contentType.Contains("spreadsheetml.sheet") ? ".xlsx" : ".bin");
                string fileName = $"DanhSachHoaDon_{fromDate:ddMMyyyy}_{toDate:ddMMyyyy}{extension}";
                string fullPath = Path.Combine(savePath, fileName);

                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(fullPath, bytes);

                AppendLog($"✅ Đã lưu file export: {fileName} (kích thước: {bytes.Length} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"⚠️ Lỗi tải export Excel: {ex.Message}");
                return false;
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

        private async Task SaveOriginalInvoicesAsync()
        {
            if (_latestResults == null || !_latestResults.Any())
            {
                MessageBox.Show("Không có hóa đơn nào để lưu!", "Thông báo");
                return;
            }

            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục để lưu và giải nén dữ liệu hóa đơn"
            };
            if (fbd.ShowDialog() != DialogResult.OK) return;

            // Vô hiệu hóa các nút và hiển thị progress bar
            btnSearch.Enabled = false;
            btnSaveOriginal.Enabled = false;
            downloadProgressBar.Visible = true;
            lblDownloadStatus.Visible = true;
            downloadProgressBar.Maximum = _latestResults.Count + 4; // +4 cho các bước
            downloadProgressBar.Value = 0;

            string tempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-Export_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDirectory);

            try
            {
                // --- GIAI ĐOẠN 1: TỔNG HỢP DỮ LIỆU (Giữ nguyên) ---
                lblDownloadStatus.Text = "Bước 1: Đang tạo file Excel danh sách...";
                await ExportSearchResultsToExcelAsync(_latestResults, Path.Combine(tempDirectory, "DanhSachHoaDon.xlsx"));
                downloadProgressBar.PerformStep();

                lblDownloadStatus.Text = "Bước 2: Đang tạo file Excel chi tiết...";
                await ExportInvoiceDetailsToExcelAsync(Path.Combine(tempDirectory, "ChiTietHoaDon.xlsx"));
                downloadProgressBar.PerformStep();

                int successCount = 0;
                for (int i = 0; i < _latestResults.Count; i++)
                {
                    lblDownloadStatus.Text = $"Bước 3: Đang tải hóa đơn gốc ({i + 1}/{_latestResults.Count})...";
                    if (await DownloadSingleInvoiceZipAsync(_latestResults[i], tempDirectory))
                    {
                        successCount++;
                    }
                    downloadProgressBar.PerformStep();
                    await Task.Delay(100);
                }

                // --- GIAI ĐOẠN 2: NÉN VÀ GIẢI NÉN (Cập nhật) ---
                lblDownloadStatus.Text = "Bước 4: Đang tổng hợp dữ liệu...";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"Temp_HoaDon_TongHop_{Guid.NewGuid()}.zip");
                ZipFile.CreateFromDirectory(tempDirectory, tempZipPath);

                string finalExtractionPath = Path.Combine(fbd.SelectedPath, Path.GetFileNameWithoutExtension(tempZipPath).Replace("Temp_", ""));
                Directory.CreateDirectory(finalExtractionPath);

                lblDownloadStatus.Text = "Bước 5: Đang giải nén file tổng hợp...";
                ZipFile.ExtractToDirectory(tempZipPath, finalExtractionPath, true);
                downloadProgressBar.PerformStep();

                // === BƯỚC MỚI: GIẢI NÉN CÁC FILE ZIP CON ===
                await Task.Run(() => UnzipInnerArchives(finalExtractionPath));
                downloadProgressBar.PerformStep();
                // ==========================================

                MessageBox.Show($"✅ Hoàn tất! \n\nĐã lưu và giải nén thành công {successCount} hóa đơn và 2 file báo cáo vào thư mục:\n\n{finalExtractionPath}",
                                "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Dọn dẹp
                try
                {
                    if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
                    foreach (var tempZip in Directory.GetFiles(Path.GetTempPath(), "Temp_HoaDon_TongHop_*.zip"))
                    {
                        File.Delete(tempZip);
                    }
                }
                catch { /* Bỏ qua lỗi dọn dẹp */ }

                // Bật lại các nút và ẩn progress bar
                btnSearch.Enabled = true;
                btnSaveOriginal.Enabled = true;
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
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

        private async void btnExportList_Click(object sender, EventArgs e)
        {
            if (_latestResults == null || _latestResults.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất danh sách!", "Thông báo");
                return;
            }

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Cursor current = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                // Tính khoảng thời gian từ dữ liệu tìm kiếm
                DateTime fromDate = _latestResults.Min(r => DateTime.Parse(r.Ngay_lap ?? "2025-01-01"));
                DateTime toDate = _latestResults.Max(r => DateTime.Parse(r.Ngay_lap ?? "2025-12-31"));

                // Gọi API export
                bool success = await DownloadExportExcelAsync(fromDate, toDate, desktop);
                if (success)
                {
                    MessageBox.Show($"✅ Đã xuất danh sách hóa đơn thành công!\nVị trí: {desktop}", "Hoàn tất");
                }
                else
                {
                    MessageBox.Show("❌ Không thể xuất danh sách. Kiểm tra log.", "Lỗi");
                }
            }
            finally
            {
                Cursor.Current = current;
            }
        }

        private async void btnExportDetails_Click(object sender, EventArgs e)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktop, $"ChiTietHoaDon_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            await ExportInvoiceDetailsToExcelAsync(filePath);
        }

        private async void btnSaveOriginal_Click(object sender, EventArgs e)
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
                    int daysLeft = LicenseManager.GetDaysRemaining();
                    this.Text = $"E-Tax (Bản dùng thử - Còn lại {daysLeft} ngày)";
                    panelActivation.Visible = false;
                    panelLogin.Enabled = true;
                    break;

                case LicenseStatus.Expired:
                    this.Text = "E-Tax (Bản dùng thử đã hết hạn)";
                    panelActivation.Visible = true;
                    panelLogin.Enabled = false;
                    MessageBox.Show("Thời gian dùng thử của bạn đã kết thúc. Vui lòng kích hoạt sản phẩm để tiếp tục sử dụng.", "Hết hạn dùng thử", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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


    }
}