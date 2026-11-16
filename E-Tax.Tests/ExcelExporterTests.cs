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
