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
            "Trạng thái hóa đơn", "Kết quả kiểm tra", "Hóa đơn liên quan", "Loại hóa đơn", "Ghi chú", "Rủi ro NCC"
        };

        private const string ClassificationHeader = "Phân loại";

        public static void ExportSearchResults(IReadOnlyList<SearchResult> results, string filePath, Func<SearchResult, string>? getNote = null, Func<SearchResult, string>? getRisk = null)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("DanhSachHoaDon");
            WriteInvoiceSheet(ws, results, getNote, getRisk, withClassification: false);
            wb.SaveAs(filePath);
        }

        private const int SheetNameMaxLength = 31;
        private static readonly char[] InvalidSheetNameChars = { '\\', '/', '?', '*', '[', ']', ':' };

        /// <summary>
        /// Mỗi doanh nghiệp (MST) một sheet cùng bố cục với ExportSearchResults, thêm cột "Phân loại".
        /// MST trùng được thêm hậu tố " (n)"; doanh nghiệp không có hóa đơn vẫn có sheet với dòng tiêu đề.
        /// </summary>
        public static void ExportCompanies(IReadOnlyList<(string TaxCode, IReadOnlyList<SearchResult> Invoices)> companies, string filePath)
        {
            if (companies == null || companies.Count == 0)
                throw new ArgumentException("Cần ít nhất một doanh nghiệp để xuất Excel.", nameof(companies));

            using var wb = new XLWorkbook();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (taxCode, invoices) in companies)
            {
                var ws = wb.Worksheets.Add(UniqueSheetName(taxCode, used));
                WriteInvoiceSheet(ws, invoices, null, null, withClassification: true);
            }
            wb.SaveAs(filePath);
        }

        private static string UniqueSheetName(string? taxCode, HashSet<string> used)
        {
            string baseName = string.Concat((taxCode ?? "").Trim().Select(c => Array.IndexOf(InvalidSheetNameChars, c) >= 0 ? '_' : c));
            baseName = baseName.Trim('\'');
            if (baseName.Length == 0) baseName = "DoanhNghiep";
            if (baseName.Length > SheetNameMaxLength) baseName = baseName.Substring(0, SheetNameMaxLength);

            string name = baseName;
            for (int n = 2; !used.Add(name); n++)
            {
                string suffix = $" ({n})";
                name = baseName.Substring(0, Math.Min(baseName.Length, SheetNameMaxLength - suffix.Length)) + suffix;
            }
            return name;
        }

        private static void WriteInvoiceSheet(IXLWorksheet ws, IReadOnlyList<SearchResult> results, Func<SearchResult, string>? getNote, Func<SearchResult, string>? getRisk, bool withClassification)
        {
            int colCount = SearchResultHeaders.Length + (withClassification ? 1 : 0);

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
            for (int i = 0; i < SearchResultHeaders.Length; i++)
                ws.Cell(headerRow, i + 1).Value = SearchResultHeaders[i].ToUpper();
            if (withClassification)
                ws.Cell(headerRow, colCount).Value = ClassificationHeader.ToUpper();

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
                ws.Cell(row, 5).Value = Text(InvoiceDate.ToDisplay(item.Ngay_lap));
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
                ws.Cell(row, 17).Value = getNote?.Invoke(item) ?? "";
                ws.Cell(row, 18).Value = getRisk?.Invoke(item) ?? "";
                if (withClassification)
                    ws.Cell(row, colCount).Value = InvoiceClassifier.Label(InvoiceClassifier.RelationOf(item.Trang_thai_hoa_don));

                ws.Range(row, 7, row, 11).Style.NumberFormat.Format = NumberFormat;

                // Màu xen kẽ
                if (i % 2 == 0)
                    ws.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 240, 240);
                row++;
            }

            SetThinBorders(ws.Range(headerRow, 1, row - 1, colCount));

            AutoFit(ws, colCount);
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

        private static readonly string[] TaxListingHeaders =
        {
            "STT", "Ký hiệu", "Số HĐ", "Ngày HĐ", "Tên đối tác", "MST đối tác",
            "Doanh số chưa thuế", "Thuế suất", "Tiền thuế", "Trạng thái HĐ"
        };

        /// <summary>
        /// Bảng kê mua vào/bán ra theo kỳ: mỗi thuế suất một nhóm có dòng cộng, cuối là dòng tổng cộng.
        /// Không loại hóa đơn theo trạng thái: dòng trạng thái khác 1 (hoặc không rõ) tô vàng,
        /// dòng thiếu số tiền tô đỏ (đỏ ưu tiên hơn vàng).
        /// </summary>
        public static void ExportTaxListing(IReadOnlyList<TaxListingGroup> groups, InvoiceType type, TaxPeriod period, string filePath)
        {
            int colCount = TaxListingHeaders.Length;
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("BangKe");

            string kind = type == InvoiceType.Bought ? "MUA VÀO" : "BÁN RA";
            var title = ws.Cell(1, 1);
            title.Value = $"BẢNG KÊ HÓA ĐƠN {kind} - {period.Label}";
            ws.Range(1, 1, 1, colCount).Merge();
            title.Style.Font.FontSize = 16;
            title.Style.Font.Bold = true;
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, colCount).Merge();
            ws.Cell(2, 1).Style.Font.Italic = true;

            // Ghi chú ngay trên bảng để người đọc file biết tổng có gồm dòng cần kiểm tra
            var notes = TaxListing.ExportNotes(groups);
            if (notes.Count > 0)
            {
                var noteCell = ws.Cell(3, 1);
                noteCell.Value = "Lưu ý: " + string.Join("\n", notes);
                ws.Range(3, 1, 3, colCount).Merge();
                noteCell.Style.Font.Bold = true;
                noteCell.Style.Font.FontColor = XLColor.DarkRed;
                noteCell.Style.Alignment.WrapText = true;
                noteCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                ws.Row(3).Height = 48 * notes.Count; // ô gộp không tự giãn chiều cao
            }

            const int headerRow = 4;
            for (int i = 0; i < colCount; i++)
                ws.Cell(headerRow, i + 1).Value = TaxListingHeaders[i];
            var headerRange = ws.Range(headerRow, 1, headerRow, colCount);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            int row = headerRow + 1;
            int stt = 1;
            decimal totalDoanhSo = 0m, totalThue = 0m;
            foreach (var group in groups)
            {
                foreach (var r in group.Rows)
                {
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = Text(r.KyHieu);
                    ws.Cell(row, 3).Value = Number(r.SoHD);
                    ws.Cell(row, 4).Value = Text(r.NgayHD);
                    ws.Cell(row, 5).Value = Text(r.TenDoiTac);
                    ws.Cell(row, 6).Value = Text(r.MstDoiTac);
                    ws.Cell(row, 7).Value = r.DoanhSo;
                    ws.Cell(row, 8).Value = Text(r.ThueSuat);
                    ws.Cell(row, 9).Value = r.TienThue;
                    ws.Cell(row, 10).Value = Number(r.TrangThai);
                    ws.Cell(row, 7).Style.NumberFormat.Format = NumberFormat;
                    ws.Cell(row, 9).Style.NumberFormat.Format = NumberFormat;

                    if (r.ThieuSoTien)
                        ws.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.Red;
                    else if (r.TrangThai != 1)
                        ws.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.Yellow;
                    row++;
                }

                ws.Cell(row, 1).Value = $"Cộng thuế suất {group.ThueSuat}";
                ws.Range(row, 1, row, 6).Merge();
                ws.Cell(row, 7).Value = group.TongDoanhSo;
                ws.Cell(row, 9).Value = group.TongThue;
                StyleTotalRow(ws, row, colCount, XLColor.LightGray);
                totalDoanhSo += group.TongDoanhSo;
                totalThue += group.TongThue;
                row++;
            }

            ws.Cell(row, 1).Value = "TỔNG CỘNG";
            ws.Range(row, 1, row, 6).Merge();
            ws.Cell(row, 7).Value = totalDoanhSo;
            ws.Cell(row, 9).Value = totalThue;
            StyleTotalRow(ws, row, colCount, XLColor.LightBlue);

            SetThinBorders(ws.Range(headerRow, 1, row, colCount));
            AutoFit(ws, colCount);
            wb.SaveAs(filePath);
        }

        private static void StyleTotalRow(IXLWorksheet ws, int row, int colCount, XLColor fill)
        {
            var range = ws.Range(row, 1, row, colCount);
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = fill;
            ws.Cell(row, 7).Style.NumberFormat.Format = NumberFormat;
            ws.Cell(row, 9).Style.NumberFormat.Format = NumberFormat;
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
