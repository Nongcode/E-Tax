using System;
using System.Collections.Generic;
using System.Linq;

namespace E_Tax
{
    /// <summary>Một dòng bảng kê: một hóa đơn với một thuế suất.</summary>
    internal sealed record TaxListingRow(string KyHieu, int? SoHD, string NgayHD, string TenDoiTac, string MstDoiTac,
        string ThueSuat, decimal DoanhSo, decimal TienThue, int? TrangThai, bool ThieuSoTien, string InvoiceKey = "")
    {
        /// <summary>Khóa đếm hóa đơn: một hóa đơn nhiều thuế suất là nhiều dòng nhưng chỉ một hóa đơn.</summary>
        public string CountKey => InvoiceKey.Length > 0 ? InvoiceKey : $"{MstDoiTac}|{KyHieu}|{SoHD}";
    }

    /// <summary>Nhóm các dòng bảng kê cùng thuế suất, kèm tổng.</summary>
    internal sealed record TaxListingGroup(string ThueSuat, IReadOnlyList<TaxListingRow> Rows, decimal TongDoanhSo, decimal TongThue);

    /// <summary>Dựng bảng kê mua vào/bán ra theo thuế suất từ các dòng hàng của hóa đơn.</summary>
    internal static class TaxListing
    {
        public const string UnknownRate = "Không rõ";
        private const string Bought = "Mua vào";
        private const string Sold = "Bán ra";

        /// <summary>Nhãn chiều hóa đơn dùng trong dữ liệu.</summary>
        public static string DirectionLabel(InvoiceType type) => type == InvoiceType.Bought ? Bought : Sold;

        /// <summary>
        /// Chuẩn hóa thuế suất: có dấu % thì lấy giá trị là phần trăm; số trần 0 &lt; x &lt; 1 là tỷ lệ (x*100%),
        /// 0 là 0%, x &gt;= 1 là x%. Chấp nhận dấu . hoặc , thập phân. KCT/KKKNT giữ nguyên; rỗng thành "Không rõ";
        /// giá trị lạ giữ nguyên (viết hoa) thành nhóm riêng.
        /// </summary>
        public static string NormalizeRate(string? rate)
        {
            string r = (rate ?? "").Trim().ToUpperInvariant();
            if (r.Length == 0) return UnknownRate;
            if (IsNoTaxRate(r)) return r;

            bool percent = r.EndsWith("%");
            string num = (percent ? r.Substring(0, r.Length - 1) : r).Trim().Replace(',', '.');
            if (!decimal.TryParse(num, System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal x))
                return r;
            if (!percent && x > 0m && x < 1m) x *= 100m;
            return x.ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>Số hóa đơn thuộc loại đã chọn có đơn vị tiền tệ khác VND (rỗng được coi là VND).</summary>
        public static int CountNonVnd(IEnumerable<SearchResult> invoices, InvoiceType type)
        {
            string loai = type == InvoiceType.Bought ? Bought : Sold;
            return invoices.Count(i =>
            {
                if (i == null || i.Loai_hoa_don != loai) return false;
                string c = (i.Don_vi_tien_te ?? "").Trim();
                return c.Length > 0 && !c.Equals("VND", StringComparison.OrdinalIgnoreCase)
                    && !c.Equals("VNĐ", StringComparison.OrdinalIgnoreCase);
            });
        }

        private static bool IsNoTaxRate(string rate) => rate == "KCT" || rate == "KKKNT";

        private static int RateRank(string rate) => rate switch
        {
            "KCT" => 0,
            "KKKNT" => 1,
            "0%" => 2,
            "5%" => 3,
            "8%" => 4,
            "10%" => 5,
            UnknownRate => 7,
            _ => 6
        };

        /// <summary>
        /// Chỉ lấy HĐ được tính thuế (TaxableInvoices), gộp theo (InvoiceKey, thuế suất) rồi nhóm theo thuế suất.
        /// Số tiền null tính là 0 khi cộng nhưng dòng đó được đánh dấu ThieuSoTien.
        /// </summary>
        public static IReadOnlyList<TaxListingGroup> Build(IEnumerable<InvoiceDetailItem> items, InvoiceType type)
        {
            string loai = type == InvoiceType.Bought ? Bought : Sold;

            var rows = TaxableInvoices.Select(items, null, loai).Items
                .GroupBy(i => (Key: i.InvoiceKey ?? "", Rate: NormalizeRate(i.ThueSuat)))
                .Select(g => BuildRow(g.ToList(), g.Key.Rate, type))
                .ToList();

            return rows
                .GroupBy(r => r.Row.ThueSuat)
                .OrderBy(g => RateRank(g.Key))
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g =>
                {
                    var sorted = g
                        .OrderBy(r => r.Date ?? DateTime.MaxValue)
                        .ThenBy(r => r.Row.KyHieu, StringComparer.Ordinal)
                        .ThenBy(r => r.Row.SoHD ?? int.MaxValue)
                        .Select(r => r.Row)
                        .ToList();
                    return new TaxListingGroup(g.Key, sorted, sorted.Sum(r => r.DoanhSo), sorted.Sum(r => r.TienThue));
                })
                .ToList();
        }

        private static (TaxListingRow Row, DateTime? Date) BuildRow(List<InvoiceDetailItem> lines, string rate, InvoiceType type)
        {
            var first = lines[0];
            bool thieu = lines.Any(l => l.DoanhSoChuaThue == null || (l.TienThueGTGT == null && !IsNoTaxRate(rate)));
            // Mua vào: đối tác là người bán. Bán ra: đối tác là người mua, thiếu thì để trống (không lấy người bán).
            string ten = (type == InvoiceType.Bought ? first.TenNguoiBan : first.TenNguoiMua) ?? "";
            string mst = (type == InvoiceType.Bought ? first.MSTNguoiBan : first.MSTNguoiMua) ?? "";
            var row = new TaxListingRow(first.KyHieuHD ?? "", first.SoHD, first.NgayHD ?? "", ten, mst, rate,
                lines.Sum(l => l.DoanhSoChuaThue ?? 0m), lines.Sum(l => l.TienThueGTGT ?? 0m), first.TrangThaiHD, thieu,
                string.IsNullOrEmpty(first.InvoiceKey) ? "" : $"{first.LoaiHoaDon}|{first.InvoiceKey}");
            return (row, InvoiceDate.ToVietnamDate(first.NgayHD));
        }

        /// <summary>Số hóa đơn thay thế/điều chỉnh/bị điều chỉnh thực sự có trong các dòng (đếm hóa đơn, không đếm dòng).</summary>
        public static int CountAdjustmentInvoices(IEnumerable<TaxListingRow> rows) =>
            rows.Where(r => TaxableInvoices.IsAdjustment(r.TrangThai)).Select(r => r.CountKey).Distinct().Count();

        /// <summary>Ghi chú ghi vào file bảng kê: hóa đơn thay thế/điều chỉnh đã tính vào tổng và dòng thiếu số tiền.</summary>
        public static IReadOnlyList<string> ExportNotes(IReadOnlyList<TaxListingGroup> groups)
        {
            var rows = groups.SelectMany(g => g.Rows).ToList();
            var notes = new List<string>();
            int adjustments = CountAdjustmentInvoices(rows);
            if (adjustments > 0)
                notes.Add(TaxableInvoices.AdjustmentWarning(adjustments));
            int missing = rows.Count(r => r.ThieuSoTien);
            if (missing > 0)
                notes.Add($"Có {missing} dòng thiếu số tiền (tô đỏ). Số tiền thiếu được tính là 0 trong các tổng nên tổng có thể sai lệch.");
            return notes;
        }

        /// <summary>Các cảnh báo cần người dùng xác nhận trước khi xuất; rỗng thì không cần hỏi.</summary>
        public static IReadOnlyList<string> ExportWarnings(IReadOnlyList<TaxListingGroup> groups, int noLineCount, int nonVndCount)
        {
            var warnings = new List<string>(ExportNotes(groups));
            if (noLineCount > 0)
                warnings.Add($"Có {noLineCount} hóa đơn không có dòng hàng hóa/dịch vụ nên sẽ không có trong bảng kê.");
            if (nonVndCount > 0)
                warnings.Add($"Có {nonVndCount} hóa đơn không tính bằng VND. Bảng kê cộng nguyên số tiền của các hóa đơn này vào tổng, không quy đổi.");
            return warnings;
        }

        /// <summary>Lý do không được xuất bảng kê (có hóa đơn thiếu chi tiết); null nếu được phép.</summary>
        public static string? ExportBlockReason(int failedInvoices) =>
            failedInvoices > 0
                ? $"Có {failedInvoices} hóa đơn/trang kết quả chưa lấy đầy đủ dữ liệu (lỗi lấy chi tiết, thiếu thông tin hoặc lỗi trang tìm kiếm). " +
                  "Bảng kê sẽ thiếu số liệu nên không được xuất. Hãy tìm kiếm lại cho đến khi không còn lỗi."
                : null;
    }
}
