using System.Collections.Generic;
using System.Text.Json;

namespace E_Tax
{
    /// <summary>
    /// Các hàm dùng chung để đọc JSON trả về từ API hóa đơn (danh sách và chi tiết).
    /// </summary>
    internal static class InvoiceJson
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Thứ tự và tiêu đề cột khi xuất Excel chi tiết hàng hóa.
        /// </summary>
        public static readonly (string Key, string Header)[] DetailExportColumns =
        {
            ("stt", "STT"), ("khhdon", "Ký hiệu"), ("shdon", "Số hóa đơn"), ("tdlap", "Ngày hóa đơn"),
            ("nbten", "Tên người bán"), ("nbmst", "Mã số thuế"), ("ten", "Tên sản phẩm"),
            ("dvtinh", "Đơn vị tính"), ("sluong", "Số lượng"), ("dgia", "Đơn giá"),
            ("thtien", "Tổng tiền hàng"), ("stckhau", "Tiền chiết khấu"), ("thtcthue", "Doanh số bán chưa thuế"),
            ("tsuat", "Thuế suất"), ("tthue", "Thuế GTGT"), ("tgtttbso", "Tổng tiền thanh toán"),
            ("solo", "Số lô"), ("hsd", "Hạn sử dụng"), ("tthai", "Trạng thái hóa đơn"), ("ghichu", "GHI CHÚ")
        };

        public static bool IsNumericColumn(string key) =>
            key == "sluong" || key == "dgia" || key.Contains("tien") || key == "tthue" || key == "tgtttbso" || key == "stckhau";

        /// <summary>
        /// Một số API bọc dữ liệu trong { "data": { ... } }; trả về phần data nếu có.
        /// </summary>
        public static JsonElement GetDataElement(JsonElement root)
        {
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                return data;
            return root;
        }

        /// <summary>
        /// Tìm đệ quy mảng hàng hóa/dịch vụ "hdhhdvu".
        /// </summary>
        public static JsonElement? FindProductArray(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("hdhhdvu", out var products) && products.ValueKind == JsonValueKind.Array)
                    return products;

                foreach (var prop in element.EnumerateObject())
                {
                    var found = FindProductArray(prop.Value);
                    if (found.HasValue) return found;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindProductArray(item);
                    if (found.HasValue) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Trang chi tiết hợp lệ khi có mảng "hdhhdvu" toàn phần tử object, hoặc có "shdon" khớp số HĐ (HĐ không có dòng hàng).
        /// "shdon" có mà không khớp, "hdhhdvu" sai kiểu, hoặc không có cả hai đều là trang lỗi (thân lỗi kèm HTTP 200).
        /// </summary>
        public static bool IsValidDetailPage(JsonElement root, int? expectedNumber)
        {
            if (root.ValueKind != JsonValueKind.Object) return false;
            var data = GetDataElement(root);

            var number = FindValue(data, "shdon");
            if (number.HasValue && !NumberMatches(number.Value, expectedNumber)) return false;

            var products = FindProductArray(data);
            if (products.HasValue)
            {
                foreach (var product in products.Value.EnumerateArray())
                    if (product.ValueKind != JsonValueKind.Object) return false;
                return true;
            }
            return number.HasValue && !FindValue(data, "hdhhdvu").HasValue;
        }

        // Giá trị khác null đầu tiên theo tên, ưu tiên cấp ngoài; không đi vào các dòng hàng.
        private static JsonElement? FindValue(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null)
                    return value;
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.NameEquals("hdhhdvu")) continue;
                    var found = FindValue(prop.Value, name);
                    if (found.HasValue) return found;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindValue(item, name);
                    if (found.HasValue) return found;
                }
            }
            return null;
        }

        private static bool NumberMatches(JsonElement value, int? expected)
        {
            if (!expected.HasValue) return false;
            if (value.ValueKind == JsonValueKind.Number)
                return value.TryGetDecimal(out decimal number) && number == expected.Value;
            if (value.ValueKind == JsonValueKind.String)
                return int.TryParse((value.GetString() ?? "").Trim(), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out int parsed) && parsed == expected.Value;
            return false;
        }

        /// <summary>
        /// Mỗi sản phẩm thành một dòng: thông tin chung của hóa đơn (các trường đơn) ghép với các trường của sản phẩm.
        /// Trả về danh sách rỗng nếu hóa đơn không có hàng hóa.
        /// </summary>
        public static List<Dictionary<string, string>> BuildDetailRows(JsonElement dataEl)
        {
            var rows = new List<Dictionary<string, string>>();
            var products = FindProductArray(dataEl);
            if (!products.HasValue) return rows;

            var common = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in dataEl.EnumerateObject())
                if (prop.Value.ValueKind != JsonValueKind.Object && prop.Value.ValueKind != JsonValueKind.Array)
                    common[prop.Name] = prop.Value.ToString();

            foreach (var product in products.Value.EnumerateArray())
            {
                var row = new Dictionary<string, string>(common, StringComparer.OrdinalIgnoreCase);
                if (product.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in product.EnumerateObject())
                        row[prop.Name] = prop.Value.ToString();
                }
                rows.Add(row);
            }
            return rows;
        }
    }
}
