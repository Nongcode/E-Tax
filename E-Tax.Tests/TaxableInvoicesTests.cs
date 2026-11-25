using System.Linq;
using ClosedXML.Excel;
using E_Tax;
using Xunit;

namespace E_Tax.Tests
{
    public class TaxableInvoicesTests
    {
        private const string Mua = "Mua vào";
        private const string Ban = "Bán ra";

        private static InvoiceDetailItem Line(string key, int? status, decimal amount = 100m, decimal tax = 10m, string loai = Mua, string rate = "10%") => new()
        {
            InvoiceKey = key, TrangThaiHD = status, LoaiHoaDon = loai, ThueSuat = rate, DoanhSoChuaThue = amount, TienThueGTGT = tax,
            KyHieuHD = "C26TAA", SoHD = 1, NgayHD = "01/05/2026", TenSanPham = "Hàng " + key
        };

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        [InlineData(3, 0)]
        [InlineData(5, 0)]
        [InlineData(4, 1)]
        [InlineData(6, 1)]
        [InlineData(0, 2)]
        [InlineData(7, 2)]
        [InlineData(null, 2)]
        public void Classify_FollowsRelation(int? status, int expected) =>
            Assert.Equal((TaxInclusion)expected, TaxableInvoices.Classify(status));

        [Fact]
        public void Select_CountsInvoicesNotLines_AndKeepsOnlyIncluded()
        {
            var s = TaxableInvoices.Select(new[]
            {
                Line("goc", 1), Line("goc", 1), Line("huy", 6), Line("huy", 6), Line("thay", 4), Line("dc", 3), Line("bidc", 5), Line("tt", 2), Line("la", null), Line("la", null)
            });
            Assert.Equal(new[] { "bidc", "dc", "goc", "goc", "tt" }, s.Items.Select(i => i.InvoiceKey).OrderBy(k => k, System.StringComparer.Ordinal));
            Assert.Equal(1, s.Cancelled);
            Assert.Equal(1, s.Replaced);
            Assert.Equal(1, s.Unknown);
            Assert.Equal(3, s.Adjustments);
        }

        [Fact]
        public void Select_SameKeyInBothDirections_CountsTwoInvoices()
        {
            var s = TaxableInvoices.Select(new[] { Line("x", 6), Line("x", 6, loai: Ban) });
            Assert.Equal(2, s.Cancelled);
        }

        [Fact]
        public void Select_HeadersWithoutLines_AreCounted()
        {
            var headers = new[]
            {
                new SearchResult { Id = "h1", Trang_thai_hoa_don = null, Loai_hoa_don = Mua },
                new SearchResult { Id = "h2", Trang_thai_hoa_don = 6, Loai_hoa_don = Mua },
                new SearchResult { Id = "h3", Trang_thai_hoa_don = 6, Loai_hoa_don = Ban }
            };
            var s = TaxableInvoices.Select(new[] { Line("h2", 6) }, headers);
            Assert.Equal(1, s.Unknown);
            Assert.Equal(2, s.Cancelled);
            Assert.Equal(1, TaxableInvoices.Select(new InvoiceDetailItem[0], headers, Ban).Cancelled);
        }

        [Fact]
        public void Notes_StateExcludedCountAndReason()
        {
            var note = Assert.Single(TaxableInvoices.Select(new[] { Line("a", 6), Line("b", 6), Line("c", 4) }).ExclusionNotes());
            Assert.Contains("3 hóa đơn", note);
            Assert.Contains("2 đã hủy", note);
            Assert.Contains("1 bị thay thế", note);
            Assert.Empty(TaxableInvoices.Select(new[] { Line("a", 1) }).ExclusionNotes());
        }

        [Fact]
        public void UnknownBlockReason_NamesCount()
        {
            Assert.Contains("2 hóa đơn", TaxableInvoices.UnknownBlockReason(2));
            Assert.Null(TaxableInvoices.UnknownBlockReason(0));
        }

        [Fact]
        public void TaxListing_ExcludesCancelledReplacedAndUnknown_KeepsAdjustmentPairs()
        {
            var g = TaxListing.Build(new[] { Line("g", 1), Line("h", 6), Line("t", 4), Line("n", null), Line("d", 3), Line("b", 5), Line("r", 2) }, InvoiceType.Bought);
            Assert.Equal(4, g[0].Rows.Count);
            Assert.Equal(400m, g[0].TongDoanhSo);
            Assert.Equal(40m, g[0].TongThue);
        }

        [Fact]
        public void TaxListing_StatusNote_CountsInvoicesNotRows()
        {
            var g = TaxListing.Build(new[] { Line("d", 3, rate: "10%"), Line("d", 3, rate: "8%"), Line("g", 1) }, InvoiceType.Bought);
            var note = Assert.Single(TaxListing.ExportNotes(g));
            Assert.Contains("1 hóa đơn", note);
            Assert.Contains("dấu", note);
        }

        [Fact]
        public void VatReturn_CancelledInvoiceDoesNotChangeTotals()
        {
            var items = new[] { Line("g", 1, 1000m, 100m), Line("h", 6, 500m, 50m) };
            var r = VatReturnBuilder.Build(TaxListing.Build(items, InvoiceType.Bought), TaxListing.Build(items, InvoiceType.Sold),
                new VatReturnInput(0, 0, 0, 0, 0, 0, 0, 0, 0));
            Assert.Equal(1000m, r.Values["23"]);
            Assert.Equal(100m, r.Values["24"]);
        }

        [Fact]
        public void VatReturn_StatusWarning_CountsInvoicesNotRows()
        {
            var items = new[] { Line("d", 3, rate: "10%"), Line("d", 3, rate: "5%"), Line("g", 1) };
            var r = VatReturnBuilder.Build(TaxListing.Build(items, InvoiceType.Bought), new TaxListingGroup[0], new VatReturnInput(0, 0, 0, 0, 0, 0, 0, 0, 0));
            var w = Assert.Single(r.Warnings);
            Assert.Contains("1 hóa đơn", w);
        }

        [Fact]
        public void ReductionAppendix_ExcludesCancelledReplacedAndUnknown()
        {
            var lines = ReductionAppendix.Build(new[]
            {
                Line("g", 1, 100m, 8m, Ban, "8%"), Line("h", 6, 200m, 16m, Ban, "8%"), Line("t", 4, 300m, 24m, Ban, "8%"), Line("n", null, 400m, 32m, Ban, "8%")
            });
            var l = Assert.Single(lines);
            Assert.Equal(100m, l.GiaTri);
        }

        [Fact]
        public void AccountingExport_WritesOnlyIncludedInvoices()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"acc_{System.Guid.NewGuid():N}.xlsx");
            try
            {
                var profile = new AccountingProfile("T", true, new[] { new AccountingColumn("Tên", "TenSanPham", null) });
                AccountingExporter.Export(profile, new[] { Line("g", 1), Line("h", 6), Line("t", 4), Line("n", null) }, path);
                using var wb = new XLWorkbook(path);
                var ws = wb.Worksheet(1);
                Assert.Equal("Hàng g", ws.Cell(2, 1).GetString());
                Assert.True(ws.Cell(3, 1).IsEmpty());
            }
            finally { System.IO.File.Delete(path); }
        }

        [Fact]
        public void BuiltInProfiles_CarryDirectionPartyAndStatusColumns()
        {
            var fields = AccountingProfiles.BuiltIn()[0].Columns.Select(c => c.Field).ToList();
            Assert.Contains(nameof(InvoiceDetailItem.LoaiHoaDon), fields);
            Assert.Contains(nameof(InvoiceDetailItem.TenNguoiMua), fields);
            Assert.Contains(nameof(InvoiceDetailItem.MSTNguoiMua), fields);
            Assert.Contains(nameof(InvoiceDetailItem.TrangThaiHD), fields);
        }
    }
}
