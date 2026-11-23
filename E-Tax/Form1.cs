using System.Diagnostics;
using System.Globalization;
using System.Drawing.Printing;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PuppeteerSharp;
using Svg;

namespace E_Tax
{
    public partial class Form1 : Form
    {
        // Giới hạn của API tìm kiếm: tối đa 30 ngày mỗi lần và 50 hóa đơn mỗi trang
        private const int SearchChunkDays = 30;
        private const int SearchPageSize = 50;

        private readonly AppSettings _settings = AppSettings.Load(AppSettings.DefaultPath);
        private readonly CookieContainer cookieContainer = new CookieContainer();
        private readonly HttpClient client;
        /// <summary>HttpClient riêng cho kiểm tra rủi ro NCC (timeout 30 giây), hủy cùng form.</summary>
        private readonly HttpClient _riskHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        /// <summary>Null khi chưa cấu hình token.</summary>
        private readonly RiskCheckService? _riskService;
        /// <summary>Kết quả kiểm tra rủi ro theo MST người bán của lần kiểm tra hàng loạt; xóa khi tìm kiếm mới. Luôn thay cả đối tượng, không sửa tại chỗ.</summary>
        private Dictionary<string, RiskResult> _riskResults = new Dictionary<string, RiskResult>();
        private readonly InvoiceDetailService _detailService;
        private readonly DetailGridManager _detailGridManager;
        private readonly UserPreferences _prefs = new UserPreferences(UserPreferences.DefaultPath);
        private readonly NotesStore _notes = new NotesStore(NotesStore.DefaultPath, Path.Combine(Application.StartupPath, "notes.json"));

        private readonly LocalInvoiceStore _store = new LocalInvoiceStore(LocalInvoiceStore.DefaultRoot);
        private readonly DownloadJobStore _jobStore = new DownloadJobStore(DownloadJobStore.DefaultDirectory);
        /// <summary>MST doanh nghiệp = tên đăng nhập của phiên hiện tại nếu là MST hợp lệ; null thì không đối chiếu bên mua/bán và không ghi kho.</summary>
        private string? _currentTaxCode;
        /// <summary>Các tác vụ ghi kho đang chạy nền (chỉ truy cập từ luồng UI); kho chỉ được mở sau khi chúng xong.</summary>
        private readonly List<Task> _storeTasks = new List<Task>();
        /// <summary>Đang gắn file vào kho: không cho người dùng đóng form.</summary>
        private bool _attaching;
        /// <summary>Cổng bận chung: mỗi lúc chỉ một luồng dài chạy; nút Kho hóa đơn bật/tắt theo cổng này.</summary>
        private readonly BusyGate _gate = new BusyGate();
        /// <summary>Liên kết HĐ thay thế/điều chỉnh với HĐ gốc của lần tìm kiếm gần nhất (theo Id HĐ).</summary>
        private IReadOnlyDictionary<string, InvoiceLink> _invoiceLinks = new Dictionary<string, InvoiceLink>();

        private string jwtToken = "";
        private string captchaKey = "";
        private List<SearchResult> _latestResults = new List<SearchResult>();
        /// <summary>Danh sách đang hiện trên dgvMain (đã lọc); là nguồn cho nút xuất danh sách.</summary>
        private List<SearchResult> _visibleResults = new List<SearchResult>();
        private string? _chromiumExecutablePath;
        /// <summary>Kỳ thuế của lần tìm kiếm gần nhất (chụp lúc bấm Tìm kiếm); bảng kê xuất theo kỳ này, không theo ô chọn hiện tại.</summary>
        private TaxPeriod _lastSearchPeriod;
        /// <summary>Số trang tìm kiếm bị lỗi của lần tìm kiếm gần nhất.</summary>
        private int _lastFailedSearchPages;

        public Form1()
        {
            InitializeComponent();
            FormClosing += Form1_FormClosing;
            _gate.Changed += () => { if (!IsDisposed) btnKhoHoaDon.Enabled = !_gate.IsBusy; };
            InitPeriodSelector();
            ConfigureGiamThueGridColumns();
            dgvVatNop.DataError += DataGridView_DataError;
            dgvMain.DataError += DataGridView_DataError;
            dgvMain.CellFormatting += DgvMain_CellFormatting;
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

            _detailService = new InvoiceDetailService(client, () => jwtToken, AppendLog);
            _detailGridManager = new DetailGridManager(
                _detailService,
                dgvDetails,
                dgvMua,
                dgvBan,
                dgvVatNop,
                downloadProgressBar,
                lblDownloadStatus,
                AppendLog);

            _riskService = string.IsNullOrEmpty(_settings.BlacklistApiToken)
                ? null
                : new RiskCheckService(_riskHttp, _settings.BlacklistApiToken, AppendLog);

            panelLogin.Visible = true;
            panelSearch.Visible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                client?.Dispose();
                _riskHttp.Dispose();
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
            _prefs.Load();
            if (!string.IsNullOrEmpty(_prefs.LastUsername)) txtUser.Text = _prefs.LastUsername;
            dgvMain.AutoGenerateColumns = false;
            _ = CheckForUpdateAsync();
            await LoadCaptchaAsync();
        }

        #region Đăng nhập & bản quyền

        /// <summary>Kiểm tra bản cập nhật ở nền; mọi lỗi bị bỏ qua. ManifestUrl rỗng thì không gọi mạng.</summary>
        private async Task CheckForUpdateAsync()
        {
            try
            {
                var current = typeof(Form1).Assembly.GetName().Version ?? new Version(1, 0, 0);
                var info = await UpdateChecker.CheckAsync(_riskHttp, UpdateChecker.ManifestUrl, current, CancellationToken.None);
                if (info == null || IsDisposed) return;

                AppendLog($"ℹ️ Có phiên bản mới {info.Version}.");
                string notes = string.IsNullOrWhiteSpace(info.Notes) ? "" : Environment.NewLine + info.Notes;
                MessageBox.Show(this,
                    $"Có phiên bản mới {info.Version} (đang dùng {current.ToString(3)}).{notes}{Environment.NewLine}{Environment.NewLine}Tải về tại: {info.DownloadUrl}",
                    "Cập nhật E-Tax", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                // Kiểm tra cập nhật không được ảnh hưởng ứng dụng
            }
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

        private async void btnLogin_Click(object sender, EventArgs e) => await RunFlowAsync("Đăng nhập", () => btnLogin_RunAsync(sender, e));

        private async Task btnLogin_RunAsync(object sender, EventArgs e)
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
                    SaveLastUsername(username);
                    // Thay MST của phiên trước; chỉ giữ tên đăng nhập (không bao giờ mật khẩu) và chỉ khi là MST hợp lệ
                    _currentTaxCode = InvoiceGridLogic.CurrentTaxCode(username);
                    MessageBox.Show("✅ Đăng nhập thành công!");

                    panelLogin.Visible = false;
                    panelSearch.Visible = true;
                    if (_currentTaxCode == null)
                    {
                        AppendLog("ℹ️ Tên đăng nhập không phải MST hợp lệ: không đối chiếu bên mua/bán và không ghi kho hóa đơn.");
                        ShowStatusMessage("Tên đăng nhập không phải MST: sẽ không lưu vào kho hóa đơn.", Color.OrangeRed, 8000);
                    }

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

        /// <summary>Chỉ lưu tên đăng nhập, không bao giờ lưu mật khẩu/captcha/JWT. Lỗi ghi không làm hỏng đăng nhập.</summary>
        private void SaveLastUsername(string username)
        {
            try
            {
                _prefs.LastUsername = username;
                _prefs.Save();
            }
            catch (Exception ex)
            {
                AppendLog("⚠️ Không lưu được tên đăng nhập: " + ex.GetType().Name);
            }
        }

        /// <summary>Xóa phiên cũ để đăng nhập lại thất bại không dùng lại token/cookie cũ.</summary>
        private void ClearStoredSession()
        {
            jwtToken = "";
            client.DefaultRequestHeaders.Authorization = null;
            GdtApi.ExpireSessionCookies(cookieContainer, new Uri(GdtApi.PortalUrl));
        }

        private async Task<bool> LoginAsync(string username, string password, string captchaValue)
        {
            ClearStoredSession();
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
                // Có thể đăng nhập lại bằng MST khác: bỏ dữ liệu của phiên trước
                ClearSearchData();
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
                    SetLoginInputsEnabled(true);
                    break;

                case LicenseStatus.ValidTrial:
                    this.Text = "E-Tax (Bản dùng thử)";
                    panelActivation.Visible = false;
                    SetLoginInputsEnabled(true);
                    break;

                case LicenseStatus.Expired:
                    this.Text = "E-Tax (Bản dùng thử đã hết hạn)";
                    txtMachineId.Text = MachineId.Get();
                    panelActivation.Visible = true;
                    SetLoginInputsEnabled(false);
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

            switch (LicenseManager.Activate(key))
            {
                case LicenseKeyResult.Valid:
                    MessageBox.Show("✅ Kích hoạt thành công! Cảm ơn bạn đã sử dụng sản phẩm.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CheckForLicense();
                    break;
                case LicenseKeyResult.WrongMachine:
                    MessageBox.Show("❌ Mã kích hoạt không dành cho máy này.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case LicenseKeyResult.Expired:
                    MessageBox.Show("❌ Mã kích hoạt đã hết hạn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                default:
                    MessageBox.Show("❌ Mã kích hoạt không hợp lệ. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void btnCopyMachineId_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(txtMachineId.Text);
                MessageBox.Show("Đã sao chép mã máy. Gửi mã này cho chủ sở hữu để nhận mã kích hoạt.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không sao chép được mã máy: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Bật/tắt các ô đăng nhập. Không tắt cả panelLogin vì panelActivation nằm bên trong,
        /// tắt panel cha sẽ làm khóa luôn ô nhập mã kích hoạt.
        /// </summary>
        private void SetLoginInputsEnabled(bool enabled)
        {
            panelLogin.Enabled = true;
            foreach (Control c in panelLogin.Controls)
            {
                if (c != panelActivation)
                    c.Enabled = enabled;
            }
        }

        #endregion

        #region Kỳ thuế

        private void InitPeriodSelector()
        {
            nudPeriodYear.Value = DateTime.Today.Year;
            cbPeriodKind.SelectedIndex = 0;
        }

        private void cbPeriodKind_SelectedIndexChanged(object sender, EventArgs e)
        {
            int kind = cbPeriodKind.SelectedIndex;
            int count = kind == 1 ? 12 : kind == 2 ? 4 : 0;
            cbPeriodNumber.Items.Clear();
            for (int i = 1; i <= count; i++) cbPeriodNumber.Items.Add(i);
            if (count > 0) cbPeriodNumber.SelectedIndex = 0;

            cbPeriodNumber.Enabled = count > 0;
            nudPeriodYear.Enabled = kind > 0;
            ApplyPeriod();
        }

        private void PeriodValue_Changed(object sender, EventArgs e) => ApplyPeriod();

        /// <summary>
        /// Kỳ thuế của lần tìm kiếm: lấy từ bộ chọn khi khoảng ngày còn khớp, ngược lại (Tùy chọn,
        /// hoặc người dùng sửa ngày sau khi chọn kỳ) dùng khoảng ngày thực tế đã tìm.
        /// </summary>
        private TaxPeriod CaptureSelectedPeriod(DateTime from, DateTime to)
        {
            int kind = cbPeriodKind.SelectedIndex;
            if (kind > 0)
            {
                int year = (int)nudPeriodYear.Value;
                TaxPeriod? selected = null;
                if (kind == 3) selected = TaxPeriod.Year(year);
                else if (cbPeriodNumber.SelectedItem is int number)
                    selected = kind == 1 ? TaxPeriod.Month(year, number) : TaxPeriod.Quarter(year, number);
                if (selected is { } p && p.From.Date == from.Date && p.To.Date == to.Date) return p;
            }
            return TaxPeriod.Custom(from, to);
        }

        /// <summary>Gán khoảng ngày theo kỳ đã chọn. "Tùy chọn" giữ nguyên ô ngày người dùng nhập.</summary>
        private void ApplyPeriod()
        {
            int kind = cbPeriodKind.SelectedIndex;
            if (kind <= 0) return;
            int year = (int)nudPeriodYear.Value;
            TaxPeriod period;
            if (kind == 3) period = TaxPeriod.Year(year);
            else if (cbPeriodNumber.SelectedItem is not int number) return;
            else period = kind == 1 ? TaxPeriod.Month(year, number) : TaxPeriod.Quarter(year, number);
            dtpFromDate.Value = period.From;
            dtpToDate.Value = period.To;
        }

        #endregion

        #region Gọi API hóa đơn

        /// <summary>
        /// Gọi API danh sách hóa đơn. Trả về JSON, hoặc chuỗi bắt đầu bằng "❌" nếu lỗi.
        /// </summary>
        private async Task<string> GetInvoiceListAsync(string endpoint, string queryString, CancellationToken ct = default)
        {
            try
            {
                using var req = GdtApi.CreateRequest(HttpMethod.Get, $"{endpoint}?{queryString}", "application/json", jwtToken);
                var response = await client.SendAsync(req, ct);
                string text = await response.Content.ReadAsStringAsync(ct);
                AppendLog($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Lỗi khi gọi API: {text}";
                }
                return text;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // Người dùng hủy: không đổi thành chuỗi lỗi "❌"
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
            DateTime fromDate, DateTime toDate, bool includeSold, bool includeBought, Action<string> reportStatus,
            CancellationToken ct = default)
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
                    ct.ThrowIfCancellationRequested();
                    reportStatus($"Đang tìm [{loopStartDate:dd/MM/yy} - {loopEndDate:dd/MM/yy}], Trang {currentPage + 1}...");

                    if (keepFetchingSold)
                        keepFetchingSold = await FetchSearchPageAsync(InvoiceType.Sold, loopStartDate, preciseLoopEndDate, currentPage, allResults, failedChunks, ct);

                    if (keepFetchingBought)
                        keepFetchingBought = await FetchSearchPageAsync(InvoiceType.Bought, loopStartDate, preciseLoopEndDate, currentPage, allResults, failedChunks, ct);

                    if (keepFetchingSold || keepFetchingBought)
                    {
                        currentPage++;
                        await Task.Delay(200, ct);
                    }
                }

                loopStartDate = loopEndDate.AddDays(1);
            }

            var results = InvoiceSearchResults.Deduplicate(allResults);
            return (results, failedChunks);
        }

        /// <summary>
        /// Lấy một trang kết quả. Trả về true nếu có thể còn trang tiếp theo.
        /// </summary>
        private async Task<bool> FetchSearchPageAsync(InvoiceType type, DateTime from, DateTime to, int page,
            List<SearchResult> results, List<string> failedChunks, CancellationToken ct = default)
        {
            string endpoint = type == InvoiceType.Sold ? "query/invoices/sold" : "query/invoices/purchase";
            string typeLabel = type == InvoiceType.Sold ? "Bán ra" : "Mua vào";

            string response = await GetInvoiceListAsync(endpoint, BuildSearchQuery(from, to, type, page, SearchPageSize), ct);
            ct.ThrowIfCancellationRequested(); // trang bị hủy không được tính là trang lỗi
            if (response.StartsWith("❌"))
            {
                failedChunks.Add($"{typeLabel} [{from:dd/MM} - {to:dd/MM}], Trang {page + 1}");
                return false;
            }

            // Phản hồi 200 không đọc được/sai cấu trúc là trang lỗi (không làm mất các lát khác), không phải hết dữ liệu
            var (items, _, failed) = InvoiceSearchResults.ParsePage(response);
            if (failed)
            {
                failedChunks.Add($"{typeLabel} [{from:dd/MM} - {to:dd/MM}], Trang {page + 1} (phản hồi không hợp lệ)");
                return false;
            }
            if (items == null)
                return false;

            items.ForEach(item => item.Loai_hoa_don = typeLabel);
            results.AddRange(items);
            return items.Count >= SearchPageSize;
        }

        /// <summary>
        /// Tải file ZIP gốc (XML + bản thể hiện) của một hóa đơn vào thư mục saveDirectory.
        /// Trả về đường dẫn file ZIP, hoặc null nếu lỗi.
        /// </summary>
        private async Task<string?> DownloadSingleInvoiceZipAsync(SearchResult invoice, string saveDirectory, CancellationToken ct = default, string? fileIdentifier = null)
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
                var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!response.IsSuccessStatusCode)
                {
                    string errorText = await response.Content.ReadAsStringAsync(ct);
                    AppendLog($"❌ Lỗi khi tải HĐ [{invoice.Ky_hieu_hoa_don} - {invoice.So_hoa_don}]: {(int)response.StatusCode} - {errorText}");
                    return null;
                }

                string fullPath = Path.Combine(saveDirectory, $"HD_{fileIdentifier ?? invoice.FileIdentifier}.zip");
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream, ct);
                }

                AppendLog($"✅ Đã lưu thành công file: {Path.GetFileName(fullPath)}");
                return fullPath;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // Hủy: không trả null như lỗi tải
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

            var risks = _riskResults; // chụp lại: dictionary chỉ được thay cả đối tượng nên an toàn khi đọc trong Task.Run
            await Task.Run(() => ExcelExporter.ExportSearchResults(results, filePath, r => _notes.Get(r.Id ?? ""), r => RiskLabelFor(r, risks)));
            AppendLog($"✅ Đã tạo file Excel danh sách tại: {filePath}");
        }

        private const string DetailFetchError =
            "❌ Không lấy được chi tiết hóa đơn từ cổng thuế (phiên đăng nhập có thể đã hết hạn hoặc lỗi kết nối). Xem log để biết thêm.";

        /// <summary>
        /// Gộp dòng hàng của mọi trang chi tiết một hóa đơn (mỗi trang tối đa 50 sản phẩm).
        /// </summary>
        private static List<Dictionary<string, string>> BuildDetailRows(IReadOnlyList<string> pages)
        {
            var rows = new List<Dictionary<string, string>>();
            foreach (string json in pages)
            {
                using var doc = JsonDocument.Parse(json);
                rows.AddRange(InvoiceJson.BuildDetailRows(InvoiceJson.GetDataElement(doc.RootElement)));
            }
            return rows;
        }

        /// <summary>
        /// Lấy chi tiết hàng hóa của nhiều hóa đơn và xuất ra một file Excel tổng hợp.
        /// </summary>
        /// <returns>Ký hiệu-số các hóa đơn không lấy được chi tiết (không có trong file).</returns>
        private async Task<List<string>> ExportInvoiceDetailsToExcelAsync(List<SearchResult> invoices, string filePath, CancellationToken ct = default)
        {
            var failedInvoices = new List<string>();
            if (invoices == null || !invoices.Any())
            {
                AppendLog("⚠️ (Excel Chi Tiết) Không có dữ liệu hóa đơn để lấy chi tiết.");
                return failedInvoices;
            }

            var detailsList = new List<Dictionary<string, string>>();
            int globalStt = 1;

            // Dùng lại chi tiết đã tải (cache), HĐ chưa có thì tải song song
            var allPages = await _detailService.PrefetchAsync(invoices, null, ct);
            // PrefetchAsync trả null khi hủy: không được ghi Excel thiếu
            ct.ThrowIfCancellationRequested();

            for (int i = 0; i < invoices.Count; i++)
            {
                var invoice = invoices[i];
                if (invoice == null || !invoice.HasLookupKeys())
                {
                    AppendLog($"⚠️ (Excel Chi Tiết) Bỏ qua hóa đơn ID {invoice?.Id} vì thiếu thông tin MST/KHHĐ/SHĐ/KHMHS.");
                    continue;
                }

                var pages = allPages[i];
                if (pages == null)
                {
                    AppendLog($"❌ (Excel Chi Tiết) Lỗi khi lấy chi tiết HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don} (xem log phía trên).");
                    failedInvoices.Add($"{invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}");
                    continue;
                }

                try
                {
                    foreach (var row in BuildDetailRows(pages))
                    {
                        row["stt"] = (globalStt++).ToString();
                        detailsList.Add(row);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"❌ (Excel Chi Tiết) Lỗi khi xử lý chi tiết HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}: {ex.Message}");
                    failedInvoices.Add($"{invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don}");
                }
            }

            if (!detailsList.Any())
            {
                AppendLog("⚠️ (Excel Chi Tiết) Không có chi tiết sản phẩm nào từ các hóa đơn hợp lệ để xuất Excel.");
                await Task.Run(() => ExcelExporter.ExportDetailSummary(detailsList, filePath));
                return failedInvoices;
            }

            await Task.Run(() => ExcelExporter.ExportDetailSummary(detailsList, filePath));
            AppendLog($"✅ (Excel Chi Tiết) Đã tạo file Excel chi tiết tổng hợp tại: {filePath}");
            return failedInvoices;
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

            try
            {
                string path = await ChromiumUnavailableException.RunAsync(async () =>
                {
                    var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions());
                    var installedBrowser = await browserFetcher.DownloadAsync();

                    string? found = null;
                    if (installedBrowser != null && !string.IsNullOrEmpty(installedBrowser.BuildId))
                    {
                        found = browserFetcher.GetExecutablePath(installedBrowser.BuildId);
                    }
                    return found;
                }, CancellationToken.None, ex => AppendLog("❌ Tải Chromium thất bại: " + ex.GetType().Name));

                _chromiumExecutablePath = path;
                return path;
            }
            finally
            {
                lblDownloadStatus.Text = previousStatus;
            }
        }

        private static string? FindFirstFile(string directory, string pattern) =>
            Directory.GetFiles(directory, pattern, SearchOption.AllDirectories).FirstOrDefault();

        #endregion

        #region Bảng kê mua vào, bán ra

        /// <summary>
        /// Xóa toàn bộ dữ liệu của lần tìm kiếm trước (kết quả, chi tiết, cache, kỳ, bộ đếm lỗi và các lưới)
        /// để không xuất hay hiển thị số liệu cũ. Dùng khi tìm mới, không có kết quả, hủy và đăng nhập lại.
        /// </summary>
        private void ClearSearchData()
        {
            _detailService.ClearCache();
            _detailGridManager.ClearLatest();
            _latestResults = new List<SearchResult>();
            _visibleResults = new List<SearchResult>();
            _riskResults = new Dictionary<string, RiskResult>();
            _lastSearchPeriod = default;
            _lastFailedSearchPages = 0;
            dgvMain.DataSource = null;
            dgvDetails.DataSource = null;
            dgvMua.DataSource = null;
            dgvBan.DataSource = null;
            dgvVatNop.DataSource = null;
            dgvGiamThue.DataSource = null;
        }

        private void btnExportBangKeMua_Click(object sender, EventArgs e) => ExportTaxListing(InvoiceType.Bought);

        private void btnExportBangKeBan_Click(object sender, EventArgs e) => ExportTaxListing(InvoiceType.Sold);

        /// <summary>
        /// Xuất bảng kê toàn bộ kết quả lần tìm kiếm gần nhất (không theo bộ lọc lưới) theo kỳ đã tìm.
        /// Dừng nếu còn HĐ/trang thiếu dữ liệu để không xuất bảng kê thiếu mà không báo.
        /// </summary>
        private void ExportTaxListing(InvoiceType type)
        {
            string kindText = type == InvoiceType.Bought ? "mua vào" : "bán ra";
            var items = _detailGridManager.LatestItems;
            if (items.Count == 0)
            {
                MessageBox.Show(this, "Chưa có dữ liệu chi tiết hóa đơn. Hãy tìm kiếm trước khi xuất bảng kê.", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string? blockReason = TaxListing.ExportBlockReason(_detailGridManager.LastFailedCount + _lastFailedSearchPages);
            if (blockReason != null)
            {
                MessageBox.Show(this, blockReason, "Không thể xuất bảng kê", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var groups = TaxListing.Build(items, type);
            if (groups.Count == 0)
            {
                MessageBox.Show(this, $"Lần tìm kiếm gần nhất không có hóa đơn {kindText} nào để lập bảng kê.", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Một xác nhận duy nhất cho mọi điểm cần lưu ý; chọn No thì dừng xuất
            var warnings = TaxListing.ExportWarnings(groups, _detailGridManager.LastNoLineCount, TaxListing.CountNonVnd(_latestResults, type));
            if (warnings.Count > 0 && MessageBox.Show(this,
                    $"Bảng kê {kindText} có các điểm cần lưu ý:\n\n - {string.Join("\n - ", warnings)}\n\nVẫn xuất bảng kê?",
                    "Cảnh báo trước khi xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var period = _lastSearchPeriod;
            var invalid = Path.GetInvalidFileNameChars();
            string label = string.Concat(period.Label.Select(c => invalid.Contains(c) ? '-' : c)).Replace(' ', '_');
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BangKe_{(type == InvoiceType.Bought ? "Mua" : "Ban")}_{label}.xlsx",
                Title = $"Lưu bảng kê {kindText}"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                ExcelExporter.ExportTaxListing(groups, type, period, sfd.FileName);
                MessageBox.Show(this, $"Đã xuất bảng kê {kindText} ({period.Label}) vào file:\n{sfd.FileName}", "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi xuất bảng kê: {ex.Message}");
                MessageBox.Show(this, $"Không xuất được bảng kê: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Tìm kiếm & lọc

        private async void btnLeftSearch_Click(object sender, EventArgs e) => await RunFlowAsync("Tìm kiếm", () => btnLeftSearch_RunAsync(sender, e));

        private async Task btnLeftSearch_RunAsync(object sender, EventArgs e)
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

            ClearSearchData();
            _lastSearchPeriod = CaptureSelectedPeriod(globalFromDate, globalToDate);
            btnLeftSearch.Enabled = false;
            btnLeftSearch.Text = "Đang tìm...";
            this.Cursor = Cursors.WaitCursor;
            ResetFilterInputs();

            var modal = ProcessingForm.ShowFor(this);
            var ct = modal.CancellationToken;
            bool showCompleted = false;

            try
            {
                var (results, failedChunks) = await SearchInvoicesAsync(
                    globalFromDate, globalToDate,
                    includeSold: rbSold.Checked || rbAllInvoices.Checked,
                    includeBought: rbBought.Checked || rbAllInvoices.Checked,
                    modal.UpdateStatus, ct);

                ct.ThrowIfCancellationRequested();
                modal.UpdateStatus("Đang tổng hợp kết quả...");
                _latestResults = results;
                _lastFailedSearchPages = failedChunks.Count;

                if (_latestResults.Any())
                {
                    // Chỉ lưu những gì đã lấy được; trang lỗi không xóa gì khỏi kho (Upsert không bao giờ xóa)
                    TrackStoreTask(StoreInvoicesAsync(_latestResults));
                    modal.UpdateStatus($"Đang hiển thị {_latestResults.Count} hóa đơn...");
                    BindMainGrid(_latestResults);

                    dgvGiamThue.DataSource = null;
                    dgvGiamThue.DataSource = _latestResults;

                    modal.UpdateStatus($"Đang tải chi tiết cho {_latestResults.Count} hóa đơn...");
                    int detailFailed = await _detailGridManager.PopulateDetailGridAsync(_latestResults, ct,
                        done => modal.Report(new BatchProgress("Tải chi tiết HĐ", done, _latestResults.Count, 0)));
                    showCompleted = !failedChunks.Any() && detailFailed == 0;
                    if (detailFailed > 0)
                    {
                        MessageBox.Show($"Không lấy được chi tiết hàng hóa của {detailFailed}/{_latestResults.Count} hóa đơn (phiên đăng nhập có thể đã hết hạn hoặc lỗi kết nối).\nCác bảng chi tiết có thể bị thiếu, hãy tìm kiếm lại. Xem log để biết thêm.", "Thiếu dữ liệu chi tiết", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    ClearSearchData();
                    // Có trang lỗi thì chưa thể nói là không có hóa đơn
                    string? loadError = InvoiceSearchResults.LoadFailureMessage(failedChunks);
                    if (loadError != null)
                    {
                        lblDownloadStatus.Text = "Không tải được danh sách hóa đơn.";
                        MessageBox.Show(loadError, "Lỗi tải danh sách", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        lblDownloadStatus.Text = "Không tìm thấy hóa đơn nào.";
                        MessageBox.Show("Không tìm thấy hóa đơn nào phù hợp với điều kiện tìm kiếm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                if (_latestResults.Any() && failedChunks.Any())
                {
                    string errorList = string.Join("\n - ", failedChunks);
                    MessageBox.Show($"Đã hoàn tất tìm kiếm, tuy nhiên đã xảy ra lỗi ở các khoảng thời gian/trang sau:\n - {errorList}\n\nKết quả có thể bị thiếu.", "Lỗi trong quá trình tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Hủy: bỏ mọi kết quả dở dang, không hiện lỗi/"Hoàn tất"
                AppendLog("ℹ️ Người dùng đã hủy tìm kiếm.");
                ClearSearchData();
                showCompleted = false;
                ShowStatusMessage("Đã hủy tìm kiếm.", Color.OrangeRed);
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
                modal.CloseFromOwner();

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
        /// Bind danh sách (đã lọc) vào lưới tổng hợp, điền cột loại HĐ và đánh lại STT.
        /// Ngày lập hiển thị qua CellFormatting, Ngay_lap trong model giữ nguyên giá trị gốc.
        /// </summary>
        private void BindMainGrid(List<SearchResult> items)
        {
            _visibleResults = items;
            _invoiceLinks = InvoiceLinker.Link(_latestResults);
            dgvMain.DataSource = null;
            dgvMain.DataSource = items;
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
                }
            }
            finally
            {
                dgvMain.ResumeLayout();
            }
        }

        private void ResetFilterInputs()
        {
            txtLocSoHD.Clear();
            txtLocNCC.Clear();
            cboLocLoai.SelectedIndex = 0;
            chkChiCoGhiChu.Checked = false;
        }

        private void btnRightSearch_Click(object sender, EventArgs e)
        {
            if (_latestResults == null || !_latestResults.Any())
            {
                MessageBox.Show("Chưa có dữ liệu hóa đơn để lọc. Vui lòng nhấn nút 'Tìm kiếm' trước.", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string? typeLabel = cboLocLoai.SelectedIndex > 0 ? cboLocLoai.SelectedItem?.ToString() : null;
            var criteria = new InvoiceFilterCriteria(txtLocSoHD.Text, txtLocNCC.Text, typeLabel, chkChiCoGhiChu.Checked);
            var filteredResults = InvoiceFilter.Apply(_latestResults, criteria, id => _notes.Get(id));

            if (filteredResults.Any())
            {
                BindMainGrid(filteredResults);
            }
            else
            {
                _visibleResults = new List<SearchResult>();
                dgvMain.DataSource = null;
                MessageBox.Show("Không tìm thấy hóa đơn nào phù hợp với điều kiện lọc.", "Không có kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private async void btnTaiHDGoc_Click(object sender, EventArgs e) => await RunFlowAsync("Tải HĐ gốc", () => btnTaiHDGoc_RunAsync(sender, e));

        private async Task btnTaiHDGoc_RunAsync(object sender, EventArgs e)
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
            this.Cursor = Cursors.WaitCursor;

            // Cửa sổ tiến trình có nút Hủy thay cho thanh tiến trình của form chính
            var modal = new ProcessingForm();
            modal.Show(this);
            modal.Enabled = true;
            var ct = modal.CancellationToken;
            bool modalOpen = true;
            void CloseModal()
            {
                this.Enabled = true; // trả kích hoạt cho form chủ trước khi đóng cửa sổ tiến trình
                if (!modalOpen) return;
                modalOpen = false;
                modal.CloseFromOwner();
            }

            // Mặc định là thư mục tạm riêng của lượt này; có kế hoạch tải rồi thì chuyển sang thư mục làm việc bền vững để tiếp tục được
            string baseTempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-Export_{Guid.NewGuid()}");
            string reportSubFolder = Path.Combine(baseTempDirectory, "0_BaoCaoExcel");
            string zipSubFolder = Path.Combine(baseTempDirectory, "1_HoaDonGoc_ZIP");
            string pdfSubFolder = Path.Combine(baseTempDirectory, "2_HoaDonPDF");
            string xmlSubFolder = Path.Combine(baseTempDirectory, "3_HoaDonXML");
            bool isWorkFolder = false;   // true: baseTempDirectory là thư mục làm việc bền vững
            bool cancelled = false;
            bool outputSaved = false;    // ZIP kết quả đã được lưu vào thư mục người dùng chọn
            int itemsNotDone = 0;        // số HĐ chưa xong trong lượt này (lỗi hoặc chưa xử lý)
            DownloadJobStore? jobStore = null;
            string? jobId = null;
            int sinceFlush = 0;
            string? storeOwner = _currentTaxCode; // chụp lúc bắt đầu: đổi phiên giữa chừng không làm lẫn doanh nghiệp
            // HĐ đã xong trong lượt này (hoặc bỏ qua hợp lệ khi tiếp tục) kèm đường dẫn file kết quả, để gắn vào kho trước khi xóa thư mục làm việc
            var finishedForStore = new List<(SearchResult Invoice, string PdfPath, string XmlPath)>();

            void JobMark(string key, DownloadItemState state, string? error)
            {
                if (jobStore == null || jobId == null) return;
                try { jobStore.Mark(jobId, key, state, error); }
                catch (Exception exJob) { AppendLog($"⚠️ (Tải gốc) Không ghi được tiến độ tải: {exJob.GetType().Name}"); jobStore = null; }
            }
            void JobFlush()
            {
                if (jobStore == null || jobId == null) return;
                try { jobStore.Flush(jobId); }
                catch (Exception exJob) { AppendLog($"⚠️ (Tải gốc) Không lưu được tiến độ tải: {exJob.GetType().Name}"); jobStore = null; }
            }

            try
            {
                // --- Bước 1: Tìm kiếm ---
                modal.UpdateStatus("Bước 1: Đang tìm kiếm hóa đơn...");
                modal.Report(new BatchProgress("Tìm hóa đơn", 0, 0, 0));

                var (invoicesToDownload, failedChunks) = await SearchInvoicesAsync(
                    globalFromDate, globalToDate,
                    includeSold: rbSold.Checked || rbAllInvoices.Checked,
                    includeBought: rbBought.Checked || rbAllInvoices.Checked,
                    status => modal.UpdateStatus("Bước 1: " + status), ct);

                if (!invoicesToDownload.Any())
                {
                    CloseModal();
                    // Có trang lỗi thì chưa thể nói là không có hóa đơn
                    string? loadError = InvoiceSearchResults.LoadFailureMessage(failedChunks);
                    if (loadError != null)
                        MessageBox.Show(this, loadError, "Lỗi tải danh sách", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        MessageBox.Show(this, "Không tìm thấy hóa đơn nào trong khoảng thời gian đã chọn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowStatusMessage($"Tìm thấy {invoicesToDownload.Count} hóa đơn. Bắt đầu tải và xử lý...", Color.Green);
                TrackStoreTask(StoreInvoicesAsync(invoicesToDownload));

                // Excel giữ đủ theo loại; phần tải chỉ lấy mỗi hóa đơn thật một lần
                var downloadPlan = InvoiceDownloadPlan.Build(invoicesToDownload);
                int duplicateCount = invoicesToDownload.Count - downloadPlan.Count;
                if (duplicateCount > 0)
                    AppendLog($"ℹ️ (Tải gốc) {duplicateCount} dòng là cùng hóa đơn ở cả Bán ra và Mua vào, chỉ tải một lần.");
                int totalInvoices = downloadPlan.Count;

                // Thư mục làm việc bền vững theo tập HĐ; lỗi thì dùng thư mục tạm cũ và chạy không tiếp tục được
                var planKeys = downloadPlan.Select(p => p.FileId).ToList();
                var doneKeys = new HashSet<string>(StringComparer.Ordinal);
                // Dọn thư mục/job cũ và tìm job có thể chậm: chạy nền, ghi log sau khi xong (không gọi AppendLog từ luồng nền)
                var workLogs = new List<string>();
                string? workFolder = null;
                DownloadJobStore? foundStore = null;
                string? foundJobId = null;
                await Task.Run(() =>
                {
                    try
                    {
                        DownloadWorkFolder.CleanupStale(DownloadWorkFolder.DefaultRoot, DateTime.UtcNow, TimeSpan.FromDays(30));
                        string wf = DownloadWorkFolder.PathFor(DownloadWorkFolder.DefaultRoot, planKeys);
                        Directory.CreateDirectory(wf);
                        foreach (string leftover in Directory.GetDirectories(wf, "_temp_extract_*"))
                            Directory.Delete(leftover, true); // thư mục giải nén dở của lượt trước không được lọt vào ZIP
                        workFolder = wf;
                    }
                    catch (Exception exWork)
                    {
                        workLogs.Add($"⚠️ (Tải gốc) Không tạo được thư mục làm việc, chạy không tiếp tục được: {exWork.GetType().Name}");
                        return;
                    }

                    try
                    {
                        var store = _jobStore; // dùng chung một instance với màn hình kho để không bị cache cũ
                        store.DeleteOrphans(DownloadWorkFolder.DefaultRoot); // job của thư mục làm việc đã bị xóa
                        string? found = store.FindLatest(workFolder!, planKeys);
                        if (found != null)
                        {
                            store.RetryFailed(found);
                            doneKeys.UnionWith(store.DoneKeys(found));
                            workLogs.Add($"ℹ️ (Tải gốc) Tiếp tục lượt tải trước: {doneKeys.Count}/{planKeys.Count} HĐ đã xong.");
                        }
                        else
                        {
                            found = store.Create(workFolder!, planKeys);
                        }
                        foundStore = store;
                        foundJobId = found;
                    }
                    catch (Exception exJob)
                    {
                        workLogs.Add($"⚠️ (Tải gốc) Không dùng được kho tiến độ tải, chạy không lưu tiến độ: {exJob.GetType().Name}");
                        doneKeys.Clear();
                    }
                });
                foreach (string line in workLogs) AppendLog(line);
                if (workFolder != null)
                {
                    baseTempDirectory = workFolder;
                    reportSubFolder = Path.Combine(baseTempDirectory, "0_BaoCaoExcel");
                    zipSubFolder = Path.Combine(baseTempDirectory, "1_HoaDonGoc_ZIP");
                    pdfSubFolder = Path.Combine(baseTempDirectory, "2_HoaDonPDF");
                    xmlSubFolder = Path.Combine(baseTempDirectory, "3_HoaDonXML");
                    isWorkFolder = true;
                    jobStore = foundStore;
                    jobId = foundJobId;
                }

                Directory.CreateDirectory(reportSubFolder);
                Directory.CreateDirectory(zipSubFolder);
                Directory.CreateDirectory(pdfSubFolder);
                Directory.CreateDirectory(xmlSubFolder);

                // --- Bước 2, 3: Excel danh sách và chi tiết ---
                ct.ThrowIfCancellationRequested();
                modal.UpdateStatus("Bước 2: Đang tạo file Excel danh sách...");
                await ExportSearchResultsToExcelAsync(invoicesToDownload, Path.Combine(reportSubFolder, "0_DanhSachHoaDon.xlsx"));

                modal.UpdateStatus("Bước 3: Đang tạo file Excel chi tiết...");
                var detailFailed = await ExportInvoiceDetailsToExcelAsync(invoicesToDownload, Path.Combine(reportSubFolder, "0_ChiTietHoaDon.xlsx"), ct);
                failedChunks.AddRange(detailFailed.Select(id => $"{id} (Lỗi lấy chi tiết cho Excel)"));

                // --- Bước 4: Tải ZIP, giải nén, lấy PDF (hoặc convert HTML) và XML ---
                await using var pdfConverter = new PdfConverter(EnsureChromiumAsync);
                int successCount = 0;
                int convertSuccessCount = 0;
                var producedPdfs = new List<(InvoiceDownloadItem Item, string PdfPath)>();

                for (int i = 0; i < downloadPlan.Count; i++)
                {
                    var planItem = downloadPlan[i];
                    var invoice = planItem.Invoice;
                    string invoiceIdentifier = planItem.FileId;
                    string outputPdfPath = Path.Combine(pdfSubFolder, $"HD_{invoiceIdentifier}.pdf");
                    bool pdfProduced = false;
                    ct.ThrowIfCancellationRequested();
                    modal.UpdateStatus($"Bước 4: Xử lý HĐ {invoiceIdentifier} ({i + 1}/{totalInvoices})...");
                    modal.Report(new BatchProgress("Xử lý HĐ", i, totalInvoices, failedChunks.Count));
                    string tempExtractPath = Path.Combine(baseTempDirectory, $"_temp_extract_{i}");
                    string itemZipPath = Path.Combine(zipSubFolder, $"HD_{invoiceIdentifier}.zip");
                    string itemXmlPath = Path.Combine(xmlSubFolder, $"HD_{invoiceIdentifier}.xml");

                    // Đã xong ở lượt trước và đủ file (ZIP, PDF không rỗng): dùng lại, vẫn tính vào số đếm và file gộp
                    if (doneKeys.Contains(invoiceIdentifier) &&
                        DownloadWorkFolder.CanSkipInvoice(itemZipPath, outputPdfPath, itemXmlPath))
                    {
                        successCount++;
                        convertSuccessCount++;
                        producedPdfs.Add((planItem, outputPdfPath));
                        finishedForStore.Add((invoice, outputPdfPath, itemXmlPath));
                        continue;
                    }

                    // Tải lại: xóa file cũ của HĐ này để bản dở không lọt vào ZIP kết quả
                    foreach (string stale in new[] { itemZipPath, outputPdfPath, itemXmlPath })
                    {
                        try { if (File.Exists(stale)) File.Delete(stale); }
                        catch (Exception exStale) { AppendLog($"⚠️ Không xóa được file cũ {Path.GetFileName(stale)}: {exStale.Message}"); }
                    }

                    try
                    {
                        string? zipFilePath = await DownloadSingleInvoiceZipAsync(invoice, zipSubFolder, ct, invoiceIdentifier);
                        if (zipFilePath == null)
                        {
                            failedChunks.Add($"{invoiceIdentifier} (Lỗi tải ZIP)");
                            JobMark(invoiceIdentifier, DownloadItemState.Failed, "Lỗi tải ZIP");
                            itemsNotDone++;
                            continue;
                        }
                        successCount++;

                        Directory.CreateDirectory(tempExtractPath);
                        await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath, true));
                        ct.ThrowIfCancellationRequested();

                        string? pdfFile = FindFirstFile(tempExtractPath, "*.pdf");
                        string? htmlFile = FindFirstFile(tempExtractPath, "*.html");
                        string? xmlFile = FindFirstFile(tempExtractPath, "*.xml");

                        if (pdfFile != null)
                        {
                            File.Copy(pdfFile, outputPdfPath, true);
                            pdfProduced = true;
                            producedPdfs.Add((planItem, outputPdfPath));
                            convertSuccessCount++;
                        }
                        else if (htmlFile != null)
                        {
                            modal.UpdateStatus($"Convert HTML... ({i + 1}/{totalInvoices})");
                            await pdfConverter.ConvertAsync(htmlFile, outputPdfPath);
                            pdfProduced = true;
                            producedPdfs.Add((planItem, outputPdfPath));
                            convertSuccessCount++;
                        }
                        else
                        {
                            AppendLog($"⚠️ (Tải gốc) Không tìm thấy PDF/HTML cho HĐ: {invoiceIdentifier}");
                            failedChunks.Add($"{invoiceIdentifier} (Không tìm thấy PDF/HTML)");
                        }

                        if (xmlFile != null)
                        {
                            File.Copy(xmlFile, itemXmlPath, true);
                        }

                        if (pdfProduced)
                        {
                            JobMark(invoiceIdentifier, DownloadItemState.Done, null);
                            finishedForStore.Add((invoice, outputPdfPath, itemXmlPath));
                        }
                        else
                        {
                            JobMark(invoiceIdentifier, DownloadItemState.Failed, "Không tìm thấy PDF/HTML");
                            itemsNotDone++;
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw; // Hủy: không tính là lỗi giải nén/convert của HĐ này
                    }
                    catch (Exception exConvert) when (exConvert is not ChromiumUnavailableException)
                    {
                        AppendLog($"❌ (Tải gốc) Lỗi Giải nén/Convert HĐ {invoiceIdentifier}: {exConvert.Message}");
                        failedChunks.Add($"{invoiceIdentifier} (Lỗi giải nén/convert)");
                        JobMark(invoiceIdentifier, DownloadItemState.Failed, "Lỗi giải nén/convert: " + exConvert.GetType().Name); // không lưu exConvert.Message
                        itemsNotDone++;
                        // PDF ghi dở không được nằm trong file kết quả
                        if (!pdfProduced)
                        {
                            try { if (File.Exists(outputPdfPath)) File.Delete(outputPdfPath); }
                            catch (Exception exDelete) { AppendLog($"⚠️ Không xóa được PDF lỗi {Path.GetFileName(outputPdfPath)}: {exDelete.Message}"); }
                        }
                    }
                    finally
                    {
                        try { if (Directory.Exists(tempExtractPath)) Directory.Delete(tempExtractPath, true); }
                        catch (Exception exCleanTemp) { AppendLog($"⚠️ Lỗi dọn dẹp thư mục tạm {tempExtractPath}: {exCleanTemp.Message}"); }
                        if (++sinceFlush >= 20) { sinceFlush = 0; JobFlush(); }
                    }
                }

                ct.ThrowIfCancellationRequested();
                modal.Report(new BatchProgress("Xử lý HĐ", totalInvoices, totalInvoices, failedChunks.Count));

                // --- Gộp tất cả PDF thành một file (theo ký hiệu rồi số HĐ) ---
                // Xóa bản gộp cũ (và file tạm của PdfMerger) trước; không xóa được thì ném lỗi IO, không đóng gói bản cũ
                DownloadWorkFolder.RemoveMergedOutputs(pdfSubFolder, "_GopTatCa.pdf");
                try
                {
                    // Chỉ gộp PDF thực sự tạo được cho từng HĐ trong lượt này
                    var orderedPdfs = InvoiceDownloadPlan.OrderForMerge(producedPdfs);
                    if (orderedPdfs.Count > 0)
                    {
                        modal.UpdateStatus("Đang gộp PDF...");
                        var mergeResult = await Task.Run(() => PdfMerger.Merge(orderedPdfs, Path.Combine(pdfSubFolder, "_GopTatCa.pdf")));
                        failedChunks.AddRange(mergeResult.FailedFiles.Select(f => $"{Path.GetFileName(f)} (Không gộp được vào _GopTatCa.pdf)"));
                        if (mergeResult.PagesWritten == 0)
                            failedChunks.Add("_GopTatCa.pdf (Không có PDF hợp lệ để gộp)");
                    }
                }
                catch (Exception exMerge)
                {
                    AppendLog($"❌ (Tải gốc) Lỗi gộp PDF: {exMerge.Message}");
                    failedChunks.Add($"_GopTatCa.pdf (Lỗi gộp PDF: {exMerge.Message})");
                }

                // --- Bước 5: Nén kết quả ---
                // Sau khi hủy thì không mở hộp thoại chọn thư mục, không tạo ZIP
                ct.ThrowIfCancellationRequested();
                // Ẩn cửa sổ tiến trình để hộp thoại chọn thư mục không bị che; từ đây không còn hủy được
                CloseModal();

                using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục để lưu file ZIP kết quả" };

                if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
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

                    lblDownloadStatus.Visible = true;
                    lblDownloadStatus.Text = "Bước 5: Đang nén kết quả...";
                    this.Enabled = false; // khóa lại trong lúc nén
                    await Task.Run(() => ZipFile.CreateFromDirectory(baseTempDirectory, finalZipPath));
                    outputSaved = true;
                    this.Enabled = true; // bật lại trước hộp thoại kết quả để giữ kích hoạt

                    string duplicateNote = duplicateCount > 0 ? $" ({duplicateCount} dòng trùng ở cả Bán ra và Mua vào chỉ tải một lần)" : "";
                    string successMessage = $"✅ Hoàn tất! \n\nĐã tải {successCount} HĐ gốc{duplicateNote}, xử lý/convert {convertSuccessCount} HĐ sang PDF.\n" +
                                            $"Đã lưu tất cả file (Excel, PDF, XML, ZIP gốc) vào một file nén duy nhất:\n\n{finalZipPath}";
                    if (failedChunks.Any())
                    {
                        string errorList = string.Join("\n - ", failedChunks);
                        successMessage += $"\n\nLưu ý: Đã xảy ra lỗi khi xử lý các mục sau (kết quả có thể bị thiếu):\n - {errorList}";
                    }
                    MessageBox.Show(this, successMessage,
                         failedChunks.Any() ? "Hoàn tất (Có lỗi)" : "Thành Công",
                         MessageBoxButtons.OK,
                         failedChunks.Any() ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Hủy: không hộp thoại chọn thư mục, không ZIP, không thông báo thành công; thư mục tạm dọn ở finally
                cancelled = true;
                AppendLog("ℹ️ (Tải gốc) Người dùng đã hủy.");
                ShowStatusMessage("Đã hủy tải HĐ gốc.", Color.OrangeRed);
            }
            catch (ChromiumUnavailableException exChromium)
            {
                // Dừng cả lô ở lỗi đầu tiên; HĐ chưa xử lý giữ trạng thái Pending, thư mục làm việc được giữ để chạy tiếp
                itemsNotDone++;
                AppendLog("❌ (Tải gốc) Không có Chromium, dừng lô: " + exChromium.GetType().Name);
                CloseModal();
                MessageBox.Show(this, exChromium.Message, "Không chuyển được PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (JsonException jsonEx)
            {
                AppendLog($"❌ Lỗi phân tích JSON trong SaveOriginalInvoicesAsync: {jsonEx}");
                CloseModal();
                MessageBox.Show(this, $"Lỗi xử lý dữ liệu trả về từ API: {jsonEx.Message}", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ioEx)
            {
                AppendLog($"❌ Lỗi IO trong SaveOriginalInvoicesAsync: {ioEx}");
                CloseModal();
                MessageBox.Show(this, $"Đã xảy ra lỗi khi xử lý file hoặc thư mục: {ioEx.Message}\n\nThường do đường dẫn quá dài, không có quyền ghi, hoặc file đang được sử dụng.", "Lỗi File/Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AppendLog($"🐞 LỖI NGHIÊM TRỌNG trong SaveOriginalInvoicesAsync: {ex}");
                CloseModal();
                MessageBox.Show(this, $"Đã xảy ra lỗi không mong muốn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CloseModal();
                JobFlush();
                // Gắn file vào kho trước khi thư mục làm việc bị xóa; lỗi chỉ cảnh báo
                var attachTask = AttachDownloadedFilesAsync(storeOwner, finishedForStore);
                TrackStoreTask(attachTask);
                await attachTask;
                try
                {
                    // Thư mục làm việc chỉ xóa khi không hủy, mọi HĐ đã xong và kết quả đã lưu; còn lại giữ để tiếp tục
                    bool keepWork = isWorkFolder && (cancelled || !outputSaved || itemsNotDone > 0);
                    if (!keepWork && Directory.Exists(baseTempDirectory))
                    {
                        Directory.Delete(baseTempDirectory, true);
                        if (isWorkFolder && jobStore != null && jobId != null)
                        {
                            try { jobStore.Delete(jobId); }
                            catch (Exception exJob) { AppendLog($"⚠️ (Tải gốc) Không xóa được job đã xong: {exJob.GetType().Name}"); }
                        }
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

        private void ShowStatusMessage(string message, Color color, int durationMs = 2000)
        {
            lblStatusMessage.Text = message;
            lblStatusMessage.ForeColor = color;
            lblStatusMessage.Visible = true;

            statusTimer.Stop();
            statusTimer.Interval = durationMs;
            statusTimer.Start();
        }

        /// <summary>Lưu HĐ đã lấy được vào kho cục bộ ở luồng nền; lỗi chỉ ghi log và cảnh báo, không làm hỏng tìm kiếm/tải.</summary>
        private async Task StoreInvoicesAsync(IReadOnlyCollection<SearchResult> invoices)
        {
            string? own = _currentTaxCode;
            if (own == null || invoices.Count == 0) return;
            var snapshot = invoices.ToList();
            try
            {
                var result = await Task.Run(() => _store.Upsert(own, snapshot));
                AppendLog($"ℹ️ Kho hóa đơn: thêm {result.Added}, cập nhật {result.Updated}, không đổi {result.Unchanged}.");
            }
            catch (Exception ex)
            {
                AppendLog($"⚠️ Không ghi được kho hóa đơn: {ex.GetType().Name}: {ex.Message}");
                ShowStatusMessage("Cảnh báo: không lưu được vào kho hóa đơn cục bộ (xem log).", Color.OrangeRed, 6000);
            }
        }

        /// <summary>Chép PDF/XML đã tải xong vào kho (luồng nền). Chỉ file có thật và không rỗng; lỗi từng HĐ không dừng các HĐ khác.</summary>
        private async Task AttachDownloadedFilesAsync(string? own, IReadOnlyList<(SearchResult Invoice, string PdfPath, string XmlPath)> items)
        {
            if (own == null || items.Count == 0) return;
            _attaching = true;
            try
            {
                var (attached, failed) = await Task.Run(() =>
                {
                    int ok = 0, bad = 0;
                    foreach (var (invoice, pdfPath, xmlPath) in items)
                    {
                        try
                        {
                            foreach (string file in InvoiceGridLogic.ExistingNonEmptyFiles(pdfPath, xmlPath))
                                _store.AttachFile(own, invoice, file);
                            ok++;
                        }
                        catch (Exception) { bad++; }
                    }
                    return (ok, bad);
                });
                AppendLog($"ℹ️ Kho hóa đơn: đã gắn file cho {attached} HĐ" + (failed > 0 ? $", lỗi {failed} HĐ." : "."));
                if (failed > 0)
                    ShowStatusMessage($"Cảnh báo: không gắn được file vào kho cho {failed} hóa đơn.", Color.OrangeRed, 6000);
            }
            catch (Exception ex)
            {
                AppendLog($"⚠️ Không gắn được file vào kho hóa đơn: {ex.GetType().Name}");
                ShowStatusMessage("Cảnh báo: không gắn được file vào kho hóa đơn (xem log).", Color.OrangeRed, 6000);
            }
            finally
            {
                _attaching = false;
            }
        }

        private void TrackStoreTask(Task task)
        {
            _storeTasks.RemoveAll(t => t.IsCompleted);
            _storeTasks.Add(task);
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Chỉ chặn người dùng tự đóng; tắt máy/Windows không bị chặn
            if (_attaching && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                ShowStatusMessage("Đang lưu file vào kho hóa đơn, vui lòng đợi rồi đóng lại.", Color.OrangeRed, 5000);
            }
        }

        private async void btnKhoHoaDon_Click(object? sender, EventArgs e)
        {
            // Mở được cả khi chưa đăng nhập (xem dữ liệu offline), lúc đó _currentTaxCode có thể null.
            // Giữ cổng bận suốt lúc đợi ghi kho và lúc kho mở, nên không luồng nào khác bắt đầu được.
            await RunFlowAsync("Kho hóa đơn", async () =>
            {
                const string waitMessage = "Đang ghi dữ liệu vào kho, vui lòng đợi...";
                bool waited = false;
                // Đợi đến khi không còn tác vụ ghi kho nào (snapshot mới mỗi vòng); tác vụ lỗi không chặn
                while (true)
                {
                    var pending = _storeTasks.Where(t => !t.IsCompleted).ToList();
                    if (pending.Count == 0) break;
                    if (!waited)
                    {
                        waited = true;
                        ShowStatusMessage(waitMessage, Color.OrangeRed, 60000);
                    }
                    foreach (var t in pending)
                    {
                        try { await t; }
                        catch (Exception ex) { AppendLog($"⚠️ Tác vụ ghi kho lỗi, vẫn mở kho: {ex.GetType().Name}"); }
                    }
                }
                // Chỉ xóa thông báo chờ nếu nó vẫn đang hiện (không che cảnh báo ghi kho vừa hiện)
                if (waited && lblStatusMessage.Visible && lblStatusMessage.Text == waitMessage)
                {
                    lblStatusMessage.Visible = false;
                    statusTimer.Stop();
                }
                if (IsDisposed) return;
                using var form = new KhoHoaDonForm(_store, _jobStore, _notes, _currentTaxCode);
                form.ShowDialog(this);
            });
        }

        /// <summary>
        /// Chạy một luồng dài qua cổng bận chung: vào cổng trước khi đụng giao diện; bị từ chối thì báo tên luồng đang chạy.
        /// Exit luôn nằm trong finally. Chỉ gọi ở handler ngoài cùng (handler lồng nhau không được vào cổng lần nữa).
        /// </summary>
        private async Task RunFlowAsync(string flowName, Func<Task> body)
        {
            if (!_gate.TryEnter(flowName))
            {
                ShowStatusMessage($"Đang chạy \"{_gate.CurrentFlow}\". Vui lòng đợi xong rồi thử lại.", Color.OrangeRed, 4000);
                return;
            }
            try
            {
                await body();
            }
            finally
            {
                _gate.Exit();
            }
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

        private async void btnXemHD_Click(object sender, EventArgs e) => await RunFlowAsync("Xem hóa đơn", () => btnXemHD_RunAsync(sender, e));

        private async Task btnXemHD_RunAsync(object sender, EventArgs e)
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

        private async void btnExportDS_Click(object sender, EventArgs e) => await RunFlowAsync("Xuất danh sách", () => btnExportDS_RunAsync(sender, e));

        private async Task btnExportDS_RunAsync(object sender, EventArgs e)
        {
            if (_visibleResults == null || !_visibleResults.Any())
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
                await ExportSearchResultsToExcelAsync(_visibleResults, sfd.FileName);
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
        private async void btnExportChiTiet_Click(object sender, EventArgs e) => await RunFlowAsync("Xuất chi tiết", () => btnExportChiTiet_RunAsync(sender, e));

        private async Task btnExportChiTiet_RunAsync(object sender, EventArgs e)
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
                var pages = await _detailService.GetDetailPagesAsync(selectedInvoice);
                if (pages == null)
                {
                    throw new Exception(DetailFetchError);
                }

                var detailRows = BuildDetailRows(pages);
                string filePath = sfd.FileName;
                await Task.Run(() => ExcelExporter.ExportSingleInvoiceDetail(detailRows, filePath));

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
        private async void btnCnKoPdf_Click(object sender, EventArgs e) => await RunFlowAsync("In hóa đơn", () => btnCnKoPdf_RunAsync(sender, e));

        private async Task btnCnKoPdf_RunAsync(object sender, EventArgs e)
        {
            List<SearchResult> validInvoices = GetSelectedInvoicesForPdf();

            if (!validInvoices.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một hóa đơn hợp lệ từ danh sách để in/lưu PDF.",
                                "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnCnKoPdf.Enabled = false;
            btnGopPdf.Enabled = false;
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
                var prepared = await PrepareInvoicePdfsAsync(validInvoices, baseTempDirectory, pdfConverter,
                    (finished, status) =>
                    {
                        downloadProgressBar.Value = Math.Min(finished * 4, downloadProgressBar.Maximum);
                        lblDownloadStatus.Text = status;
                    });
                finalPdfPaths = prepared.Paths;
                failedInvoicesInfo = prepared.Failed;

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
            catch (ChromiumUnavailableException exChromium)
            {
                AppendLog("❌ Không có Chromium (in HĐ): " + exChromium.GetType().Name);
                MessageBox.Show(exChromium.Message, "Không chuyển được PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
                downloadProgressBar.Value = downloadProgressBar.Maximum;
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
                btnGopPdf.Enabled = true;
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

        /// <summary>
        /// Các hóa đơn đang chọn trên lưới (hoặc dòng hiện tại), chỉ lấy HĐ đủ khóa tra cứu, sắp theo ký hiệu rồi số HĐ.
        /// </summary>
        private List<SearchResult> GetSelectedInvoicesForPdf()
        {
            var selectedInvoices = new List<SearchResult>();

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

            // Cùng một hóa đơn được chọn ở cả Bán ra và Mua vào chỉ xử lý một lần
            var sorted = selectedInvoices
                .Where(invoice => invoice.HasLookupKeys())
                .OrderBy(inv => inv.Ky_hieu_hoa_don).ThenBy(inv => inv.So_hoa_don);
            return InvoiceDownloadPlan.Build(sorted).Select(item => item.Invoice).ToList();
        }

        /// <summary>
        /// Gộp các hóa đơn đang chọn thành một file PDF duy nhất.
        /// </summary>
        private async void btnGopPdf_Click(object sender, EventArgs e) => await RunFlowAsync("Gộp PDF", () => btnGopPdf_RunAsync(sender, e));

        private async Task btnGopPdf_RunAsync(object sender, EventArgs e)
        {
            List<SearchResult> validInvoices = GetSelectedInvoicesForPdf();
            if (!validInvoices.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một hóa đơn hợp lệ từ danh sách để gộp PDF.",
                                "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hỏi nơi lưu trước khi tải để người dùng hủy được mà không tốn công
            using var sfd = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"GopHoaDon_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                OverwritePrompt = true
            };
            if (sfd.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(sfd.FileName))
                return;
            string outputPath = sfd.FileName;

            btnGopPdf.Enabled = false;
            btnCnKoPdf.Enabled = false;
            string oldText = btnGopPdf.Text;
            btnGopPdf.Text = $"Gộp {validInvoices.Count}...";
            downloadProgressBar.Visible = true;
            downloadProgressBar.Minimum = 0;
            downloadProgressBar.Maximum = validInvoices.Count * 4;
            downloadProgressBar.Value = 0;
            downloadProgressBar.Style = ProgressBarStyle.Blocks;
            lblDownloadStatus.Text = "Bắt đầu xử lý...";
            lblDownloadStatus.Visible = true;
            this.Cursor = Cursors.WaitCursor;

            string baseTempDirectory = Path.Combine(Path.GetTempPath(), $"E-Tax-MergePdf_{Guid.NewGuid()}");

            try
            {
                await using var pdfConverter = new PdfConverter(EnsureChromiumAsync);

                var (paths, failedInvoices) = await PrepareInvoicePdfsAsync(validInvoices, baseTempDirectory, pdfConverter,
                    (finished, status) =>
                    {
                        downloadProgressBar.Value = Math.Min(finished * 4, downloadProgressBar.Maximum);
                        lblDownloadStatus.Text = status;
                    });

                PdfMergeResult mergeResult = new PdfMergeResult(0, Array.Empty<string>());
                if (paths.Count > 0)
                {
                    lblDownloadStatus.Text = "Đang gộp PDF...";
                    mergeResult = await Task.Run(() => PdfMerger.Merge(paths, outputPath));
                }

                var problems = new List<string>(failedInvoices);
                problems.AddRange(mergeResult.FailedFiles.Select(f => $"{Path.GetFileName(f)} (Không gộp được)"));

                if (mergeResult.PagesWritten == 0)
                {
                    string msg = "Không gộp được PDF nào, chưa ghi file.";
                    if (problems.Count > 0) msg += $"\n\nCác mục gặp lỗi:\n - {string.Join("\n - ", problems)}";
                    MessageBox.Show(msg, "Gộp PDF thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string message = $"Đã gộp {mergeResult.PagesWritten} trang từ {paths.Count - mergeResult.FailedFiles.Count}/{validInvoices.Count} hóa đơn vào:\n{outputPath}";
                if (problems.Count > 0)
                    message += $"\n\nCác mục gặp lỗi:\n - {string.Join("\n - ", problems)}";
                MessageBox.Show(message,
                                problems.Count > 0 ? "Gộp PDF (Có lỗi)" : "Gộp PDF",
                                MessageBoxButtons.OK,
                                problems.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (ChromiumUnavailableException exChromium)
            {
                AppendLog("❌ Không có Chromium (gộp PDF): " + exChromium.GetType().Name);
                MessageBox.Show(exChromium.Message, "Không chuyển được PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi nghiêm trọng trong btnGopPdf_Click: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn khi gộp PDF: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGopPdf.Enabled = true;
                btnGopPdf.Text = oldText;
                btnCnKoPdf.Enabled = true;
                downloadProgressBar.Visible = false;
                lblDownloadStatus.Visible = false;
                lblDownloadStatus.Text = "";
                this.Cursor = Cursors.Default;

                try
                {
                    if (Directory.Exists(baseTempDirectory))
                        Directory.Delete(baseTempDirectory, true);
                }
                catch (Exception exClean)
                {
                    AppendLog($"⚠️ Không thể xóa thư mục tạm {baseTempDirectory}: {exClean.Message}");
                }
            }
        }

        /// <summary>
        /// Tải ZIP, giải nén và chuẩn bị đúng một file PDF cho mỗi hóa đơn (lấy PDF sẵn có hoặc convert từ HTML).
        /// Thứ tự file trả về theo thứ tự hóa đơn đầu vào. <paramref name="report"/> nhận số hóa đơn đã xong và dòng trạng thái.
        /// </summary>
        private async Task<(List<string> Paths, List<string> Failed)> PrepareInvoicePdfsAsync(
            IReadOnlyList<SearchResult> selectedInvoices, string tempDir, PdfConverter converter, Action<int, string> report)
        {
            var plan = InvoiceDownloadPlan.Build(selectedInvoices);
            var invoices = plan.Select(item => item.Invoice).ToList();
            var paths = new List<string>();
            var failed = new List<string>();

            for (int i = 0; i < invoices.Count; i++)
            {
                SearchResult invoice = invoices[i];
                string invoiceIdentifier = plan[i].FileId;
                string invoiceTempDir = Path.Combine(tempDir, $"Invoice_{i}_{invoiceIdentifier}");
                string extractPath = Path.Combine(invoiceTempDir, "extracted");
                string outputPdfPath = Path.Combine(invoiceTempDir, $"HD_{invoiceIdentifier}.pdf");

                try
                {
                    Directory.CreateDirectory(extractPath);

                    report(i, $"({i + 1}/{invoices.Count}) Đang tải HĐ {invoice.So_hoa_don}...");
                    string? zipFilePath = await DownloadSingleInvoiceZipAsync(invoice, invoiceTempDir, default, invoiceIdentifier);

                    if (zipFilePath == null)
                    {
                        failed.Add($"{invoiceIdentifier} (Lỗi tải file ZIP)");
                        report(i + 1, $"({i + 1}/{invoices.Count}) Lỗi tải HĐ {invoice.So_hoa_don}");
                        continue;
                    }

                    report(i, $"({i + 1}/{invoices.Count}) Đang giải nén HĐ {invoice.So_hoa_don}...");
                    await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, extractPath, true));

                    report(i, $"({i + 1}/{invoices.Count}) Đang tìm file HĐ {invoice.So_hoa_don}...");
                    string? pdfFile = FindFirstFile(extractPath, "*.pdf");
                    string? htmlFile = FindFirstFile(extractPath, "*.html");

                    if (pdfFile != null)
                    {
                        File.Move(pdfFile, outputPdfPath);
                        paths.Add(outputPdfPath);
                    }
                    else if (htmlFile != null)
                    {
                        report(i, $"({i + 1}/{invoices.Count}) Đang convert HTML sang PDF...");
                        try
                        {
                            await converter.ConvertAsync(htmlFile, outputPdfPath);
                            paths.Add(outputPdfPath);
                        }
                        catch (Exception exConvert) when (exConvert is not ChromiumUnavailableException)
                        {
                            AppendLog($"  -> Lỗi convert HTML->PDF HĐ {invoiceIdentifier}: {exConvert}");
                            failed.Add($"{invoiceIdentifier} (Lỗi convert HTML sang PDF)");
                        }
                    }
                    else
                    {
                        failed.Add($"{invoiceIdentifier} (Không tìm thấy file PDF/HTML)");
                    }
                }
                catch (Exception exInvoice) when (exInvoice is not ChromiumUnavailableException)
                {
                    AppendLog($"❌ Lỗi xử lý HĐ {invoiceIdentifier}: {exInvoice}");
                    failed.Add($"{invoiceIdentifier} (Lỗi chung: {exInvoice.Message})");
                }
                report(i + 1, $"({i + 1}/{invoices.Count}) Xong HĐ {invoice.So_hoa_don}");
            }

            return (paths, failed);
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
            if (e.RowIndex < 0 || e.ColumnIndex != dgvMain.Columns["colDgvTraCuu"].Index) return;
            await RunFlowAsync("Tra cứu hóa đơn", () => dgvMain_CellContentClick_RunAsync(sender, e));
        }

        private async Task dgvMain_CellContentClick_RunAsync(object sender, DataGridViewCellEventArgs e)
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
                var pages = await _detailService.GetDetailPagesAsync(selectedInvoice);
                if (pages == null || pages.Count == 0)
                {
                    MessageBox.Show($"Lỗi khi lấy chi tiết hóa đơn:\n{DetailFetchError.Substring(2)}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string jsonDetail = pages[0];

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

            if (string.IsNullOrEmpty(selectedInvoice.Id))
            {
                MessageBox.Show("Hóa đơn này không có mã định danh nên không lưu được ghi chú.", "Không thể ghi chú", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private const string RiskNotConfiguredText = "Chức năng kiểm tra doanh nghiệp chưa được cấu hình.";
        private const string BoughtLabel = "Mua vào";
        private static readonly Color RiskyRowColor = Color.FromArgb(255, 199, 206);
        private static readonly Color UnknownRowColor = Color.FromArgb(217, 217, 217);

        /// <summary>
        /// Kết quả rủi ro của hóa đơn. Chỉ HĐ "Mua vào" mới có (người bán là NCC); HĐ "Bán ra" có người bán là chính công ty.
        /// </summary>
        private static RiskResult? RiskFor(SearchResult? item, IReadOnlyDictionary<string, RiskResult> risks) =>
            item != null && item.Loai_hoa_don == BoughtLabel && risks.TryGetValue((item.Ma_so_thue ?? "").Trim(), out var r) ? r : null;

        /// <summary>Nhãn dùng chung cho lưới và Excel; rỗng khi chưa kiểm tra.</summary>
        private static string RiskLabelFor(SearchResult item, IReadOnlyDictionary<string, RiskResult> risks) =>
            RiskFor(item, risks) is RiskResult r ? RiskCheckService.Label(r.Status) : "";

        private void DgvMain_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvMain.Rows.Count) return;
            if (e.ColumnIndex == dgvMain.Columns["colDgvNgayLap"]?.Index && e.Value is string ngayLap)
            {
                e.Value = InvoiceDate.ToDisplay(ngayLap);
                e.FormattingApplied = true;
            }
            if (dgvMain.Rows[e.RowIndex].DataBoundItem is not SearchResult item) return;

            // Cột "Phân loại" chỉ để hiển thị (không bind, nằm cuối lưới)
            if (e.ColumnIndex == dgvMain.Columns["colDgvPhanLoai"]?.Index)
            {
                InvoiceLink? link = null;
                if (!string.IsNullOrEmpty(item.Id)) _invoiceLinks.TryGetValue(item.Id, out link);
                e.Value = InvoiceGridLogic.ClassificationText(item, link);
                e.FormattingApplied = true;
            }

            var risk = RiskFor(item, _riskResults);
            if (risk != null && e.ColumnIndex == dgvMain.Columns["colDgvRuiRo"]?.Index)
            {
                e.Value = RiskCheckService.Label(risk.Status);
                e.FormattingApplied = true;
            }

            // Thứ tự ưu tiên: rủi ro, chưa kiểm tra được, rồi màu phân loại. Chỉ đặt BackColor nên dòng đang chọn vẫn dùng SelectionBackColor.
            var color = InvoiceGridLogic.RowColor(risk?.Status, InvoiceClassifier.RelationOf(item.Trang_thai_hoa_don), RiskyRowColor, UnknownRowColor);
            if (color.HasValue && e.CellStyle != null) e.CellStyle.BackColor = color.Value;
        }

        /// <summary>
        /// Kiểm tra hàng loạt MST người bán của các HĐ "Mua vào" đang hiển thị. Hủy thì bỏ toàn bộ kết quả lần này.
        /// </summary>
        private async void btnKiemTraNCC_Click(object? sender, EventArgs e) => await RunFlowAsync("Kiểm tra NCC", () => btnKiemTraNCC_RunAsync(sender, e));

        private async Task btnKiemTraNCC_RunAsync(object? sender, EventArgs e)
        {
            var codes = _visibleResults
                .Where(r => r.Loai_hoa_don == BoughtLabel)
                .Select(r => (r.Ma_so_thue ?? "").Trim())
                .Where(c => c.Length > 0)
                .Distinct()
                .ToList();
            if (codes.Count == 0)
            {
                MessageBox.Show(this, "Không có hóa đơn mua vào nào đang hiển thị để kiểm tra nhà cung cấp.", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_riskService == null)
            {
                CheckSuppliersWithLocalList(codes);
                return;
            }

            btnKiemTraNCC.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            var modal = ProcessingForm.ShowFor(this);
            var ct = modal.CancellationToken;
            IReadOnlyDictionary<string, RiskResult>? results = null;
            bool cancelled = false;
            bool failed = false;

            try
            {
                modal.UpdateStatus($"Đang kiểm tra {codes.Count} nhà cung cấp...");
                modal.Report(new BatchProgress("Kiểm tra NCC", 0, codes.Count, 0));
                results = await _riskService.CheckManyAsync(codes,
                    done => modal.Report(new BatchProgress("Kiểm tra NCC", done, codes.Count, 0)), ct);
                ct.ThrowIfCancellationRequested(); // Hủy sát cuối: bỏ toàn bộ kết quả
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                cancelled = true;
                AppendLog("ℹ️ Người dùng đã hủy kiểm tra NCC.");
            }
            catch (Exception ex)
            {
                failed = true;
                AppendLog($"❌ Lỗi kiểm tra NCC hàng loạt: {ex.GetType().Name}");
            }
            finally
            {
                this.Enabled = true;
                modal.CloseFromOwner();
                btnKiemTraNCC.Enabled = true;
                this.Cursor = Cursors.Default;
            }

            if (cancelled)
            {
                ShowStatusMessage("Đã hủy kiểm tra NCC.", Color.OrangeRed);
                return;
            }
            if (failed || results == null)
            {
                MessageBox.Show(this, "Không kiểm tra được nhà cung cấp do lỗi không mong muốn. Chưa có kết quả nào được ghi nhận. Xem log để biết thêm.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var merged = new Dictionary<string, RiskResult>(_riskResults);
            foreach (var kv in results) merged[kv.Key] = kv.Value;
            _riskResults = merged;
            dgvMain.Invalidate();

            int risky = results.Values.Count(r => r.Status == RiskStatus.Risky);
            int safe = results.Values.Count(r => r.Status == RiskStatus.Safe);
            int unknown = results.Values.Count(r => r.Status == RiskStatus.Unknown);
            string summary = $"Đã xử lý {results.Count} nhà cung cấp:\n - {RiskCheckService.Label(RiskStatus.Risky)}: {risky}\n - {RiskCheckService.Label(RiskStatus.Safe)}: {safe}\n - {RiskCheckService.Label(RiskStatus.Unknown)}: {unknown}";
            if (unknown > 0)
                summary += $"\n\nCó {unknown} nhà cung cấp CHƯA được kiểm tra (lỗi mạng hoặc máy chủ), không có nghĩa là an toàn. Hãy kiểm tra lại các dòng tô xám.";
            MessageBox.Show(this, summary, "Kết quả kiểm tra NCC", MessageBoxButtons.OK, unknown > 0 || risky > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private LocalRiskList? _localRiskList;

        /// <summary>
        /// Không có token: đối chiếu với danh sách rủi ro nhập từ file. Chỉ có "Rủi ro" hoặc "Không kiểm tra được",
        /// không có trong danh sách KHÔNG có nghĩa là an toàn. Danh sách rỗng thì mời nhập file.
        /// </summary>
        private void CheckSuppliersWithLocalList(List<string> codes)
        {
            _localRiskList ??= new LocalRiskList(LocalRiskList.DefaultPath, AppendLog);
            if (_localRiskList.Count == 0)
            {
                var ask = MessageBox.Show(this,
                    "Chưa cấu hình kiểm tra trực tuyến và chưa có danh sách nhà cung cấp rủi ro cục bộ.\n\nBạn có muốn nhập danh sách từ file CSV/XLSX (có cột MST) ngay bây giờ không?",
                    "Chưa có danh sách rủi ro", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (ask != DialogResult.Yes || !ImportLocalRiskList()) return;
            }

            var merged = new Dictionary<string, RiskResult>(_riskResults);
            foreach (var code in codes) merged[code] = _localRiskList.Check(code);
            _riskResults = merged;
            dgvMain.Invalidate();

            int risky = codes.Count(c => merged[c].Status == RiskStatus.Risky);
            int unknown = codes.Count - risky;
            string summary = $"Đối chiếu {codes.Count} nhà cung cấp với danh sách cục bộ ({_localRiskList.Count} MST):\n - {RiskCheckService.Label(RiskStatus.Risky)}: {risky}\n - {RiskCheckService.Label(RiskStatus.Unknown)}: {unknown}" +
                "\n\nNhà cung cấp không có trong danh sách KHÔNG có nghĩa là an toàn.\n\nBạn có muốn nhập lại danh sách từ file khác (thay thế danh sách hiện tại) không?";
            var again = MessageBox.Show(this, summary, "Kết quả đối chiếu NCC", MessageBoxButtons.YesNo,
                risky > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            if (again == DialogResult.Yes && ImportLocalRiskList())
                CheckSuppliersWithLocalList(codes);
        }

        /// <summary>Chọn file và nhập danh sách; trả true nếu nhập thành công.</summary>
        private bool ImportLocalRiskList()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Chọn file danh sách nhà cung cấp rủi ro",
                Filter = "Danh sách MST (*.csv;*.xlsx)|*.csv;*.xlsx"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return false;
            try
            {
                int n = _localRiskList!.Import(dlg.FileName);
                AppendLog($"ℹ️ Đã nhập {n} MST vào danh sách NCC rủi ro cục bộ.");
                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Nhập danh sách NCC rủi ro thất bại: {ex.GetType().Name}");
                MessageBox.Show(this, ex is InvalidDataException ? ex.Message : "Không đọc hoặc ghi được file. Danh sách hiện tại được giữ nguyên.",
                    "Không nhập được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        private async void btnKtraDNMuaLe_Click(object sender, EventArgs e) => await RunFlowAsync("Kiểm tra doanh nghiệp", () => btnKtraDNMuaLe_RunAsync(sender, e));

        private async Task btnKtraDNMuaLe_RunAsync(object sender, EventArgs e)
        {
            string taxCode = txtKtraMaDoanhNghiep.Text.Trim();
            if (string.IsNullOrEmpty(taxCode))
            {
                MessageBox.Show("Vui lòng nhập một Mã số thuế vào ô 'Nhập mã doanh nghiệp' (hoặc click vào một hóa đơn trên lưới để chọn).",
                        "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtKtraMaDoanhNghiep.Focus();
                return;
            }

            if (_riskService == null)
            {
                MessageBox.Show(this, RiskNotConfiguredText, "Chưa cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnKtraDNMuaLe.Enabled = false;
            btnKtraDNMuaLe.Text = "Đang KT...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var result = await _riskService.CheckAsync(taxCode);

                string message = $"Mã số thuế: {taxCode}\n" +
                                 (!string.IsNullOrEmpty(result.CompanyName) ? $"(Tên: {result.CompanyName})\n\n" : "\n");

                switch (result.Status)
                {
                    case RiskStatus.Risky:
                        MessageBox.Show(this, message + "Trạng thái: 🔴 Công ty nằm trong danh sách cảnh báo rủi ro!",
                                "Nguy Hiểm", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    case RiskStatus.Safe:
                        MessageBox.Show(this, message + "Trạng thái: ✅ An toàn (Công ty không nằm trong danh sách cảnh báo rủi ro).",
                                "An Toàn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    default:
                        MessageBox.Show(this, message + "Trạng thái: Không kiểm tra được. Chưa xác định được công ty có nằm trong danh sách cảnh báo hay không (không có nghĩa là an toàn). Xem log để biết thêm.",
                                "Không kiểm tra được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi kiểm tra doanh nghiệp: {ex.GetType().Name}");
                MessageBox.Show(this, "Không kiểm tra được do lỗi không mong muốn. Xem log để biết thêm.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
