using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace E_Tax
{
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
