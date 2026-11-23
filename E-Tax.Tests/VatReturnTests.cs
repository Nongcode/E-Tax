using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using E_Tax;
using Xunit;

namespace E_Tax.Tests
{
    public class VatReturnTests
    {
        private static TaxListingGroup G(string rate, decimal doanhSo, decimal thue, bool thieu = false) =>
            new(rate, new[] { new TaxListingRow("A", 1, "01/05/2026", "X", "0101234567", rate, doanhSo, thue, 1, thieu) }, doanhSo, thue);

        // Mua vào: 10% 1.000/100, 5% 200/10, KCT 50/0 -> tổng mua 1.250 và thuế 110.
        private static readonly IReadOnlyList<TaxListingGroup> Bought = new[] { G("KCT", 50, 0), G("5%", 200, 10), G("10%", 1000, 100) };

        // Bán ra: KCT 300; 0% 400; 5% 1.000/50; 10% 2.000/200; KKKNT 100.
        private static readonly IReadOnlyList<TaxListingGroup> Sold =
            new[] { G("KCT", 300, 0), G("KKKNT", 100, 0), G("0%", 400, 0), G("5%", 1000, 50), G("10%", 2000, 200) };

        private static VatReturnInput Input(decimal c22 = 0, decimal c37 = 0, decimal c38 = 0, decimal c39a = 0, decimal c40b = 0,
            decimal c42 = 0, decimal c23a = 0, decimal c24a = 0, decimal adj = 0) =>
            new(c22, c37, c38, c39a, c40b, c42, c23a, c24a, adj);

        [Fact]
        public void Build_PayableCase_ComputesEveryIndicator()
        {
            // [23]=1.250+50=1.300; [24]=110+10=120; [25]=120-5=115
            // [26]=300; [29]=400; [30]=1.000; [31]=50; [32]=2.000; [33]=200; [32a]=100
            // [27]=400+1.000+2.000+100=3.500; [28]=250; [34]=3.800; [35]=250; [36]=250-115=135
            // X=135-20+5-3-2=115; [40a]=115; [40]=115-20=95; [41]=0; [43]=0-0
            var r = VatReturnBuilder.Build(Bought, Sold, Input(c22: 20, c37: 5, c38: 3, c39a: 2, c40b: 20, c23a: 50, c24a: 10, adj: -5));
            var v = r.Values;
            Assert.Equal(VatReturnBuilder.Codes.OrderBy(x => x), v.Keys.OrderBy(x => x));
            Assert.Equal(1300m, v["23"]); Assert.Equal(50m, v["23a"]);
            Assert.Equal(120m, v["24"]); Assert.Equal(10m, v["24a"]);
            Assert.Equal(115m, v["25"]);
            Assert.Equal(300m, v["26"]); Assert.Equal(400m, v["29"]);
            Assert.Equal(1000m, v["30"]); Assert.Equal(50m, v["31"]);
            Assert.Equal(2000m, v["32"]); Assert.Equal(200m, v["33"]); Assert.Equal(100m, v["32a"]);
            Assert.Equal(3500m, v["27"]); Assert.Equal(250m, v["28"]);
            Assert.Equal(3800m, v["34"]); Assert.Equal(250m, v["35"]); Assert.Equal(135m, v["36"]);
            Assert.Equal(115m, v["40a"]); Assert.Equal(95m, v["40"]);
            Assert.Equal(0m, v["41"]); Assert.Equal(0m, v["43"]);
            Assert.Empty(r.Warnings);
        }

        [Fact]
        public void Build_CarryForwardCase_Uses41And42()
        {
            // [24]=110 -> [25]=110; [35]=250; [36]=140; CT22=300 -> X=140-300=-160
            // [40a]=0; [40]=0; [41]=160; [42]=60; [43]=160-60=100
            var r = VatReturnBuilder.Build(Bought, Sold, Input(c22: 300, c42: 60));
            Assert.Equal(140m, r.Values["36"]);
            Assert.Equal(0m, r.Values["40a"]); Assert.Equal(0m, r.Values["40"]);
            Assert.Equal(160m, r.Values["41"]);
            Assert.Equal(100m, r.Values["43"]);
            Assert.Empty(r.Warnings);
        }

        [Fact]
        public void Build_Sold8Percent_GoesTo32And33WithWarning()
        {
            var sold = new[] { G("8%", 500, 40), G("10%", 2000, 200) };
            var r = VatReturnBuilder.Build(Bought, sold, Input());
            Assert.Equal(2500m, r.Values["32"]); Assert.Equal(240m, r.Values["33"]);
            var w = Assert.Single(r.Warnings);
            Assert.Contains("8%", w); Assert.Contains("HTKK", w);
        }

        [Fact]
        public void Build_UnknownRate_IsExcludedWithWarning()
        {
            var bought = new[] { G("10%", 1000, 100), G(TaxListing.UnknownRate, 700, 70) };
            var sold = new[] { G("10%", 2000, 200), G("7%", 900, 63) };
            var r = VatReturnBuilder.Build(bought, sold, Input());
            Assert.Equal(1000m, r.Values["23"]); Assert.Equal(100m, r.Values["24"]);
            Assert.Equal(2000m, r.Values["32"]); Assert.Equal(200m, r.Values["33"]);
            Assert.Equal(2, r.Warnings.Count);
            Assert.Contains(r.Warnings, w => w.Contains(TaxListing.UnknownRate) && w.Contains("700") && w.Contains("70"));
            Assert.Contains(r.Warnings, w => w.Contains("7%") && w.Contains("900") && w.Contains("63"));
        }

        [Fact]
        public void Build_MissingAmountRows_Warn()
        {
            var r = VatReturnBuilder.Build(new[] { G("10%", 1000, 100, thieu: true) }, new[] { G("10%", 10, 1, thieu: true) }, Input());
            Assert.Equal(2, r.Warnings.Count);
        }

        [Fact]
        public void Build_Ct42GreaterThanCt41_Warns()
        {
            var r = VatReturnBuilder.Build(Bought, Sold, Input(c22: 300, c42: 200));
            Assert.Equal(160m, r.Values["41"]);
            Assert.Equal(-40m, r.Values["43"]);
            Assert.Contains(r.Warnings, w => w.Contains("[42]") && w.Contains("[41]"));
        }

        [Fact]
        public void Build_NegativeInputs_WarnExceptCt25Adjust()
        {
            var ok = VatReturnBuilder.Build(Bought, Sold, Input(adj: -5));
            Assert.Empty(ok.Warnings);
            var bad = VatReturnBuilder.Build(Bought, Sold, Input(c22: -1));
            Assert.Contains(bad.Warnings, w => w.Contains("[22]"));
        }

        [Fact]
        public void ExportVatReturn_WritesDraftBannerIndicatorsAndWarnings()
        {
            string path = Path.Combine(Path.GetTempPath(), $"vat_{System.Guid.NewGuid():N}.xlsx");
            try
            {
                var r = VatReturnBuilder.Build(Bought, new[] { G("8%", 500, 40) }, Input());
                ExcelExporter.ExportVatReturn(r, TaxPeriod.Month(2026, 5), "0101234567", path);

                using var wb = new XLWorkbook(path);
                var ws = wb.Worksheet(1);
                Assert.Equal("CHƯA ĐỐI CHIẾU VỚI MẪU GỐC", ws.Cell(1, 1).GetString());
                var text = string.Join("\n", ws.CellsUsed().Select(c => c.GetString()));
                Assert.Contains("0101234567", text);
                Assert.Contains("Tháng 5/2026", text);
                Assert.DoesNotContain("tương thích", text);
                Assert.Contains(r.Warnings[0], text);

                var code32 = ws.CellsUsed().First(c => c.GetString() == "[32]");
                Assert.Equal(500m, ws.Cell(code32.Address.RowNumber, 3).GetValue<decimal>());
            }
            finally { File.Delete(path); }
        }
    }
}
