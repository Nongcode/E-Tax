using System.ComponentModel;
using System.Diagnostics;

namespace E_Tax
{
    /// <summary>
    /// Màn hình kho hóa đơn cục bộ: xem theo doanh nghiệp, lọc, phân trang 200 dòng, mở PDF/XML, ghi chú,
    /// xuất Excel, sao lưu/khôi phục và quản lý tác vụ tải. Dựng bằng code (không có Designer).
    /// Mọi thao tác dài chạy bằng Task.Run, khóa giao diện trong lúc làm việc.
    /// </summary>
    internal sealed class KhoHoaDonForm : Form
    {
        private const string AllLabel = "Tất cả";

        private readonly LocalInvoiceStore _store;
        private readonly DownloadJobStore _jobs;
        private readonly NotesStore _notes;
        private readonly string? _currentTaxCode;

        private readonly Control _root;
        // ListView có checkbox: chọn dòng chỉ để xem, chỉ ô tick mới chọn doanh nghiệp để xuất (CheckedListBox cần bấm đôi nên không dùng)
        private readonly CompanyList _lvCompanies = new()
        {
            View = View.Details, CheckBoxes = true, FullRowSelect = true, MultiSelect = false, HeaderStyle = ColumnHeaderStyle.None,
            HideSelection = false, Dock = DockStyle.Fill
        };
        private readonly CheckBox _chkFrom = new() { Text = "Từ", AutoSize = true };
        private readonly DateTimePicker _dtpFrom = new() { Format = DateTimePickerFormat.Short, Width = 105 };
        private readonly CheckBox _chkTo = new() { Text = "Đến", AutoSize = true };
        private readonly DateTimePicker _dtpTo = new() { Format = DateTimePickerFormat.Short, Width = 105 };
        private readonly TextBox _txtSupplier = new() { Width = 150, PlaceholderText = "MST / tên người bán" };
        private readonly TextBox _txtNumber = new() { Width = 70, PlaceholderText = "Số HĐ" };
        private readonly ComboBox _cboType = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
        private readonly ComboBox _cboStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        private readonly Button _btnSearch = new() { Text = "Lọc", AutoSize = true };
        private readonly DataGridView _dgv = new();
        private readonly Button _btnPrev = new() { Text = "< Trước", AutoSize = true };
        private readonly Button _btnNext = new() { Text = "Sau >", AutoSize = true };
        private readonly Label _lblPage = new() { AutoSize = true, Padding = new Padding(8, 6, 8, 0) };
        private readonly Button _btnPdf = new() { Text = "Mở PDF", AutoSize = true };
        private readonly Button _btnXml = new() { Text = "Mở XML", AutoSize = true };
        private readonly TextBox _txtNote = new() { Width = 300, PlaceholderText = "Ghi chú cho hóa đơn đang chọn" };
        private readonly Button _btnSaveNote = new() { Text = "Lưu ghi chú", AutoSize = true };
        private readonly Button _btnExport = new() { Text = "Xuất Excel (DN đã tick)", AutoSize = true };
        private readonly Button _btnBackup = new() { Text = "Sao lưu", AutoSize = true };
        private readonly Button _btnRestore = new() { Text = "Khôi phục", AutoSize = true };
        private readonly Label _lblStatus = new() { AutoSize = true, Padding = new Padding(0, 6, 0, 0) };

        private readonly DataGridView _dgvJobs = new();
        private readonly Button _btnJobRefresh = new() { Text = "Làm mới", AutoSize = true };
        private readonly Button _btnJobRetry = new() { Text = "Thử lại mục lỗi", AutoSize = true };
        private readonly Button _btnJobDelete = new() { Text = "Xóa job", AutoSize = true };

        private List<SearchResult> _all = new();
        private IReadOnlyDictionary<string, InvoiceLink> _links = new Dictionary<string, InvoiceLink>();
        private string? _viewed;
        private int _page;
        private bool _busy;
        private bool _loadingCompanies;

        public KhoHoaDonForm(LocalInvoiceStore store, DownloadJobStore jobs, NotesStore notes, string? currentTaxCode)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
            _notes = notes ?? throw new ArgumentNullException(nameof(notes));
            _currentTaxCode = currentTaxCode;

            Text = "Kho hóa đơn cục bộ";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 520);
            Size = new Size(1150, 680);

            _cboType.Items.AddRange(new object[] { AllLabel, "Bán ra", "Mua vào" });
            _cboType.SelectedIndex = 0;
            _cboStatus.Items.Add(AllLabel);
            for (int s = 1; s <= 6; s++) _cboStatus.Items.Add(InvoiceClassifier.Label(InvoiceClassifier.RelationOf(s)));
            _cboStatus.SelectedIndex = 0;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            var tabInvoices = new TabPage("Hóa đơn");
            var tabJobs = new TabPage("Tác vụ tải");
            tabs.TabPages.Add(tabInvoices);
            tabs.TabPages.Add(tabJobs);
            _root = tabs;
            Controls.Add(tabs);

            BuildInvoicesTab(tabInvoices);
            BuildJobsTab(tabJobs);
            WireEvents(tabs);

            Load += async (_, _) => await LoadCompaniesAsync();
            FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
            UiTheme.ApplyTo(this);
        }

        /// <summary>ListView bỏ thông điệp bấm đúp để bấm nhanh hai lần không tự đổi tick.</summary>
        private sealed class CompanyList : ListView
        {
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x0203) return; // WM_LBUTTONDBLCLK
                base.WndProc(ref m);
            }
        }

        private void BuildInvoicesTab(TabPage tab)
        {
            var left = new Panel { Dock = DockStyle.Left, Width = 190 };
            _lvCompanies.Columns.Add("MST", 160);
            _lvCompanies.SizeChanged += (_, _) => _lvCompanies.Columns[0].Width = Math.Max(60, _lvCompanies.ClientSize.Width - 2);
            left.Controls.Add(_lvCompanies);
            left.Controls.Add(new Label { Text = "Doanh nghiệp (bấm để xem, tick ô vuông để xuất Excel)", Dock = DockStyle.Top, Height = 36, Padding = new Padding(2, 4, 2, 0) });

            var filter = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
            filter.Controls.AddRange(new Control[] { _chkFrom, _dtpFrom, _chkTo, _dtpTo, _txtSupplier, _txtNumber, _cboType, _cboStatus, _btnSearch });

            var note = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = true };
            note.Controls.AddRange(new Control[] { _btnPdf, _btnXml, _txtNote, _btnSaveNote, _btnExport, _btnBackup, _btnRestore });

            var paging = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
            paging.Controls.AddRange(new Control[] { _btnPrev, _lblPage, _btnNext, _lblStatus });

            _dgv.Dock = DockStyle.Fill;
            _dgv.AutoGenerateColumns = false;
            _dgv.AllowUserToAddRows = false;
            _dgv.AllowUserToDeleteRows = false;
            _dgv.ReadOnly = true;
            _dgv.RowHeadersVisible = false;
            _dgv.MultiSelect = false;
            _dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgv.BackgroundColor = SystemColors.ButtonHighlight;
            _dgv.Columns.AddRange(
                Col(nameof(StoreRow.Loai), "Loại", 70),
                Col(nameof(StoreRow.Mst), "MST người bán", 100),
                Col(nameof(StoreRow.KyHieu), "Ký hiệu", 80),
                Col(nameof(StoreRow.SoHd), "Số HĐ", 70),
                Col(nameof(StoreRow.NgayLap), "Ngày lập", 85),
                Col(nameof(StoreRow.NguoiBan), "Người bán", 200),
                Col(nameof(StoreRow.TongTien), "Tổng tiền", 110, "N0"),
                Col(nameof(StoreRow.PhanLoai), "Phân loại", 170),
                Col(nameof(StoreRow.CanhBao), "Cảnh báo", 170),
                Col(nameof(StoreRow.Files), "File", 70),
                Col(nameof(StoreRow.GhiChu), "Ghi chú", 160));

            tab.Controls.Add(_dgv);
            tab.Controls.Add(filter);
            tab.Controls.Add(paging);
            tab.Controls.Add(note);
            tab.Controls.Add(left);
            left.BringToFront();
            _dgv.BringToFront();
        }

        private void BuildJobsTab(TabPage tab)
        {
            _dgvJobs.Dock = DockStyle.Fill;
            _dgvJobs.AutoGenerateColumns = false;
            _dgvJobs.AllowUserToAddRows = false;
            _dgvJobs.AllowUserToDeleteRows = false;
            _dgvJobs.ReadOnly = true;
            _dgvJobs.RowHeadersVisible = false;
            _dgvJobs.MultiSelect = false;
            _dgvJobs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgvJobs.BackgroundColor = SystemColors.ButtonHighlight;
            _dgvJobs.Columns.AddRange(
                Col(nameof(DownloadJobSummary.Title), "Thư mục làm việc", 420),
                Col(nameof(DownloadJobSummary.CreatedUtc), "Tạo lúc", 140),
                Col(nameof(DownloadJobSummary.Pending), "Chờ", 60),
                Col(nameof(DownloadJobSummary.Done), "Xong", 60),
                Col(nameof(DownloadJobSummary.Failed), "Lỗi", 60));

            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            bar.Controls.AddRange(new Control[] { _btnJobRefresh, _btnJobRetry, _btnJobDelete });
            tab.Controls.Add(_dgvJobs);
            tab.Controls.Add(bar);
            _dgvJobs.BringToFront();
        }

        private static DataGridViewTextBoxColumn Col(string property, string header, int width, string? format = null)
        {
            var c = new DataGridViewTextBoxColumn { DataPropertyName = property, Name = "col" + property, HeaderText = header, Width = width, ReadOnly = true };
            if (format != null) c.DefaultCellStyle.Format = format;
            return c;
        }

        private void WireEvents(TabControl tabs)
        {
            _lvCompanies.SelectedIndexChanged += async (_, _) =>
            {
                if (_loadingCompanies || _busy || _lvCompanies.SelectedItems.Count == 0) return;
                string selected = _lvCompanies.SelectedItems[0].Text;
                if (selected == _viewed) return;
                _viewed = selected;
                await QueryAsync();
            };
            _btnSearch.Click += async (_, _) => await QueryAsync();
            _btnPrev.Click += async (_, _) => { _page--; await RunBusyAsync("Đang tải trang...", ShowPageAsync); };
            _btnNext.Click += async (_, _) => { _page++; await RunBusyAsync("Đang tải trang...", ShowPageAsync); };
            _btnPdf.Click += (_, _) => OpenSelected(".pdf");
            _btnXml.Click += (_, _) => OpenSelected(".xml");
            _btnSaveNote.Click += async (_, _) => await SaveNoteAsync();
            _btnExport.Click += async (_, _) => await ExportAsync();
            _btnBackup.Click += async (_, _) => await BackupAsync();
            _btnRestore.Click += async (_, _) => await RestoreAsync();
            _dgv.SelectionChanged += (_, _) => _txtNote.Text = (_dgv.CurrentRow?.DataBoundItem as StoreRow)?.GhiChu ?? "";
            _dgv.CellFormatting += DgvCellFormatting;
            _dgv.DataError += (_, e) => e.ThrowException = false;
            _dgvJobs.DataError += (_, e) => e.ThrowException = false;
            _dgvJobs.CellFormatting += (_, e) =>
            {
                if (e.Value is DateTime utc && _dgvJobs.Columns[e.ColumnIndex].DataPropertyName == nameof(DownloadJobSummary.CreatedUtc))
                {
                    e.Value = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                    e.FormattingApplied = true;
                }
            };
            tabs.SelectedIndexChanged += async (_, _) => { if (tabs.SelectedIndex == 1) await RefreshJobsAsync(); };
            _btnJobRefresh.Click += async (_, _) => await RefreshJobsAsync();
            _btnJobRetry.Click += async (_, _) => await RetryJobAsync();
            _btnJobDelete.Click += async (_, _) => await DeleteJobAsync();
        }

        private void DgvCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _dgv.Rows.Count || e.CellStyle == null) return;
            if (_dgv.Rows[e.RowIndex].DataBoundItem is not StoreRow row) return;
            var color = InvoiceGridLogic.ClassificationColor(InvoiceClassifier.RelationOf(row.Invoice.Trang_thai_hoa_don));
            // Chỉ đặt BackColor: dòng đang chọn vẫn dùng SelectionBackColor nên đọc được
            if (color.HasValue) e.CellStyle.BackColor = color.Value;
        }

        // ---------- Chạy việc dài ----------

        private void SetBusy(bool busy, string status)
        {
            _busy = busy;
            _root.Enabled = !busy;
            UseWaitCursor = busy;
            _lblStatus.Text = status;
        }

        /// <summary>Khóa giao diện trong lúc làm việc; lỗi được ghi log và báo, không làm sập form.</summary>
        private async Task RunBusyAsync(string status, Func<Task> work)
        {
            if (_busy) return;
            SetBusy(true, status);
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                Logger.Write($"❌ Kho hóa đơn: {ex.GetType().Name}: {ex.Message}");
                MessageBox.Show(this, ex.Message, "Lỗi kho hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false, "");
            }
        }

        // ---------- Danh sách doanh nghiệp, truy vấn, phân trang ----------

        private async Task LoadCompaniesAsync()
        {
            string? keep = _viewed;
            await RunBusyAsync("Đang đọc danh sách doanh nghiệp...", async () =>
            {
                var companies = await Task.Run(() => _store.Companies());
                _loadingCompanies = true;
                try
                {
                    var ticked = _lvCompanies.CheckedItems.Cast<ListViewItem>().Select(i => i.Text).ToHashSet();
                    _lvCompanies.Items.Clear();
                    foreach (string c in companies) _lvCompanies.Items.Add(new ListViewItem(c) { Checked = ticked.Contains(c) });
                    string? pick = new[] { keep, _currentTaxCode }.FirstOrDefault(c => c != null && companies.Contains(c)) ?? companies.FirstOrDefault();
                    _viewed = pick;
                    if (pick != null) _lvCompanies.Items.Cast<ListViewItem>().First(i => i.Text == pick).Selected = true;
                }
                finally { _loadingCompanies = false; }
                await QueryCoreAsync();
            });
        }

        private Task QueryAsync() => RunBusyAsync("Đang truy vấn kho...", QueryCoreAsync);

        private StoreQuery BuildQuery()
        {
            int? status = _cboStatus.SelectedIndex > 0 ? _cboStatus.SelectedIndex : null;
            string? type = _cboType.SelectedIndex > 0 ? _cboType.SelectedItem as string : null;
            return new StoreQuery(
                _chkFrom.Checked ? _dtpFrom.Value.Date : null,
                _chkTo.Checked ? _dtpTo.Value.Date : null,
                _txtSupplier.Text, type, status, _txtNumber.Text);
        }

        private async Task QueryCoreAsync()
        {
            string? company = _viewed;
            if (company == null)
            {
                _all = new List<SearchResult>();
                _links = new Dictionary<string, InvoiceLink>();
                _page = 0;
                await ShowPageAsync();
                return;
            }
            var query = BuildQuery();
            var quarantined = new List<string>();
            while (true)
            {
                try
                {
                    var (list, links) = await Task.Run(() =>
                    {
                        var l = _store.Query(company, query);
                        return (l, InvoiceLinker.Link(l));
                    });
                    _all = list;
                    _links = links;
                    break;
                }
                catch (InvalidDataException ex) when (LocalInvoiceStore.CorruptFileOf(ex) != null)
                {
                    string file = LocalInvoiceStore.CorruptFileOf(ex)!;
                    Logger.Write($"❌ Kho hóa đơn: file tháng bị hỏng: {company}\\{file}");
                    var answer = MessageBox.Show(this,
                        $"File dữ liệu {file} của doanh nghiệp {company} bị hỏng nên chưa mở được kho.\n\n" +
                        "Có: cách ly file này (đổi tên thành .corrupt, không xóa) rồi mở các tháng còn lại.\n" +
                        "Không: giữ nguyên, không mở doanh nghiệp này.",
                        "File kho bị hỏng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (answer != DialogResult.Yes)
                    {
                        _all = new List<SearchResult>();
                        _links = new Dictionary<string, InvoiceLink>();
                        _page = 0;
                        await ShowPageAsync();
                        return;
                    }
                    await Task.Run(() => _store.QuarantineCorruptFile(company, file));
                    quarantined.Add(file);
                }
            }
            _page = 0;
            await ShowPageAsync();
            if (quarantined.Count > 0)
                MessageBox.Show(this,
                    $"Đã cách ly: {string.Join(", ", quarantined)}.\nDữ liệu của các tháng này chưa có trong kho. Hãy dùng nút Khôi phục với một bản sao lưu để lấy lại.",
                    "Cần khôi phục dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async Task ShowPageAsync()
        {
            _page = InvoiceGridLogic.ClampPage(_page, _all.Count);
            var slice = InvoiceGridLogic.Slice(_all, _page);
            string? company = _viewed;
            string? own = company == null ? null : InvoiceGridLogic.PartyCheckTaxCode(_currentTaxCode, company);
            var links = _links;

            var rows = company == null
                ? new List<StoreRow>()
                : await Task.Run(() => slice.Select(inv =>
                {
                    IReadOnlyList<string> files;
                    try { files = _store.FilesFor(company, inv); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { files = Array.Empty<string>(); }
                    InvoiceLink? link = null;
                    if (!string.IsNullOrEmpty(inv.Id)) links.TryGetValue(inv.Id, out link);
                    return InvoiceGridLogic.ToRow(inv, link, own, _notes.Get(inv.Id), files);
                }).ToList());

            _dgv.DataSource = new BindingList<StoreRow>(rows);
            int pages = InvoiceGridLogic.PageCount(_all.Count);
            _lblPage.Text = $"Trang {_page + 1}/{pages} ({_all.Count} hóa đơn)";
            _btnPrev.Enabled = _page > 0;
            _btnNext.Enabled = _page < pages - 1;
            if (company != null && _all.Count == 0)
                _lblPage.Text += " - không có hóa đơn phù hợp";
        }

        // ---------- Mở file, ghi chú ----------

        private StoreRow? CurrentRow() => _dgv.CurrentRow?.DataBoundItem as StoreRow;

        private void OpenSelected(string extension)
        {
            var row = CurrentRow();
            if (row == null || _viewed == null)
            {
                MessageBox.Show(this, "Vui lòng chọn một hóa đơn.", "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                // Chỉ mở đường dẫn do kho trả về (FilesFor)
                string? path = _store.FilesFor(_viewed, row.Invoice)
                    .FirstOrDefault(f => string.Equals(Path.GetExtension(f), extension, StringComparison.OrdinalIgnoreCase));
                if (path == null)
                {
                    MessageBox.Show(this, $"Hóa đơn này chưa có file {extension.TrimStart('.').ToUpperInvariant()} trong kho. Hãy dùng 'Tải tất cả HĐ Gốc' để thêm vào kho.",
                        "Chưa có file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Write($"❌ Kho hóa đơn: không mở được file: {ex.GetType().Name}");
                MessageBox.Show(this, "Không mở được file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveNoteAsync()
        {
            var row = CurrentRow();
            if (row == null || string.IsNullOrEmpty(row.Invoice.Id))
            {
                MessageBox.Show(this, "Vui lòng chọn một hóa đơn có mã định danh để ghi chú.", "Không thể ghi chú", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string id = row.Invoice.Id;
            string text = _txtNote.Text;
            int index = _dgv.CurrentRow?.Index ?? 0;
            await RunBusyAsync("Đang lưu ghi chú...", async () =>
            {
                await Task.Run(() => { _notes.Set(id, text); _notes.Save(); });
                await ShowPageAsync();
                if (index >= 0 && index < _dgv.Rows.Count) _dgv.CurrentCell = _dgv.Rows[index].Cells[0];
            });
        }

        // ---------- Xuất Excel, sao lưu, khôi phục ----------

        private async Task ExportAsync()
        {
            var codes = _lvCompanies.CheckedItems.Cast<ListViewItem>().Select(i => i.Text).ToList();
            if (codes.Count == 0)
            {
                if (_viewed == null)
                {
                    MessageBox.Show(this, "Chưa có doanh nghiệp nào để xuất.", "Chưa chọn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show(this, $"Chưa tick doanh nghiệp nào. Xuất doanh nghiệp đang xem ({_viewed})?", "Xuất Excel",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                codes.Add(_viewed);
            }
            using var dlg = new SaveFileDialog { Title = "Xuất Excel: " + string.Join(", ", codes), Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"KhoHoaDon_{DateTime.Now:yyyyMMdd}.xlsx" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            string path = dlg.FileName;
            var query = BuildQuery(); // đọc bộ lọc trên UI thread, áp dụng cho mọi doanh nghiệp đã tick

            await RunBusyAsync("Đang xuất Excel...", async () =>
            {
                await Task.Run(() =>
                {
                    var data = new List<(string TaxCode, IReadOnlyList<SearchResult> Invoices)>();
                    foreach (string c in codes)
                        data.Add((c, _store.Query(c, query)));
                    ExcelExporter.ExportCompanies(data, path);
                });
                MessageBox.Show(this, $"Đã xuất {codes.Count} doanh nghiệp theo bộ lọc hiện tại ra:\n{path}", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private async Task BackupAsync()
        {
            using var dlg = new SaveFileDialog { Filter = "File sao lưu (*.zip)|*.zip", FileName = $"E-Tax_Kho_{DateTime.Now:yyyyMMdd_HHmm}.zip" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            string path = dlg.FileName;
            await RunBusyAsync("Đang sao lưu...", async () =>
            {
                var warnings = await Task.Run(() => BackupService.Create(_store.Root, NotesStore.DefaultPath, path));
                string extra = warnings.Count > 0 ? "\n\nLưu ý:\n - " + string.Join("\n - ", warnings) : "";
                MessageBox.Show(this, "Đã sao lưu kho hóa đơn và ghi chú ra:\n" + path + extra, "Hoàn tất", MessageBoxButtons.OK,
                    warnings.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            });
        }

        private async Task RestoreAsync()
        {
            using var dlg = new OpenFileDialog { Filter = "File sao lưu (*.zip)|*.zip" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            string path = dlg.FileName;

            var answer = MessageBox.Show(this,
                "Khôi phục từ file sao lưu:\n" + path + "\n\n" +
                "Hóa đơn được GỘP theo từng hóa đơn: hóa đơn chỉ có trong kho vẫn được giữ, hóa đơn chỉ có trong bản sao lưu được thêm.\n\n" +
                "Có: khi trùng hóa đơn thì lấy bản trong file sao lưu; file đính kèm và ghi chú bị ghi đè.\n" +
                "Không: khi trùng hóa đơn thì giữ bản trong kho; không ghi đè file đính kèm và ghi chú.\n" +
                "Hủy: không làm gì.\n\n" +
                "Kho hiện tại được tự động sao lưu trước khi khôi phục.",
                "Xác nhận khôi phục", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button3);
            if (answer == DialogResult.Cancel) return;
            bool overwrite = answer == DialogResult.Yes;

            await RunBusyAsync("Đang khôi phục...", async () =>
            {
                var (written, safety) = await Task.Run(() =>
                {
                    int n = BackupService.Restore(path, _store.Root, NotesStore.DefaultPath, null, overwrite, out string? safetyPath);
                    return (n, safetyPath);
                });
                _notes.Load();
                string safetyText = safety != null ? $"\nBản sao lưu an toàn của kho trước khi khôi phục:\n{safety}" : "";
                MessageBox.Show(this, $"Đã khôi phục {written} file ({(overwrite ? "ưu tiên bản sao lưu khi trùng" : "ưu tiên kho hiện tại khi trùng")}).{safetyText}", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            await LoadCompaniesAsync(); // tải lại danh sách sau khôi phục
        }

        // ---------- Tác vụ tải ----------

        private DownloadJobSummary? CurrentJob() => _dgvJobs.CurrentRow?.DataBoundItem as DownloadJobSummary;

        private Task RefreshJobsAsync() => RunBusyAsync("Đang đọc danh sách tác vụ...", async () =>
        {
            var list = await Task.Run(() => _jobs.List());
            _dgvJobs.DataSource = new BindingList<DownloadJobSummary>(list.ToList());
        });

        private async Task RetryJobAsync()
        {
            var job = CurrentJob();
            if (job == null)
            {
                MessageBox.Show(this, "Vui lòng chọn một tác vụ.", "Chưa chọn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            await RunBusyAsync("Đang đặt lại mục lỗi...", async () =>
            {
                int n = await Task.Run(() => _jobs.RetryFailed(job.JobId));
                MessageBox.Show(this,
                    n == 0 ? "Tác vụ này không có mục lỗi." :
                    $"Đã đặt lại {n} mục lỗi về trạng thái chờ. Hãy chạy lại 'Tải tất cả HĐ Gốc' với cùng khoảng ngày để tải tiếp.",
                    "Thử lại", MessageBoxButtons.OK, MessageBoxIcon.Information);
                var list = await Task.Run(() => _jobs.List());
                _dgvJobs.DataSource = new BindingList<DownloadJobSummary>(list.ToList());
            });
        }

        private async Task DeleteJobAsync()
        {
            var job = CurrentJob();
            if (job == null)
            {
                MessageBox.Show(this, "Vui lòng chọn một tác vụ.", "Chưa chọn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this, "Xóa tác vụ này? Lần tải sau sẽ không tiếp tục từ tiến độ đã lưu (file đã tải trong kho không bị xóa).",
                    "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            await RunBusyAsync("Đang xóa tác vụ...", async () =>
            {
                await Task.Run(() => _jobs.Delete(job.JobId));
                var list = await Task.Run(() => _jobs.List());
                _dgvJobs.DataSource = new BindingList<DownloadJobSummary>(list.ToList());
            });
        }
    }
}
