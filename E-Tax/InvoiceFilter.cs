using System.Globalization;

namespace E_Tax
{
    /// <summary>
    /// Điều kiện lọc danh sách HĐ trên lưới chính. Giá trị rỗng/null = không lọc theo tiêu chí đó.
    /// </summary>
    internal sealed record InvoiceFilterCriteria(string? InvoiceNumber, string? Supplier, string? InvoiceTypeLabel, bool OnlyWithNotes);

    internal static class InvoiceFilter
    {
        public static List<SearchResult> Apply(IEnumerable<SearchResult> source, InvoiceFilterCriteria c, Func<string, string> getNote)
        {
            string number = (c.InvoiceNumber ?? "").Trim();
            string supplier = Normalize((c.Supplier ?? "").Trim());
            string type = (c.InvoiceTypeLabel ?? "").Trim();
            bool filterNumber = number.Length > 0;
            bool numberValid = int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int numberValue);

            var result = new List<SearchResult>();
            foreach (var item in source)
            {
                if (filterNumber && (!numberValid || item.So_hoa_don != numberValue)) continue;
                if (supplier.Length > 0 &&
                    !Normalize(item.Ma_so_thue).Contains(supplier, StringComparison.Ordinal) &&
                    !Normalize(item.Thong_tin_hoa_don).Contains(supplier, StringComparison.Ordinal)) continue;
                if (type.Length > 0 && !string.Equals(item.Loai_hoa_don, type, StringComparison.Ordinal)) continue;
                if (c.OnlyWithNotes && string.IsNullOrWhiteSpace(getNote(item.Id ?? ""))) continue;
                result.Add(item);
            }
            return result;
        }

        /// <summary>Bỏ dấu, không phân biệt hoa thường (đ/Đ không tách dấu nên đổi riêng).</summary>
        private static string Normalize(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace('đ', 'd').Replace('Đ', 'D');
            string d = s.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(d.Length);
            foreach (char ch in d)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
        }
    }
}
