using System.Linq;
using ClosedXML.Excel;
using E_Tax;
using Xunit;

namespace E_Tax.Tests
{
    public class KeKhaiExportRulesTests
    {
        private const string Mua = "Mua vào";
        private const string Ban = "Bán ra";
        private static readonly SearchScope Both = new(true, true);
        private static readonly TaxPeriod May = TaxPeriod.Month(2026, 5);

        private static InvoiceDetailItem Line(string key, decimal? amount, decimal? tax, string loai = Mua, int? status = 1) => new()
        {
            InvoiceKey = key, LoaiHoaDon = loai, TrangThaiHD = status, DoanhSoChuaThue = amount, TienThueGTGT = tax, ThueSuat = "10%",
            KyHieuHD = "C26TAA", SoHD = 1, NgayHD = "01/05/2026", TenSanPham = "Hàng"
        };

        private static SearchResult Header(string id, decimal? amount, decimal? tax, string loai = Mua, int? status = 1, string? originalDate = null) => new()
        {
            Id = id, Loai_hoa_don = loai, Trang_thai_hoa_don = status, Tong_tien_chua_thue = amount, Tong_tien_thue = tax,
            Ngay_lap = "2026-05-10T00:00:00", Ngay_lap_goc = originalDate, Ma_so_thue = "0300000001"
        };

        // C2
        [Fact]
        public void Blocks_BothDirections_AllowsEverything()
        {
            var b = KeKhaiLogic.DecideBlocks(0, 0, 0, Both);
            Assert.Null(b.Vat); Assert.Null(b.Appendix); Assert.Null(b.Accounting);
        }

        [Fact]
        public void Blocks_BoughtOnly_BlocksVatAndAppendix_AllowsAccounting()
        {
            var b = KeKhaiLogic.DecideBlocks(0, 0, 0, new SearchScope(true, false));
            Assert.Contains("Bán ra", b.Vat);
            Assert.Contains("Bán ra", b.Appendix);
            Assert.Null(b.Accounting);
        }

        [Fact]
        public void Blocks_SoldOnly_BlocksVat_AllowsAppendixAndAccounting()
        {
            var b = KeKhaiLogic.DecideBlocks(0, 0, 0, new SearchScope(false, true));
            Assert.Contains("Mua vào", b.Vat);
            Assert.Null(b.Appendix);
            Assert.Null(b.Accounting);
        }

        // C1, C3
        [Theory]
        [InlineData(2, 0, "2 hóa đơn")]
        [InlineData(0, 3, "3 hóa đơn")]
        public void Blocks_UnknownOrForeignCurrency_BlockAllThree(int unknown, int nonVnd, string expected)
        {
            var b = KeKhaiLogic.DecideBlocks(0, unknown, nonVnd, Both);
            Assert.Contains(expected, b.Vat);
            Assert.Contains(expected, b.Appendix);
            Assert.Contains(expected, b.Accounting);
        }

        // N4: vấn đề chỉ ở Mua vào không được khóa phụ lục (phụ lục chỉ dùng Bán ra)
        [Theory]
        [InlineData(2, 0)]
        [InlineData(0, 3)]
        public void Blocks_ProblemOnlyInPurchases_LeavesAppendixOpen(int unknown, int nonVnd)
        {
            var b = KeKhaiLogic.DecideBlocks(0, unknown, nonVnd, Both, unknownSold: 0, nonVndSold: 0);
            Assert.NotNull(b.Vat);
            Assert.NotNull(b.Accounting);
            Assert.Null(b.Appendix);
        }

        [Fact]
        public void Blocks_ProblemInSales_BlocksAppendixWithSalesCount()
        {
            var b = KeKhaiLogic.DecideBlocks(0, 5, 0, Both, unknownSold: 2, nonVndSold: 0);
            Assert.Contains("5 hóa đơn", b.Vat);
            Assert.Contains("2 hóa đơn", b.Appendix);
        }

        [Fact]
        public void Blocks_FailedInvoices_BlockAllThree()
        {
            var b = KeKhaiLogic.DecideBlocks(1, 0, 0, Both);
            Assert.NotNull(b.Vat); Assert.NotNull(b.Appendix); Assert.NotNull(b.Accounting);
        }

        [Fact]
        public void ComposeWarnings_RemovesDuplicates()
        {
            Assert.Equal(new[] { "a", "b" }, KeKhaiLogic.ComposeWarnings(null, new[] { "a", "b" }, new[] { "a" }));
        }

        // I1
        [Fact]
        public void CrossPeriod_CountsAdjustmentAndReplacementWithOriginalOutsidePeriod()
        {
            var headers = new[]
            {
                Header("a", 1, 1, status: 3, originalDate: "2026-04-15T00:00:00"),
                Header("b", 1, 1, status: 2, originalDate: "2026-03-01T00:00:00"),
                Header("c", 1, 1, status: 3, originalDate: "2026-05-02T00:00:00"),
                Header("d", 1, 1, status: 1, originalDate: "2026-04-15T00:00:00"),
                Header("e", 1, 1, loai: Ban, status: 3, originalDate: "2026-04-15T00:00:00")
            };
            Assert.Equal(3, TaxableInvoices.CountCrossPeriod(headers, May));
            Assert.Equal(1, TaxableInvoices.CountCrossPeriod(headers, May, Ban));
        }

        [Fact]
        public void OutputNotes_CrossPeriod_SaysAccountantDecides()
        {
            var notes = TaxableInvoices.OutputNotes(new InvoiceDetailItem[0], new[] { Header("a", null, null, status: 3, originalDate: "2026-04-15T00:00:00") }, May);
            var n = Assert.Single(notes);
            Assert.Contains("1 hóa đơn", n);
            Assert.Contains("kế toán", n);
        }

        // I2
        [Fact]
        public void Mismatch_WithinOneVndPerLine_IsNotCounted()
        {
            var items = new[] { Line("a", 100.5m, 10m), Line("a", 200.5m, 20m) };
            Assert.Equal(0, TaxableInvoices.CountTotalMismatches(items, new[] { Header("a", 302m, 29m) }));
        }

        [Theory]
        [InlineData(303.5, 30)]
        [InlineData(301, 27.5)]
        public void Mismatch_BeyondTolerance_IsCounted(double headerAmount, double headerTax)
        {
            var items = new[] { Line("a", 100.5m, 10m), Line("a", 200.5m, 20m) };
            Assert.Equal(1, TaxableInvoices.CountTotalMismatches(items, new[] { Header("a", (decimal)headerAmount, (decimal)headerTax) }));
        }

        [Fact]
        public void Mismatch_MissingHeaderValues_AndExcludedInvoices_AreSkipped()
        {
            var items = new[] { Line("a", 100m, 10m), Line("h", 100m, 10m, status: 6) };
            var headers = new[] { Header("a", null, null), Header("h", 999m, 999m, status: 6) };
            Assert.Equal(0, TaxableInvoices.CountTotalMismatches(items, headers));
        }

        [Fact]
        public void OutputNotes_Mismatch_IsReported()
        {
            var notes = TaxableInvoices.OutputNotes(new[] { Line("a", 100m, 10m) }, new[] { Header("a", 500m, 10m) }, May);
            Assert.Contains(notes, n => n.Contains("1 hóa đơn") && n.Contains("không khớp"));
        }

        // I3
        [Fact]
        public void SearchTotals_FewerRetrieved_Warns_PerDirection()
        {
            var totals = new SearchTotals();
            totals.Record(Mua, new System.DateTime(2026, 5, 1), 2);
            totals.Record(Mua, new System.DateTime(2026, 5, 31), 1);
            totals.Record(Ban, new System.DateTime(2026, 5, 1), 1);
            var w = Assert.Single(totals.Warnings(new[] { Header("a", 1, 1), Header("b", 1, 1), Header("c", 1, 1, loai: Ban) }.Concat(new[] { Header("a", 1, 1) })));
            Assert.Contains(Mua, w);
            Assert.Contains("3", w);
            Assert.Contains("2", w);
        }

        [Fact]
        public void SearchTotals_SameChunkKeepsLargestTotal_CompleteHasNoWarning()
        {
            var totals = new SearchTotals();
            totals.Record(Mua, new System.DateTime(2026, 5, 1), 1);
            totals.Record(Mua, new System.DateTime(2026, 5, 1), 2);
            Assert.Single(totals.Warnings(new[] { Header("a", 1, 1) }));
            Assert.Empty(totals.Warnings(new[] { Header("a", 1, 1), Header("b", 1, 1) }));
        }

        [Theory]
        [InlineData("{\"datas\":[],\"total\":7}", 7)]
        [InlineData("{\"datas\":[]}", null)]
        [InlineData("khong phai json", null)]
        [InlineData("{\"total\":\"x\"}", null)]
        public void ParseTotal_ReadsOnlyNumericTotal(string json, int? expected) =>
            Assert.Equal(expected, InvoiceSearchResults.ParseTotal(json));

        // C3: cảnh báo phải có trong file
        [Fact]
        public void ReductionAppendix_WritesNotesIntoFile()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pl_{System.Guid.NewGuid():N}.xlsx");
            try
            {
                var lines = new[] { new ReductionLine("A", 100m, 10m, 8m, 2m) };
                ExcelExporter.ExportReductionAppendix(lines, May, "0101234567", path, new[] { "Cảnh báo phụ lục" });
                using var wb = new XLWorkbook(path);
                Assert.Contains(wb.Worksheet(1).CellsUsed(), c => c.GetString() == "Cảnh báo phụ lục");
            }
            finally { System.IO.File.Delete(path); }
        }

        [Fact]
        public void AccountingExport_VerifiedProfile_StillWritesNotes()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"acc_{System.Guid.NewGuid():N}.xlsx");
            try
            {
                var profile = new AccountingProfile("T", true, new[] { new AccountingColumn("Tên", "TenSanPham", null) });
                AccountingExporter.Export(profile, new[] { Line("a", 1m, 1m) }, path, new[] { "Cảnh báo kế toán" });
                using var wb = new XLWorkbook(path);
                Assert.True(wb.TryGetWorksheet("Ghi chú", out var note));
                Assert.Contains(note.CellsUsed(), c => c.GetString() == "Cảnh báo kế toán");
                Assert.DoesNotContain(note.CellsUsed(), c => c.GetString().Contains(AccountingExporter.UnverifiedWarning));
            }
            finally { System.IO.File.Delete(path); }
        }

        [Fact]
        public void TaxListing_WritesSuppliedNotesIntoFile()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bk_{System.Guid.NewGuid():N}.xlsx");
            try
            {
                var groups = TaxListing.Build(new[] { Line("a", 100m, 10m) }, InvoiceType.Bought);
                ExcelExporter.ExportTaxListing(groups, InvoiceType.Bought, May, path, new[] { "Có 2 hóa đơn không có dòng hàng" });
                using var wb = new XLWorkbook(path);
                Assert.Contains("Có 2 hóa đơn không có dòng hàng", wb.Worksheet(1).Cell("A3").GetString());
            }
            finally { System.IO.File.Delete(path); }
        }
    }
}
