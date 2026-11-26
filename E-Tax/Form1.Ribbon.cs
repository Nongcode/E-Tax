using System.Drawing;
using System.Windows.Forms;

namespace E_Tax
{
    /// <summary>
    /// Bố cục Ribbon kiểu Office (docs/features/giao-dien/thiet-ke-lai/design.md).
    /// Designer vẫn tạo control và gắn handler như cũ; hàm này chỉ chuyển các control sang khung mới và áp style,
    /// nên không đụng nghiệp vụ. Bố cục dựng bằng code để sau này có thể thêm kiểu bố cục khác.
    /// </summary>
    public partial class Form1
    {
        private const int BigButtonWidth = 60;
        private const int BigButtonHeight = 74;

        private readonly ToolTip _ribbonTips = new() { InitialDelay = 400 };
        private Panel? _tabStrip, _ribbon, _filterBar;
        private FlowLayoutPanel? _ribbonFlow, _filterFlow;

        /// <summary>
        /// Cửa sổ hẹp: ribbon cuộn ngang (nới chiều cao để thanh cuộn không che nhãn nhóm),
        /// thanh lọc xuống dòng (nới chiều cao theo số dòng). Gọi lại mỗi khi vùng làm việc đổi kích thước.
        /// </summary>
        private void FitBars()
        {
            if (_ribbon == null || _ribbonFlow == null || _filterBar == null || _filterFlow == null) return;
            int needed = _ribbonFlow.Padding.Horizontal + _ribbonFlow.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Horizontal);
            int ribbonHeight = RibbonHeight + (needed > panelSearch.ClientSize.Width ? SystemInformation.HorizontalScrollBarHeight : 0);
            if (_ribbon.Height != ribbonHeight) _ribbon.Height = ribbonHeight;

            int flowWidth = panelSearch.ClientSize.Width - (lblStatusMessage.Visible ? lblStatusMessage.Width : 0);
            int filterHeight = Math.Max(40, _filterFlow.GetPreferredSize(new Size(Math.Max(1, flowWidth), 0)).Height);
            if (_filterBar.Height != filterHeight) _filterBar.Height = filterHeight;
        }

        /// <summary>Kiểu Ribbon: dải lệnh chia nhóm + thanh lọc cố định. Trả các khung đặt ở trên (từ trên xuống).</summary>
        private Control[] BuildRibbonLayout()
        {
            var ribbon = BuildRibbon();
            var filterBar = BuildFilterBar();
            _fitLayout = FitBars;
            return new Control[] { ribbon, filterBar };
        }

        #region Ribbon

        private Panel BuildRibbon()
        {
            var flow = new FlowLayoutPanel
            {
                Name = "ribbonGroups",
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(4, 4, 4, 0),
                BackColor = UiTheme.RibbonBack
            };
            flow.Controls.Add(Group("grpTraCuu", "Tra cứu", SearchInputs(), Big(btnLeftSearch, "search", "Tìm kiếm", "Tìm hóa đơn trên cổng thuế theo kỳ và loại đã chọn")));
            flow.Controls.Add(Group("grpHoaDon", "Hóa đơn",
                Big(btnXemHD, "eye", "Xem hóa đơn", "Xem hóa đơn đang chọn"),
                Stack(Small(btnCnKoPdf, "print", "In PDF", "In các hóa đơn đã chọn ra PDF"),
                      Small(btnGopPdf, "layers", "Gộp PDF", "Gộp các hóa đơn đã chọn thành một file PDF"),
                      Small(btnTaiHDGoc, "download", "Tải HĐ gốc", "Tải file gốc (XML/PDF) của tất cả hóa đơn"))));
            flow.Controls.Add(Group("grpXuat", "Xuất Excel",
                Big(btnExportDS, "sheet", "Danh sách", "Xuất danh sách hóa đơn ra Excel"),
                Stack(Small(btnExportChiTiet, "list", "Chi tiết hàng hóa", "Xuất chi tiết hàng hóa của hóa đơn ra Excel"),
                      Small(btnExportBangKeMua, "file", "Bảng kê mua", "Xuất bảng kê hóa đơn mua vào"),
                      Small(btnExportBangKeBan, "file", "Bảng kê bán", "Xuất bảng kê hóa đơn bán ra"))));
            flow.Controls.Add(Group("grpKeKhai", "Kê khai",
                Big(btnKeKhai, "calc", "Lập tờ khai", "Lập tờ khai 01/GTGT, phụ lục và file kế toán"),
                Big(btnKhoHoaDon, "box", "Kho hóa đơn", "Mở kho hóa đơn đã lưu trên máy")));
            flow.Controls.Add(Group("grpKiemTra", "Kiểm tra",
                Big(btnKiemTraNCC, "shield", "Kiểm tra NCC", "Kiểm tra nhà cung cấp rủi ro của các hóa đơn mua vào"),
                RetailCheck()));

            // Chỉ cuộn ngang: tắt thanh cuộn dọc trước khi bật AutoScroll (mẹo WinForms)
            flow.AutoScroll = false;
            flow.VerticalScroll.Maximum = 0;
            flow.VerticalScroll.Enabled = false;
            flow.VerticalScroll.Visible = false;
            flow.AutoScroll = true;

            var ribbon = new Panel { Name = "ribbon", Dock = DockStyle.Top, Height = RibbonHeight, BackColor = UiTheme.RibbonBack };
            ribbon.Controls.Add(flow);
            _ribbon = ribbon;
            _ribbonFlow = flow;
            ribbon.Paint += (_, e) => e.Graphics.DrawLine(BorderPen, 0, ribbon.Height - 1, ribbon.Width, ribbon.Height - 1);
            return ribbon;
        }

        private const int RibbonHeight = 104;

        private static readonly Pen BorderPen = new(UiTheme.Border);
        private static readonly Pen DividerPen = new(UiTheme.Divider);

        /// <summary>Nhóm ribbon: nội dung ngang ở trên, nhãn nhóm ở đáy, vạch ngăn bên phải.</summary>
        private Control Group(string name, string caption, params Control[] items)
        {
            var content = new FlowLayoutPanel
            {
                Name = "content_" + name,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = UiTheme.RibbonBack
            };
            content.Controls.AddRange(items);
            var label = new Label
            {
                Name = "lblCaption_" + name,
                Text = caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = UiTheme.SmallFont,
                ForeColor = UiTheme.MutedText,
                Margin = Padding.Empty
            };
            var group = new TableLayoutPanel
            {
                Name = name,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 4, 0),
                Padding = new Padding(2, 0, 6, 0),
                BackColor = UiTheme.RibbonBack
            };
            group.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            group.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
            group.Controls.Add(content, 0, 0);
            group.Controls.Add(label, 0, 1);
            group.Paint += (_, e) => e.Graphics.DrawLine(DividerPen, group.Width - 1, 4, group.Width - 1, group.Height - 4);
            return group;
        }

        /// <summary>Nút lớn: icon 24px trên, chữ dưới (tối đa 2 dòng), nền trong suốt, hover xanh nhạt.</summary>
        private Button Big(Button button, string icon, string text, string tip)
        {
            ResetButton(button);
            SetCommandText(button, text);
            button.AutoSize = false;
            button.Image = RibbonIcons.Get(icon, 24, UiTheme.Primary);
            button.TextImageRelation = button.Image != null ? TextImageRelation.ImageAboveText : TextImageRelation.Overlay;
            button.ImageAlign = ContentAlignment.TopCenter;
            button.TextAlign = ContentAlignment.TopCenter;
            button.Size = new Size(BigButtonWidth, BigButtonHeight);
            button.Margin = new Padding(1, 2, 1, 0);
            button.Padding = new Padding(0, 6, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = UiTheme.HoverBack;
            button.FlatAppearance.MouseDownBackColor = UiTheme.Selection;
            button.BackColor = UiTheme.RibbonBack;
            button.ForeColor = UiTheme.Text;
            button.Font = UiTheme.BaseFont;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            button.Dock = DockStyle.None;
            _ribbonTips.SetToolTip(button, tip);
            return button;
        }

        /// <summary>Nút nhỏ (lệnh phụ): icon 16px bên trái, cao 24px, xếp chồng 3 hàng trong một cột.</summary>
        private Button Small(Button button, string icon, string text, string tip)
        {
            ResetButton(button);
            SetCommandText(button, text);
            button.AutoSize = false;
            button.Image = WithGap(RibbonIcons.Get(icon, 16, UiTheme.Primary), 6);
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Size = new Size(134, 24);
            button.Margin = new Padding(0, 1, 0, 1);
            button.Padding = new Padding(4, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = UiTheme.HoverBack;
            button.FlatAppearance.MouseDownBackColor = UiTheme.Selection;
            button.BackColor = UiTheme.RibbonBack;
            button.ForeColor = UiTheme.Text;
            button.Font = UiTheme.BaseFont;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            button.Dock = DockStyle.None;
            _ribbonTips.SetToolTip(button, tip);
            return button;
        }

        /// <summary>Thêm khoảng trống bên phải icon để chữ không dính sát icon (Button không có thuộc tính khoảng cách).</summary>
        private static Bitmap? WithGap(Bitmap? icon, int gap)
        {
            if (icon == null) return null;
            var padded = new Bitmap(icon.Width + gap, icon.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(padded)) g.DrawImage(icon, 0, 0, icon.Width, icon.Height);
            icon.Dispose();
            return padded;
        }

        private static FlowLayoutPanel Stack(params Control[] items)
        {
            var stack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(2, 3, 2, 0),
                Padding = Padding.Empty,
                BackColor = UiTheme.RibbonBack
            };
            stack.Controls.AddRange(items);
            return stack;
        }

        /// <summary>Kỳ thuế, khoảng ngày, loại hóa đơn: nhãn bên trái mỗi hàng, ô nhập cao đồng nhất.</summary>
        private Control SearchInputs()
        {
            var rows = new TableLayoutPanel
            {
                Name = "searchInputs",
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 6, 6, 0),
                BackColor = UiTheme.RibbonBack
            };
            for (int i = 0; i < 2; i++) rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            Input(cbPeriodKind, 84);
            Input(cbPeriodNumber, 50);
            Input(nudPeriodYear, 64);
            Input(dtpFromDate, 104);
            Input(dtpToDate, 104);
            foreach (var dtp in new[] { dtpFromDate, dtpToDate })
            {
                dtp.Format = DateTimePickerFormat.Custom;
                dtp.CustomFormat = "dd/MM/yyyy";
            }

            var period = Row(cbPeriodKind, cbPeriodNumber, nudPeriodYear);
            var range = Row(dtpFromDate, Dash(), dtpToDate);
            rows.Controls.Add(FieldLabel("Kỳ thuế"), 0, 0);
            rows.Controls.Add(period, 1, 0);
            rows.Controls.Add(FieldLabel("Ngày"), 0, 1);
            rows.Controls.Add(range, 1, 1);

            var kinds = new FlowLayoutPanel
            {
                Name = "invoiceKinds",
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(8, 4, 4, 0),
                BackColor = UiTheme.RibbonBack
            };
            foreach (var (rb, text) in new[] { (rbSold, "Bán ra"), (rbBought, "Mua vào"), (rbAllInvoices, "Tất cả") })
            {
                rb.Appearance = Appearance.Normal; // kiểu khác dùng nút ghép
                rb.FlatStyle = FlatStyle.Standard;
                rb.BackColor = UiTheme.RibbonBack;
                rb.TextAlign = ContentAlignment.MiddleLeft;
                rb.Cursor = Cursors.Default;
                rb.Text = text;
                rb.AutoSize = true;
                rb.Font = UiTheme.BaseFont;
                rb.ForeColor = UiTheme.Text;
                rb.Margin = new Padding(0, 1, 0, 1);
                rb.Anchor = AnchorStyles.Left;
                kinds.Controls.Add(rb);
            }

            var wrap = new FlowLayoutPanel
            {
                Name = "searchInputsWrap",
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty,
                BackColor = UiTheme.RibbonBack
            };
            wrap.Controls.Add(rows);
            wrap.Controls.Add(kinds);
            return wrap;
        }

        /// <summary>Ô MST + nút kiểm tra DN mua lẻ, xếp dọc có nhãn.</summary>
        private Control RetailCheck()
        {
            var stack = new FlowLayoutPanel
            {
                Name = "retailCheck",
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(6, 4, 2, 0),
                BackColor = UiTheme.RibbonBack
            };
            var label = FieldLabel("MST DN mua lẻ");
            label.Margin = new Padding(0, 0, 0, 2);
            Input(txtKtraMaDoanhNghiep, 136);
            txtKtraMaDoanhNghiep.PlaceholderText = "Nhập MST";
            txtKtraMaDoanhNghiep.Margin = new Padding(0, 0, 0, 4);
            ResetButton(btnKtraDNMuaLe);
            UiTheme.StyleButton(btnKtraDNMuaLe, primary: false);
            SetCommandText(btnKtraDNMuaLe, "Kiểm tra DN mua lẻ");
            btnKtraDNMuaLe.AutoSize = false;
            btnKtraDNMuaLe.Image = null;
            btnKtraDNMuaLe.Size = new Size(136, 26);
            btnKtraDNMuaLe.Margin = Padding.Empty;
            _ribbonTips.SetToolTip(btnKtraDNMuaLe, "Kiểm tra doanh nghiệp theo MST có trong danh sách rủi ro không");
            stack.Controls.Add(label);
            stack.Controls.Add(txtKtraMaDoanhNghiep);
            stack.Controls.Add(btnKtraDNMuaLe);
            return stack;
        }

        #endregion

        #region Thanh lọc

        private Panel BuildFilterBar()
        {
            // Cửa sổ hẹp: tự xuống dòng thay vì cắt mất ô/nút; thanh lọc cao theo nội dung
            var flow = new FlowLayoutPanel
            {
                Name = "filterFlow",
                Dock = DockStyle.Fill,
                WrapContents = true,
                Padding = new Padding(8, 6, 8, 6),
                BackColor = UiTheme.FilterBack
            };
            Input(txtLocSoHD, 80);
            txtLocSoHD.PlaceholderText = "Số HĐ";
            Input(txtLocNCC, 150);
            txtLocNCC.PlaceholderText = "Tên hoặc MST";
            Input(cboLocLoai, 90);
            chkChiCoGhiChu.Text = "Chỉ HĐ có ghi chú";
            chkChiCoGhiChu.AutoSize = true;
            chkChiCoGhiChu.Font = UiTheme.BaseFont;
            chkChiCoGhiChu.ForeColor = UiTheme.Text;
            chkChiCoGhiChu.Margin = new Padding(8, 5, 8, 0);
            SmallButton(btnRightSearch, "filter", "Lọc", 68);

            txtGhiChu.Multiline = false;
            Input(txtGhiChu, 170);
            txtGhiChu.PlaceholderText = "Ghi chú cho HĐ đang chọn";
            SmallButton(btnMoGhiChu, "note", "Lưu", 64);
            _ribbonTips.SetToolTip(btnMoGhiChu, "Lưu ghi chú cho hóa đơn đang chọn");

            flow.Controls.AddRange(new Control[]
            {
                FieldLabel("Số HĐ"), txtLocSoHD, FieldLabel("MST/Tên"), txtLocNCC, FieldLabel("Loại"), cboLocLoai,
                chkChiCoGhiChu, btnRightSearch, Separator(), FieldLabel("Ghi chú"), txtGhiChu, btnMoGhiChu
            });

            lblStatusMessage.Dock = DockStyle.Right;
            lblStatusMessage.AutoSize = false;
            lblStatusMessage.Width = 320;
            lblStatusMessage.TextAlign = ContentAlignment.MiddleRight;
            lblStatusMessage.Padding = new Padding(0, 0, 10, 0);
            lblStatusMessage.BackColor = UiTheme.FilterBack;

            var bar = new Panel { Name = "filterBar", Dock = DockStyle.Top, Height = 40, BackColor = UiTheme.FilterBack };
            _filterBar = bar;
            _filterFlow = flow;
            bar.Controls.Add(flow);
            bar.Controls.Add(lblStatusMessage);
            bar.Paint += (_, e) => e.Graphics.DrawLine(BorderPen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            return bar;
        }

        private void SmallButton(Button button, string icon, string text, int width)
        {
            ResetButton(button);
            UiTheme.StyleButton(button, primary: false);
            SetCommandText(button, text);
            button.AutoSize = false;
            button.Image = WithGap(RibbonIcons.Get(icon, 14, UiTheme.Primary), 4);
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.Size = new Size(width, 26);
            button.Margin = new Padding(4, 1, 4, 0);
        }

        #endregion

        #region Tab bảng

        private static readonly (string Page, string Text)[] TabTitles =
        {
            ("tabTongHop", "Tổng hợp"), ("tabChiTiet", "Chi tiết"), ("tabDkMua", "Bảng kê mua"),
            ("tabBKBan", "Bảng kê bán"), ("tabVATNop", "Bảng kê thuế"), ("tabGiamThue", "VAT nộp")
        };

        /// <summary>Ẩn tiêu đề tab gốc (WinForms không đổi màu được); dải tab phẳng tự vẽ thay thế.</summary>
        private void ConfigureTabs()
        {
            tabControlMain.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.Appearance = TabAppearance.FlatButtons;
            tabControlMain.ItemSize = new Size(0, 1);
            tabControlMain.SizeMode = TabSizeMode.Fixed;
            foreach (TabPage page in tabControlMain.TabPages)
            {
                var title = TabTitles.FirstOrDefault(t => t.Page == page.Name).Text;
                if (title != null) page.Text = title;
                page.BackColor = UiTheme.Surface;
                page.Padding = Padding.Empty;
                if (GridOf(page) is DataGridView grid)
                {
                    grid.RowsAdded += (_, _) => _tabStrip?.Invalidate();
                    grid.RowsRemoved += (_, _) => _tabStrip?.Invalidate();
                    grid.DataBindingComplete += (_, _) => _tabStrip?.Invalidate();
                }
            }
            tabControlMain.SelectedIndexChanged += (_, _) => _tabStrip?.Invalidate();
        }

        private static DataGridView? GridOf(TabPage page) => page.Controls.OfType<DataGridView>().FirstOrDefault();

        private Panel BuildTabStrip()
        {
            var strip = new DoubleBufferedPanel { Name = "tabStrip", Dock = DockStyle.Top, Height = 36, BackColor = UiTheme.Surface, Cursor = Cursors.Hand };
            strip.Paint += (_, e) => PaintTabs(e.Graphics, strip);
            strip.MouseClick += (_, e) =>
            {
                if (_gearRect.Contains(e.Location)) { OpenColumnChooser(); return; }
                int i = TabHitTest(strip, e.Location);
                if (i >= 0) tabControlMain.SelectedIndex = i;
            };
            return strip;
        }

        private readonly List<Rectangle> _tabRects = new();

        private void PaintTabs(Graphics g, Control strip)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            _tabRects.Clear();
            int x = 10;
            for (int i = 0; i < tabControlMain.TabPages.Count; i++)
            {
                var page = tabControlMain.TabPages[i];
                bool selected = i == tabControlMain.SelectedIndex;
                int? count = GridOf(page)?.Rows.Count;
                string countText = count is int c && c > 0 ? c.ToString("#,##0") : "";
                var font = selected ? UiTheme.BoldFont : UiTheme.BaseFont;
                var titleSize = TextRenderer.MeasureText(g, page.Text, font, Size.Empty, TextFormatFlags.NoPadding);
                var countSize = countText.Length > 0 ? TextRenderer.MeasureText(g, countText, UiTheme.SmallFont, Size.Empty, TextFormatFlags.NoPadding) : Size.Empty;
                int width = 24 + titleSize.Width + (countText.Length > 0 ? countSize.Width + 6 : 0);
                var rect = new Rectangle(x, 0, width, strip.Height);
                _tabRects.Add(rect);
                TextRenderer.DrawText(g, page.Text, font, new Point(x + 12, (strip.Height - titleSize.Height) / 2),
                    selected ? UiTheme.Primary : UiTheme.Text, TextFormatFlags.NoPadding);
                if (countText.Length > 0)
                    TextRenderer.DrawText(g, countText, UiTheme.SmallFont, new Point(x + 12 + titleSize.Width + 6, (strip.Height - countSize.Height) / 2 + 1),
                        UiTheme.MutedText, TextFormatFlags.NoPadding);
                if (selected)
                    using (var accent = new SolidBrush(UiTheme.Primary))
                        g.FillRectangle(accent, x + 6, strip.Height - 3, width - 12, 3);
                x += width + 2;
            }
            // Bánh răng "Cột" ở mép phải: chọn cột hiển thị cho bảng của tab đang mở
            _gearIcon ??= RibbonIcons.Get("gear", 16, UiTheme.MutedText);
            var gearText = TextRenderer.MeasureText(g, "Cột", UiTheme.BaseFont, Size.Empty, TextFormatFlags.NoPadding);
            _gearRect = new Rectangle(strip.Width - gearText.Width - 40, 4, gearText.Width + 30, strip.Height - 8);
            if (_gearIcon != null) g.DrawImage(_gearIcon, _gearRect.X + 6, (strip.Height - 16) / 2);
            TextRenderer.DrawText(g, "Cột", UiTheme.BaseFont, new Point(_gearRect.X + 26, (strip.Height - gearText.Height) / 2), UiTheme.MutedText, TextFormatFlags.NoPadding);
            g.DrawLine(BorderPen, 0, strip.Height - 1, strip.Width, strip.Height - 1);
        }

        private Rectangle _gearRect;
        private Bitmap? _gearIcon;

        private int TabHitTest(Control strip, Point p)
        {
            for (int i = 0; i < _tabRects.Count; i++)
                if (_tabRects[i].Contains(p)) return i;
            return -1;
        }

        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel() { DoubleBuffered = true; ResizeRedraw = true; }
        }

        #endregion

        /// <summary>
        /// Cột Phân loại và Rủi ro NCC đưa lên ngay sau Tổng tiền và vẽ thành nhãn màu.
        /// Chữ vẫn lấy từ DgvMain_CellFormatting nên xuất Excel/sao chép không đổi.
        /// </summary>
        private void ConfigureStatusChips()
        {
            int after = colDgvTongTien.DisplayIndex;
            colDgvPhanLoai.DisplayIndex = after + 1;
            colDgvRuiRo.DisplayIndex = after + 2;
            colDgvPhanLoai.Width = 150;
            colDgvRuiRo.Width = 140;
            dgvMain.CellPainting += (_, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                bool phanLoai = e.ColumnIndex == colDgvPhanLoai.Index, ruiRo = e.ColumnIndex == colDgvRuiRo.Index;
                if (!phanLoai && !ruiRo) return;
                if (dgvMain.Rows[e.RowIndex].DataBoundItem is not SearchResult item) return;
                string text = e.FormattedValue?.ToString() ?? "";
                if (text.Length == 0) return;
                ChipKind kind;
                if (phanLoai) kind = UiTheme.ChipKindOf(InvoiceClassifier.RelationOf(item.Trang_thai_hoa_don));
                else if (RiskFor(item, _riskResults) is RiskResult risk) kind = UiTheme.ChipKindOf(risk.Status);
                else return;
                e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
                UiTheme.DrawChip(e.Graphics, e.CellBounds, text, kind);
                e.Handled = true;
            };
        }

        /// <summary>Thanh tiến độ mảnh ở đáy vùng làm việc; code cũ vẫn bật/tắt Visible như trước.</summary>
        private void ConfigureProgress()
        {
            downloadProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            downloadProgressBar.Dock = DockStyle.Bottom;
            downloadProgressBar.Height = 6;
            lblDownloadStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            lblDownloadStatus.Dock = DockStyle.Bottom;
            lblDownloadStatus.AutoSize = false;
            lblDownloadStatus.Height = 22;
            lblDownloadStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblDownloadStatus.Padding = new Padding(10, 0, 0, 0);
            lblDownloadStatus.BackColor = UiTheme.FilterBack;
            lblDownloadStatus.ForeColor = UiTheme.Text;
            lblDownloadStatus.Font = UiTheme.BaseFont;
        }

        #region Phần tử nhỏ

        private static void Input(Control c, int width)
        {
            c.Width = width;
            c.Font = UiTheme.BaseFont;
            c.ForeColor = UiTheme.Text;
            c.Margin = new Padding(0, 3, 4, 0);
            c.Anchor = AnchorStyles.Left;
            c.Dock = DockStyle.None;
            if (c is TextBox t) t.BorderStyle = BorderStyle.FixedSingle;
            if (c is NumericUpDown n) n.BorderStyle = BorderStyle.Fixed3D; // kiểu khác bỏ viền (ô có khung bo)
            if (c is ComboBox cb) cb.FlatStyle = FlatStyle.Flat;
        }

        private static Label FieldLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.MutedText,
            Margin = new Padding(6, 7, 4, 0),
            Anchor = AnchorStyles.Left
        };

        private static Label Dash() => new()
        {
            Text = "–",
            AutoSize = true,
            Font = UiTheme.BaseFont,
            ForeColor = UiTheme.MutedText,
            Margin = new Padding(0, 6, 4, 0),
            Anchor = AnchorStyles.Left
        };

        private static Control Separator() => new Panel { Width = 1, Height = 22, BackColor = UiTheme.Border, Margin = new Padding(8, 3, 4, 0) };

        private static FlowLayoutPanel Row(params Control[] items)
        {
            var row = new FlowLayoutPanel
            {
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = UiTheme.RibbonBack
            };
            row.Controls.AddRange(items);
            return row;
        }

        #endregion
    }
}
