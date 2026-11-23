using System;
using System.Linq;
using E_Tax;
using Xunit;

namespace E_Tax.Tests
{
    public class TaxListingTests
    {
        private const string Mua = "Mua vào";
        private const string Ban = "Bán ra";

        private static InvoiceDetailItem Line(string kyHieu, int so, string? rate, decimal? doanhSo, decimal? thue,
            string loai = Mua, string? key = null, string? ngay = "01/05/2026", string? mstNb = "0101234567", int? tt = 1) =>
            new InvoiceDetailItem
            {
                InvoiceKey = key ?? $"{mstNb}|{kyHieu}|{so}",
                KyHieuHD = kyHieu, SoHD = so, NgayHD = ngay!,
                LoaiHoaDon = loai,
                TenNguoiBan = "Công ty bán", MSTNguoiBan = mstNb!,
                TenNguoiMua = "Công ty mua", MSTNguoiMua = "0309999999",
                ThueSuat = rate!, DoanhSoChuaThue = doanhSo, TienThueGTGT = thue, TrangThaiHD = tt
            };

        [Fact]
        public void Build_OneRowPerInvoiceAndRate()
        {
            var g = TaxListing.Build(new[] { Line("A", 1, "10%", 100, 10), Line("A", 1, "10%", 200, 20), Line("A", 1, "8%", 50, 4) }, InvoiceType.Bought);
            Assert.Equal(new[] { "8%", "10%" }, g.Select(x => x.ThueSuat));
            Assert.Equal(300m, g[1].TongDoanhSo);
            Assert.Equal(30m, g[1].TongThue);
            Assert.Single(g[1].Rows);
        }

        [Fact]
        public void Build_SameKyHieuAndSoFromTwoSuppliers_AreNotMerged()
        {
            var g = TaxListing.Build(new[]
            {
                Line("A", 1, "10%", 100, 10, mstNb: "0101111111"),
                Line("A", 1, "10%", 200, 20, mstNb: "0102222222")
            }, InvoiceType.Bought);
            Assert.Equal(2, g[0].Rows.Count);
            Assert.Equal(300m, g[0].TongDoanhSo);
            Assert.Equal(new[] { "0101111111", "0102222222" }, g[0].Rows.Select(r => r.MstDoiTac).OrderBy(x => x));
        }

        [Fact]
        public void Build_RateOrder() =>
            Assert.Equal(new[] { "KCT", "KKKNT", "0%", "5%", "8%", "10%", "3.5%", "Không rõ" },
                TaxListing.Build(new[] { "10%", null, "5%", "KCT", "3.5%", "8%", "0%", "KKKNT" }
                    .Select((r, i) => Line("A", i, r, 1, 0)), InvoiceType.Bought).Select(x => x.ThueSuat));

        [Fact]
        public void Build_RateIsTrimmedAndUpperCased_EmptyIsUnknown()
        {
            var g = TaxListing.Build(new[] { Line("A", 1, " kct ", 1, null), Line("A", 2, "", 1, 0), Line("A", 3, "KCT", 1, null) }, InvoiceType.Bought);
            Assert.Equal(new[] { "KCT", "Không rõ" }, g.Select(x => x.ThueSuat));
            Assert.Equal(2, g[0].Rows.Count);
        }

        [Fact]
        public void Build_Sold_UsesBuyer() => Assert.Equal("0309999999", TaxListing.Build(new[] { Line("A", 1, "10%", 1, 0, loai: Ban) }, InvoiceType.Sold)[0].Rows[0].MstDoiTac);

        [Fact]
        public void Build_Sold_MissingBuyer_StaysEmpty()
        {
            var item = Line("A", 1, "10%", 1, 0, loai: Ban);
            item.MSTNguoiMua = null;
            item.TenNguoiMua = null;
            var row = TaxListing.Build(new[] { item }, InvoiceType.Sold)[0].Rows[0];
            Assert.Equal("", row.MstDoiTac);
            Assert.Equal("", row.TenDoiTac);
        }

        [Fact]
        public void Build_Bought_UsesSeller() => Assert.Equal("0101234567", TaxListing.Build(new[] { Line("A", 1, "10%", 1, 0) }, InvoiceType.Bought)[0].Rows[0].MstDoiTac);

        [Fact]
        public void Build_FiltersByType() => Assert.Single(TaxListing.Build(new[] { Line("A", 1, "10%", 1, 0), Line("B", 2, "10%", 1, 0, loai: Ban) }, InvoiceType.Bought).SelectMany(x => x.Rows));

        [Fact]
        public void Build_MissingAmount_IsFlagged() => Assert.True(TaxListing.Build(new[] { Line("A", 1, "10%", null, 1) }, InvoiceType.Bought)[0].Rows[0].ThieuSoTien);

        [Fact]
        public void Build_NullTaxOnKct_NotFlagged_OnPercent_Flagged()
        {
            var g = TaxListing.Build(new[] { Line("A", 1, "KCT", 100, null), Line("A", 2, "10%", 100, null) }, InvoiceType.Bought);
            Assert.False(g.Single(x => x.ThueSuat == "KCT").Rows[0].ThieuSoTien);
            Assert.True(g.Single(x => x.ThueSuat == "10%").Rows[0].ThieuSoTien);
        }

        [Fact]
        public void Build_NullAmountCountsAsZeroInSums()
        {
            var g = TaxListing.Build(new[] { Line("A", 1, "10%", 100, null), Line("A", 1, "10%", null, 5) }, InvoiceType.Bought);
            Assert.Equal(100m, g[0].TongDoanhSo);
            Assert.Equal(5m, g[0].TongThue);
            Assert.True(g[0].Rows[0].ThieuSoTien);
        }

        [Fact]
        public void Build_RowsSortedByDateThenKyHieuThenSo()
        {
            var g = TaxListing.Build(new[]
            {
                Line("B", 1, "10%", 1, 0, ngay: "02/05/2026"),
                Line("B", 2, "10%", 1, 0, ngay: "01/05/2026"),
                Line("A", 9, "10%", 1, 0, ngay: "01/05/2026"),
                Line("A", 3, "10%", 1, 0, ngay: "01/05/2026"),
                Line("A", 4, "10%", 1, 0, ngay: "15/04/2026")
            }, InvoiceType.Bought);
            Assert.Equal(new[] { 4, 3, 9, 2, 1 }, g[0].Rows.Select(r => r.SoHD!.Value));
        }

        [Fact]
        public void Build_NullStatusIsKept()
        {
            var row = TaxListing.Build(new[] { Line("A", 1, "10%", 1, 0, tt: null) }, InvoiceType.Bought)[0].Rows[0];
            Assert.Null(row.TrangThai);
        }

        [Theory]
        [InlineData("10%")]
        [InlineData("10 %")]
        [InlineData("10.0%")]
        [InlineData("10")]
        [InlineData("0.1")]
        [InlineData("0,1")]
        [InlineData(" 10,0 % ")]
        public void NormalizeRate_TenPercentVariants(string raw) => Assert.Equal("10%", TaxListing.NormalizeRate(raw));

        [Theory]
        [InlineData("0", "0%")]
        [InlineData("0%", "0%")]
        [InlineData("3.5%", "3.5%")]
        [InlineData("0.035", "3.5%")]
        [InlineData("khac", "KHAC")]
        [InlineData("kkknt", "KKKNT")]
        [InlineData(null, "Không rõ")]
        public void NormalizeRate_Others(string? raw, string expected) => Assert.Equal(expected, TaxListing.NormalizeRate(raw));

        [Fact]
        public void Build_RateVariantsShareOneGroup_OtherStaysSeparate()
        {
            var g = TaxListing.Build(new[] { Line("A", 1, "10%", 100, 10), Line("A", 2, "0.1", 100, 10), Line("A", 3, "10", 100, 10), Line("A", 4, "KHAC", 1, 1) }, InvoiceType.Bought);
            Assert.Equal(new[] { "10%", "KHAC" }, g.Select(x => x.ThueSuat));
            Assert.Equal(3, g[0].Rows.Count);
            Assert.Equal(30m, g[0].TongThue);
        }

        [Fact]
        public void CountNonVnd_FiltersByTypeAndTreatsEmptyAsVnd()
        {
            SearchResult S(string loai, string? cur) => new SearchResult { Loai_hoa_don = loai, Don_vi_tien_te = cur! };
            var list = new[] { S("Mua vào", "USD"), S("Mua vào", " vnd "), S("Mua vào", "VNĐ"), S("Mua vào", ""), S("Mua vào", null), S("Mua vào", "eur"), S("Bán ra", "USD") };
            Assert.Equal(2, TaxListing.CountNonVnd(list, InvoiceType.Bought));
            Assert.Equal(1, TaxListing.CountNonVnd(list, InvoiceType.Sold));
        }

        [Fact]
        public void ExportBlocked_WhenDetailFailed()
        {
            Assert.Contains("2", TaxListing.ExportBlockReason(2));
            Assert.Null(TaxListing.ExportBlockReason(0));
        }
    }
}
