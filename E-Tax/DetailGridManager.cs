using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E_Tax
{
    /// <summary>
    /// Một dòng hàng hóa/dịch vụ của hóa đơn, dùng cho các lưới chi tiết.
    /// </summary>
    public class InvoiceDetailItem
    {
        [DisplayName("STT")]
        public int? STT { get; set; }
        [DisplayName("Ký hiệu")]
        public string KyHieuHD { get; set; }
        [DisplayName("Số HĐ")]
        public int? SoHD { get; set; }
        [DisplayName("Ngày HĐ")]
        public string NgayHD { get; set; }
        [DisplayName("Tên NB")]
        public string TenNguoiBan { get; set; }
        [DisplayName("MST NB")]
        public string MSTNguoiBan { get; set; }
        [DisplayName("Tên SP/DV")]
        public string TenSanPham { get; set; }
        [DisplayName("ĐVT")]
        public string DonViTinh { get; set; }
        [DisplayName("Số lượng")]
        public decimal? SoLuong { get; set; }
        [DisplayName("Đơn giá")]
        public decimal? DonGia { get; set; }
        [DisplayName("Thành tiền")]
        public decimal? ThanhTien { get; set; }
        [DisplayName("Tiền CK")]
        public decimal? TienChietKhau { get; set; }
        [DisplayName("Doanh số")]
        public decimal? DoanhSoChuaThue { get; set; }
        [DisplayName("Thuế suất")]
        public string ThueSuat { get; set; }
        [DisplayName("Tiền thuế")]
        public decimal? TienThueGTGT { get; set; }
        [DisplayName("Tổng TT (HĐ)")]
        public decimal? TongTienThanhToan_HD { get; set; }
        [DisplayName("Số lô")]
        public string SoLo { get; set; }
        [DisplayName("Hạn SD")]
        public string HanSuDung { get; set; }
        [DisplayName("Ghi chú SP")]
        public string GhiChuSanPham { get; set; }
    }

    /// <summary>
    /// Lấy chi tiết hàng hóa của danh sách hóa đơn và đổ vào các lưới: Chi tiết, BK mua, BK bán, Bảng kê thuế.
    /// </summary>
    public class DetailGridManager
    {
        private const int ProductPageSize = 50;

        private readonly HttpClient _client;
        private readonly DataGridView _dgvDetails;
        private readonly DataGridView _dgvMua;
        private readonly DataGridView _dgvBan;
        private readonly DataGridView _dgvVatNop;
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;
        private readonly Action<string> _logAction;
        private string _jwtToken;

        public DetailGridManager(HttpClient client, DataGridView dgvDetails, DataGridView dgvMua,
            DataGridView dgvBan, DataGridView dgvVatNop, ProgressBar progressBar, Label statusLabel,
            Action<string> logAction)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _dgvDetails = dgvDetails ?? throw new ArgumentNullException(nameof(dgvDetails));
            _dgvMua = dgvMua ?? throw new ArgumentNullException(nameof(dgvMua));
            _dgvBan = dgvBan ?? throw new ArgumentNullException(nameof(dgvBan));
            _dgvVatNop = dgvVatNop ?? throw new ArgumentNullException(nameof(dgvVatNop));
            _progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
            _statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));

            ConfigureFullDetailGrid(_dgvDetails);
            ConfigureFullDetailGrid(_dgvMua);
            ConfigureFullDetailGrid(_dgvBan);
            ConfigureVatNopGrid(_dgvVatNop);
        }

        public void SetJwtToken(string token)
        {
            _jwtToken = token;
            _logAction("🔑 Token đã được cập nhật cho DetailGridManager.");
        }

        private static void ApplyReadOnlyStyle(DataGridView dgv)
        {
            GridColumnHelper.ApplyDefaultStyle(dgv);
            dgv.AllowUserToAddRows = false;
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
        }

        private static void ConfigureFullDetailGrid(DataGridView dgv)
        {
            ApplyReadOnlyStyle(dgv);
            GridColumnHelper.AddTextColumn(dgv, "colDetailSTT", "STT", nameof(InvoiceDetailItem.STT), 40, frozen: true);
            GridColumnHelper.AddTextColumn(dgv, "colDetailKyHieu", "Ký hiệu", nameof(InvoiceDetailItem.KyHieuHD), 80);
            GridColumnHelper.AddTextColumn(dgv, "colDetailSoHD", "Số HĐ", nameof(InvoiceDetailItem.SoHD), 80);
            GridColumnHelper.AddTextColumn(dgv, "colDetailNgayHD", "Ngày HĐ", nameof(InvoiceDetailItem.NgayHD), 90);
            GridColumnHelper.AddTextColumn(dgv, "colDetailTenNB", "Tên NB", nameof(InvoiceDetailItem.TenNguoiBan), 200);
            GridColumnHelper.AddTextColumn(dgv, "colDetailMSTNB", "MST NB", nameof(InvoiceDetailItem.MSTNguoiBan), 110);
            GridColumnHelper.AddTextColumn(dgv, "colDetailTenSP", "Tên SP/DV", nameof(InvoiceDetailItem.TenSanPham), 250);
            GridColumnHelper.AddTextColumn(dgv, "colDetailDVT", "ĐVT", nameof(InvoiceDetailItem.DonViTinh), 50);
            GridColumnHelper.AddNumericColumn(dgv, "colDetailSoLuong", "Số lượng", nameof(InvoiceDetailItem.SoLuong), 80, "#,##0.##");
            GridColumnHelper.AddNumericColumn(dgv, "colDetailDonGia", "Đơn giá", nameof(InvoiceDetailItem.DonGia), 100, "#,##0.##");
            GridColumnHelper.AddNumericColumn(dgv, "colDetailThanhTien", "Thành tiền", nameof(InvoiceDetailItem.ThanhTien), 110, "#,##0");
            GridColumnHelper.AddNumericColumn(dgv, "colDetailTienCK", "Tiền CK", nameof(InvoiceDetailItem.TienChietKhau), 90, "#,##0");
            GridColumnHelper.AddNumericColumn(dgv, "colDetailDoanhSo", "Doanh số", nameof(InvoiceDetailItem.DoanhSoChuaThue), 110, "#,##0");
            GridColumnHelper.AddTextColumn(dgv, "colDetailThueSuat", "Thuế suất", nameof(InvoiceDetailItem.ThueSuat), 60);
            GridColumnHelper.AddNumericColumn(dgv, "colDetailTienThue", "Tiền thuế", nameof(InvoiceDetailItem.TienThueGTGT), 100, "#,##0");
            GridColumnHelper.AddNumericColumn(dgv, "colDetailTongTT", "Tổng TT (HĐ)", nameof(InvoiceDetailItem.TongTienThanhToan_HD), 120, "#,##0");
            GridColumnHelper.AddTextColumn(dgv, "colDetailSoLo", "Số lô", nameof(InvoiceDetailItem.SoLo), 80);
            GridColumnHelper.AddTextColumn(dgv, "colDetailHSD", "Hạn SD", nameof(InvoiceDetailItem.HanSuDung), 90);
            GridColumnHelper.AddTextColumn(dgv, "colDetailGhiChuSP", "Ghi chú SP", nameof(InvoiceDetailItem.GhiChuSanPham), 150);
        }

        private static void ConfigureVatNopGrid(DataGridView dgv)
        {
            ApplyReadOnlyStyle(dgv);
            GridColumnHelper.AddTextColumn(dgv, "colVatSTT", "STT", nameof(InvoiceDetailItem.STT), 40, frozen: true);
            GridColumnHelper.AddTextColumn(dgv, "colVatKyHieu", "Ký hiệu", nameof(InvoiceDetailItem.KyHieuHD), 80);
            GridColumnHelper.AddTextColumn(dgv, "colVatSoHD", "Số HĐ", nameof(InvoiceDetailItem.SoHD), 80);
            GridColumnHelper.AddTextColumn(dgv, "colVatNgayHD", "Ngày HĐ", nameof(InvoiceDetailItem.NgayHD), 90);
            GridColumnHelper.AddTextColumn(dgv, "colVatMSTNB", "MST NB", nameof(InvoiceDetailItem.MSTNguoiBan), 110);
            GridColumnHelper.AddTextColumn(dgv, "colVatTenSP", "Tên SP/DV", nameof(InvoiceDetailItem.TenSanPham), 300);
            GridColumnHelper.AddNumericColumn(dgv, "colVatDoanhSo", "Doanh số", nameof(InvoiceDetailItem.DoanhSoChuaThue), 150, "#,##0");
            GridColumnHelper.AddTextColumn(dgv, "colVatThueSuat", "Thuế suất", nameof(InvoiceDetailItem.ThueSuat), 80);
            GridColumnHelper.AddNumericColumn(dgv, "colVatTienThue", "Tiền thuế", nameof(InvoiceDetailItem.TienThueGTGT), 150, "#,##0");
        }

        private void StepProgress()
        {
            if (_progressBar.Value < _progressBar.Maximum) _progressBar.PerformStep();
        }

        /// <summary>
        /// Gọi API chi tiết cho từng hóa đơn (có phân trang sản phẩm) và bind kết quả vào 4 lưới.
        /// Phải được gọi từ UI thread.
        /// </summary>
        public async Task PopulateDetailGridAsync(List<SearchResult> invoiceHeaders)
        {
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

            if (invoiceHeaders == null || !invoiceHeaders.Any())
            {
                _logAction("ℹ️ (Detail Mgr) Không có hóa đơn nào để lấy chi tiết.");
                _statusLabel.Visible = false;
                _progressBar.Visible = false;
                return;
            }

            if (string.IsNullOrEmpty(_jwtToken))
            {
                _logAction("❌ (Detail Mgr) Lỗi: JWT Token chưa được cung cấp.");
                _statusLabel.Text = "Lỗi: Chưa đăng nhập.";
                _progressBar.Visible = false;
                return;
            }

            _logAction($"⚙️ (Detail Mgr) Bắt đầu lấy chi tiết cho {invoiceHeaders.Count} hóa đơn...");
            var allDetailItems = new List<InvoiceDetailItem>();
            var boughtDetailItems = new List<InvoiceDetailItem>();
            var soldDetailItems = new List<InvoiceDetailItem>();
            int currentInvoiceIndex = 0;
            int globalStt = 1;

            foreach (var invoiceHeader in invoiceHeaders)
            {
                currentInvoiceIndex++;
                _statusLabel.Text = $"Đang tải chi tiết HĐ ({currentInvoiceIndex}/{invoiceHeaders.Count})...";

                if (invoiceHeader == null || !invoiceHeader.HasLookupKeys())
                {
                    _logAction($"⚠️ (Detail Mgr) Bỏ qua HĐ {invoiceHeader?.Id} do thiếu thông tin.");
                    StepProgress();
                    continue;
                }

                int currentPage = 0;
                bool keepFetchingDetails = true;
                try
                {
                    while (keepFetchingDetails)
                    {
                        _statusLabel.Text = $"Tải HĐ ({currentInvoiceIndex}/{invoiceHeaders.Count}), SP trang {currentPage + 1}...";

                        string jsonDetail = await GetInvoiceDetailInternalAsync(invoiceHeader, currentPage, ProductPageSize);
                        if (string.IsNullOrEmpty(jsonDetail) || jsonDetail.StartsWith("❌"))
                        {
                            _logAction($"  -> ❌ (Detail Mgr) Lỗi lấy chi tiết HĐ {invoiceHeader.So_hoa_don} (Trang {currentPage + 1}): {jsonDetail}");
                            break;
                        }

                        using JsonDocument doc = JsonDocument.Parse(jsonDetail);
                        JsonElement root = InvoiceJson.GetDataElement(doc.RootElement);
                        JsonElement? productArray = InvoiceJson.FindProductArray(root);

                        if (!productArray.HasValue || productArray.Value.GetArrayLength() == 0)
                        {
                            _logAction($"  -> ℹ️ (Detail Mgr) HĐ {invoiceHeader.So_hoa_don} (Trang {currentPage + 1}) không có sản phẩm (hdhhdvu).");
                            break;
                        }

                        int productsFoundOnThisPage = productArray.Value.GetArrayLength();
                        foreach (var product in productArray.Value.EnumerateArray())
                        {
                            if (product.ValueKind != JsonValueKind.Object) continue;

                            var detailItem = CreateDetailItem(root, product, invoiceHeader);
                            detailItem.STT = globalStt++;
                            allDetailItems.Add(detailItem);
                            if (invoiceHeader.Thong_tin_lien_quan == "Mua vào")
                                boughtDetailItems.Add(detailItem);
                            else
                                soldDetailItems.Add(detailItem);
                        }

                        if (productsFoundOnThisPage < ProductPageSize)
                        {
                            keepFetchingDetails = false;
                        }
                        else
                        {
                            currentPage++;
                            await Task.Delay(100);
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logAction($"❌ (Detail Mgr) Lỗi parse JSON HĐ {invoiceHeader.So_hoa_don}: {jsonEx.Message}");
                }
                catch (Exception ex)
                {
                    _logAction($"❌ (Detail Mgr) Lỗi khác khi xử lý HĐ {invoiceHeader.So_hoa_don}: {ex}");
                }

                StepProgress();
                await Task.Delay(500);
            }

            try
            {
                _dgvDetails.DataSource = new BindingList<InvoiceDetailItem>(allDetailItems);
                _dgvMua.DataSource = new BindingList<InvoiceDetailItem>(boughtDetailItems);
                _dgvBan.DataSource = new BindingList<InvoiceDetailItem>(soldDetailItems);
                // Bảng kê thuế hiển thị tất cả dòng hàng
                _dgvVatNop.DataSource = new BindingList<InvoiceDetailItem>(allDetailItems.ToList());
                _logAction($"✅ (Detail Mgr) Đã tải: {allDetailItems.Count} dòng chi tiết tổng, {boughtDetailItems.Count} Mua, {soldDetailItems.Count} Bán.");
            }
            catch (Exception bindEx)
            {
                _logAction($"❌ (Detail Mgr) Lỗi khi gán DataSource: {bindEx.Message}");
            }
            finally
            {
                _statusLabel.Text = $"Hiển thị {allDetailItems.Count} dòng chi tiết.";
                _statusLabel.Visible = true;
                _progressBar.Visible = false;
                _ = HideStatusLabelLaterAsync();
            }
        }

        private async Task HideStatusLabelLaterAsync()
        {
            await Task.Delay(3000);
            _statusLabel.Visible = false;
        }

        private InvoiceDetailItem CreateDetailItem(JsonElement invoice, JsonElement product, SearchResult header)
        {
            return new InvoiceDetailItem
            {
                KyHieuHD = GetString(invoice, "khhdon") ?? header.Ky_hieu_hoa_don,
                SoHD = GetInt(invoice, "shdon") ?? header.So_hoa_don,
                NgayHD = FormatNgayLap(GetString(invoice, "tdlap")) ?? header.Ngay_lap,
                TenNguoiBan = GetString(invoice, "nbten") ?? header.Thong_tin_hoa_don,
                MSTNguoiBan = GetString(invoice, "nbmst") ?? header.Ma_so_thue,
                TongTienThanhToan_HD = GetDecimal(invoice, "tgtttbso") ?? header.Tong_tien_thanh_toan,
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
        }

        private async Task<string> GetInvoiceDetailInternalAsync(SearchResult invoice, int page, int size)
        {
            try
            {
                string url = GdtApi.BuildDetailUrl(invoice.Ma_so_thue, invoice.Ky_hieu_hoa_don,
                    invoice.So_hoa_don!.Value, invoice.Ky_hieu_ma_so!.Value, page, size);
                using var req = GdtApi.CreateRequest(HttpMethod.Get, url, "application/json", _jwtToken);

                var response = await _client.SendAsync(req);
                string jsonResponse = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logAction($"❌ (Detail Mgr) Lỗi API chi tiết HĐ {invoice.So_hoa_don}: {response.StatusCode} - {jsonResponse}");
                    return $"❌ Lỗi từ API ({response.StatusCode}): {jsonResponse}";
                }
                return jsonResponse;
            }
            catch (HttpRequestException httpEx)
            {
                _logAction($"❌ (Detail Mgr) Lỗi HTTP khi lấy chi tiết HĐ {invoice.So_hoa_don}: {httpEx.Message}");
                return $"❌ Lỗi kết nối: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                _logAction($"❌ (Detail Mgr) Lỗi khác khi lấy chi tiết HĐ {invoice.So_hoa_don}: {ex}");
                return $"❌ Lỗi hệ thống: {ex.Message}";
            }
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement prop)) return null;
            if (prop.ValueKind == JsonValueKind.String) return prop.GetString();
            if (prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined) return prop.ToString();
            return null;
        }

        private static decimal? GetDecimal(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number) return prop.GetDecimal();
                if (prop.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val)) return val;
            }
            return null;
        }

        private static int? GetInt(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int intVal)) return intVal;
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out int intValFromString)) return intValFromString;
            }
            return null;
        }

        private static string FormatNgayLap(string ngayLapJson)
        {
            if (DateTime.TryParse(ngayLapJson, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dt))
                return dt.ToString("dd/MM/yyyy");
            if (DateTime.TryParseExact(ngayLapJson, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("dd/MM/yyyy");
            return ngayLapJson;
        }
    }
}
