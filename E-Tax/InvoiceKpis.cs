namespace E_Tax
{
    /// <summary>
    /// Số liệu cho thẻ tổng hợp: tổng theo chiều (mọi HĐ, giống thanh trạng thái) và thuế GTGT tạm tính
    /// = thuế bán ra − thuế mua vào của các HĐ được tính thuế (bỏ HĐ mua vào mẫu 2 vì không có thuế khấu trừ).
    /// Chỉ để tham khảo nhanh; số trên tờ khai do KeKhaiForm tính.
    /// </summary>
    internal sealed record InvoiceKpis(int SoldCount, decimal SoldTotal, decimal SoldTax,
        int BoughtCount, decimal BoughtTotal, decimal BoughtTax, decimal VatEstimate, int Attention)
    {
        public static InvoiceKpis Compute(IEnumerable<SearchResult> invoices)
        {
            string sold = TaxListing.DirectionLabel(InvoiceType.Sold), bought = TaxListing.DirectionLabel(InvoiceType.Bought);
            int sc = 0, bc = 0, attention = 0;
            decimal st = 0, bt = 0, sTax = 0, bTax = 0, vatOut = 0, vatIn = 0;
            foreach (var h in invoices)
            {
                if (h == null) continue;
                decimal total = h.Tong_tien_thanh_toan ?? 0m, tax = h.Tong_tien_thue ?? 0m;
                bool included = TaxableInvoices.IsIncluded(h.Trang_thai_hoa_don);
                if (InvoiceClassifier.RelationOf(h.Trang_thai_hoa_don) != InvoiceRelation.Goc) attention++;
                if (h.Loai_hoa_don == sold)
                {
                    sc++; st += total; sTax += tax;
                    if (included) vatOut += tax;
                }
                else if (h.Loai_hoa_don == bought)
                {
                    bc++; bt += total; bTax += tax;
                    if (included && h.Ky_hieu_ma_so != 2) vatIn += tax;
                }
            }
            return new InvoiceKpis(sc, st, sTax, bc, bt, bTax, vatOut - vatIn, attention);
        }

        /// <summary>Số điều kiện lọc trong kết quả đang bật (hiện trên nút "Bộ lọc").</summary>
        public static int ActiveFilterCount(string? soHd, string? ncc, int loaiIndex, bool chiCoGhiChu) =>
            (string.IsNullOrWhiteSpace(soHd) ? 0 : 1) + (string.IsNullOrWhiteSpace(ncc) ? 0 : 1) + (loaiIndex > 0 ? 1 : 0) + (chiCoGhiChu ? 1 : 0);
    }
}
