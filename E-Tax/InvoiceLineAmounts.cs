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

        /// <summary>
        /// Như <see cref="Fill"/> nhưng cho dòng JSON thô (file "Tải chi tiết HĐ"): bổ sung "thtcthue" và "tthue" còn trống.
        /// </summary>
        /// <param name="invoiceRows">Các dòng của MỘT hóa đơn.</param>
        public static int FillRows(IList<Dictionary<string, string>> invoiceRows)
        {
            var items = invoiceRows.Select(r => new InvoiceDetailItem
            {
                ThanhTien = Number(r, "thtien"),
                DoanhSoChuaThue = Number(r, "thtcthue"),
                ThueSuat = r.TryGetValue("tsuat", out var rate) ? rate : null!,
                TienThueGTGT = Number(r, "tthue")
            }).ToList();

            int computed = Fill(items);
            for (int i = 0; i < invoiceRows.Count; i++)
            {
                SetIfEmpty(invoiceRows[i], "thtcthue", items[i].DoanhSoChuaThue);
                SetIfEmpty(invoiceRows[i], "tthue", items[i].TienThueGTGT);
            }
            return computed;
        }

        private static decimal? Number(Dictionary<string, string> row, string key) =>
            row.TryGetValue(key, out var s) && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal v) ? v : null;

        private static void SetIfEmpty(Dictionary<string, string> row, string key, decimal? value)
        {
            if (value is decimal v && (!row.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s)))
                row[key] = v.ToString(CultureInfo.InvariantCulture);
        }

        // "8%" -> 0.08; KCT, KKKNT, Không rõ và giá trị lạ -> null
        private static decimal? RateFraction(string normalized) =>
            normalized.EndsWith('%') && decimal.TryParse(normalized.TrimEnd('%'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal p)
                ? p / 100m
                : null;
    }
}
