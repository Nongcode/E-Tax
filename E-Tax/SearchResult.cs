using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace E_Tax
{
    public enum InvoiceType { Sold, Bought };

    /// <summary>
    /// Một hóa đơn trong kết quả tìm kiếm (query/invoices/sold | purchase).
    /// </summary>
    public class SearchResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("nbmst")]
        public string Ma_so_thue { get; set; }

        [JsonPropertyName("khmshdon")]
        public int? Ky_hieu_ma_so { get; set; }

        [JsonPropertyName("khhdon")]
        public string Ky_hieu_hoa_don { get; set; }

        [JsonPropertyName("shdon")]
        public int? So_hoa_don { get; set; }

        [JsonPropertyName("tdlap")]
        public string Ngay_lap { get; set; }

        [JsonPropertyName("nbten")]
        public string Thong_tin_hoa_don { get; set; }

        [JsonPropertyName("tgtcthue")]
        public decimal? Tong_tien_chua_thue { get; set; }

        [JsonPropertyName("tgtthue")]
        public decimal? Tong_tien_thue { get; set; }

        [JsonPropertyName("ttcktmai")]
        public decimal? Tong_tien_chiet_khau { get; set; }

        [JsonPropertyName("thttlphi")]
        public List<decimal?> Tong_tien_phi { get; set; }

        [JsonPropertyName("tgtttbso")]
        public decimal? Tong_tien_thanh_toan { get; set; }

        [JsonPropertyName("dvtte")]
        public string Don_vi_tien_te { get; set; }

        [JsonPropertyName("tthai")]
        public int? Trang_thai_hoa_don { get; set; }

        [JsonPropertyName("kqcht")]
        public string Ket_qua_kiem_tra_hoa_don { get; set; }

        [JsonPropertyName("shdgoc")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Hoa_don_lien_quan { get; set; }

        [JsonPropertyName("nmdchi")]
        public string Thong_tin_lien_quan { get; set; }

        [JsonPropertyName("nmmst")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Ma_so_thue_nguoi_mua { get; set; }

        [JsonPropertyName("nmten")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Ten_nguoi_mua { get; set; }

        [JsonPropertyName("khmshdgoc")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Ky_hieu_ma_so_goc { get; set; }

        [JsonPropertyName("khhdgoc")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Ky_hieu_hoa_don_goc { get; set; }

        [JsonPropertyName("tdlhdgoc")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Ngay_lap_goc { get; set; }

        /// <summary>
        /// "Bán ra" / "Mua vào" — gán sau khi tìm kiếm theo endpoint đã gọi, không có trong JSON.
        /// </summary>
        [JsonIgnore]
        public string Loai_hoa_don { get; set; } = "";

        /// <summary>
        /// Đủ 4 khóa (MST, KHHĐ, Số HĐ, KH mẫu số) để gọi API chi tiết / tải ZIP.
        /// </summary>
        public bool HasLookupKeys() =>
            !string.IsNullOrEmpty(Ma_so_thue) &&
            !string.IsNullOrEmpty(Ky_hieu_hoa_don) &&
            So_hoa_don.HasValue &&
            Ky_hieu_ma_so.HasValue;

        /// <summary>
        /// Chuỗi định danh an toàn cho tên file: {MST người bán}_{KH mẫu số}_{KHHĐ}_{Số HĐ}.
        /// Ký tự không hợp lệ thành "_", phần thiếu thành *_NA.
        /// </summary>
        public string FileIdentifier =>
            $"{SafeFilePart(Ma_so_thue, "MST_NA")}_{Ky_hieu_ma_so?.ToString() ?? "KHMS_NA"}_{SafeFilePart(Ky_hieu_hoa_don, "KH_NA")}_{So_hoa_don?.ToString() ?? "SHD_NA"}";

        private static string SafeFilePart(string? value, string missing)
        {
            string text = (value ?? "").Trim();
            if (text.Length == 0) return missing;
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Concat(text.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
        }
    }

    public class SearchResponse
    {
        [JsonPropertyName("datas")]
        public List<SearchResult> Datas { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("time")]
        public int Time { get; set; }
    }
}
