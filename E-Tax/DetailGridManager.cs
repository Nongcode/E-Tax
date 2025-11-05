using System;
using System.Collections.Generic;
using System.Drawing; // Cần cho Font
using System.Globalization; // Cần cho CultureInfo khi parse decimal
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers; // Cần cho AuthenticationHeaderValue
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E_Tax // Đảm bảo đúng namespace
{
    /// <summary>
    /// Class đại diện cho một dòng dữ liệu chi tiết (sản phẩm/dịch vụ) trong hóa đơn
    /// </summary>
    public class InvoiceDetailItem
    {
        // --- PHẦN NÀY ĐÃ ĐẦY ĐỦ, GIỮ NGUYÊN ---
        [System.ComponentModel.DisplayName("STT")]
        public int? STT { get; set; }

        [System.ComponentModel.DisplayName("Ký hiệu")]
        public string KyHieuHD { get; set; }

        [System.ComponentModel.DisplayName("Số HĐ")]
        public int? SoHD { get; set; }

        [System.ComponentModel.DisplayName("Ngày HĐ")]
        public string NgayHD { get; set; }

        [System.ComponentModel.DisplayName("Tên NB")]
        public string TenNguoiBan { get; set; }

        [System.ComponentModel.DisplayName("MST NB")]
        public string MSTNguoiBan { get; set; }

        [System.ComponentModel.DisplayName("Tên SP/DV")]
        public string TenSanPham { get; set; }

        [System.ComponentModel.DisplayName("ĐVT")]
        public string DonViTinh { get; set; }

        [System.ComponentModel.DisplayName("Số lượng")]
        public decimal? SoLuong { get; set; }

        [System.ComponentModel.DisplayName("Đơn giá")]
        public decimal? DonGia { get; set; }

        [System.ComponentModel.DisplayName("Thành tiền")]
        public decimal? ThanhTien { get; set; }

        [System.ComponentModel.DisplayName("Tiền CK")]
        public decimal? TienChietKhau { get; set; }

        [System.ComponentModel.DisplayName("Doanh số")]
        public decimal? DoanhSoChuaThue { get; set; }

        [System.ComponentModel.DisplayName("Thuế suất")]
        public string ThueSuat { get; set; }

        [System.ComponentModel.DisplayName("Tiền thuế")]
        public decimal? TienThueGTGT { get; set; }

        [System.ComponentModel.DisplayName("Tổng TT (HĐ)")]
        public decimal? TongTienThanhToan_HD { get; set; }

        [System.ComponentModel.DisplayName("Số lô")]
        public string SoLo { get; set; }

        [System.ComponentModel.DisplayName("Hạn SD")]
        public string HanSuDung { get; set; }

        [System.ComponentModel.DisplayName("Ghi chú SP")]
        public string GhiChuSanPham { get; set; }
        // --- KẾT THÚC PHẦN GIỮ NGUYÊN ---
    }

    /// <summary>
    /// Class quản lý việc lấy và hiển thị dữ liệu chi tiết hóa đơn lên DataGridView
    /// </summary>
    public class DetailGridManager
    {
        private readonly HttpClient _client;
        private readonly DataGridView _dgvDetails;
        private readonly DataGridView _dgvMua;
        private readonly DataGridView _dgvBan;
        private readonly DataGridView _dgvVatNop;
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;
        private readonly Action<string> _logAction;
        private readonly string _browserUserAgent;
        private string _jwtToken;

        // --- (Constructor và các hàm cấu hình grid giữ nguyên) ---
        public DetailGridManager(HttpClient client, DataGridView dgvDetails, DataGridView dgvMua,
         DataGridView dgvBan, DataGridView dgvVatNop, ProgressBar progressBar, Label statusLabel,
         Action<string> logAction, string userAgent)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _dgvDetails = dgvDetails ?? throw new ArgumentNullException(nameof(dgvDetails));
            _dgvMua = dgvMua ?? throw new ArgumentNullException(nameof(dgvMua));
            _dgvBan = dgvBan ?? throw new ArgumentNullException(nameof(dgvBan));
            _dgvVatNop = dgvVatNop ?? throw new ArgumentNullException(nameof(dgvVatNop));
            _progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
            _statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
            _browserUserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));

            ConfigureFullDetailGridColumns(_dgvDetails);
            ConfigureFullDetailGridColumns(_dgvMua);
            ConfigureFullDetailGridColumns(_dgvBan);
            ConfigureVatNopGridColumns();
        }

        public void SetJwtToken(string token)
        {
            _jwtToken = token;
            _logAction?.Invoke("🔑 Token đã được cập nhật cho DetailGridManager.");
        }

        private void ConfigureFullDetailGridColumns(DataGridView dgv)
        {
            try
            {
                if (dgv.InvokeRequired)
                {
                    dgv.Invoke(new Action(() => ConfigureFullDetailGridInternal(dgv)));
                }
                else
                {
                    ConfigureFullDetailGridInternal(dgv);
                }
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"❌ Lỗi khi cấu hình lưới {dgv.Name}: {ex.Message}");
            }
        }

        private void ConfigureFullDetailGridInternal(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgv.Columns.Clear();
            AddTextColumn(dgv, "colDetailSTT", "STT", nameof(InvoiceDetailItem.STT), 40, frozen: true);
            AddTextColumn(dgv, "colDetailKyHieu", "Ký hiệu", nameof(InvoiceDetailItem.KyHieuHD), 80);
            AddTextColumn(dgv, "colDetailSoHD", "Số HĐ", nameof(InvoiceDetailItem.SoHD), 80);
            AddTextColumn(dgv, "colDetailNgayHD", "Ngày HĐ", nameof(InvoiceDetailItem.NgayHD), 90);
            AddTextColumn(dgv, "colDetailTenNB", "Tên NB", nameof(InvoiceDetailItem.TenNguoiBan), 200);
            AddTextColumn(dgv, "colDetailMSTNB", "MST NB", nameof(InvoiceDetailItem.MSTNguoiBan), 110);
            AddTextColumn(dgv, "colDetailTenSP", "Tên SP/DV", nameof(InvoiceDetailItem.TenSanPham), 250);
            AddTextColumn(dgv, "colDetailDVT", "ĐVT", nameof(InvoiceDetailItem.DonViTinh), 50);
            AddNumericColumn(dgv, "colDetailSoLuong", "Số lượng", nameof(InvoiceDetailItem.SoLuong), 80, "#,##0.##");
            AddNumericColumn(dgv, "colDetailDonGia", "Đơn giá", nameof(InvoiceDetailItem.DonGia), 100, "#,##0.##");
            AddNumericColumn(dgv, "colDetailThanhTien", "Thành tiền", nameof(InvoiceDetailItem.ThanhTien), 110, "#,##0");
            AddNumericColumn(dgv, "colDetailTienCK", "Tiền CK", nameof(InvoiceDetailItem.TienChietKhau), 90, "#,##0");
            AddNumericColumn(dgv, "colDetailDoanhSo", "Doanh số", nameof(InvoiceDetailItem.DoanhSoChuaThue), 110, "#,##0");
            AddTextColumn(dgv, "colDetailThueSuat", "Thuế suất", nameof(InvoiceDetailItem.ThueSuat), 60);
            AddNumericColumn(dgv, "colDetailTienThue", "Tiền thuế", nameof(InvoiceDetailItem.TienThueGTGT), 100, "#,##0");
            AddNumericColumn(dgv, "colDetailTongTT", "Tổng TT (HĐ)", nameof(InvoiceDetailItem.TongTienThanhToan_HD), 120, "#,##0");
            AddTextColumn(dgv, "colDetailSoLo", "Số lô", nameof(InvoiceDetailItem.SoLo), 80);
            AddTextColumn(dgv, "colDetailHSD", "Hạn SD", nameof(InvoiceDetailItem.HanSuDung), 90);
            AddTextColumn(dgv, "colDetailGhiChuSP", "Ghi chú SP", nameof(InvoiceDetailItem.GhiChuSanPham), 150);
        }

        private void ConfigureVatNopGridColumns()
        {
            try
            {
                if (_dgvVatNop.InvokeRequired)
                {
                    _dgvVatNop.Invoke(new Action(() => ConfigureVatNopGridInternal()));
                }
                else
                {
                    ConfigureVatNopGridInternal();
                }
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"❌ Lỗi khi cấu hình lưới {_dgvVatNop.Name}: {ex.Message}");
            }
        }

        private void ConfigureVatNopGridInternal()
        {
            DataGridView dgv = _dgvVatNop;
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgv.Columns.Clear();
            AddTextColumn(dgv, "colVatSTT", "STT", nameof(InvoiceDetailItem.STT), 40, frozen: true);
            AddTextColumn(dgv, "colVatKyHieu", "Ký hiệu", nameof(InvoiceDetailItem.KyHieuHD), 80);
            AddTextColumn(dgv, "colVatSoHD", "Số HĐ", nameof(InvoiceDetailItem.SoHD), 80);
            AddTextColumn(dgv, "colVatNgayHD", "Ngày HĐ", nameof(InvoiceDetailItem.NgayHD), 90);
            AddTextColumn(dgv, "colVatMSTNB", "MST NB", nameof(InvoiceDetailItem.MSTNguoiBan), 110);
            AddTextColumn(dgv, "colVatTenSP", "Tên SP/DV", nameof(InvoiceDetailItem.TenSanPham), 300);
            AddNumericColumn(dgv, "colVatDoanhSo", "Doanh số", nameof(InvoiceDetailItem.DoanhSoChuaThue), 150, "#,##0");
            AddTextColumn(dgv, "colVatThueSuat", "Thuế suất", nameof(InvoiceDetailItem.ThueSuat), 80);
            AddNumericColumn(dgv, "colVatTienThue", "Tiền thuế", nameof(InvoiceDetailItem.TienThueGTGT), 150, "#,##0");
        }

        private void AddTextColumn(DataGridView dgv, string name, string header, string dataProperty, int width, bool frozen = false)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProperty,
                Width = width,
                ReadOnly = true,
                Frozen = frozen
            };
            dgv.Columns.Add(column);
        }

        private void AddNumericColumn(DataGridView dgv, string name, string header, string dataProperty, int width, string format)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProperty,
                Width = width,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = format, Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dgv.Columns.Add(column);
        }
        // --- (Hết phần giữ nguyên) ---


        // =================================================================
        // === HÀM POPULATE ĐÃ SỬA (THÊM VÒNG LẶP LỒNG NHAU) ===
        // =================================================================
        public async Task PopulateDetailGridAsync(List<SearchResult> invoiceHeaders, System.Text.StringBuilder logBuilder)
        {
            // Bỏ MessageBox ở đây vì logBuilder sẽ hiển thị nó ở cuối
            logBuilder.AppendLine("--- (Detail Mgr) Bắt đầu PopulateDetailGridAsync ---");

            MethodInvoker clearGridsAction = () => {
                _dgvDetails.DataSource = null;
                _dgvMua.DataSource = null;
                _dgvBan.DataSource = null;
                _dgvVatNop.DataSource = null;
                _statusLabel.Text = "Đang chuẩn bị tải chi tiết...";
                _statusLabel.Visible = true;
                _progressBar.Style = ProgressBarStyle.Blocks;
                _progressBar.Maximum = invoiceHeaders?.Count ?? 0;
                _progressBar.Value = 0;
                _progressBar.Visible = true;
            };
            if (_dgvDetails.InvokeRequired) _dgvDetails.Invoke(clearGridsAction); else clearGridsAction();

            if (invoiceHeaders == null || !invoiceHeaders.Any())
            {
                logBuilder.AppendLine("ℹ️ (Detail Mgr) Không có hóa đơn nào để lấy chi tiết.");
                MethodInvoker hideProgress = () => { _statusLabel.Visible = false; _progressBar.Visible = false; };
                if (_statusLabel.InvokeRequired) _statusLabel.Invoke(hideProgress); else hideProgress();
                return;
            }
            if (string.IsNullOrEmpty(_jwtToken))
            {
                logBuilder.AppendLine("❌ (Detail Mgr) Lỗi: JWT Token chưa được cung cấp.");
                MethodInvoker hideProgress = () => { _statusLabel.Text = "Lỗi: Chưa đăng nhập."; _progressBar.Visible = false; };
                if (_statusLabel.InvokeRequired) _statusLabel.Invoke(hideProgress); else hideProgress();
                return;
            }

            logBuilder.AppendLine($"⚙️ (Detail Mgr) Bắt đầu lấy chi tiết cho {invoiceHeaders.Count} hóa đơn...");

            var allDetailItems = new List<InvoiceDetailItem>();
            var boughtDetailItems = new List<InvoiceDetailItem>();
            var soldDetailItems = new List<InvoiceDetailItem>();
            var vatDetailItems = new List<InvoiceDetailItem>();

            int currentInvoiceIndex = 0;
            int globalStt = 1;
            const int pageSize = 50;

            foreach (var invoiceHeader in invoiceHeaders)
            {
                currentInvoiceIndex++;
                MethodInvoker updateStatusAction = () => {
                    _statusLabel.Text = $"Đang tải chi tiết HĐ ({currentInvoiceIndex}/{invoiceHeaders.Count})...";
                };
                if (_statusLabel.InvokeRequired) _statusLabel.Invoke(updateStatusAction); else updateStatusAction();

                if (invoiceHeader == null || string.IsNullOrEmpty(invoiceHeader.Ma_so_thue) ||
                    string.IsNullOrEmpty(invoiceHeader.Ky_hieu_hoa_don) ||
                    !invoiceHeader.So_hoa_don.HasValue ||
                    !invoiceHeader.Ky_hieu_ma_so.HasValue)
                {
                    logBuilder.AppendLine($"⚠️ (Detail Mgr) Bỏ qua HĐ {invoiceHeader?.Id} do thiếu thông tin.");
                    MethodInvoker stepProgress = () => { if (_progressBar.Value < _progressBar.Maximum) _progressBar.PerformStep(); };
                    if (_progressBar.InvokeRequired) _progressBar.Invoke(stepProgress); else stepProgress();
                    continue;
                }

                int currentPage = 0;
                bool keepFetchingDetails = true;

                try
                {
                    while (keepFetchingDetails)
                    {
                        MethodInvoker updatePageStatusAction = () => {
                            _statusLabel.Text = $"Tải HĐ ({currentInvoiceIndex}/{invoiceHeaders.Count}), SP trang {currentPage + 1}...";
                        };
                        if (_statusLabel.InvokeRequired) _statusLabel.Invoke(updatePageStatusAction); else updatePageStatusAction();

                        // --- LOG GỠ LỖI MỚI ---
                        logBuilder.AppendLine($"  -> (Detail Mgr) Đang lấy HĐ {invoiceHeader.So_hoa_don}, Trang SP {currentPage + 1} (Page={currentPage}, Size={pageSize})");

                        string jsonDetail = await GetInvoiceDetailInternalAsync(
                            invoiceHeader.Ma_so_thue, invoiceHeader.Ky_hieu_hoa_don,
                            invoiceHeader.So_hoa_don, invoiceHeader.Ky_hieu_ma_so,
                            currentPage, pageSize
                        );

                        if (string.IsNullOrEmpty(jsonDetail) || jsonDetail.StartsWith("❌"))
                        {
                            logBuilder.AppendLine($"  -> ❌ (Detail Mgr) Lỗi lấy chi tiết HĐ {invoiceHeader.So_hoa_don} (Trang {currentPage + 1}): {jsonDetail}");
                            keepFetchingDetails = false;
                        }
                        else
                        {
                            using (JsonDocument doc = JsonDocument.Parse(jsonDetail))
                            {
                                JsonElement root = doc.RootElement;
                                if (root.TryGetProperty("data", out JsonElement dataEl) && dataEl.ValueKind == JsonValueKind.Object) { root = dataEl; }

                                JsonElement? productArray = null;
                                void FindProductArrayLocal(JsonElement element)
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
                                FindProductArrayLocal(root);

                                if (productArray.HasValue && productArray.Value.GetArrayLength() > 0)
                                {
                                    int productsFoundOnThisPage = productArray.Value.GetArrayLength();
                                    // --- LOG GỠ LỖI MỚI ---
                                    logBuilder.AppendLine($"    -> (Detail Mgr) HĐ {invoiceHeader.So_hoa_don} (Trang {currentPage + 1}) tìm thấy: {productsFoundOnThisPage} sản phẩm.");

                                    foreach (var product in productArray.Value.EnumerateArray())
                                    {
                                        if (product.ValueKind == JsonValueKind.Object)
                                        {
                                            var detailItem = new InvoiceDetailItem { /* ... (toàn bộ code gán thuộc tính giữ nguyên) ... */ };
                                            // (Phần gán thuộc tính đã đầy đủ, tôi rút gọn ở đây cho dễ đọc)
                                            detailItem.STT = globalStt;
                                            detailItem.KyHieuHD = GetString(root, "khhdon") ?? invoiceHeader.Ky_hieu_hoa_don;
                                            detailItem.SoHD = GetInt(root, "shdon") ?? invoiceHeader.So_hoa_don;
                                            detailItem.NgayHD = FormatNgayLap(GetString(root, "tdlap")) ?? invoiceHeader.Ngay_lap;
                                            detailItem.TenNguoiBan = GetString(root, "nbten") ?? invoiceHeader.Thong_tin_hoa_don;
                                            detailItem.MSTNguoiBan = GetString(root, "nbmst") ?? invoiceHeader.Ma_so_thue;
                                            detailItem.TongTienThanhToan_HD = GetDecimal(root, "tgtttbso") ?? invoiceHeader.Tong_tien_thanh_toan;
                                            detailItem.TenSanPham = GetString(product, "ten");
                                            detailItem.DonViTinh = GetString(product, "dvtinh");
                                            detailItem.SoLuong = GetDecimal(product, "sluong");
                                            detailItem.DonGia = GetDecimal(product, "dgia");
                                            detailItem.ThanhTien = GetDecimal(product, "thtien");
                                            detailItem.TienChietKhau = GetDecimal(product, "stckhau");
                                            detailItem.DoanhSoChuaThue = GetDecimal(product, "thtcthue");
                                            detailItem.ThueSuat = GetString(product, "tsuat");
                                            detailItem.TienThueGTGT = GetDecimal(product, "tthue");
                                            detailItem.SoLo = GetString(product, "solo");
                                            detailItem.HanSuDung = GetString(product, "hsd");
                                            detailItem.GhiChuSanPham = GetString(product, "ghichu");

                                            allDetailItems.Add(detailItem);
                                            if (invoiceHeader.Thong_tin_lien_quan == "Mua vào")
                                                boughtDetailItems.Add(detailItem);
                                            else
                                                soldDetailItems.Add(detailItem);
                                            vatDetailItems.Add(detailItem);
                                            globalStt++;
                                        }
                                    }

                                    if (productsFoundOnThisPage < pageSize)
                                    {
                                        logBuilder.AppendLine($"    -> (Detail Mgr) HĐ {invoiceHeader.So_hoa_don} đã hết sản phẩm (trả về {productsFoundOnThisPage} < {pageSize}). Dừng.");
                                        keepFetchingDetails = false;
                                    }
                                    else
                                    {
                                        currentPage++;
                                        logBuilder.AppendLine($"    -> (Detail Mgr) HĐ {invoiceHeader.So_hoa_don} có thể còn SP. Chuyển sang trang {currentPage + 1}...");
                                        await Task.Delay(100);
                                    }
                                }
                                else
                                {
                                    logBuilder.AppendLine($"  -> ℹ️ (Detail Mgr) HĐ {invoiceHeader.So_hoa_don} (Trang {currentPage + 1}) không có sản phẩm (hdhhdvu). Dừng.");
                                    keepFetchingDetails = false;
                                }
                            }
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    logBuilder.AppendLine($"❌ (Detail Mgr) Lỗi parse JSON HĐ {invoiceHeader.So_hoa_don}: {jsonEx.Message}");
                }
                catch (Exception ex)
                {
                    logBuilder.AppendLine($"❌ (Detail Mgr) Lỗi khác khi xử lý HĐ {invoiceHeader.So_hoa_don}: {ex.ToString()}");
                }

                MethodInvoker stepProgressFinal = () => { if (_progressBar.Value < _progressBar.Maximum) _progressBar.PerformStep(); };
                if (_progressBar.InvokeRequired) _progressBar.Invoke(stepProgressFinal); else stepProgressFinal();

                await Task.Delay(500);
            }

            MethodInvoker bindDataAction = () => {
                try
                {
                    _dgvDetails.DataSource = new System.ComponentModel.BindingList<InvoiceDetailItem>(allDetailItems);
                    _dgvMua.DataSource = new System.ComponentModel.BindingList<InvoiceDetailItem>(boughtDetailItems);
                    _dgvBan.DataSource = new System.ComponentModel.BindingList<InvoiceDetailItem>(soldDetailItems);
                    _dgvVatNop.DataSource = new System.ComponentModel.BindingList<InvoiceDetailItem>(vatDetailItems);
                    logBuilder.AppendLine($"✅ (Detail Mgr) Đã tải: {allDetailItems.Count} dòng chi tiết tổng, {boughtDetailItems.Count} Mua, {soldDetailItems.Count} Bán, {vatDetailItems.Count} VAT.");
                }
                catch (Exception bindEx)
                {
                    logBuilder.AppendLine($"❌ (Detail Mgr) Lỗi khi gán DataSource: {bindEx.Message}");
                }
                finally
                {
                    _statusLabel.Text = $"Hiển thị {allDetailItems.Count} dòng chi tiết.";
                    _statusLabel.Visible = true;
                    _progressBar.Visible = false;

                    Task.Delay(3000).ContinueWith(t => {
                        if (_statusLabel.InvokeRequired)
                            _statusLabel.Invoke(new Action(() => _statusLabel.Visible = false));
                        else
                            _statusLabel.Visible = false;
                    });
                }
            };
            if (_dgvDetails.InvokeRequired) _dgvDetails.Invoke(bindDataAction); else bindDataAction();
        }

        // =================================================================
        // === HÀM LẤY CHI TIẾT ĐÃ SỬA (THÊM PAGE VÀ SIZE) ===
        // =================================================================
        private async Task<string> GetInvoiceDetailInternalAsync(string nbmst, string khhdon, int? shdon, int? khmshdon, int page = 0, int size = 50)
        {
            if (string.IsNullOrEmpty(nbmst) || string.IsNullOrEmpty(khhdon) || !shdon.HasValue || !khmshdon.HasValue)
            {
                return "❌ Lỗi nội bộ: Thiếu thông tin HĐ.";
            }

            try
            {
                // THAY ĐỔI: Thêm page và size vào URL
                string url = $"query/invoices/detail?" +
                             $"nbmst={Uri.EscapeDataString(nbmst)}&" +
                             $"khhdon={Uri.EscapeDataString(khhdon)}&" +
                             $"shdon={shdon.Value}&" +
                             $"khmshdon={khmshdon.Value}&" +
                             $"page={page}&" + // <-- ĐÃ THÊM
                             $"size={size}";   // <-- ĐÃ THÊM

                // Ghi log URL này vào hàm gọi (Populate...) để logBuilder bắt được

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd(_browserUserAgent);
                req.Headers.Add("Accept", "application/json");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

                var response = await _client.SendAsync(req);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logAction?.Invoke($"❌ (Detail Mgr) Lỗi API chi tiết HĐ {shdon}: {response.StatusCode} - {jsonResponse}");
                    return $"❌ Lỗi từ API ({response.StatusCode}): {jsonResponse}";
                }
                return jsonResponse;
            }
            catch (HttpRequestException httpEx) { _logAction?.Invoke($"❌ (Detail Mgr) Lỗi HTTP khi lấy chi tiết HĐ {shdon}: {httpEx.Message}"); return $"❌ Lỗi kết nối: {httpEx.Message}"; }
            catch (Exception ex) { _logAction?.Invoke($"❌ (Detail Mgr) Lỗi khác khi lấy chi tiết HĐ {shdon}: {ex.ToString()}"); return $"❌ Lỗi hệ thống: {ex.Message}"; }
        }


        // (Các hàm GetString, GetDecimal, GetInt, FormatNgayLap... giữ nguyên)
        private string GetString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop) && prop.ValueKind == JsonValueKind.String) { return prop.GetString(); }
            else if (element.TryGetProperty(propertyName, out prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined) { return prop.ToString(); }
            return null;
        }
        private decimal? GetDecimal(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number) { return prop.GetDecimal(); }
                else if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val)) { return val; }
            }
            return null;
        }
        private int? GetInt(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int intVal)) { return intVal; }
                else if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out int intValFromString)) { return intValFromString; }
            }
            return null;
        }
        private string FormatNgayLap(string ngayLapJson)
        {
            if (DateTime.TryParse(ngayLapJson, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dt))
            {
                return dt.ToString("dd/MM/yyyy");
            }
            else if (DateTime.TryParseExact(ngayLapJson, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                return dt.ToString("dd/MM/yyyy");
            }
            return ngayLapJson;
        }
    }
}