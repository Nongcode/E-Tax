using System.Collections.Generic;
using System.Linq;

namespace E_Tax
{
    public record InvoiceLink(string InvoiceId, string? OriginalId, DateTime? OriginalDate);

    public static class InvoiceLinker
    {
        private static string Num(string? v)
        {
            string t = (v ?? "").Trim();
            if (t.Length > 0 && t.All(char.IsAsciiDigit)) { t = t.TrimStart('0'); if (t.Length == 0) t = "0"; }
            return t;
        }

        private static string Key(string? seller, string? khms, string? kh, string? so) =>
            $"{(seller ?? "").Trim()}|{Num(khms)}|{(kh ?? "").Trim()}|{Num(so)}";

        /// <summary>Gắn HĐ thay thế/điều chỉnh với HĐ gốc trong danh sách (cùng MST người bán, mẫu số, ký hiệu, số HĐ).</summary>
        public static IReadOnlyDictionary<string, InvoiceLink> Link(IReadOnlyList<SearchResult> invoices)
        {
            var byKey = new Dictionary<string, SearchResult>();
            foreach (var inv in invoices)
            {
                if (inv.Id == null) continue;
                byKey.TryAdd(Key(inv.Ma_so_thue, inv.Ky_hieu_ma_so?.ToString(), inv.Ky_hieu_hoa_don, inv.So_hoa_don?.ToString()), inv);
            }

            var result = new Dictionary<string, InvoiceLink>();
            foreach (var inv in invoices)
            {
                if (inv.Id == null) continue;
                var relation = InvoiceClassifier.RelationOf(inv.Trang_thai_hoa_don);
                if (relation != InvoiceRelation.ThayThe && relation != InvoiceRelation.DieuChinh) continue;

                string key = Key(inv.Ma_so_thue, inv.Ky_hieu_ma_so_goc, inv.Ky_hieu_hoa_don_goc, inv.Hoa_don_lien_quan);
                byKey.TryGetValue(key, out var original);
                var originalDate = InvoiceDate.ToVietnamDate(inv.Ngay_lap_goc)
                    ?? (original == null ? null : InvoiceDate.ToVietnamDate(original.Ngay_lap));
                result[inv.Id] = new InvoiceLink(inv.Id, original?.Id, originalDate);
            }
            return result;
        }

        /// <summary>Ngày tính kỳ: ngày HĐ gốc nếu có, ngược lại ngày lập của chính HĐ.</summary>
        public static DateTime? EffectiveDate(SearchResult invoice, InvoiceLink? link) =>
            link?.OriginalDate ?? InvoiceDate.ToVietnamDate(invoice.Ngay_lap);
    }
}
