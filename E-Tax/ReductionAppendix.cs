using System;
using System.Collections.Generic;
using System.Linq;

namespace E_Tax
{
    /// <summary>Một dòng phụ lục giảm thuế dự thảo: giá trị hàng bán ra thuế suất 8% gộp theo tên hàng.</summary>
    internal sealed record ReductionLine(string TenHang, decimal GiaTri, decimal ThueSuatGoc, decimal ThueSuatGiam, decimal ThueDuocGiam);

    internal static class ReductionAppendix
    {
        public static IReadOnlyList<ReductionLine> Build(IEnumerable<InvoiceDetailItem> items)
        {
            const string NoName = "(Không có tên hàng)";
            // Khóa gộp: tên đã cắt khoảng trắng, không phân biệt hoa thường; giữ cách viết gặp đầu tiên
            var totals = new Dictionary<string, (string Name, decimal Sum)>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in TaxableInvoices.Select(items).Items)
            {
                if (item.LoaiHoaDon != "Bán ra" || TaxListing.NormalizeRate(item.ThueSuat) != "8%" || item.DoanhSoChuaThue is not decimal amount)
                    continue;
                string name = item.TenSanPham?.Trim() ?? "";
                if (name.Length == 0) name = NoName;
                totals[name] = totals.TryGetValue(name, out var t) ? (t.Name, t.Sum + amount) : (name, amount);
            }
            return totals.Values
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new ReductionLine(t.Name, t.Sum, 10m, 8m, Math.Round(t.Sum * 2 / 100, 0, MidpointRounding.AwayFromZero)))
                .ToList();
        }
    }
}
