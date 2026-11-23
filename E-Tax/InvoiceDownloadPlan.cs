using System;
using System.Collections.Generic;
using System.Linq;

namespace E_Tax
{
    /// <summary>Một hóa đơn cần tải, kèm tên file không trùng trong lượt tải.</summary>
    internal sealed record InvoiceDownloadItem(SearchResult Invoice, string FileId);

    /// <summary>Lập danh sách tải bản gốc: mỗi hóa đơn thật một lần, mỗi hóa đơn một tên file riêng.</summary>
    internal static class InvoiceDownloadPlan
    {
        /// <summary>
        /// Loại trùng theo (MST người bán, ký hiệu mã số, ký hiệu, số), giữ thứ tự đầu vào. HĐ thiếu khóa không được gộp
        /// với nhau (vẫn đi tiếp để báo lỗi riêng). HĐ khác nhau nhưng trùng tên sau khi làm sạch được thêm hậu tố.
        /// </summary>
        public static List<InvoiceDownloadItem> Build(IEnumerable<SearchResult> invoices)
        {
            var seen = new HashSet<(string, int, string, int)>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // tên file Windows không phân biệt hoa thường
            var plan = new List<InvoiceDownloadItem>();

            foreach (var invoice in invoices)
            {
                if (invoice == null) continue;
                if (invoice.HasLookupKeys() &&
                    !seen.Add((invoice.Ma_so_thue, invoice.Ky_hieu_ma_so!.Value, invoice.Ky_hieu_hoa_don, invoice.So_hoa_don!.Value)))
                    continue;

                string baseName = invoice.FileIdentifier;
                string name = baseName;
                for (int n = 2; !usedNames.Add(name); n++)
                    name = $"{baseName}_{n}";
                plan.Add(new InvoiceDownloadItem(invoice, name));
            }
            return plan;
        }

        /// <summary>Đường dẫn PDF đã tạo trong lượt này, theo ký hiệu rồi số HĐ, không trùng.</summary>
        public static List<string> OrderForMerge(IEnumerable<(InvoiceDownloadItem Item, string PdfPath)> produced) =>
            produced
                .DistinctBy(p => p.PdfPath, StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p.Item.Invoice.Ky_hieu_hoa_don, StringComparer.Ordinal)
                .ThenBy(p => p.Item.Invoice.So_hoa_don ?? int.MaxValue)
                .ThenBy(p => p.Item.FileId, StringComparer.Ordinal)
                .Select(p => p.PdfPath)
                .ToList();
    }
}
