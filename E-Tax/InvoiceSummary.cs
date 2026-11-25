using System.Globalization;

namespace E_Tax
{
    /// <summary>
    /// Số hóa đơn và tổng tiền thanh toán theo Bán ra / Mua vào, hiển thị ở thanh trạng thái.
    /// </summary>
    internal sealed record InvoiceSummary(int SoldCount, decimal SoldTotal, int BoughtCount, decimal BoughtTotal)
    {
        private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

        public static InvoiceSummary Compute(IEnumerable<SearchResult> invoices)
        {
            int soldCount = 0, boughtCount = 0;
            decimal soldTotal = 0, boughtTotal = 0;
            foreach (var invoice in invoices)
            {
                var total = invoice.Tong_tien_thanh_toan ?? 0;
                if (invoice.Loai_hoa_don == "Bán ra") { soldCount++; soldTotal += total; }
                else if (invoice.Loai_hoa_don == "Mua vào") { boughtCount++; boughtTotal += total; }
            }
            return new InvoiceSummary(soldCount, soldTotal, boughtCount, boughtTotal);
        }

        public string Format()
        {
            if (SoldCount == 0 && BoughtCount == 0) return "Chưa có dữ liệu";
            return $"Bán ra: {SoldCount} HĐ · {Money(SoldTotal)} ₫   |   Mua vào: {BoughtCount} HĐ · {Money(BoughtTotal)} ₫";
        }

        private static string Money(decimal value) => value.ToString("#,##0", Vietnamese);
    }
}
