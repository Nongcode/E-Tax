using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;

namespace E_Tax
{
    /// <summary>Một cột của file nhập. Field rỗng/null: luôn ghi Default (cột cố định).</summary>
    internal record AccountingColumn(string Header, string? Field, string? Default);

    internal record AccountingProfile(string Name, bool Verified, IReadOnlyList<AccountingColumn> Columns);

    /// <summary>
    /// Mẫu cột xuất cho phần mềm kế toán. Các mẫu có sẵn chỉ là bộ cột chung, CHƯA đối chiếu với mẫu gốc của
    /// phần mềm nào; người dùng thay bằng file JSON cùng tên trong thư mục mẫu.
    /// </summary>
    internal static class AccountingProfiles
    {
        public static string DefaultDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "accounting-profiles");

        /// <summary>Danh sách trường được phép (bảng tên → getter), không dùng reflection theo tên người dùng nhập.</summary>
        internal static readonly IReadOnlyDictionary<string, Func<InvoiceDetailItem, object?>> Getters =
            new Dictionary<string, Func<InvoiceDetailItem, object?>>(StringComparer.Ordinal)
            {
                [nameof(InvoiceDetailItem.STT)] = i => i.STT,
                [nameof(InvoiceDetailItem.KyHieuHD)] = i => i.KyHieuHD,
                [nameof(InvoiceDetailItem.SoHD)] = i => i.SoHD,
                [nameof(InvoiceDetailItem.NgayHD)] = i => i.NgayHD,
                [nameof(InvoiceDetailItem.TenNguoiBan)] = i => i.TenNguoiBan,
                [nameof(InvoiceDetailItem.MSTNguoiBan)] = i => i.MSTNguoiBan,
                [nameof(InvoiceDetailItem.TenSanPham)] = i => i.TenSanPham,
                [nameof(InvoiceDetailItem.DonViTinh)] = i => i.DonViTinh,
                [nameof(InvoiceDetailItem.SoLuong)] = i => i.SoLuong,
                [nameof(InvoiceDetailItem.DonGia)] = i => i.DonGia,
                [nameof(InvoiceDetailItem.ThanhTien)] = i => i.ThanhTien,
                [nameof(InvoiceDetailItem.TienChietKhau)] = i => i.TienChietKhau,
                [nameof(InvoiceDetailItem.DoanhSoChuaThue)] = i => i.DoanhSoChuaThue,
                [nameof(InvoiceDetailItem.ThueSuat)] = i => i.ThueSuat,
                [nameof(InvoiceDetailItem.TienThueGTGT)] = i => i.TienThueGTGT,
                [nameof(InvoiceDetailItem.TongTienThanhToan_HD)] = i => i.TongTienThanhToan_HD,
                [nameof(InvoiceDetailItem.SoLo)] = i => i.SoLo,
                [nameof(InvoiceDetailItem.HanSuDung)] = i => i.HanSuDung,
                [nameof(InvoiceDetailItem.GhiChuSanPham)] = i => i.GhiChuSanPham,
                [nameof(InvoiceDetailItem.LoaiHoaDon)] = i => i.LoaiHoaDon,
                [nameof(InvoiceDetailItem.TenNguoiMua)] = i => i.TenNguoiMua,
                [nameof(InvoiceDetailItem.MSTNguoiMua)] = i => i.MSTNguoiMua,
                [nameof(InvoiceDetailItem.TrangThaiHD)] = i => i.TrangThaiHD,
            };

        private static readonly string[] BuiltInNames = { "MISA", "1C", "Bravo", "Fast", "Vacom" };

        private static IReadOnlyList<AccountingColumn> GenericColumns() => new[]
        {
            new AccountingColumn("STT", nameof(InvoiceDetailItem.STT), null),
            new AccountingColumn("Loại hóa đơn", nameof(InvoiceDetailItem.LoaiHoaDon), null),
            new AccountingColumn("Trạng thái HĐ", nameof(InvoiceDetailItem.TrangThaiHD), null),
            new AccountingColumn("Ngày hóa đơn", nameof(InvoiceDetailItem.NgayHD), null),
            new AccountingColumn("Ký hiệu", nameof(InvoiceDetailItem.KyHieuHD), null),
            new AccountingColumn("Số hóa đơn", nameof(InvoiceDetailItem.SoHD), null),
            new AccountingColumn("MST người bán", nameof(InvoiceDetailItem.MSTNguoiBan), null),
            new AccountingColumn("Tên người bán", nameof(InvoiceDetailItem.TenNguoiBan), null),
            new AccountingColumn("MST người mua", nameof(InvoiceDetailItem.MSTNguoiMua), null),
            new AccountingColumn("Tên người mua", nameof(InvoiceDetailItem.TenNguoiMua), null),
            new AccountingColumn("Tên hàng hóa, dịch vụ", nameof(InvoiceDetailItem.TenSanPham), null),
            new AccountingColumn("Đơn vị tính", nameof(InvoiceDetailItem.DonViTinh), null),
            new AccountingColumn("Số lượng", nameof(InvoiceDetailItem.SoLuong), null),
            new AccountingColumn("Đơn giá", nameof(InvoiceDetailItem.DonGia), null),
            new AccountingColumn("Thành tiền", nameof(InvoiceDetailItem.ThanhTien), null),
            new AccountingColumn("Tiền chiết khấu", nameof(InvoiceDetailItem.TienChietKhau), null),
            new AccountingColumn("Tiền chưa thuế", nameof(InvoiceDetailItem.DoanhSoChuaThue), null),
            new AccountingColumn("Thuế suất", nameof(InvoiceDetailItem.ThueSuat), null),
            new AccountingColumn("Tiền thuế GTGT", nameof(InvoiceDetailItem.TienThueGTGT), null),
            new AccountingColumn("Tổng tiền thanh toán", nameof(InvoiceDetailItem.TongTienThanhToan_HD), null),
        };

        /// <summary>5 mẫu có sẵn, cùng một bộ cột chung, Verified=false.</summary>
        public static IReadOnlyList<AccountingProfile> BuiltIn() =>
            BuiltInNames.Select(n => new AccountingProfile(n, false, GenericColumns())).ToList();

        /// <summary>
        /// Mẫu có sẵn, thay bằng file *.json trong thư mục (cùng tên, không phân biệt hoa thường).
        /// File hỏng hoặc không hợp lệ bị ghi log và bỏ qua; mẫu có sẵn cùng tên được giữ.
        /// </summary>
        public static IReadOnlyList<AccountingProfile> LoadAll(string? directory = null)
        {
            var result = BuiltIn().ToList();
            directory ??= DefaultDirectory;
            if (!Directory.Exists(directory)) return result;

            foreach (var file in Directory.GetFiles(directory, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var profile = Parse(File.ReadAllText(file), out var error);
                    if (profile == null)
                    {
                        Logger.Write($"Bỏ qua mẫu xuất kế toán '{Path.GetFileName(file)}': {error}");
                        continue;
                    }
                    int idx = result.FindIndex(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) result[idx] = profile; else result.Add(profile);
                }
                catch (Exception ex)
                {
                    Logger.Write($"Bỏ qua mẫu xuất kế toán '{Path.GetFileName(file)}': {ex.Message}");
                }
            }
            return result;
        }

        private sealed class ProfileDto
        {
            public string? Name { get; set; }
            public bool Verified { get; set; }
            public List<ColumnDto>? Columns { get; set; }
        }

        private sealed class ColumnDto
        {
            public string? Header { get; set; }
            public string? Field { get; set; }
            public string? Default { get; set; }
        }

        private static AccountingProfile? Parse(string json, out string error)
        {
            error = "";
            var dto = JsonSerializer.Deserialize<ProfileDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) { error = "thiếu tên mẫu"; return null; }
            if (dto.Columns == null || dto.Columns.Count == 0) { error = "không có cột"; return null; }

            var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var columns = new List<AccountingColumn>();
            foreach (var c in dto.Columns)
            {
                if (string.IsNullOrWhiteSpace(c.Header)) { error = "tiêu đề cột rỗng"; return null; }
                if (!headers.Add(c.Header.Trim())) { error = $"trùng tiêu đề cột '{c.Header}'"; return null; }
                if (!string.IsNullOrEmpty(c.Field) && !Getters.ContainsKey(c.Field))
                {
                    error = $"trường không hợp lệ '{c.Field}'";
                    return null;
                }
                columns.Add(new AccountingColumn(c.Header, string.IsNullOrEmpty(c.Field) ? null : c.Field, c.Default));
            }
            return new AccountingProfile(dto.Name.Trim(), dto.Verified, columns);
        }
    }

    internal static class AccountingExporter
    {
        private const string NumberFormat = "#,##0.####";
        private const string NoteSheet = "Ghi chú";
        internal const string UnverifiedWarning = "CHƯA ĐỐI CHIẾU VỚI MẪU GỐC";

        /// <summary>Chỉ ghi các hóa đơn được tính thuế (TaxableInvoices).</summary>
        public static void Export(AccountingProfile profile, IReadOnlyList<InvoiceDetailItem> items, string filePath)
        {
            items = TaxableInvoices.Select(items).Items;
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Dữ liệu");

            for (int c = 0; c < profile.Columns.Count; c++)
            {
                WriteCell(ws.Cell(1, c + 1), profile.Columns[c].Header);
                ws.Cell(1, c + 1).Style.Font.Bold = true;
            }

            for (int r = 0; r < items.Count; r++)
            {
                for (int c = 0; c < profile.Columns.Count; c++)
                {
                    var col = profile.Columns[c];
                    object? value = null;
                    if (!string.IsNullOrEmpty(col.Field) && AccountingProfiles.Getters.TryGetValue(col.Field, out var getter))
                        value = getter(items[r]);
                    value ??= col.Default;
                    if (value != null) WriteCell(ws.Cell(r + 2, c + 1), value);
                }
            }
            ws.SheetView.FreezeRows(1);

            if (!profile.Verified)
            {
                var note = wb.Worksheets.Add(NoteSheet);
                note.Cell(1, 1).Value = UnverifiedWarning;
                note.Cell(1, 1).Style.Font.Bold = true;
                note.Cell(2, 1).Value = $"Mẫu '{profile.Name}' chỉ là bộ cột chung, chưa được so với mẫu nhập của phần mềm kế toán. Hãy kiểm tra trước khi nhập.";
            }

            wb.SaveAs(filePath);
        }

        private static void WriteCell(IXLCell cell, object value)
        {
            switch (value)
            {
                case decimal d: cell.SetValue(d); cell.Style.NumberFormat.Format = NumberFormat; break;
                case int i: cell.SetValue(i); break;
                case long l: cell.SetValue(l); break;
                case double db: cell.SetValue(db); break;
                default:
                    var s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                    cell.SetValue(s); // SetValue(string) là văn bản, không phân tích thành công thức
                    if (s.Length > 0 && (s[0] == '=' || s[0] == '+' || s[0] == '-' || s[0] == '@'))
                        cell.Style.IncludeQuotePrefix = true;
                    break;
            }
        }
    }
}
