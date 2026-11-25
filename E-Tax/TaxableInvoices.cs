using System;
using System.Collections.Generic;
using System.Linq;

namespace E_Tax
{
    internal enum TaxInclusion { Included, Excluded, Unknown }

    /// <summary>Kết quả chọn hóa đơn được tính thuế. Các bộ đếm tính theo hóa đơn (loại HĐ + khóa), không theo dòng hàng.</summary>
    internal sealed record TaxableSelection(IReadOnlyList<InvoiceDetailItem> Items, int Cancelled, int Replaced, int Unknown, int Adjustments)
    {
        /// <summary>Ghi chú số hóa đơn đã bị loại và lý do; rỗng nếu không loại hóa đơn nào.</summary>
        public IReadOnlyList<string> ExclusionNotes()
        {
            int excluded = Cancelled + Replaced;
            if (excluded == 0) return Array.Empty<string>();
            return new[] { $"Đã loại {excluded} hóa đơn khỏi số liệu: {Cancelled} đã hủy, {Replaced} bị thay thế." };
        }
    }

    /// <summary>
    /// Bộ chọn duy nhất cho bảng kê, tờ khai, phụ lục và file kế toán. Loại HĐ đã hủy và bị thay thế; giữ HĐ gốc, thay thế,
    /// điều chỉnh và bị điều chỉnh. Trạng thái không rõ (kể cả null) không được tính và phải chặn xuất.
    /// </summary>
    internal static class TaxableInvoices
    {
        public static TaxInclusion Classify(int? status) => InvoiceClassifier.RelationOf(status) switch
        {
            InvoiceRelation.Goc or InvoiceRelation.ThayThe or InvoiceRelation.DieuChinh or InvoiceRelation.BiDieuChinh => TaxInclusion.Included,
            InvoiceRelation.BiHuy or InvoiceRelation.BiThayThe => TaxInclusion.Excluded,
            _ => TaxInclusion.Unknown
        };

        public static bool IsIncluded(int? status) => Classify(status) == TaxInclusion.Included;

        /// <summary>HĐ được tính và không phải HĐ gốc: cần kiểm tra dấu số tiền điều chỉnh.</summary>
        public static bool IsAdjustment(int? status) => IsIncluded(status) && InvoiceClassifier.RelationOf(status) != InvoiceRelation.Goc;

        /// <summary>
        /// Chọn dòng hàng được tính. headers bổ sung các HĐ không có dòng hàng vào bộ đếm loại/không rõ;
        /// direction ("Mua vào"/"Bán ra") giới hạn theo chiều, null là cả hai.
        /// </summary>
        public static TaxableSelection Select(IEnumerable<InvoiceDetailItem> items, IEnumerable<SearchResult>? headers = null, string? direction = null)
        {
            var lines = items.Where(i => i != null && (direction == null || i.LoaiHoaDon == direction)).ToList();
            var statusByInvoice = new Dictionary<(string, string), int?>();
            foreach (var l in lines) statusByInvoice[(l.LoaiHoaDon ?? "", LineKey(l))] = l.TrangThaiHD;
            if (headers != null)
                foreach (var h in headers.Where(h => h != null && (direction == null || h.Loai_hoa_don == direction)))
                    statusByInvoice[(h.Loai_hoa_don ?? "", InvoiceSearchResults.KeyOf(h))] = h.Trang_thai_hoa_don;

            int Count(InvoiceRelation r) => statusByInvoice.Values.Count(s => InvoiceClassifier.RelationOf(s) == r);
            var included = lines.Where(l => IsIncluded(l.TrangThaiHD)).ToList();
            int adjustments = included.Where(l => IsAdjustment(l.TrangThaiHD))
                .Select(l => (l.LoaiHoaDon ?? "", LineKey(l))).Distinct().Count();
            return new TaxableSelection(included, Count(InvoiceRelation.BiHuy), Count(InvoiceRelation.BiThayThe),
                statusByInvoice.Values.Count(s => Classify(s) == TaxInclusion.Unknown), adjustments);
        }

        /// <summary>Các hóa đơn được tính thuế trong danh sách kết quả tìm kiếm.</summary>
        public static IEnumerable<SearchResult> IncludedHeaders(IEnumerable<SearchResult> headers) =>
            headers.Where(h => h != null && IsIncluded(h.Trang_thai_hoa_don));

        /// <summary>Lý do chặn xuất khi có hóa đơn không rõ trạng thái; null nếu không có.</summary>
        public static string? UnknownBlockReason(int count) =>
            count > 0
                ? $"Có {count} hóa đơn không rõ trạng thái nên chưa thể quyết định có được tính thuế hay không. " +
                  "Không được xuất cho đến khi xác định được trạng thái các hóa đơn này."
                : null;

        /// <summary>Cảnh báo chung cho HĐ thay thế/điều chỉnh/bị điều chỉnh vẫn được tính vào tổng.</summary>
        public static string AdjustmentWarning(int count) =>
            $"Có {count} hóa đơn có trạng thái khác 1 (thay thế, điều chỉnh hoặc bị điều chỉnh) đã bao gồm trong tổng. " +
            "Phải kiểm tra dấu của số tiền điều chỉnh trên dữ liệu thật.";

        /// <summary>
        /// Số HĐ thay thế/điều chỉnh có ngày HĐ gốc ở kỳ khác. Số liệu kỳ vẫn tính theo ngày lập;
        /// việc đưa vào kỳ nào do kế toán quyết định.
        /// </summary>
        public static int CountCrossPeriod(IEnumerable<SearchResult> headers, TaxPeriod period, string? direction = null)
        {
            var list = headers.Where(h => h != null).ToList();
            var links = InvoiceLinker.Link(list);
            return list.Count(h =>
            {
                if (direction != null && h.Loai_hoa_don != direction) return false;
                var relation = InvoiceClassifier.RelationOf(h.Trang_thai_hoa_don);
                if (relation != InvoiceRelation.ThayThe && relation != InvoiceRelation.DieuChinh) return false;
                InvoiceLink? link = null;
                if (h.Id != null) links.TryGetValue(h.Id, out link);
                var original = link?.OriginalDate ?? InvoiceDate.ToVietnamDate(h.Ngay_lap_goc);
                return original is DateTime d && (d < period.From.Date || d > period.To.Date);
            });
        }

        /// <summary>
        /// Số HĐ được tính có tổng dòng hàng lệch tổng trên hóa đơn quá 1 đồng mỗi dòng (so riêng tiền chưa thuế và tiền thuế,
        /// chỉ khi hóa đơn có giá trị đó). Chỉ cảnh báo vì ý nghĩa của tchat chưa được xác minh.
        /// </summary>
        public static int CountTotalMismatches(IEnumerable<InvoiceDetailItem> items, IEnumerable<SearchResult> headers, string? direction = null)
        {
            var byInvoice = items.Where(i => i != null).ToLookup(i => (i.LoaiHoaDon ?? "", LineKey(i)));
            return headers.Count(h =>
            {
                if (h == null || !IsIncluded(h.Trang_thai_hoa_don)) return false;
                if (direction != null && h.Loai_hoa_don != direction) return false;
                var lines = byInvoice[(h.Loai_hoa_don ?? "", InvoiceSearchResults.KeyOf(h))].ToList();
                if (lines.Count == 0) return false;
                decimal tolerance = lines.Count;
                bool amountOff = h.Tong_tien_chua_thue is decimal a && Math.Abs(lines.Sum(l => l.DoanhSoChuaThue ?? 0m) - a) > tolerance;
                bool taxOff = h.Tong_tien_thue is decimal t && Math.Abs(lines.Sum(l => l.TienThueGTGT ?? 0m) - t) > tolerance;
                return amountOff || taxOff;
            });
        }

        /// <summary>Ghi chú cho file xuất: số HĐ bị loại, HĐ có HĐ gốc ở kỳ khác và HĐ lệch tổng.</summary>
        public static IReadOnlyList<string> OutputNotes(IEnumerable<InvoiceDetailItem> items, IEnumerable<SearchResult> headers, TaxPeriod period, string? direction = null)
        {
            var itemList = items.ToList();
            var headerList = headers.ToList();
            var notes = new List<string>(Select(itemList, headerList, direction).ExclusionNotes());
            int crossPeriod = CountCrossPeriod(headerList, period, direction);
            if (crossPeriod > 0)
                notes.Add($"Có {crossPeriod} hóa đơn thay thế/điều chỉnh có hóa đơn gốc thuộc kỳ khác. Số liệu đang tính theo ngày lập; kế toán phải quyết định kỳ kê khai.");
            int mismatches = CountTotalMismatches(itemList, headerList, direction);
            if (mismatches > 0)
                notes.Add($"Có {mismatches} hóa đơn có tổng dòng hàng không khớp tổng tiền trên hóa đơn (lệch quá 1 đồng mỗi dòng). Hãy đối chiếu trước khi dùng.");
            return notes;
        }

        private static string LineKey(InvoiceDetailItem l) =>
            !string.IsNullOrEmpty(l.InvoiceKey) ? l.InvoiceKey : $"{l.MSTNguoiBan}|{l.KyHieuHD}|{l.SoHD}";
    }
}
