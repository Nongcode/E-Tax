using System.Globalization;
using ClosedXML.Excel;

namespace E_Tax
{
    /// <summary>
    /// Xuất các file Excel (danh sách HĐ, chi tiết tổng hợp, chi tiết một HĐ) bằng ClosedXML.
    /// Chỉ ghi file, không gọi API hay đụng tới giao diện.
    /// </summary>
    internal static class ExcelExporter
    {
        private const string NumberFormat = "#,##0";

        private static readonly string[] SearchResultHeaders =
        {
            "Mã số thuế", "Ký hiệu mã số", "Ký hiệu hóa đơn", "Số hóa đơn", "Ngày lập",
            "Thông tin hóa đơn", "Tổng tiền chưa thuế", "Tổng tiền thuế", "Tổng tiền chiết khấu",
            "Tổng tiền phí", "Tổng tiền thanh toán", "Đơn vị tiền tệ",
            "Trạng thái hóa đơn", "Kết quả kiểm tra", "Hóa đơn liên quan", "Loại hóa đơn"
        };

        public static void ExportSearchResults(IReadOnlyList<SearchResult> results, string filePath)
        {
            int colCount = SearchResultHeaders.Length;
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("DanhSachHoaDon");

            var title = ws.Cell(1, 1);
            title.Value = "DANH SÁCH HÓA ĐƠN";
            ws.Range(1, 1, 1, colCount).Merge();
            title.Style.Font.FontSize = 16;
            title.Style.Font.Bold = true;
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, colCount).Merge();
            ws.Cell(2, 1).Style.Font.Italic = true;

            const int headerRow = 4;
            for (int i = 0; i < colCount; i++)
                ws.Cell(headerRow, i + 1).Value = SearchResultHeaders[i].ToUpper();

            var headerRange = ws.Range(headerRow, 1, headerRow, colCount);
            headerRange.Style.Font.FontName = "Calibri";
            headerRange.Style.Font.FontSize = 12;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            SetThinBorders(headerRange);

            int row = headerRow + 1;
            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                ws.Cell(row, 1).Value = Text(item.Ma_so_thue);
                ws.Cell(row, 2).Value = Number(item.Ky_hieu_ma_so);
                ws.Cell(row, 3).Value = Text(item.Ky_hieu_hoa_don);
                ws.Cell(row, 4).Value = Number(item.So_hoa_don);
                ws.Cell(row, 5).Value = Text(item.Ngay_lap);
                ws.Cell(row, 6).Value = Text(item.Thong_tin_hoa_don);
                ws.Cell(row, 7).Value = Number(item.Tong_tien_chua_thue);
                ws.Cell(row, 8).Value = Number(item.Tong_tien_thue);
                ws.Cell(row, 9).Value = Number(item.Tong_tien_chiet_khau);
                ws.Cell(row, 10).Value = item.Tong_tien_phi?.Sum() ?? 0;
                ws.Cell(row, 11).Value = Number(item.Tong_tien_thanh_toan);
                ws.Cell(row, 12).Value = Text(item.Don_vi_tien_te);
                ws.Cell(row, 13).Value = Number(item.Trang_thai_hoa_don);
                ws.Cell(row, 14).Value = Text(item.Ket_qua_kiem_tra_hoa_don);
                ws.Cell(row, 15).Value = Text(item.Hoa_don_lien_quan);
                ws.Cell(row, 16).Value = Text(item.Loai_hoa_don);

                ws.Range(row, 7, row, 11).Style.NumberFormat.Format = NumberFormat;

                // Màu xen kẽ
                if (i % 2 == 0)
                    ws.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 240, 240);
                row++;
            }

            SetThinBorders(ws.Range(headerRow, 1, row - 1, colCount));

            AutoFit(ws, colCount);
            wb.SaveAs(filePath);
        }

        /// <summary>
        /// File chi tiết tổng hợp nhiều HĐ (mỗi dòng đã có "stt"); rỗng → sheet "Lỗi".
        /// </summary>
        public static void ExportDetailSummary(IReadOnlyList<Dictionary<string, string>> rows, string filePath)
        {
            using var wb = new XLWorkbook();

            if (rows.Count == 0)
            {
                var emptyWs = wb.Worksheets.Add("Lỗi");
                emptyWs.Cell("A1").Value = "Không tìm thấy chi tiết hàng hóa/dịch vụ nào từ các hóa đơn đã tìm.";
                wb.SaveAs(filePath);
                return;
            }

            var columns = InvoiceJson.DetailExportColumns;
            var ws = wb.Worksheets.Add("Chi Tiết Hóa Đơn");

            var title = ws.Cell("A1");
            title.Value = "CHI TIẾT HÓA ĐƠN";
            title.Style.Font.FontSize = 18;
            title.Style.Font.Bold = true;
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            var created = ws.Cell("A2");
            created.Value = $"Ngày tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            created.Style.Font.Italic = true;
            created.Style.Font.FontColor = XLColor.DarkGray;
            created.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(1, 1, 2, columns.Length).Merge();

            const int headerRow = 4;
            for (int i = 0; i < columns.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = columns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 204);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            int rowIndex = headerRow + 1;
            foreach (var rowData in rows)
            {
                for (int c = 0; c < columns.Length; c++)
                {
                    rowData.TryGetValue(columns[c].Key, out var value);
                    var cell = ws.Cell(rowIndex, c + 1);
                    SetDetailCellValue(cell, columns[c].Key, value);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                rowIndex++;
            }

            AutoFit(ws, columns.Length);
            ApplySheetFont(ws);
            wb.SaveAs(filePath);
        }

        /// <summary>
        /// File chi tiết hàng hóa/dịch vụ của một hóa đơn.
        /// </summary>
        public static void ExportSingleInvoiceDetail(IReadOnlyList<Dictionary<string, string>> rows, string filePath)
        {
            var columns = InvoiceJson.DetailExportColumns;
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("ChiTietHoaDon");

            int currentRow = 1;
            var title = ws.Cell(currentRow, 1);
            title.Value = "Chi tiết hàng hóa/dịch vụ";
            title.Style.Font.Bold = true;
            title.Style.Font.FontSize = 14;
            ws.Range(currentRow, 1, currentRow, 15).Merge();
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentRow += 2;

            int headerRow = currentRow;
            for (int i = 0; i < columns.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = columns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            currentRow++;

            if (rows.Count > 0)
            {
                foreach (var rowData in rows)
                {
                    for (int c = 0; c < columns.Length; c++)
                    {
                        rowData.TryGetValue(columns[c].Key, out var value);
                        SetDetailCellValue(ws.Cell(currentRow, c + 1), columns[c].Key, value);
                    }
                    currentRow++;
                }

                SetThinBorders(ws.Range(headerRow, 1, currentRow - 1, columns.Length));
            }
            else
            {
                ws.Cell(currentRow, 1).Value = "Không tìm thấy chi tiết hàng hóa/dịch vụ (mảng 'hdhhdvu').";
                ws.Range(currentRow, 1, currentRow, columns.Length).Merge();
            }

            AutoFit(ws, columns.Length);
            ApplySheetFont(ws);
            wb.SaveAs(filePath);
        }

        /// <summary>
        /// Ghi giá trị vào ô; cột tiền/số lượng được chuyển sang số và định dạng #,##0.
        /// </summary>
        private static void SetDetailCellValue(IXLCell cell, string key, string? value)
        {
            if (value != null && InvoiceJson.IsNumericColumn(key) &&
                decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal numValue))
            {
                cell.Value = numValue;
                cell.Style.NumberFormat.Format = NumberFormat;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
            else
            {
                cell.Value = Text(value);
            }
        }

        // Độ rộng theo nội dung, không hẹp hơn cột mặc định (giống AutoFitColumns của EPPlus)
        private static void AutoFit(IXLWorksheet ws, int colCount) =>
            ws.Columns(1, colCount).AdjustToContents(ws.ColumnWidth, double.MaxValue);

        // Font chung cho cả sheet (áp sau AutoFit như bản EPPlus cũ, kể cả dòng tiêu đề)
        private static void ApplySheetFont(IXLWorksheet ws)
        {
            ws.Style.Font.FontName = "Segoe UI";
            ws.Style.Font.FontSize = 10;
        }

        private static void SetThinBorders(IXLRange range)
        {
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        private static XLCellValue Text(string? value) => value == null ? Blank.Value : value;

        private static XLCellValue Number(decimal? value) => value.HasValue ? value.Value : Blank.Value;

        private static XLCellValue Number(int? value) => value.HasValue ? value.Value : Blank.Value;
    }
}
