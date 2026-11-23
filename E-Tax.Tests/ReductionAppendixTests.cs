using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using E_Tax;
using Xunit;

namespace E_Tax.Tests
{
    public class ReductionAppendixTests
    {
        private static InvoiceDetailItem L(string? ten, decimal? doanhSo, string? rate = "8%", string loai = "Bán ra") =>
            new() { LoaiHoaDon = loai, TenSanPham = ten!, DoanhSoChuaThue = doanhSo, ThueSuat = rate!, TrangThaiHD = 1 };

        [Fact]
        public void Build_OnlySoldLinesAtEightPercent_AcceptingRateVariants()
        {
            var lines = ReductionAppendix.Build(new[]
            {
                L("A", 100, "0.08"), L("B", 200, "8"), L("C", 300, "8%"),
                L("D", 400, "10%"), L("E", 500, "8%", "Mua vào"), L("F", 600, null)
            });
            Assert.Equal(new[] { "A", "B", "C" }, lines.Select(l => l.TenHang));
        }

        [Fact]
        public void Build_GroupsByNameIgnoringCaseAndSpaces_KeepsFirstSpelling()
        {
            var lines = ReductionAppendix.Build(new[] { L("Gạo ST25", 600_000), L("  gạo st25 ", 400_000) });
            var line = Assert.Single(lines);
            Assert.Equal("Gạo ST25", line.TenHang);
            Assert.Equal(1_000_000m, line.GiaTri);
            Assert.Equal(10m, line.ThueSuatGoc);
            Assert.Equal(8m, line.ThueSuatGiam);
            Assert.Equal(20_000m, line.ThueDuocGiam);
        }

        [Fact]
        public void Build_RoundsHalfAwayFromZero_OnGroupedTotal()
        {
            // 25 * 2% = 0,5 -> 1; 2 dòng 12 + 13 gộp trước khi làm tròn
            var lines = ReductionAppendix.Build(new[] { L("X", 12), L("X", 13), L("Y", -25) });
            Assert.Equal(1m, lines.Single(l => l.TenHang == "X").ThueDuocGiam);
            Assert.Equal(-1m, lines.Single(l => l.TenHang == "Y").ThueDuocGiam);
        }

        [Fact]
        public void Build_MissingAmountIsIgnored_EmptyNameGoesToPlaceholder_SortedByName()
        {
            var lines = ReductionAppendix.Build(new[] { L("b", 100), L("A", null), L("a", 100), L("  ", 50), L(null, 50) });
            Assert.Equal(new[] { "(Không có tên hàng)", "a", "b" }, lines.Select(l => l.TenHang));
            Assert.Equal(100m, lines[0].GiaTri);
        }

        [Fact]
        public void ExportReductionAppendix_BannerFirst_HeaderRowsAndTotal()
        {
            string path = Path.Combine(Path.GetTempPath(), $"pl_{System.Guid.NewGuid():N}.xlsx");
            try
            {
                var lines = ReductionAppendix.Build(new[] { L("A", 1_000_000), L("B", 12.5m) });
                ExcelExporter.ExportReductionAppendix(lines, TaxPeriod.Month(2026, 5), "0101234567", path);
                using var wb = new XLWorkbook(path);
                var ws = wb.Worksheet(1);
                Assert.Equal("CHƯA ĐỐI CHIẾU VỚI MẪU GỐC", ws.Cell(1, 1).GetString());
                var text = string.Join("\n", ws.CellsUsed().Select(c => c.GetString()));
                Assert.Contains("0101234567", text);
                Assert.Contains("Tháng 5/2026", text);
                Assert.DoesNotContain("tương thích", text);
                var a = ws.CellsUsed().First(c => c.GetString() == "A").Address.RowNumber;
                Assert.Equal(1m, ws.Cell(a, 1).GetValue<decimal>());
                Assert.Equal(1_000_000m, ws.Cell(a, 3).GetValue<decimal>());
                Assert.Equal("#,##0", ws.Cell(a, 3).Style.NumberFormat.Format);
                Assert.Equal(20_000m, ws.Cell(a, 6).GetValue<decimal>());
                var b = ws.CellsUsed().First(c => c.GetString() == "B").Address.RowNumber;
                Assert.Equal("#,##0.############", ws.Cell(b, 3).Style.NumberFormat.Format);
                var total = ws.CellsUsed().First(c => c.GetString() == "TỔNG CỘNG").Address.RowNumber;
                Assert.Equal(1_000_012.5m, ws.Cell(total, 3).GetValue<decimal>());
                Assert.Equal(20_000m, ws.Cell(total, 6).GetValue<decimal>());
            }
            finally { File.Delete(path); }
        }
    }
}
