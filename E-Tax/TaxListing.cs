using System;
using System.Collections.Generic;
using System.Linq;

namespace E_Tax
{
    /// <summary>Một dòng bảng kê: một hóa đơn với một thuế suất.</summary>
    internal sealed record TaxListingRow(string KyHieu, int? SoHD, string NgayHD, string TenDoiTac, string MstDoiTac,
        string ThueSuat, decimal DoanhSo, decimal TienThue, int? TrangThai, bool ThieuSoTien);

    /// <summary>Nhóm các dòng bảng kê cùng thuế suất, kèm tổng.</summary>
    internal sealed record TaxListingGroup(string ThueSuat, IReadOnlyList<TaxListingRow> Rows, decimal TongDoanhSo, decimal TongThue);

    /// <summary>Dựng bảng kê mua vào/bán ra theo thuế suất từ các dòng hàng của hóa đơn.</summary>
    internal static class TaxListing
    {
        public const string UnknownRate = "Không rõ";
        private const string Bought = "Mua vào";
        private const string Sold = "Bán ra";

        /// <summary>Chuẩn hóa thuế suất: cắt khoảng trắng, viết hoa; rỗng thành "Không rõ".</summary>
        public static string NormalizeRate(string? rate)
        {
            string r = (rate ?? "").Trim().ToUpperInvariant();
            return r.Length == 0 ? UnknownRate : r;
        }

        private static bool IsNoTaxRate(string rate) => rate == "KCT" || rate == "KKKNT";

        private static int RateRank(string rate) => rate switch
        {
            "KCT" => 0,
            "KKKNT" => 1,
            "0%" => 2,
            "5%" => 3,
            "8%" => 4,
            "10%" => 5,
            UnknownRate => 7,
            _ => 6
        };

        /// <summary>
        /// Gộp theo (InvoiceKey, thuế suất) rồi nhóm theo thuế suất. Số tiền null tính là 0 khi cộng
        /// nhưng dòng đó được đánh dấu ThieuSoTien.
        /// </summary>
        public static IReadOnlyList<TaxListingGroup> Build(IEnumerable<InvoiceDetailItem> items, InvoiceType type)
        {
            string loai = type == InvoiceType.Bought ? Bought : Sold;

            var rows = items
                .Where(i => i != null && i.LoaiHoaDon == loai)
                .GroupBy(i => (Key: i.InvoiceKey ?? "", Rate: NormalizeRate(i.ThueSuat)))
                .Select(g => BuildRow(g.ToList(), g.Key.Rate, type))
                .ToList();

            return rows
                .GroupBy(r => r.Row.ThueSuat)
                .OrderBy(g => RateRank(g.Key))
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g =>
                {
                    var sorted = g
                        .OrderBy(r => r.Date ?? DateTime.MaxValue)
                        .ThenBy(r => r.Row.KyHieu, StringComparer.Ordinal)
                        .ThenBy(r => r.Row.SoHD ?? int.MaxValue)
                        .Select(r => r.Row)
                        .ToList();
                    return new TaxListingGroup(g.Key, sorted, sorted.Sum(r => r.DoanhSo), sorted.Sum(r => r.TienThue));
                })
                .ToList();
        }

        private static (TaxListingRow Row, DateTime? Date) BuildRow(List<InvoiceDetailItem> lines, string rate, InvoiceType type)
        {
            var first = lines[0];
            bool thieu = lines.Any(l => l.DoanhSoChuaThue == null || (l.TienThueGTGT == null && !IsNoTaxRate(rate)));
            // Mua vào: đối tác là người bán. Bán ra: đối tác là người mua, thiếu thì để trống (không lấy người bán).
            string ten = (type == InvoiceType.Bought ? first.TenNguoiBan : first.TenNguoiMua) ?? "";
            string mst = (type == InvoiceType.Bought ? first.MSTNguoiBan : first.MSTNguoiMua) ?? "";
            var row = new TaxListingRow(first.KyHieuHD ?? "", first.SoHD, first.NgayHD ?? "", ten, mst, rate,
                lines.Sum(l => l.DoanhSoChuaThue ?? 0m), lines.Sum(l => l.TienThueGTGT ?? 0m), first.TrangThaiHD, thieu);
            return (row, InvoiceDate.ToVietnamDate(first.NgayHD));
        }

        /// <summary>Lý do không được xuất bảng kê (có hóa đơn thiếu chi tiết); null nếu được phép.</summary>
        public static string? ExportBlockReason(int failedInvoices) =>
            failedInvoices > 0
                ? $"Có {failedInvoices} hóa đơn/trang kết quả chưa lấy đầy đủ dữ liệu (lỗi lấy chi tiết, thiếu thông tin hoặc lỗi trang tìm kiếm). " +
                  "Bảng kê sẽ thiếu số liệu nên không được xuất. Hãy tìm kiếm lại cho đến khi không còn lỗi."
                : null;
    }
}
