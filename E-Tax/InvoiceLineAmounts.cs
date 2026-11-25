using System.Globalization;

namespace E_Tax
{
    /// <summary>
    /// Bổ sung số tiền dòng hàng còn thiếu trong chi tiết từ cổng thuế. Dữ liệu thật thường chỉ có thtien (thành tiền chưa thuế)
    /// và tsuat dạng tỷ lệ (0.08), không có thtcthue/tthue. Tiền thuế được tính theo từng thuế suất trên tổng tiền của hóa đơn
    /// rồi chia lại cho các dòng, để tổng khớp cách hóa đơn tự tính (làm tròn một lần).
    /// </summary>
    internal static class InvoiceLineAmounts
    {
        /// <param name="invoiceLines">Các dòng của MỘT hóa đơn.</param>
        /// <returns>Số dòng có tiền thuế do app tự tính.</returns>
        public static int Fill(IList<InvoiceDetailItem> invoiceLines)
        {
            foreach (var l in invoiceLines)
                l.DoanhSoChuaThue ??= l.ThanhTien;

            int computed = 0;
            var missing = invoiceLines.Where(l => l.TienThueGTGT == null && l.DoanhSoChuaThue != null)
                .GroupBy(l => TaxListing.NormalizeRate(l.ThueSuat));
            foreach (var group in missing)
            {
                if (RateFraction(group.Key) is not decimal rate) continue;
                var lines = group.ToList();
                decimal total = Math.Round(lines.Sum(l => l.DoanhSoChuaThue!.Value) * rate, 0, MidpointRounding.AwayFromZero);
                decimal assigned = 0m;
                for (int i = 0; i < lines.Count; i++)
                {
                    decimal tax = i == lines.Count - 1
                        ? total - assigned
                        : Math.Round(lines[i].DoanhSoChuaThue!.Value * rate, 0, MidpointRounding.AwayFromZero);
                    lines[i].TienThueGTGT = tax;
                    assigned += tax;
                }
                computed += lines.Count;
            }
            return computed;
        }

        // "8%" -> 0.08; KCT, KKKNT, Không rõ và giá trị lạ -> null
        private static decimal? RateFraction(string normalized) =>
            normalized.EndsWith('%') && decimal.TryParse(normalized.TrimEnd('%'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal p)
                ? p / 100m
                : null;
    }
}
