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
    }

    /// <summary>
    /// Class quản lý việc lấy và hiển thị dữ liệu chi tiết hóa đơn lên DataGridView
    /// </summary>
    public class DetailGridManager
    {
        private readonly HttpClient _client;
        private readonly DataGridView _dgvDetails;
        private readonly DataGridView _dgvMua; // Lưới cho BK Mua
        private readonly DataGridView _dgvBan; // Lưới cho BK Bán
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;
        private readonly Action<string> _logAction;
        private readonly string _browserUserAgent;
        private string _jwtToken;

        public DetailGridManager(HttpClient client, DataGridView dgvDetails, DataGridView dgvMua, DataGridView dgvBan, ProgressBar progressBar, Label statusLabel, Action<string> logAction, string userAgent)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _dgvDetails = dgvDetails ?? throw new ArgumentNullException(nameof(dgvDetails));
            _dgvMua = dgvMua ?? throw new ArgumentNullException(nameof(dgvMua));
            _dgvBan = dgvBan ?? throw new ArgumentNullException(nameof(dgvBan));
            _progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
            _statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
            _browserUserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));

            // Gọi hàm cấu hình cột cho từng lưới ngay khi khởi tạo
            ConfigureSpecificGridColumns(_dgvDetails);
            ConfigureSpecificGridColumns(_dgvMua);
            ConfigureSpecificGridColumns(_dgvBan);
        }

        public void SetJwtToken(string token)
        {
            _jwtToken = token;
            _logAction?.Invoke("🔑 Token đã được cập nhật cho DetailGridManager.");
        }

        /// <summary>
        /// Hàm chung để cấu hình các cột cho một lưới chi tiết cụ thể (Mua hoặc Bán)
        /// </summary>
        private void ConfigureSpecificGridColumns(DataGridView dgv) // Nhận dgv làm tham số
        {
            try
            {
                // Sử dụng Invoke để đảm bảo an toàn thread khi cấu hình control từ constructor
                if (dgv.InvokeRequired)
                {
                    dgv.Invoke(new Action(() => ConfigureGridInternal(dgv)));
                }
                else
                {
                    ConfigureGridInternal(dgv);
                }
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"❌ Lỗi khi cấu hình lưới {dgv.Name}: {ex.Message}");
            }
        }

        // Hàm nội bộ để thực hiện cấu hình (tránh lặp code)
        private void ConfigureGridInternal(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

            dgv.Columns.Clear();

            // Thêm các cột (Code giống hệt hàm ConfigureDetailGridColumns cũ)
            // !!! QUAN TRỌNG: Gọi AddTextColumn/AddNumericColumn với dgv được truyền vào !!!
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

        // Hàm trợ giúp thêm cột Text - !!! SỬA: Nhận dgv làm tham số !!!
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
            dgv.Columns.Add(column); // Sử dụng dgv được truyền vào
        }

        // Hàm trợ giúp thêm cột Numeric - !!! SỬA: Nhận dgv làm tham số !!!
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
            dgv.Columns.Add(column); // Sử dụng dgv được truyền vào
        }


        public async Task PopulateDetailGridAsync(List<SearchResult> invoiceHeaders)
        {
            // Xóa dữ liệu cũ và hiển thị trạng thái chờ (cho cả 3 lưới)
            MethodInvoker clearGridsAction = () => {
                _dgvDetails.DataSource = null; // << THÊM LẠI: Xóa lưới Chi tiết
                _dgvMua.DataSource = null;
                _dgvBan.DataSource = null;
                _statusLabel.Text = "Đang chuẩn bị tải chi tiết...";
                _statusLabel.Visible = true;
                _progressBar.Style = ProgressBarStyle.Blocks;
                _progressBar.Maximum = invoiceHeaders?.Count ?? 0; // Đặt Maximum dựa trên số hóa đơn
                _progressBar.Value = 0;
                _progressBar.Visible = true;
            };
            // Chọn một lưới để Invoke
            if (_dgvDetails.InvokeRequired) _dgvDetails.Invoke(clearGridsAction); else clearGridsAction();

            if (invoiceHeaders == null || !invoiceHeaders.Any())
            {
                _logAction?.Invoke("ℹ️ (Detail Mgr) Không có hóa đơn nào để lấy chi tiết.");
                MethodInvoker hideProgress = () => { _statusLabel.Visible = false; _progressBar.Visible = false; };
                if (_statusLabel.InvokeRequired) _statusLabel.Invoke(hideProgress); else hideProgress();
                return;
            }
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _logAction?.Invoke("❌ (Detail Mgr) Lỗi: JWT Token chưa được cung cấp.");
                MethodInvoker hideProgress = () => { _statusLabel.Text = "Lỗi: Chưa đăng nhập."; _progressBar.Visible = false; };
                if (_statusLabel.InvokeRequired) _statusLabel.Invoke(hideProgress); else hideProgress();
                return;
            }

            _logAction?.Invoke($"⚙️ (Detail Mgr) Bắt đầu lấy chi tiết cho {invoiceHeaders.Count} hóa đơn...");

            // !!! TẠO 3 LIST !!!
            var allDetailItems = new List<InvoiceDetailItem>();    // List tổng hợp
            var boughtDetailItems = new List<InvoiceDetailItem>(); // List Mua
            var soldDetailItems = new List<InvoiceDetailItem>();   // List Bán
                                                                   // ==================
            int currentInvoiceIndex = 0;
            int globalStt = 1;

            foreach (var invoiceHeader in invoiceHeaders)
            {
                currentInvoiceIndex++;
                MethodInvoker updateStatusAction = () => {
                    _statusLabel.Text = $"Đang tải chi tiết ({currentInvoiceIndex}/{invoiceHeaders.Count})...";
                };
                if (_statusLabel.InvokeRequired) _statusLabel.Invoke(updateStatusAction); else updateStatusAction();

                // Kiểm tra thông tin header
                if (invoiceHeader == null || string.IsNullOrEmpty(invoiceHeader.Ma_so_thue) ||
                   string.IsNullOrEmpty(invoiceHeader.Ky_hieu_hoa_don) ||
                   !invoiceHeader.So_hoa_don.HasValue ||
                   !invoiceHeader.Ky_hieu_ma_so.HasValue)
                {
                    _logAction?.Invoke($"⚠️ (Detail Mgr) Bỏ qua HĐ {invoiceHeader?.Id} do thiếu thông tin.");
                    MethodInvoker stepProgress = () => { if (_progressBar.Value < _progressBar.Maximum) _progressBar.PerformStep(); };
                    if (_progressBar.InvokeRequired) _progressBar.Invoke(stepProgress); else stepProgress();
                    continue;
                }

                try
                {
                    // Lấy JSON chi tiết
                    string jsonDetail = await GetInvoiceDetailInternalAsync(
                        invoiceHeader.Ma_so_thue, invoiceHeader.Ky_hieu_hoa_don,
                        invoiceHeader.So_hoa_don, invoiceHeader.Ky_hieu_ma_so
                    );

                    // Kiểm tra lỗi API
                    if (string.IsNullOrEmpty(jsonDetail) || jsonDetail.StartsWith("❌"))
                    {
                        _logAction?.Invoke($"❌ (Detail Mgr) Lỗi lấy chi tiết HĐ {invoiceHeader.So_hoa_don}: {jsonDetail}");
                        MethodInvoker stepProgress = () => { if (_progressBar.Value < _progressBar.Maximum) _progressBar.PerformStep(); };
                        if (_progressBar.InvokeRequired) _progressBar.Invoke(stepProgress); else stepProgress();
                        continue;
                    }

                    // Parse JSON và xử lý sản phẩm
                    using (JsonDocument doc = JsonDocument.Parse(jsonDetail))
                    {
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("data", out JsonElement dataEl) && dataEl.ValueKind == JsonValueKind.Object) { root = dataEl; }

                        JsonElement? productArray = null;
                        void FindProductArrayLocal(JsonElement element) 
                        {
                            if (productArray.HasValue) return; // Đã tìm thấy, dừng lại

                            if (element.ValueKind == JsonValueKind.Object)
                            {
                                // Thử tìm "hdhhdvu" ở cấp hiện tại
                                if (element.TryGetProperty("hdhhdvu", out var hdhhdvu) && hdhhdvu.ValueKind == JsonValueKind.Array)
                                {
                                    productArray = hdhhdvu;
                                    return;
                                }
                                // Nếu không thấy, tìm đệ quy vào các thuộc tính con
                                foreach (var innerProp in element.EnumerateObject())
                                {
                                    if (innerProp.Value.ValueKind == JsonValueKind.Object || innerProp.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        FindProductArrayLocal(innerProp.Value);
                                    }
                                }
                            }
                            else if (element.ValueKind == JsonValueKind.Array)
                            {
                                // Tìm đệ quy trong các phần tử mảng
                                foreach (var item in element.EnumerateArray())
                                {
                                    FindProductArrayLocal(item);
                                }
                            }
                        } // Hàm tìm 'hdhhdvu' giữ nguyên
                        FindProductArrayLocal(root);

                        if (productArray.HasValue && productArray.Value.GetArrayLength() > 0)
                        {
                            foreach (var product in productArray.Value.EnumerateArray())
                            {
                                if (product.ValueKind == JsonValueKind.Object)
                                {
                                    // Tạo đối tượng chi tiết
                                    var detailItem = new InvoiceDetailItem
                                    {
                                        STT = globalStt, // Dùng STT chung
                                        KyHieuHD = GetString(root, "khhdon") ?? invoiceHeader.Ky_hieu_hoa_don,
                                        SoHD = GetInt(root, "shdon") ?? invoiceHeader.So_hoa_don,
                                        NgayHD = FormatNgayLap(GetString(root, "tdlap")) ?? invoiceHeader.Ngay_lap,
                                        TenNguoiBan = GetString(root, "nbten") ?? invoiceHeader.Thong_tin_hoa_don,
                                        MSTNguoiBan = GetString(root, "nbmst") ?? invoiceHeader.Ma_so_thue,
                                        TongTienThanhToan_HD = GetDecimal(root, "tgtttbso") ?? invoiceHeader.Tong_tien_thanh_toan,
                                        TenSanPham = GetString(product, "ten"),
                                        DonViTinh = GetString(product, "dvtinh"),
                                        SoLuong = GetDecimal(product, "sluong"),
                                        DonGia = GetDecimal(product, "dgia"),
                                        ThanhTien = GetDecimal(product, "thtien"),
                                        TienChietKhau = GetDecimal(product, "stckhau"),
                                        DoanhSoChuaThue = GetDecimal(product, "thtcthue"),
                                        ThueSuat = GetString(product, "tsuat"),
                                        TienThueGTGT = GetDecimal(product, "tthue"),
                                        SoLo = GetString(product, "solo"),
                                        HanSuDung = GetString(product, "hsd"),
                                        GhiChuSanPham = GetString(product, "ghichu")
                                    };

                                    // !!! THÊM VÀO CẢ 3 LIST (nếu phù hợp) !!!
                                    allDetailItems.Add(detailItem); // Luôn thêm vào list tổng hợp

                                    if (invoiceHeader.Thong_tin_lien_quan == "Mua vào")
                                    {
                                        boughtDetailItems.Add(detailItem);
                                    }
                                    else // Mặc định là "Bán ra"
                                    {
                                        soldDetailItems.Add(detailItem);
                                    }
                                    // =====================================
                                    globalStt++; // Tăng STT sau khi dùng
                                }
                            }
                        }
                        else { _logAction?.Invoke($"ℹ️ (Detail Mgr) HĐ {invoiceHeader.So_hoa_don} không có sản phẩm (hdhhdvu)."); }
                    }
                }
                catch (JsonException jsonEx) { _logAction?.Invoke($"❌ (Detail Mgr) Lỗi parse JSON HĐ {invoiceHeader.So_hoa_don}: {jsonEx.Message}"); }
                catch (Exception ex) { _logAction?.Invoke($"❌ (Detail Mgr) Lỗi khác khi xử lý HĐ {invoiceHeader.So_hoa_don}: {ex.ToString()}"); }

                // Cập nhật progress bar
                MethodInvoker stepProgressFinal = () => { if (_progressBar.Value < _progressBar.Maximum) _progressBar.PerformStep(); };
                if (_progressBar.InvokeRequired) _progressBar.Invoke(stepProgressFinal); else stepProgressFinal();

                await Task.Delay(50); // Delay nhỏ
            } // Kết thúc foreach

            // !!! GÁN DATASOURCE CHO CẢ 3 LƯỚI !!!
            MethodInvoker bindDataAction = () => {
                try
                {
                    _dgvDetails.DataSource = new System.ComponentModel.BindingList<InvoiceDetailItem>(allDetailItems); // << GÁN LIST TỔNG HỢP
                    _dgvMua.DataSource = new System.ComponentModel.BindingList<InvoiceDetailItem>(boughtDetailItems);
                    _dgvBan.DataSource = new System.ComponentModel.BindingList<InvoiceDetailItem>(soldDetailItems);
                    _logAction?.Invoke($"✅ (Detail Mgr) Đã tải: {allDetailItems.Count} dòng chi tiết tổng, {boughtDetailItems.Count} Mua, {soldDetailItems.Count} Bán.");
                }
                catch (Exception bindEx)
                {
                    _logAction?.Invoke($"❌ (Detail Mgr) Lỗi khi gán DataSource: {bindEx.Message}");
                }
                finally
                {
                    _statusLabel.Visible = false;
                    _progressBar.Visible = false;
                }
            };
            // Chọn 1 lưới bất kỳ để Invoke
            if (_dgvDetails.InvokeRequired) _dgvDetails.Invoke(bindDataAction); else bindDataAction();
            // ====================================
        }
        private async Task<string> GetInvoiceDetailInternalAsync(string nbmst, string khhdon, int? shdon, int? khmshdon)
        {
            if (string.IsNullOrEmpty(nbmst) || string.IsNullOrEmpty(khhdon) || !shdon.HasValue || !khmshdon.HasValue)
            {
                return "❌ Lỗi nội bộ: Thiếu thông tin HĐ.";
            }

            try
            {
                string url = $"query/invoices/detail?" +
                             $"nbmst={Uri.EscapeDataString(nbmst)}&" +
                             $"khhdon={Uri.EscapeDataString(khhdon)}&" +
                             $"shdon={shdon.Value}&" +
                             $"khmshdon={khmshdon.Value}";

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
            catch (Exception ex) { _logAction?.Invoke($"❌ (Detail Mgr) Lỗi khác khi lấy chi tiết HĐ {shdon}: {ex.ToString()}"); return $"❌ Lỗi hệ thống: {ex.Message}"; } // Log full exception
        }


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
            // Thử parse theo nhiều định dạng phổ biến nếu cần
            if (DateTime.TryParse(ngayLapJson, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dt))
            {
                return dt.ToString("dd/MM/yyyy"); // Chỉ lấy ngày tháng năm
            }
            // Thử parse nếu có dạng yyyyMMddHHmmss
            else if (DateTime.TryParseExact(ngayLapJson, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                return dt.ToString("dd/MM/yyyy");
            }
            return ngayLapJson; // Trả về gốc nếu không parse được
        }
    }
}