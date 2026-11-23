using System.Globalization;

namespace E_Tax
{
    /// <summary>Logic thuần của màn hình kê khai (tách ra để kiểm thử).</summary>
    internal static class KeKhaiLogic
    {
        /// <summary>
        /// Đọc số nhập tay theo kiểu Việt Nam có dung sai. Rỗng là 0. Khi có cả '.' và ',' thì dấu xuất hiện sau cùng là
        /// dấu thập phân. Chỉ một loại dấu: lặp lại nhiều lần là phân cách nghìn; xuất hiện một lần thì ',' là thập phân,
        /// còn '.' là phân cách nghìn nếu sau nó đúng 3 chữ số và phần nguyên có 1-3 chữ số không bắt đầu bằng 0
        /// (nên "1.234" là 1234, "1234.567" và "0.123" là thập phân).
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
                else if (s.Length - lastDot - 1 == 3 && lastDot >= 1 && lastDot <= 3 && s[0] != '0') thou = '.';
                else dec = '.';
            }
            else if (commas > 0)
            {
                if (commas > 1) thou = ',';
                else if (s.Length - lastComma - 1 == 3 && lastComma >= 1 && lastComma <= 3 && s[0] != '0') thou = ',';
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
                    if (i == 0 ? (g.Length < 1 || g.Length > 3) : g.Length != 3) return false;
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

        /// <summary>Giá trị tuyệt đối tối đa của ô nhập, để các phép cộng trong tờ khai không tràn decimal.</summary>
        public const decimal MaxAbsAmount = 999_999_999_999_999_999m;

        /// <summary>Được xuất khi không bị khóa, không đang xuất, ô nhập hợp lệ và có kết quả tính hợp lệ.</summary>
        public static bool CanExport(string? blockReason, bool exporting, bool inputValid, bool hasCurrent) =>
            blockReason == null && !exporting && inputValid && hasCurrent;

        /// <summary>Danh sách cảnh báo hiển thị: lý do khóa (nếu có), cảnh báo bảng kê, rồi cảnh báo tờ khai.</summary>
        public static IReadOnlyList<string> ComposeWarnings(string? blockReason, IEnumerable<string> listingWarnings, IEnumerable<string> returnWarnings)
        {
            var list = new List<string>();
            if (blockReason != null) list.Add(blockReason);
            list.AddRange(listingWarnings);
            list.AddRange(returnWarnings);
            return list;
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
        private readonly string? _blockReason;
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
            int noLineCount = 0, int nonVndCount = 0)
        {
            _items = (items ?? throw new ArgumentNullException(nameof(items))).ToList();
            _period = period;
            _taxCode = taxCode ?? "";
            _blockReason = TaxListing.ExportBlockReason(failedInvoices);
            _bought = TaxListing.Build(_items, InvoiceType.Bought);
            _sold = TaxListing.Build(_items, InvoiceType.Sold);
            _listingWarnings = TaxListing.ExportWarnings(_bought.Concat(_sold).ToList(), noLineCount, nonVndCount);
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
                Text = _blockReason == null ? "" : _blockReason + " Mọi nút xuất đã bị khóa; vẫn xem được số liệu."
            };
            block.Visible = _blockReason != null;
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
                var txt = new TextBox { Text = "0", Width = 110, TextAlign = HorizontalAlignment.Right };
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
                        KeKhaiLogic.ComposeWarnings(_blockReason, _listingWarnings, result.Warnings).Select(w => "- " + w));
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
            bool allowed = CanExportNow();
            _btnVat.Enabled = allowed;
            _btnReduction.Enabled = allowed;
            _btnAccounting.Enabled = allowed && _cboProfile.SelectedItem is ProfileItem;
            _cboProfile.Enabled = !_exporting;
            foreach (var t in _inputs) t.Enabled = !_exporting;
        }

        private string SafeLabel()
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(_period.Label.Select(c => invalid.Contains(c) ? '-' : c)).Replace(' ', '_');
        }

        private bool CanExportNow() => KeKhaiLogic.CanExport(_blockReason, _exporting, _inputValid, _current != null);

        private Task ExportVatAsync()
        {
            if (!CanExportNow()) return Task.CompletedTask;
            var snapshot = _current!;
            string taxCode = _taxCode, label = _period.Label;
            var period = _period;
            return RunExportAsync("Lưu tờ khai dự thảo", $"ToKhai01GTGT_{SafeLabel()}.xlsx",
                path => ExcelExporter.ExportVatReturn(snapshot, period, taxCode, path),
                $"Đã xuất tờ khai dự thảo ({label}).");
        }

        private Task ExportReductionAsync()
        {
            if (!CanExportNow()) return Task.CompletedTask;
            var lines = ReductionAppendix.Build(_items);
            if (lines.Count == 0)
            {
                MessageBox.Show(this, "Kỳ này không có hàng bán ra thuế suất 8% nên không có gì để lập phụ lục giảm thuế.",
                    "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return Task.CompletedTask;
            }
            string taxCode = _taxCode;
            var period = _period;
            return RunExportAsync("Lưu phụ lục giảm thuế dự thảo", $"PhuLucGiamThue_{SafeLabel()}.xlsx",
                path => ExcelExporter.ExportReductionAppendix(lines, period, taxCode, path),
                $"Đã xuất phụ lục giảm thuế dự thảo ({period.Label}).");
        }

        private Task ExportAccountingAsync()
        {
            if (!CanExportNow() || _cboProfile.SelectedItem is not ProfileItem item) return Task.CompletedTask;
            var profile = item.Profile;
            var items = _items;
            string safeName = string.Concat(profile.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c)).Replace(' ', '_');
            return RunExportAsync($"Lưu file nhập kế toán ({profile.Name})", $"KeToan_{safeName}_{SafeLabel()}.xlsx",
                path => AccountingExporter.Export(profile, items, path),
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
