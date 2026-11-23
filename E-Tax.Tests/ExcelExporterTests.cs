using ClosedXML.Excel;
using E_Tax;

namespace E_Tax.Tests
{
    public class ExcelExporterTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-excel-" + Guid.NewGuid());

        public ExcelExporterTests()
        {
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private string NewFile(string name) => Path.Combine(_dir, name);

        [Fact]
        public void SearchResults_HeaderAndValues()
        {
            var results = new List<SearchResult>
            {
                new SearchResult
                {
                    Ma_so_thue = "0101234567", Ky_hieu_ma_so = 1, Ky_hieu_hoa_don = "C24TAA", So_hoa_don = 123,
                    Tong_tien_chua_thue = 1000000m, Tong_tien_phi = new List<decimal?> { 1m, 2m },
                    Loai_hoa_don = "Hóa đơn mua vào"
                },
                new SearchResult { Ma_so_thue = "0309999999", Loai_hoa_don = "Hóa đơn bán ra" }
            };
            string path = NewFile("ds.xlsx");

            ExcelExporter.ExportSearchResults(results, path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet("DanhSachHoaDon");
            Assert.Equal("DANH SÁCH HÓA ĐƠN", ws.Cell("A1").GetString());
            Assert.Equal("MÃ SỐ THUẾ", ws.Cell("A4").GetString());
            Assert.Equal("LOẠI HÓA ĐƠN", ws.Cell("P4").GetString());
            Assert.Equal("0101234567", ws.Cell("A5").GetString());
            Assert.Equal("Hóa đơn mua vào", ws.Cell("P5").GetString());
            Assert.Equal("Hóa đơn bán ra", ws.Cell("P6").GetString());
            Assert.Equal(1000000d, ws.Cell("G5").GetDouble());
            Assert.Equal("#,##0", ws.Cell("G5").Style.NumberFormat.Format);
            Assert.Equal(3d, ws.Cell("J5").GetDouble());
        }

        [Fact]
        public void SearchResults_NoteColumn()
        {
            var results = new List<SearchResult> { new SearchResult { Id = "1", Ma_so_thue = "0101234567" } };
            string path = NewFile("ds-note.xlsx");

            ExcelExporter.ExportSearchResults(results, path, getNote: _ => "ghi chú A");

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet("DanhSachHoaDon");
            Assert.Equal("GHI CHÚ", ws.Cell("Q4").GetString());
            Assert.Equal("ghi chú A", ws.Cell("Q5").GetString());
        }

        [Fact]
        public void DetailSummary_NumericCells()
        {
            var rows = new List<Dictionary<string, string>>
            {
                new() { ["stt"] = "1", ["ten"] = "Hàng A", ["thtien"] = "1234567.5", ["sluong"] = "abc" }
            };
            string path = NewFile("ct.xlsx");

            ExcelExporter.ExportDetailSummary(rows, path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet("Chi Tiết Hóa Đơn");
            Assert.Equal("CHI TIẾT HÓA ĐƠN", ws.Cell("A1").GetString());
            Assert.Equal("STT", ws.Cell("A4").GetString());
            Assert.Equal("1", ws.Cell("A5").GetString());
            var thtien = ws.Cell("K5");
            Assert.Equal(XLDataType.Number, thtien.DataType);
            Assert.Equal(1234567.5d, thtien.GetDouble());
            Assert.Equal("#,##0", thtien.Style.NumberFormat.Format);
            Assert.Equal(XLAlignmentHorizontalValues.Right, thtien.Style.Alignment.Horizontal);
            // Giá trị không phải số giữ nguyên dạng chữ
            Assert.Equal(XLDataType.Text, ws.Cell("I5").DataType);
            Assert.Equal("abc", ws.Cell("I5").GetString());
        }

        [Fact]
        public void DetailSummary_Empty_WritesErrorSheet()
        {
            string path = NewFile("empty.xlsx");

            ExcelExporter.ExportDetailSummary(new List<Dictionary<string, string>>(), path);

            using var wb = new XLWorkbook(path);
            Assert.Single(wb.Worksheets);
            var ws = wb.Worksheet("Lỗi");
            Assert.Equal("Không tìm thấy chi tiết hàng hóa/dịch vụ nào từ các hóa đơn đã tìm.", ws.Cell("A1").GetString());
        }

        private static TaxListingRow ListingRow(string kyHieu, int so, string rate, decimal ds, decimal thue, int? tt = 1, bool thieu = false) =>
            new TaxListingRow(kyHieu, so, "01/05/2026", "Đối tác", "0101234567", rate, ds, thue, tt, thieu);

        [Fact]
        public void TaxListing_GroupsSubtotalsAndGrandTotal()
        {
            var groups = new List<TaxListingGroup>
            {
                new("8%", new[] { ListingRow("A", 1, "8%", 100m, 8m) }, 100m, 8m),
                new("10%", new[] { ListingRow("A", 2, "10%", 200m, 20m, tt: 3), ListingRow("A", 3, "10%", 300m, 30m, thieu: true) }, 500m, 50m)
            };
            var period = TaxPeriod.Month(2026, 5);
            string path = NewFile("bangke.xlsx");

            ExcelExporter.ExportTaxListing(groups, InvoiceType.Bought, period, path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);
            Assert.Contains(period.Label, ws.Cell("A1").GetString());
            Assert.Contains("MUA VÀO", ws.Cell("A1").GetString().ToUpperInvariant());
            Assert.Equal("STT", ws.Cell("A4").GetString());
            Assert.Equal("Trạng thái HĐ", ws.Cell("J4").GetString());
            // Dòng dữ liệu: 5 (8%), cộng 6, 7-8 (10%), cộng 9, tổng 10
            Assert.Equal(100d, ws.Cell("G5").GetDouble());
            Assert.Equal(XLDataType.Number, ws.Cell("G5").DataType);
            Assert.Equal("#,##0", ws.Cell("G5").Style.NumberFormat.Format);
            Assert.Equal(100d, ws.Cell("G6").GetDouble());
            Assert.Equal(8d, ws.Cell("I6").GetDouble());
            Assert.Equal(500d, ws.Cell("G9").GetDouble());
            Assert.Equal(50d, ws.Cell("I9").GetDouble());
            Assert.Equal(600d, ws.Cell("G10").GetDouble());
            Assert.Equal(58d, ws.Cell("I10").GetDouble());
            Assert.Contains("TỔNG CỘNG", ws.Cell("A10").GetString().ToUpperInvariant());
            // Tô màu: trạng thái khác 1 vàng, thiếu số tiền đỏ
            Assert.Equal(XLColor.Yellow, ws.Cell("A7").Style.Fill.BackgroundColor);
            Assert.Equal(XLColor.Red, ws.Cell("A8").Style.Fill.BackgroundColor);
            Assert.NotEqual(XLColor.Yellow, ws.Cell("A5").Style.Fill.BackgroundColor);
        }

        [Fact]
        public void SingleInvoiceDetail_NoRows_WritesNotFoundMessage()
        {
            string path = NewFile("single.xlsx");

            ExcelExporter.ExportSingleInvoiceDetail(new List<Dictionary<string, string>>(), path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet("ChiTietHoaDon");
            Assert.Equal("Chi tiết hàng hóa/dịch vụ", ws.Cell("A1").GetString());
            Assert.Equal("STT", ws.Cell("A3").GetString());
            Assert.Equal("Không tìm thấy chi tiết hàng hóa/dịch vụ (mảng 'hdhhdvu').", ws.Cell("A4").GetString());
            Assert.True(ws.Cell("A4").IsMerged());
        }
    }
}
