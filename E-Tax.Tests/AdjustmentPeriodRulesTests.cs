using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Kỳ của HĐ điều chỉnh/thay thế có HĐ gốc ở kỳ khác (TT 91/2026 Điều 10, TT 89/2026 Phụ lục II).</summary>
    public class AdjustmentPeriodRulesTests
    {
        private const string Mua = "Mua vào";
        private const string Ban = "Bán ra";
        private static readonly TaxPeriod Oct = TaxPeriod.Month(2026, 10);

        private static SearchResult H(string id, string loai, int status, string? originalDate) => new()
        {
            Id = id, Loai_hoa_don = loai, Trang_thai_hoa_don = status, Ngay_lap = "2026-10-10T00:00:00",
            Ngay_lap_goc = originalDate, Ma_so_thue = "0300000001", Ky_hieu_hoa_don = "C26TAA", So_hoa_don = 1, Ky_hieu_ma_so = 1
        };

        private static InvoiceDetailItem L(SearchResult h, decimal amount, decimal tax) => new()
        {
            InvoiceKey = InvoiceSearchResults.KeyOf(h), LoaiHoaDon = h.Loai_hoa_don, TrangThaiHD = h.Trang_thai_hoa_don,
            DoanhSoChuaThue = amount, TienThueGTGT = tax, ThueSuat = "10%"
        };

        [Fact]
        public void SamePeriodAdjustment_StaysInCurrentPeriod()
        {
            var h = H("a", Mua, 3, "2026-10-02T00:00:00");
            var split = AdjustmentPeriodRules.Split(new[] { L(h, 100, 10) }, new[] { h }, Oct);

            Assert.Single(split.Current);
            Assert.Empty(split.Moved);
            Assert.Equal(0m, split.Ct38);
        }

        // TH2 đầu vào: tăng thuế được khấu trừ (giảm thuế phải nộp) -> khai kỳ hiện tại ở [38]
        [Fact]
        public void PurchaseAdjustmentIncreasingInputTax_GoesToCt38()
        {
            var h = H("a", Mua, 3, "2026-08-02T00:00:00");
            var split = AdjustmentPeriodRules.Split(new[] { L(h, 100, 10) }, new[] { h }, Oct);

            Assert.Empty(split.Current);
            Assert.Equal(10m, split.Ct38);
            Assert.Equal(CrossPeriodAction.CurrentPeriodCt38, Assert.Single(split.Moved).Action);
        }

        // Mua vào giảm thuế được khấu trừ: [37] kỳ này hoặc bổ sung kỳ gốc tùy trường hợp -> tách ra, kế toán chọn
        [Fact]
        public void PurchaseAdjustmentDecreasingInputTax_IsLeftForAccountant()
        {
            var h = H("a", Mua, 3, "2026-08-02T00:00:00");
            var split = AdjustmentPeriodRules.Split(new[] { L(h, -100, -10) }, new[] { h }, Oct);

            Assert.Empty(split.Current);
            Assert.Equal(0m, split.Ct38);
            var moved = Assert.Single(split.Moved);
            Assert.Equal(CrossPeriodAction.PurchaseDecrease, moved.Action);
            Assert.Equal(-10m, moved.Tax);
        }

        // Bán ra: nghiệp vụ phát sinh tính kỳ này, sửa sai bổ sung kỳ gốc -> tách ra, kế toán chọn
        [Theory]
        [InlineData(10)]
        [InlineData(-10)]
        public void SalesAdjustment_IsLeftForAccountant(decimal tax)
        {
            var h = H("a", Ban, 3, "2026-08-02T00:00:00");
            var split = AdjustmentPeriodRules.Split(new[] { L(h, tax * 10, tax) }, new[] { h }, Oct);

            Assert.Empty(split.Current);
            Assert.Equal(0m, split.Ct38);
            Assert.Equal(CrossPeriodAction.SalesAdjustment, Assert.Single(split.Moved).Action);
        }

        // HĐ thay thế: chênh lệch so với HĐ gốc chưa tính được nên không tự đưa vào kỳ nào
        [Fact]
        public void CrossPeriodReplacement_IsLeftForAccountant()
        {
            var h = H("a", Mua, 2, "2026-08-02T00:00:00");
            var split = AdjustmentPeriodRules.Split(new[] { L(h, 100, 10) }, new[] { h }, Oct);

            Assert.Empty(split.Current);
            Assert.Equal(CrossPeriodAction.Replacement, Assert.Single(split.Moved).Action);
        }

        [Fact]
        public void OriginalInvoicesAndUnknownOriginalDate_StayInCurrentPeriod()
        {
            var goc = H("a", Mua, 1, null);
            var noDate = H("b", Mua, 3, null);
            var split = AdjustmentPeriodRules.Split(new[] { L(goc, 100, 10), L(noDate, 50, 5) }, new[] { goc, noDate }, Oct);

            Assert.Equal(2, split.Current.Count);
            Assert.Empty(split.Moved);
        }

        [Fact]
        public void Notes_DescribeEachGroup()
        {
            var a = H("a", Mua, 3, "2026-08-02T00:00:00");
            var b = H("b", Ban, 3, "2026-07-02T00:00:00");
            var split = AdjustmentPeriodRules.Split(new[] { L(a, 100, 10), L(b, -100, -10) }, new[] { a, b }, Oct);

            var notes = split.Notes();
            Assert.Contains(notes, n => n.Contains("[38]") && n.Contains("10"));
            Assert.Contains(notes, n => n.Contains("bán ra") && n.Contains("1 hóa đơn") && n.Contains("khoản 5 Điều 10"));
        }
    }
}
