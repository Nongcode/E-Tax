using E_Tax;

namespace E_Tax.Tests
{
    public class InvoiceLineAmountsTests
    {
        private static InvoiceDetailItem L(decimal? thtien, string? rate = "0.08", decimal? doanhSo = null, decimal? thue = null) => new()
        {
            ThanhTien = thtien, ThueSuat = rate!, DoanhSoChuaThue = doanhSo, TienThueGTGT = thue
        };

        // Dữ liệu thật HĐ C26TQH/31: dòng chỉ có thtien và tsuat 0.08; HĐ ghi chưa thuế 12.412.037, thuế 992.963
        [Fact]
        public void MissingBaseAndTax_UsesThanhTien_AndTaxMatchesInvoiceTotal()
        {
            decimal[] thtien = { 555555, 777777, 111114, 37037, 120370, 37037, 74074, 5703704, 481481, 1527780, 416665, 541665, 2027778 };
            var lines = thtien.Select(t => L(t)).ToList();

            int computed = InvoiceLineAmounts.Fill(lines);

            Assert.Equal(13, computed);
            Assert.Equal(12412037m, lines.Sum(l => l.DoanhSoChuaThue));
            Assert.Equal(992963m, lines.Sum(l => l.TienThueGTGT));
            Assert.All(lines, l => Assert.NotNull(l.TienThueGTGT));
        }

        // Dữ liệu thật: HĐ bán ra 29, 30 và HĐ mua vào 11666
        [Theory]
        [InlineData(898148, 71852)]
        [InlineData(9629626, 770370)]
        [InlineData(296296, 23704)]
        public void SingleLine_TaxRoundsLikeInvoice(decimal thtien, decimal tax)
        {
            var lines = new List<InvoiceDetailItem> { L(thtien) };
            InvoiceLineAmounts.Fill(lines);
            Assert.Equal(tax, lines[0].TienThueGTGT);
        }

        [Fact]
        public void ExistingValues_AreKept()
        {
            var lines = new List<InvoiceDetailItem> { L(1000, "10%", doanhSo: 900, thue: 90) };
            Assert.Equal(0, InvoiceLineAmounts.Fill(lines));
            Assert.Equal(900m, lines[0].DoanhSoChuaThue);
            Assert.Equal(90m, lines[0].TienThueGTGT);
        }

        [Fact]
        public void TaxComputedPerRate_WhenInvoiceHasTwoRates()
        {
            var lines = new List<InvoiceDetailItem> { L(1005, "8%"), L(1005, "8%"), L(1000, "0.1") };
            InvoiceLineAmounts.Fill(lines);
            Assert.Equal(161m, lines[0].TienThueGTGT + lines[1].TienThueGTGT); // 2010 * 8% = 160,8 -> 161
            Assert.Equal(100m, lines[2].TienThueGTGT);
        }

        [Theory]
        [InlineData("KCT")]
        [InlineData("KKKNT")]
        [InlineData(null)]
        [InlineData("lạ")]
        public void NonNumericRate_LeavesTaxEmpty(string? rate)
        {
            var lines = new List<InvoiceDetailItem> { L(1000, rate) };
            InvoiceLineAmounts.Fill(lines);
            Assert.Equal(1000m, lines[0].DoanhSoChuaThue);
            Assert.Null(lines[0].TienThueGTGT);
        }

        [Fact]
        public void LineWithTax_IsKept_OnlyMissingLineIsComputed()
        {
            var lines = new List<InvoiceDetailItem> { L(1000, "10%", thue: 100), L(2000, "10%") };
            Assert.Equal(1, InvoiceLineAmounts.Fill(lines));
            Assert.Equal(100m, lines[0].TienThueGTGT);
            Assert.Equal(200m, lines[1].TienThueGTGT);
        }
    }
}
