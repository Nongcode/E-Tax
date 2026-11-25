using System.Collections.Generic;
using System.Linq;

namespace E_Tax
{
    public enum InvoiceRelation { Goc, ThayThe, DieuChinh, BiThayThe, BiDieuChinh, BiHuy, KhongRo }

    public record InvoiceCheckResult(InvoiceRelation Relation, IReadOnlyList<string> Issues)
    {
        public bool IsValid => Issues.Count == 0;
    }

    public static class InvoiceClassifier
    {
        // GIẢ ĐỊNH về cổng thuế (tthai), CHƯA đối chiếu với dữ liệu thật; chỉ sửa ở đây.
        public static InvoiceRelation RelationOf(int? tthai) => tthai switch
        {
            1 => InvoiceRelation.Goc,
            2 => InvoiceRelation.ThayThe,
            3 => InvoiceRelation.DieuChinh,
            4 => InvoiceRelation.BiThayThe,
            5 => InvoiceRelation.BiDieuChinh,
            6 => InvoiceRelation.BiHuy,
            _ => InvoiceRelation.KhongRo,
        };

        public static string Label(InvoiceRelation relation) => relation switch
        {
            InvoiceRelation.Goc => "Hóa đơn gốc",
            InvoiceRelation.ThayThe => "Hóa đơn thay thế",
            InvoiceRelation.DieuChinh => "Hóa đơn điều chỉnh",
            InvoiceRelation.BiThayThe => "Bị thay thế",
            InvoiceRelation.BiDieuChinh => "Bị điều chỉnh",
            InvoiceRelation.BiHuy => "Đã hủy",
            _ => "Không rõ trạng thái",
        };

        private static readonly int[] Weights = { 31, 29, 23, 19, 17, 13, 7, 5, 3 };

        public static bool IsValidTaxCode(string? mst)
        {
            string s = (mst ?? "").Trim();
            if (s.Length == 10) return CheckDigitOk(s);
            if (s.Length == 14)
                return s[10] == '-' && CheckDigitOk(s.Substring(0, 10)) && s.Skip(11).All(char.IsAsciiDigit);
            if (s.Length == 12) return s.All(char.IsAsciiDigit);
            return false;
        }

        private static bool CheckDigitOk(string ten)
        {
            if (!ten.All(char.IsAsciiDigit)) return false;
            int sum = 0;
            for (int i = 0; i < 9; i++) sum += (ten[i] - '0') * Weights[i];
            int check = 10 - (sum % 11);
            return check < 10 && check == ten[9] - '0';
        }

        public static InvoiceCheckResult Check(SearchResult invoice, string? ownTaxCode)
        {
            var relation = RelationOf(invoice.Trang_thai_hoa_don);
            var issues = new List<string>();

            if (relation == InvoiceRelation.BiHuy) issues.Add("Hóa đơn đã bị hủy");
            else if (relation == InvoiceRelation.KhongRo) issues.Add("Không xác định được trạng thái hóa đơn");

            if (!IsValidTaxCode(invoice.Ma_so_thue)) issues.Add("MST người bán không hợp lệ");

            string own = (ownTaxCode ?? "").Trim();
            if (own.Length > 0)
            {
                // MST bên đối chiếu trống thì bỏ qua (HĐ không ghi MST người mua là thường gặp), chỉ cờ khi có MST khác hẳn
                string buyer = (invoice.Ma_so_thue_nguoi_mua ?? "").Trim();
                string seller = (invoice.Ma_so_thue ?? "").Trim();
                if (invoice.Loai_hoa_don == "Mua vào" && buyer.Length > 0 &&
                    !string.Equals(buyer, own, StringComparison.Ordinal))
                    issues.Add("MST người mua không khớp MST của bạn");
                else if (invoice.Loai_hoa_don == "Bán ra" && seller.Length > 0 &&
                    !string.Equals(seller, own, StringComparison.Ordinal))
                    issues.Add("MST người bán không khớp MST của bạn");
            }

            return new InvoiceCheckResult(relation, issues);
        }
    }
}
