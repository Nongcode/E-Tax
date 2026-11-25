using System.Drawing;
using System.Globalization;

namespace E_Tax
{
    /// <summary>Một dòng hiển thị của lưới kho; Invoice là HĐ gốc để mở file/ghi chú.</summary>
    internal sealed record StoreRow(SearchResult Invoice, string Loai, string Mst, string KyHieu, int? SoHd, string NgayLap,
        string NguoiBan, decimal? TongTien, string PhanLoai, string CanhBao, string Files, string GhiChu);

    /// <summary>Logic thuần (không UI) cho lưới HĐ: phân trang, màu theo phân loại, nhãn hiển thị.</summary>
    internal static class InvoiceGridLogic
    {
        public const int PageSize = 200;

        private static readonly Color AdjustmentColor = Color.FromArgb(0xFF, 0xF9, 0xC4);
        private static readonly Color ReplacementColor = Color.FromArgb(0xFF, 0xCC, 0x80);

        public static int PageCount(int total, int pageSize = PageSize)
        {
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
            return Math.Max(1, (Math.Max(total, 0) + pageSize - 1) / pageSize);
        }

        /// <summary>Trang (đánh số từ 0) nằm trong [0, PageCount-1].</summary>
        public static int ClampPage(int page, int total, int pageSize = PageSize) =>
            Math.Min(Math.Max(page, 0), PageCount(total, pageSize) - 1);

        public static List<T> Slice<T>(IReadOnlyList<T> items, int page, int pageSize = PageSize)
        {
            int p = ClampPage(page, items.Count, pageSize);
            return items.Skip(p * pageSize).Take(pageSize).ToList();
        }

        public static Color? ClassificationColor(InvoiceRelation relation) => relation switch
        {
            InvoiceRelation.DieuChinh or InvoiceRelation.BiDieuChinh => AdjustmentColor,
            InvoiceRelation.ThayThe or InvoiceRelation.BiThayThe => ReplacementColor,
            _ => null,
        };

        /// <summary>Ưu tiên: rủi ro, rồi chưa kiểm tra được, rồi màu phân loại.</summary>
        public static Color? RowColor(RiskStatus? risk, InvoiceRelation relation, Color riskyColor, Color unknownColor)
        {
            if (risk == RiskStatus.Risky) return riskyColor;
            if (risk == RiskStatus.Unknown) return unknownColor;
            return ClassificationColor(relation);
        }

        public static string ClassificationText(SearchResult invoice, InvoiceLink? link)
        {
            string text = InvoiceClassifier.Label(InvoiceClassifier.RelationOf(invoice.Trang_thai_hoa_don));
            if (link?.OriginalDate is DateTime d)
                text += " (gốc " + d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ")";
            return text;
        }

        /// <summary>MST doanh nghiệp từ tên đăng nhập: chỉ khi là MST hợp lệ, ngược lại null (không đối chiếu, không ghi kho).</summary>
        public static string? CurrentTaxCode(string? username)
        {
            string s = (username ?? "").Trim();
            return InvoiceClassifier.IsValidTaxCode(s) ? s : null;
        }

        /// <summary>MST dùng để đối chiếu bên mua/bán trong kho: chỉ khi đang đăng nhập đúng doanh nghiệp đang xem, ngược lại null (bỏ qua).</summary>
        public static string? PartyCheckTaxCode(string? currentTaxCode, string viewedCompany) =>
            currentTaxCode != null && string.Equals(currentTaxCode, viewedCompany, StringComparison.Ordinal) ? currentTaxCode : null;

        public static StoreRow ToRow(SearchResult invoice, InvoiceLink? link, string? ownTaxCode, string note, IReadOnlyList<string> files)
        {
            var kinds = new List<string>();
            foreach (string f in files)
            {
                string ext = Path.GetExtension(f).TrimStart('.').ToUpperInvariant();
                if (ext.Length > 0 && !kinds.Contains(ext)) kinds.Add(ext);
            }
            return new StoreRow(
                invoice,
                invoice.Loai_hoa_don ?? "",
                invoice.Ma_so_thue ?? "",
                (invoice.Ky_hieu_ma_so?.ToString() ?? "") + (invoice.Ky_hieu_hoa_don ?? ""),
                invoice.So_hoa_don,
                InvoiceDate.ToDisplay(invoice.Ngay_lap) ?? "",
                invoice.Thong_tin_hoa_don ?? "",
                invoice.Tong_tien_thanh_toan,
                ClassificationText(invoice, link),
                string.Join("; ", InvoiceClassifier.Check(invoice, ownTaxCode).Issues),
                string.Join(", ", kinds),
                note ?? "");
        }

        public static List<string> ExistingNonEmptyFiles(params string[] paths)
        {
            var result = new List<string>();
            foreach (string p in paths)
            {
                try
                {
                    var fi = new FileInfo(p);
                    if (fi.Exists && fi.Length > 0) result.Add(p);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { }
            }
            return result;
        }
    }
}
