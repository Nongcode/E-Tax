using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace E_Tax
{
    /// <summary>Tổng số hóa đơn cổng thuế báo theo từng chiều và lát ngày, để phát hiện kết quả lấy về bị thiếu.</summary>
    internal sealed class SearchTotals
    {
        private readonly Dictionary<(string Direction, DateTime Chunk), int> _totals = new();

        /// <summary>Ghi total của một trang; cùng lát ngày giữ giá trị lớn nhất.</summary>
        public void Record(string direction, DateTime chunkStart, int total)
        {
            var key = (direction, chunkStart);
            if (!_totals.TryGetValue(key, out int current) || total > current) _totals[key] = total;
        }

        /// <summary>Cảnh báo cho mỗi chiều có số hóa đơn duy nhất ít hơn tổng cổng thuế báo.</summary>
        public IReadOnlyList<string> Warnings(IEnumerable<SearchResult> results)
        {
            var unique = InvoiceSearchResults.Deduplicate(results);
            var warnings = new List<string>();
            foreach (var group in _totals.GroupBy(t => t.Key.Direction).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                int expected = group.Sum(t => t.Value);
                int got = unique.Count(r => r.Loai_hoa_don == group.Key);
                if (got < expected)
                    warnings.Add($"{group.Key}: cổng thuế báo {expected} hóa đơn nhưng chỉ lấy được {got}. Số liệu có thể bị thiếu, hãy tìm kiếm lại.");
            }
            return warnings;
        }
    }

    /// <summary>Xử lý kết quả tìm kiếm hóa đơn: loại trùng và nhận biết trang cuối.</summary>
    internal static class InvoiceSearchResults
    {
        /// <summary>Khóa hóa đơn: Id, hoặc MST + ký hiệu mã số + ký hiệu + số khi thiếu Id.</summary>
        public static string KeyOf(SearchResult r) =>
            !string.IsNullOrEmpty(r.Id)
                ? r.Id
                : $"{r.Ma_so_thue}|{r.Ky_hieu_ma_so}|{r.Ky_hieu_hoa_don}|{r.So_hoa_don}";

        /// <summary>
        /// Loại trùng theo (loại hóa đơn, khóa). Cùng Id ở cả Mua vào và Bán ra là hai hóa đơn khác nhau;
        /// hóa đơn không có Id không bị gộp với nhau. Sắp theo ngày lập giảm dần rồi số hóa đơn.
        /// </summary>
        public static List<SearchResult> Deduplicate(IEnumerable<SearchResult> results) =>
            results
                .GroupBy(r => (Loai: r.Loai_hoa_don ?? "", Key: KeyOf(r)))
                .Select(g => g.First())
                .OrderByDescending(r => r.Ngay_lap)
                .ThenBy(r => r.So_hoa_don)
                .ToList();

        /// <summary>
        /// Thông báo khi không có hóa đơn nào nhưng có trang tìm kiếm lỗi: đây là lỗi tải, không phải kỳ trống.
        /// Trả về null nếu không có trang lỗi.
        /// </summary>
        public static string? LoadFailureMessage(IReadOnlyCollection<string> failedChunks) =>
            failedChunks.Count == 0
                ? null
                : "Không tải được danh sách hóa đơn (phiên đăng nhập có thể đã hết hạn hoặc lỗi kết nối). " +
                  $"Chưa thể kết luận kỳ này không có hóa đơn.\n\nCác khoảng thời gian/trang bị lỗi:\n - {string.Join("\n - ", failedChunks)}";

        /// <summary>Đọc trường "total" của phản hồi; null nếu không có hoặc không phải số nguyên.</summary>
        public static int? ParseTotal(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("total", out var total)
                    && total.ValueKind == JsonValueKind.Number
                    && total.TryGetInt32(out int value) ? value : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Phân loại một trang kết quả tìm kiếm. Items có giá trị khi trang có hóa đơn; IsEnd khi hết dữ liệu hợp lệ;
        /// Failed khi thân phản hồi không đọc được hoặc sai cấu trúc (không phải trang cuối). Không ném JsonException.
        /// </summary>
        public static (List<SearchResult>? Items, bool IsEnd, bool Failed) ParsePage(string json)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<SearchResponse>(json, InvoiceJson.Options);
                if (parsed?.Datas != null)
                    return parsed.Datas.Count == 0 ? (null, true, false) : (parsed.Datas, false, false);
            }
            catch (JsonException)
            {
                // rơi xuống phần kiểm tra trang cuối bên dưới
            }
            return IsEndOfResults(json) ? (null, true, false) : (null, false, true);
        }

        /// <summary>
        /// Trả lời có phải trang kết quả rỗng hợp lệ (hết dữ liệu): "datas" là mảng rỗng, hoặc "datas" null/thiếu
        /// và có "total" là số bằng 0. Mọi thân phản hồi khác không có mảng "datas" là trang lỗi.
        /// </summary>
        public static bool IsEndOfResults(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return false;
                if (root.TryGetProperty("datas", out var datas))
                {
                    if (datas.ValueKind == JsonValueKind.Array) return datas.GetArrayLength() == 0;
                    if (datas.ValueKind != JsonValueKind.Null) return false;
                }
                return root.TryGetProperty("total", out var total)
                    && total.ValueKind == JsonValueKind.Number
                    && total.TryGetDecimal(out var t) && t == 0m;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
