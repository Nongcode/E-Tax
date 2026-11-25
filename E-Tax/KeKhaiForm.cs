using System.Globalization;

namespace E_Tax
{
    /// <summary>Phạm vi của lần tìm kiếm gần nhất (một lần tìm là một kỳ).</summary>
    internal readonly record struct SearchScope(bool Bought, bool Sold);

    /// <summary>Lý do khóa từng nút xuất; null là được xuất. Vat là lý do đầy đủ nhất nên dùng để hiển thị.</summary>
    internal sealed record KeKhaiBlocks(string? Common, string? Vat, string? Appendix, string? Accounting);

    /// <summary>Logic thuần của màn hình kê khai (tách ra để kiểm thử).</summary>
    internal static class KeKhaiLogic
    {
        /// <summary>
        /// Đọc số nhập tay theo kiểu Việt Nam có dung sai. Rỗng là 0. Khi có cả '.' và ',' thì dấu xuất hiện sau cùng là
        /// dấu thập phân. Chỉ một loại dấu: lặp lại nhiều lần là phân cách nghìn; xuất hiện một lần thì là phân cách nghìn
        /// nếu sau nó đúng 3 chữ số và phần nguyên có 1-3 chữ số không bắt đầu bằng 0 ("1.234" là 1234), còn lại là thập phân
        /// ("0.123", "1234567.5"). Từ chối dạng mơ hồ: một dấu, đúng 3 số sau và phần nguyên từ 4 chữ số ("1234.567"),
        /// phần nguyên nhiều chữ số bắt đầu bằng 0 ("012.345") và dạng nhóm bắt đầu bằng 0 ("0.123.456").
        /// </summary>
        public static bool TryParseAmount(string? text, out decimal value)
        {
            value = 0m;
            string s = (text ?? "").Trim();
            if (s.Length == 0) return true;

            bool negative = false;
            if (s[0] == '-' || s[0] == '+')
            {
                negative = s[0] == '-';
                s = s.Substring(1);
            }
            if (s.Length == 0) return false;
            foreach (char c in s)
                if (!(c >= '0' && c <= '9') && c != '.' && c != ',') return false;

            int dots = s.Count(c => c == '.');
            int commas = s.Count(c => c == ',');
            int lastDot = s.LastIndexOf('.');
            int lastComma = s.LastIndexOf(',');

            char dec = '\0', thou = '\0';
            if (dots > 0 && commas > 0)
            {
                dec = lastDot > lastComma ? '.' : ',';
                thou = dec == '.' ? ',' : '.';
                if ((dec == '.' ? dots : commas) != 1) return false;
            }
            else if (dots > 0)
            {
                if (dots > 1) thou = '.';
                else if (IsAmbiguousSingleSeparator(s, lastDot)) return false;
                else if (s.Length - lastDot - 1 == 3 && lastDot >= 1 && s[0] != '0') thou = '.';
                else dec = '.';
            }
            else if (commas > 0)
            {
                if (commas > 1) thou = ',';
                else if (IsAmbiguousSingleSeparator(s, lastComma)) return false;
                else if (s.Length - lastComma - 1 == 3 && lastComma >= 1 && s[0] != '0') thou = ',';
                else dec = ',';
            }

            string integerPart = s, fraction = "";
            if (dec != '\0')
            {
                int idx = s.IndexOf(dec);
                integerPart = s.Substring(0, idx);
                fraction = s.Substring(idx + 1);
                if (fraction.Length == 0 || fraction.Any(c => c < '0' || c > '9')) return false;
            }

            if (thou != '\0')
            {
                var groups = integerPart.Split(thou);
                if (groups.Length < 2) return false;
                for (int i = 0; i < groups.Length; i++)
                {
                    var g = groups[i];
                    if (g.Any(c => c < '0' || c > '9')) return false;
                    if (i == 0 ? (g.Length < 1 || g.Length > 3 || g[0] == '0') : g.Length != 3) return false;
                }
                integerPart = string.Concat(groups);
            }
            else if (integerPart.Any(c => c < '0' || c > '9')) return false;

            string normalized = (integerPart.Length == 0 ? "0" : integerPart) + (fraction.Length > 0 ? "." + fraction : "");
            if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
                return false;
            if (parsed > MaxAbsAmount) return false;
            value = negative ? -parsed : parsed;
            return true;
        }

        // Một dấu phân cách tại pos: phần nguyên nhiều chữ số bắt đầu bằng 0, hoặc từ 4 chữ số với đúng 3 số sau dấu
        private static bool IsAmbiguousSingleSeparator(string s, int pos) =>
            (pos >= 2 && s[0] == '0') || (pos >= 4 && s.Length - pos - 1 == 3);

        /// <summary>Giá trị tuyệt đối tối đa của ô nhập, để các phép cộng trong tờ khai không tràn decimal.</summary>
        public const decimal MaxAbsAmount = 999_999_999_999_999_999m;

        /// <summary>Được xuất khi không bị khóa, không đang xuất, ô nhập hợp lệ và có kết quả tính hợp lệ.</summary>
        public static bool CanExport(string? blockReason, bool exporting, bool inputValid, bool hasCurrent) =>
            blockReason == null && !exporting && inputValid && hasCurrent;

        /// <summary>
        /// Thiếu dữ liệu, trạng thái không rõ hoặc ngoại tệ khóa cả ba loại xuất. Tờ khai cần cả hai chiều của cùng kỳ,
        /// phụ lục chỉ cần Bán ra, file kế toán xuất theo phần đã tìm.
        /// </summary>
        public static KeKhaiBlocks DecideBlocks(int failedInvoices, int unknownStatus, int nonVnd, SearchScope scope) =>
            DecideBlocks(failedInvoices, unknownStatus, nonVnd, scope, unknownStatus, nonVnd);

        /// <summary>unknownSold/nonVndSold: phần của Bán ra; phụ lục chỉ bị khóa bởi vấn đề ở Bán ra.</summary>
        public static KeKhaiBlocks DecideBlocks(int failedInvoices, int unknownStatus, int nonVnd, SearchScope scope, int unknownSold, int nonVndSold)
        {
            string? common = DataBlock(failedInvoices, unknownStatus, nonVnd);
            string? soldData = DataBlock(failedInvoices, unknownSold, nonVndSold);

            string? vatScope = scope.Bought && scope.Sold
                ? null
                : $"Tờ khai cần cả Mua vào và Bán ra của cùng kỳ; lần tìm kiếm này thiếu {(scope.Bought ? "Bán ra" : scope.Sold ? "Mua vào" : "Mua vào và Bán ra")}. Hãy tìm với loại Tất cả.";
            string? appendixScope = scope.Sold ? null : "Phụ lục giảm thuế cần dữ liệu Bán ra; lần tìm kiếm này không có Bán ra.";
            return new KeKhaiBlocks(common, Join(common, vatScope), Join(soldData, appendixScope), common);
        }

        private static string? DataBlock(int failedInvoices, int unknownStatus, int nonVnd)
        {
            string? foreign = nonVnd > 0
                ? $"Có {nonVnd} hóa đơn không tính bằng VND. V1 chưa quy đổi tỷ giá nên không được xuất tờ khai, phụ lục và file kế toán."
                : null;
            return Join(TaxListing.ExportBlockReason(failedInvoices), TaxableInvoices.UnknownBlockReason(unknownStatus), foreign);
        }

        private static string? Join(params string?[] reasons)
        {
            var present = reasons.Where(r => r != null).ToList();
            return present.Count == 0 ? null : string.Join(" ", present);
        }

        /// <summary>Danh sách cảnh báo hiển thị: lý do khóa (nếu có), cảnh báo bảng kê, rồi cảnh báo tờ khai.</summary>
        public static IReadOnlyList<string> ComposeWarnings(string? blockReason, IEnumerable<string> listingWarnings, IEnumerable<string> returnWarnings)
        {
            var list = new List<string>();
            if (blockReason != null) list.Add(blockReason);
            list.AddRange(listingWarnings);
            list.AddRange(returnWarnings);
            return list.Distinct().ToList();
        }
    }

    /// <summary>
    /// Màn hình tờ khai 01/GTGT dự thảo, phụ lục giảm thuế và xuất file nhập cho phần mềm kế toán. Dựng bằng code.
    /// Chỉ tính từ dữ liệu đã tải, không gọi mạng. Mọi file xuất đều CHƯA đối chiếu với mẫu gốc.
    /// </summary>
    internal sealed class KeKhaiForm : Form
    {
        private static readonly CultureInfo ViCulture = GetViCulture();

        private readonly IReadOnlyList<InvoiceDetailItem> _items;
        private readonly TaxPeriod _period;
        private readonly string _taxCode;
        private readonly KeKhaiBlocks _blocks;
        private readonly IReadOnlyList<SearchResult> _headers;
        private readonly IReadOnlyList<string> _searchWarnings;
        private readonly int _noLineCount;
        private readonly int _noLineSoldCount;
        private readonly PeriodSplit _split;
        private readonly int _nonVndSoldCount;
        private readonly int _nonVndCount;
        private readonly IReadOnlyList<string> _listingWarnings;
        private readonly IReadOnlyList<TaxListingGroup> _bought;
        private readonly IReadOnlyList<TaxListingGroup> _sold;
        private readonly IReadOnlyList<AccountingProfile> _profiles;

        private sealed record InputField(string Code, string Caption);
        private static readonly InputField[] Fields =
        {
            new("22", "[22] " + "Thuế GTGT còn được khấu trừ kỳ trước chuyển sang"),
            new("37", "[37] " + "Điều chỉnh giảm thuế GTGT còn được khấu trừ của các kỳ trước"),
            new("38", "[38] " + "Điều chỉnh tăng thuế GTGT còn được khấu trừ của các kỳ trước"),
            new("39a", "[39a] " + "Thuế GTGT nhận bàn giao được khấu trừ trong kỳ"),
            new("40b", "[40b] " + "Thuế GTGT mua vào của dự án đầu tư được bù trừ"),
            new("42", "[42] " + "Thuế GTGT đề nghị hoàn"),
            new("23a", "[23a] " + "Giá trị nhập khẩu mua vào (cộng vào [23])"),
            new("24a", "[24a] " + "Thuế GTGT hàng nhập khẩu (cộng vào [24])"),
            new("25adj", "Điều chỉnh [25] (cộng vào [25], số âm là giảm)"),
        };

        private readonly TextBox[] _inputs;
        private readonly Label[] _inputErrors;
        private readonly DataGridView _grid = new();
        private readonly Label _lblStale = new() { AutoSize = true, ForeColor = Color.OrangeRed, Visible = false };
        private readonly TextBox _txtWarnings = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = SystemColors.Window };
        private readonly Button _btnVat = new() { Text = "Xuất tờ khai 01/GTGT (dự thảo)", AutoSize = true };
        private readonly Button _btnReduction = new() { Text = "Xuất phụ lục giảm thuế (dự thảo)", AutoSize = true };
        private readonly ComboBox _cboProfile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly Button _btnAccounting = new() { Text = "Xuất file nhập kế toán", AutoSize = true };
        private readonly Label _lblStatus = new() { AutoSize = true, Padding = new Padding(8, 6, 0, 0) };

        private VatReturn? _current;
        private bool _inputValid = true;
        private bool _exporting;

        public KeKhaiForm(IReadOnlyList<InvoiceDetailItem> items, TaxPeriod period, string? taxCode, int failedInvoices,
            int noLineCount, int nonVndCount, IReadOnlyList<SearchResult> headers, SearchScope scope, IReadOnlyList<string> searchWarnings,
            int? noLineSoldCount = null)
        {
            _items = (items ?? throw new ArgumentNullException(nameof(items))).ToList();
            _period = period;
            _taxCode = taxCode ?? "";
            _headers = (headers ?? throw new ArgumentNullException(nameof(headers))).ToList();
            _searchWarnings = searchWarnings ?? Array.Empty<string>();
            _noLineCount = noLineCount;
            _nonVndCount = nonVndCount;
            string sold = TaxListing.DirectionLabel(InvoiceType.Sold);
            _nonVndSoldCount = TaxListing.CountNonVnd(TaxableInvoices.IncludedHeaders(_headers), InvoiceType.Sold);
            _noLineSoldCount = noLineSoldCount ?? noLineCount;
            _blocks = KeKhaiLogic.DecideBlocks(failedInvoices, TaxableInvoices.Select(_items, _headers).Unknown, nonVndCount, scope,
                TaxableInvoices.Select(_items, _headers, sold).Unknown, _nonVndSoldCount);
            // HĐ điều chỉnh/thay thế có HĐ gốc ở kỳ khác được tách theo quy tắc kế toán; phần [38] được điền sẵn
            _split = AdjustmentPeriodRules.Split(_items, _headers, period);
            _bought = TaxListing.Build(_split.Current, InvoiceType.Bought);
            _sold = TaxListing.Build(_split.Current, InvoiceType.Sold);
            _listingWarnings = TaxListing.ExportWarnings(_bought.Concat(_sold).ToList(), noLineCount, nonVndCount)
                .Concat(TaxableInvoices.OutputNotes(_items, _headers, period))
                .Concat(_searchWarnings)
                .Distinct()
                .ToList();
            _profiles = LoadProfiles();

            Text = $"Lập tờ khai dự thảo - {period.Label}";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(960, 640);
            Size = new Size(1120, 760);

            _inputs = new TextBox[Fields.Length];
            _inputErrors = new Label[Fields.Length];

            BuildLayout();
            foreach (var p in _profiles) _cboProfile.Items.Add(new ProfileItem(p));
            if (_cboProfile.Items.Count > 0) _cboProfile.SelectedIndex = 0;

            _btnVat.Click += async (_, _) => await ExportVatAsync();
            _btnReduction.Click += async (_, _) => await ExportReductionAsync();
            _btnAccounting.Click += async (_, _) => await ExportAccountingAsync();
            _cboProfile.SelectedIndexChanged += (_, _) => UpdateButtons();
            FormClosing += (_, e) => { if (_exporting) e.Cancel = true; };

            Recalculate();
        }

        private sealed class ProfileItem
        {
            public AccountingProfile Profile { get; }
            public ProfileItem(AccountingProfile profile) { Profile = profile; }
            public override string ToString() => Profile.Verified ? Profile.Name : Profile.Name + " (chưa đối chiếu)";
        }

        private static CultureInfo GetViCulture()
        {
            try { return CultureInfo.GetCultureInfo("vi-VN"); }
            catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
        }

        private static IReadOnlyList<AccountingProfile> LoadProfiles()
        {
            try { return AccountingProfiles.LoadAll(AccountingProfiles.DefaultDirectory); }
            catch (Exception ex)
            {
                Logger.Write($"Không đọc được thư mục mẫu xuất kế toán: {ex.Message}");
                return AccountingProfiles.BuiltIn();
            }
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(8) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var banner = new Label
            {
                Text = AccountingExporter.UnverifiedWarning,
                AutoSize = true,
                Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
                ForeColor = Color.Red
            };
            var sub = new Label
            {
                Text = $"Kỳ {_period.Label}" + (_taxCode.Length > 0 ? $" - MST {_taxCode}" : "") +
                       ". Bản dự thảo tham khảo tính từ bảng kê đã tải, cần kiểm tra lại trước khi dùng.",
                AutoSize = true
            };
            var head = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            head.Controls.Add(banner);
            head.Controls.Add(sub);
            root.Controls.Add(head, 0, 0);

            var block = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1050, 0),
                ForeColor = Color.Red,
                Font = new Font(Font, FontStyle.Bold),
                Text = _blocks.Vat == null ? "" : _blocks.Vat + " Các nút xuất liên quan đã bị khóa; vẫn xem được số liệu."
            };
            block.Visible = _blocks.Vat != null;
            root.Controls.Add(block, 0, 1);

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 500));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            main.Controls.Add(BuildInputPanel(), 0, 0);
            main.Controls.Add(BuildGridPanel(), 1, 0);
            root.Controls.Add(main, 0, 2);

            var warnBox = new GroupBox { Text = "Cảnh báo", Dock = DockStyle.Fill };
            warnBox.Controls.Add(_txtWarnings);
            root.Controls.Add(warnBox, 0, 3);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            buttons.Controls.AddRange(new Control[]
            {
                _btnVat, _btnReduction,
                new Label { Text = "Mẫu kế toán:", AutoSize = true, Padding = new Padding(12, 6, 0, 0) },
                _cboProfile, _btnAccounting, _lblStatus
            });
            root.Controls.Add(buttons, 0, 4);

            Controls.Add(root);
        }

        private Control BuildInputPanel()
        {
            var box = new GroupBox { Text = "Chỉ tiêu nhập tay (số thập phân, vd 1.234.567,5 hoặc 1234567.5)", Dock = DockStyle.Fill };
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 3 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < Fields.Length; i++)
            {
                var label = new Label { Text = Fields[i].Caption, AutoSize = true, MaximumSize = new Size(245, 0), Padding = new Padding(0, 6, 0, 2) };
                string initial = Fields[i].Code == "38" && _split.Ct38 != 0m ? _split.Ct38.ToString("0", CultureInfo.InvariantCulture) : "0";
                var txt = new TextBox { Text = initial, Width = 110, TextAlign = HorizontalAlignment.Right };
                var err = new Label { AutoSize = true, ForeColor = Color.Red, Padding = new Padding(0, 6, 0, 0) };
                txt.TextChanged += (_, _) => Recalculate();
                _inputs[i] = txt;
                _inputErrors[i] = err;
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(label, 0, i);
                table.Controls.Add(txt, 1, i);
                table.Controls.Add(err, 2, i);
            }
            box.Controls.Add(table);
            return box;
        }

        private Control BuildGridPanel()
        {
            var box = new GroupBox { Text = "Chỉ tiêu tự tính", Dock = DockStyle.Fill };
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.BackgroundColor = SystemColors.ButtonHighlight;
            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã", Width = 50, SortMode = DataGridViewColumnSortMode.NotSortable });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Chỉ tiêu",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Giá trị",
                Width = 140,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            foreach (var code in VatReturnBuilder.Codes)
                _grid.Rows.Add("[" + code + "]", VatReturnBuilder.Describe(code), "");

            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(_grid);
            panel.Controls.Add(_lblStale);
            _lblStale.Dock = DockStyle.Top;
            _grid.BringToFront();
            box.Controls.Add(panel);
            return box;
        }

        private void Recalculate()
        {
            var v = new decimal[Fields.Length];
            bool ok = true;
            for (int i = 0; i < Fields.Length; i++)
            {
                if (KeKhaiLogic.TryParseAmount(_inputs[i].Text, out v[i])) _inputErrors[i].Text = "";
                else { _inputErrors[i].Text = "Số không hợp lệ"; ok = false; }
            }
            _inputValid = ok;

            // Mọi lần tính lại đều bỏ kết quả cũ trước, để lỗi bất ngờ không để số cũ còn xuất được
            _current = null;
            try
            {
                if (ok)
                {
                    var result = VatReturnBuilder.Build(_bought, _sold, new VatReturnInput(v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8]));
                    for (int r = 0; r < VatReturnBuilder.Codes.Count; r++)
                        _grid.Rows[r].Cells[2].Value = result.Values[VatReturnBuilder.Codes[r]].ToString("#,##0.##", ViCulture);
                    _txtWarnings.Text = string.Join(Environment.NewLine,
                        KeKhaiLogic.ComposeWarnings(_blocks.Vat, _listingWarnings, result.Warnings).Select(w => "- " + w));
                    _lblStale.Visible = false;
                    _grid.ForeColor = SystemColors.ControlText;
                    _current = result;
                }
                else
                {
                    ShowStale("Có ô nhập không hợp lệ. Bảng dưới là lần tính hợp lệ gần nhất và không được dùng để xuất; hãy sửa ô báo lỗi.");
                }
            }
            catch (OverflowException)
            {
                _inputValid = false;
                ShowStale("Số quá lớn nên không tính được. Bảng dưới là lần tính gần nhất và không được dùng để xuất; hãy giảm các số nhập.");
            }
            finally
            {
                UpdateButtons();
            }
        }

        private void ShowStale(string message)
        {
            _lblStale.Text = message;
            _lblStale.Visible = true;
            _grid.ForeColor = SystemColors.GrayText;
        }

        private void UpdateButtons()
        {
            _btnVat.Enabled = CanExportNow(_blocks.Vat);
            _btnReduction.Enabled = CanExportNow(_blocks.Appendix);
            _btnAccounting.Enabled = CanExportNow(_blocks.Accounting) && _cboProfile.SelectedItem is ProfileItem;
            _cboProfile.Enabled = !_exporting;
            foreach (var t in _inputs) t.Enabled = !_exporting;
        }

        private string SafeLabel()
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(_period.Label.Select(c => invalid.Contains(c) ? '-' : c)).Replace(' ', '_');
        }

        private bool CanExportNow(string? blockReason) => KeKhaiLogic.CanExport(blockReason, _exporting, _inputValid, _current != null);

        // Cảnh báo ghi vào phụ lục: chỉ tính phần Bán ra 8% mà phụ lục thực sự dùng
        private IReadOnlyList<string> AppendixNotes() =>
            KeKhaiLogic.ComposeWarnings(null,
                TaxListing.ExportWarnings(_sold.Where(g => g.ThueSuat == "8%").ToList(), _noLineSoldCount, _nonVndSoldCount),
                TaxableInvoices.OutputNotes(_items, _headers, _period, TaxListing.DirectionLabel(InvoiceType.Sold))
                    .Concat(_searchWarnings.Where(w => w.StartsWith(TaxListing.DirectionLabel(InvoiceType.Sold), StringComparison.Ordinal))));

        private Task ExportVatAsync()
        {
            if (!CanExportNow(_blocks.Vat)) return Task.CompletedTask;
            // File tờ khai mang đủ cảnh báo đang hiển thị, không chỉ cảnh báo của phép tính
            var snapshot = _current! with { Warnings = KeKhaiLogic.ComposeWarnings(null, _listingWarnings, _current!.Warnings) };
            string taxCode = _taxCode, label = _period.Label;
            var period = _period;
            return RunExportAsync("Lưu tờ khai dự thảo", $"ToKhai01GTGT_{SafeLabel()}.xlsx",
                path => ExcelExporter.ExportVatReturn(snapshot, period, taxCode, path),
                $"Đã xuất tờ khai dự thảo ({label}).");
        }

        private Task ExportReductionAsync()
        {
            if (!CanExportNow(_blocks.Appendix)) return Task.CompletedTask;
            var lines = ReductionAppendix.Build(_split.Current);
            if (lines.Count == 0)
            {
                MessageBox.Show(this, "Kỳ này không có hàng bán ra thuế suất 8% nên không có gì để lập phụ lục giảm thuế.",
                    "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return Task.CompletedTask;
            }
            string taxCode = _taxCode;
            var period = _period;
            var notes = AppendixNotes();
            return RunExportAsync("Lưu phụ lục giảm thuế dự thảo", $"PhuLucGiamThue_{SafeLabel()}.xlsx",
                path => ExcelExporter.ExportReductionAppendix(lines, period, taxCode, path, notes),
                $"Đã xuất phụ lục giảm thuế dự thảo ({period.Label}).");
        }

        private Task ExportAccountingAsync()
        {
            if (!CanExportNow(_blocks.Accounting) || _cboProfile.SelectedItem is not ProfileItem item) return Task.CompletedTask;
            var profile = item.Profile;
            var items = _items;
            var notes = _listingWarnings;
            string safeName = string.Concat(profile.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c)).Replace(' ', '_');
            return RunExportAsync($"Lưu file nhập kế toán ({profile.Name})", $"KeToan_{safeName}_{SafeLabel()}.xlsx",
                path => AccountingExporter.Export(profile, items, path, notes),
                profile.Verified
                    ? $"Đã xuất file nhập theo mẫu {profile.Name}."
                    : $"Đã xuất file nhập theo mẫu {profile.Name}. Mẫu này {AccountingExporter.UnverifiedWarning}, hãy kiểm tra trước khi nhập vào phần mềm kế toán.");
        }

        private async Task RunExportAsync(string title, string defaultName, Action<string> write, string successMessage)
        {
            using var sfd = new SaveFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx", FileName = defaultName, Title = title };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            string path = sfd.FileName;

            _exporting = true;
            _lblStatus.Text = "Đang xuất file, vui lòng đợi...";
            UpdateButtons();
            try
            {
                await Task.Run(() => write(path));
                MessageBox.Show(this, successMessage + "\n" + path, "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IOException ex)
            {
                Logger.Write($"❌ Lỗi ghi file xuất kê khai: {ex.Message}");
                MessageBox.Show(this, "Không ghi được file. File có thể đang mở trong Excel hoặc chương trình khác; hãy đóng file đó hoặc chọn tên khác rồi thử lại.",
                    "Không ghi được file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Logger.Write($"❌ Lỗi xuất kê khai: {ex.Message}");
                MessageBox.Show(this, $"Không xuất được file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _exporting = false;
                _lblStatus.Text = "";
                if (!IsDisposed) UpdateButtons();
            }
        }
    }
}
