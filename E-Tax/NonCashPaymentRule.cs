using System.Globalization;

namespace E_Tax
{
    internal sealed record NonCashGroup(string SellerMst, DateTime Date, decimal Total, IReadOnlyList<SearchResult> Invoices);

    /// <summary>
    /// HĐ mua vào từ 5.000.000 đồng (đã gồm thuế) chỉ được khấu trừ khi có chứng từ thanh toán không dùng tiền mặt;
    /// các HĐ cùng người bán, lập cùng ngày được cộng gộp (không tách nhỏ để né ngưỡng). Cổng không cho biết cách thanh toán
    /// nên app chỉ cảnh báo, không tự loại khỏi tờ khai.
    /// </summary>
    internal static class NonCashPaymentRule
    {
        public const decimal Threshold = 5_000_000m;

        public static IReadOnlyList<NonCashGroup> Find(IEnumerable<SearchResult> headers)
        {
            string purchase = TaxListing.DirectionLabel(InvoiceType.Bought);
            return headers
                .Where(h => h != null && h.Loai_hoa_don == purchase && TaxableInvoices.IsIncluded(h.Trang_thai_hoa_don)
                    && h.Ky_hieu_ma_so != 2 && InvoiceDate.ToVietnamDate(h.Ngay_lap) != null)
                .GroupBy(h => (Seller: h.Ma_so_thue ?? "", Date: InvoiceDate.ToVietnamDate(h.Ngay_lap)!.Value.Date))
                .Select(g => new NonCashGroup(g.Key.Seller, g.Key.Date, g.Sum(Total), g.ToList()))
                .Where(g => g.Total >= Threshold)
                .OrderBy(g => g.Date).ThenBy(g => g.SellerMst)
                .ToList();
        }

        /// <summary>Ghi chú cho tờ khai và bảng kê mua vào; null nếu không có HĐ nào tới ngưỡng.</summary>
        public static string? Note(IEnumerable<SearchResult> headers)
        {
            var groups = Find(headers);
            if (groups.Count == 0) return null;
            int count = groups.Sum(g => g.Invoices.Count);
            string list = string.Join("; ", groups.Select(g =>
                $"MST {g.SellerMst} ngày {g.Date:dd/MM/yyyy}: {string.Join(", ", g.Invoices.Select(h => $"{h.Ky_hieu_hoa_don}-{h.So_hoa_don}"))}, tổng {Fmt(g.Total)}"));
            return $"Có {count} hóa đơn mua vào có tổng thanh toán từ {Fmt(Threshold)} đồng trở lên (cộng gộp hóa đơn cùng người bán, cùng ngày lập): " +
                   "chỉ được khấu trừ thuế khi có chứng từ thanh toán không dùng tiền mặt. App không biết cách thanh toán; " +
                   $"nếu đã trả bằng tiền mặt, bỏ thuế của các hóa đơn này khỏi [25] (nhập số âm ở ô Điều chỉnh [25]) ({list}).";
        }

        private static decimal Total(SearchResult h) =>
            h.Tong_tien_thanh_toan ?? (h.Tong_tien_chua_thue ?? 0m) + (h.Tong_tien_thue ?? 0m);

        private static string Fmt(decimal v) => v.ToString("#,##0", CultureInfo.InvariantCulture).Replace(',', '.');
    }
}
