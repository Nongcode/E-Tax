using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace E_Tax
{
    /// <summary>
    /// 4 kiểu giao diện dùng chung control Designer (docs/features/giao-dien/thiet-ke-lai/plan.md, CR-002):
    /// Chuẩn (PA1), Menu trái (PA2, bộ lọc popup), Ribbon (PA3), Gọn (PA5 nền sáng).
    /// Đổi kiểu = gỡ control nghiệp vụ khỏi khung cũ, hủy khung cũ, dựng khung mới. Handler nghiệp vụ không bị đụng.
    /// </summary>
    public partial class Form1
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        private LayoutKind _layout = LayoutKind.Ribbon;
        private readonly List<Control> _layoutParts = new();
        private Action? _fitLayout;
        private Panel? _filterPopup;
        private Button? _filterButton;
        private Label? _sideSummary;
        private readonly Dictionary<string, (Label Value, Label? Sub)> _kpi = new();
        private readonly List<(Button Opener, Panel Menu, Button[] Items)> _menus = new();
        private Panel? _cardInner;
        private Control[] _cardTops = Array.Empty<Control>();
        private Control? _cardBottom;
        private Label? _selectionInfo;
        private ToolStripDropDownButton? _ddLayout;
        private ToolStripStatusLabel? _lblStatusKpi;
        private readonly Dictionary<Button, string> _commandText = new();
        private readonly Dictionary<string, List<ColumnState>> _columnDefaults = new();
        private InvoiceKpis _lastKpis = InvoiceKpis.Compute(Array.Empty<SearchResult>());

        private static readonly (LayoutKind Kind, string Text)[] LayoutNames =
        {
            (LayoutKind.Classic, "Chuẩn"), (LayoutKind.SideMenu, "Menu trái"), (LayoutKind.Ribbon, "Ribbon"), (LayoutKind.Compact, "Gọn")
        };

        /// <summary>Control nghiệp vụ do bố cục sắp chỗ (bảng nằm trong tabControlMain nên không cần liệt kê).</summary>
        private Control[] PlacedControls => new Control[]
        {
            cbPeriodKind, cbPeriodNumber, nudPeriodYear, dtpFromDate, dtpToDate, rbSold, rbBought, rbAllInvoices, btnLeftSearch,
            btnXemHD, btnCnKoPdf, btnGopPdf, btnTaiHDGoc, btnExportDS, btnExportChiTiet, btnExportBangKeMua, btnExportBangKeBan,
            btnKeKhai, btnKhoHoaDon, btnKiemTraNCC, txtKtraMaDoanhNghiep, btnKtraDNMuaLe,
            txtLocSoHD, txtLocNCC, cboLocLoai, chkChiCoGhiChu, btnRightSearch, txtGhiChu, btnMoGhiChu,
            tabControlMain, _tabStrip!, lblDownloadStatus, downloadProgressBar, lblStatusMessage
        };

        private void SetCommandText(Button button, string text)
        {
            button.Text = text;
            _commandText[button] = text;
        }

        /// <summary>Chữ của nút theo kiểu giao diện hiện tại (dùng khi trả nút về sau "Đang xử lý...").</summary>
        private string LabelOf(Button button) => _commandText.TryGetValue(button, out var t) ? t : button.Text;

        /// <summary>Một lần trong constructor: bỏ khung Designer cũ, dựng phần dùng chung, áp kiểu Ribbon mặc định.</summary>
        private void InitLayouts()
        {
            SuspendLayout();
            _tabStrip = BuildTabStrip();
            ConfigureTabs();
            ConfigureProgress();
            ConfigureStatusChips();

            var old = mainSplitContainer;
            DetachPlacedControls();
            panelSearch.Controls.Clear();
            old.Dispose(); // sau khi đã gỡ hết control nghiệp vụ, nếu không sẽ hủy luôn bảng và nút

            panelSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelSearch.Dock = DockStyle.Fill;
            panelSearch.BackColor = UiTheme.AppBack;
            panelSearch.BringToFront();
            panelSearch.Resize += (_, _) => _fitLayout?.Invoke();
            lblStatusMessage.VisibleChanged += (_, _) => _fitLayout?.Invoke();

            // Gắn một lần, dùng cho mọi kiểu
            btnRightSearch.Click += (_, _) => CloseFilterPopup();
            foreach (var t in new Control[] { txtLocSoHD, txtLocNCC }) t.TextChanged += (_, _) => UpdateFilterBadge();
            cboLocLoai.SelectedIndexChanged += (_, _) => UpdateFilterBadge();
            chkChiCoGhiChu.CheckedChanged += (_, _) => UpdateFilterBadge();

            // Ô nhập có viền: viền đổi màu khi đang gõ (host = Parent.Parent, xem Boxed)
            foreach (var input in new Control[] { cbPeriodKind, cbPeriodNumber, nudPeriodYear, dtpFromDate, dtpToDate, txtLocSoHD, txtLocNCC, cboLocLoai, txtGhiChu, txtKtraMaDoanhNghiep })
            {
                input.Enter += (_, _) => (input.Parent?.Parent as InputHost)?.Invalidate();
                input.Leave += (_, _) => (input.Parent?.Parent as InputHost)?.Invalidate();
            }
            foreach (var rb in new[] { rbSold, rbBought, rbAllInvoices }) rb.CheckedChanged += (_, _) => StyleSegment(rb);
            // Lệnh nằm trong menu thả: bấm xong thì đóng menu; nút đang "Đang xử lý..." thì nút mở menu hiện theo
            foreach (var cmd in new[] { btnXemHD, btnCnKoPdf, btnGopPdf, btnTaiHDGoc, btnExportDS, btnExportChiTiet, btnExportBangKeMua, btnExportBangKeBan, btnKeKhai, btnKhoHoaDon, btnKiemTraNCC })
            {
                cmd.Click += (_, _) => CloseMenus();
                cmd.TextChanged += (_, _) => SyncMenuOpeners();
                cmd.EnabledChanged += (_, _) => SyncMenuOpeners();
            }
            foreach (var g in AllGrids()) g.MouseDown += (_, _) => CloseAllPopups();
            dgvMain.SelectionChanged += (_, _) => UpdateSelectionInfo();
            lblStatusUser.TextChanged += (_, _) => panelSearch.Controls.Find("appBar", false).FirstOrDefault()?.Invalidate();

            BuildStatusExtras();
            ApplyLayout(LayoutKind.Ribbon);
            ResumeLayout(true);
        }

        private void DetachPlacedControls()
        {
            foreach (var c in PlacedControls) c.Parent?.Controls.Remove(c);
        }

        internal void ApplyLayout(LayoutKind kind)
        {
            SuspendLayout();
            panelSearch.SuspendLayout();
            DetachPlacedControls();
            foreach (var part in _layoutParts)
            {
                panelSearch.Controls.Remove(part);
                part.Dispose();
            }
            _layoutParts.Clear();
            panelSearch.Controls.Clear();
            _fitLayout = null;
            _ribbon = null; _ribbonFlow = null; _filterBar = null; _filterFlow = null;
            _filterPopup = null; _filterButton = null; _sideSummary = null; _selectionInfo = null;
            _cardInner = null; _cardTops = Array.Empty<Control>(); _cardBottom = null;
            _kpi.Clear();
            _menus.Clear();
            UiTheme.UseAccent(kind switch
            {
                LayoutKind.Classic => UiTheme.AccentNavy,
                LayoutKind.SideMenu => UiTheme.AccentTeal,
                LayoutKind.Compact => UiTheme.AccentBlue,
                _ => UiTheme.AccentOffice
            });

            Control? left = null;
            Control[] tops = kind switch
            {
                LayoutKind.Classic => BuildClassicLayout(),
                LayoutKind.SideMenu => BuildSideMenuLayout(out left),
                LayoutKind.Compact => BuildCompactLayout(),
                _ => BuildRibbonLayout()
            };
            if (kind != LayoutKind.SideMenu) tops = tops.Prepend(BuildAppBar()).ToArray();

            // Thứ tự thêm quyết định thứ tự dock: thêm sau → dock trước
            if (_cardInner != null)
            {
                // Bảng nằm trong khung trắng viền bo trên nền xám (mẫu PA1, PA2): dải tab → thanh công cụ → bảng → thanh thao tác
                _cardInner.Controls.Add(tabControlMain);
                if (_cardBottom != null) _cardInner.Controls.Add(_cardBottom);
                for (int i = _cardTops.Length - 1; i >= 0; i--) _cardInner.Controls.Add(_cardTops[i]);
                _cardInner.Controls.Add(_tabStrip!);
                panelSearch.Controls.Add(_cardInner.Parent!);
                _layoutParts.Add(_cardInner.Parent!);
            }
            else
            {
                panelSearch.Controls.Add(tabControlMain);      // Fill
                panelSearch.Controls.Add(_tabStrip!);          // Top, sát trên bảng
            }
            for (int i = tops.Length - 1; i >= 0; i--) panelSearch.Controls.Add(tops[i]);
            if (left != null) panelSearch.Controls.Add(left);  // Left, cao hết vùng làm việc
            panelSearch.Controls.Add(lblDownloadStatus);       // Bottom
            panelSearch.Controls.Add(downloadProgressBar);     // Bottom, sát đáy
            _layoutParts.AddRange(tops);
            if (left != null) _layoutParts.Add(left);
            if (_filterPopup != null)
            {
                panelSearch.Controls.Add(_filterPopup);
                _filterPopup.BringToFront();
                _layoutParts.Add(_filterPopup);
            }
            foreach (var (_, menu, _) in _menus)
            {
                panelSearch.Controls.Add(menu);
                menu.BringToFront();
                _layoutParts.Add(menu);
            }

            int rowHeight = kind == LayoutKind.Compact ? 26 : 32;
            foreach (TabPage page in tabControlMain.TabPages)
                if (GridOf(page) is DataGridView g)
                {
                    g.RowTemplate.Height = rowHeight;
                    foreach (DataGridViewRow row in g.Rows) row.Height = rowHeight;
                    g.DefaultCellStyle.SelectionBackColor = UiTheme.Selection;
                    g.ColumnHeadersDefaultCellStyle.BackColor = g.ColumnHeadersDefaultCellStyle.SelectionBackColor = UiTheme.HoverBack;
                }
            RecolorLogin();
            foreach (var rb in new[] { rbSold, rbBought, rbAllInvoices }) StyleSegment(rb);
            _tabStrip?.Invalidate();

            _layout = kind;
            if (_lblStatusKpi != null) _lblStatusKpi.Visible = kind == LayoutKind.Compact;
            if (_ddLayout != null)
            {
                _ddLayout.Text = "Giao diện: " + LayoutNames.First(n => n.Kind == kind).Text;
                foreach (ToolStripMenuItem item in _ddLayout.DropDownItems) item.Checked = (LayoutKind)item.Tag! == kind;
            }
            UpdateKpis(null);
            UpdateFilterBadge();
            UpdateSelectionInfo();
            SyncMenuOpeners();

            panelSearch.ResumeLayout(true);
            ResumeLayout(true);
            _fitLayout?.Invoke();
        }

        #region Kiểu 1 — Chuẩn

        /// <summary>PA1: thanh tìm kiếm trắng (nhãn trên ô), 4 thẻ số liệu, khung bảng: dải tab → thanh nút có viền → bảng.</summary>
        private Control[] BuildClassicLayout()
        {
            var header = HeaderBar("classicHeader", out var flow, out var right);
            flow.Controls.AddRange(SearchFields());
            Primary(btnLeftSearch, "search", "Tìm kiếm");
            btnLeftSearch.Margin = new Padding(6, FieldTop, 0, 0);
            flow.Controls.Add(btnLeftSearch);
            Outline(btnKhoHoaDon, "box", "Kho hóa đơn");
            Outline(btnKeKhai, "calc", "Lập tờ khai");
            btnKhoHoaDon.Margin = btnKeKhai.Margin = new Padding(8, FieldTop, 0, 0);
            right.Controls.Add(btnKhoHoaDon);
            right.Controls.Add(btnKeKhai);

            var kpis = BuildKpiCards();

            var toolbar = ToolbarBar("classicToolbar", out var tools);
            tools.Padding = new Padding(10, 8, 8, 6);
            tools.Controls.AddRange(new Control[]
            {
                OutlineTool(btnXemHD, "eye", "Xem"), OutlineTool(btnCnKoPdf, "print", "In PDF"), OutlineTool(btnGopPdf, "layers", "Gộp PDF"), Separator(),
                OutlineTool(btnExportDS, "sheet", "Xuất danh sách"), OutlineTool(btnExportChiTiet, "sheet", "Xuất chi tiết"),
                OutlineTool(MenuOpener("BangKe", "file", "Bảng kê", (btnExportBangKeMua, "file", "Bảng kê mua vào"), (btnExportBangKeBan, "file", "Bảng kê bán ra")), "file", null),
                OutlineTool(btnTaiHDGoc, "download", "Tải HĐ gốc"), Separator(), OutlineTool(btnKiemTraNCC, "shield", "Kiểm tra NCC")
            });
            var opener = FilterOpener("Lọc kết quả");
            opener.Margin = new Padding(0, 8, 10, 0);
            var openerHost = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, BackColor = UiTheme.Surface };
            openerHost.Controls.Add(opener);
            toolbar.Controls.Add(openerHost);
            _filterPopup = BuildFilterPopup(withSearch: false, withNotes: true);
            Card(toolbar);
            _fitLayout = () => { FitWrap(header, flow, 76); FitWrap(toolbar, tools, 48); };
            return new Control[] { header, kpis };
        }

        #endregion

        #region Kiểu 2 — Menu trái

        /// <summary>
        /// PA2: menu trái theo phân hệ, tiêu đề trang + Bộ lọc (popup) + Tìm kiếm + menu thả "Xuất Excel ▾"/"PDF ▾",
        /// khung bảng có thanh thao tác ở đáy (số HĐ đang chọn, Xem, Tải HĐ gốc, Kiểm tra NCC).
        /// </summary>
        private Control[] BuildSideMenuLayout(out Control left)
        {
            var nav = new FlowLayoutPanel
            {
                Name = "sideNav", Dock = DockStyle.Left, Width = 208, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoScroll = true, Padding = new Padding(10, 14, 8, 10), BackColor = UiTheme.Primary
            };
            var brand = new FlowLayoutPanel { WrapContents = false, AutoSize = true, Margin = new Padding(8, 0, 0, 14), BackColor = UiTheme.Primary };
            brand.Controls.Add(new PictureBox { Image = RibbonIcons.Get("file", 22, UiTheme.OnPrimary), Size = new Size(22, 22), Margin = new Padding(0, 4, 6, 0) });
            brand.Controls.Add(new Label { Text = "E-Tax", AutoSize = true, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = UiTheme.OnPrimary, Margin = Padding.Empty });
            nav.Controls.Add(brand);
            var search = new Button { Name = "navTraCuu" };
            Nav(search, "search", "Tra cứu hóa đơn", onDark: true);
            _commandText.Remove(search);
            search.BackColor = UiTheme.PrimaryHover;
            search.Font = UiTheme.BoldFont;
            search.Click += (_, _) => ToggleFilterPopup();
            nav.Controls.AddRange(new Control[] { search, Nav(btnKhoHoaDon, "box", "Kho hóa đơn", onDark: true), Nav(btnKeKhai, "calc", "Lập tờ khai", onDark: true) });
            left = nav;

            var header = HeaderBar("sideHeader", out var flow, out var right, UiTheme.AppBack);
            var title = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(4, 4, 16, 0), BackColor = UiTheme.AppBack };
            title.Controls.Add(new Label { Text = "Tra cứu hóa đơn", AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UiTheme.Text, Margin = Padding.Empty });
            _sideSummary = new Label { Name = "sideSummary", AutoSize = true, Font = UiTheme.SmallFont, ForeColor = UiTheme.MutedText, Margin = Padding.Empty };
            title.Controls.Add(_sideSummary);
            flow.Controls.Add(title);
            var opener = FilterOpener("Bộ lọc");
            Primary(btnLeftSearch, "search", "Tìm kiếm");
            var excel = MenuOpener("Excel", "sheet", "Xuất Excel",
                (btnExportDS, "sheet", "Danh sách hóa đơn"), (btnExportChiTiet, "list", "Chi tiết hàng hóa"),
                (btnExportBangKeMua, "file", "Bảng kê mua vào"), (btnExportBangKeBan, "file", "Bảng kê bán ra"));
            var pdf = MenuOpener("Pdf", "print", "PDF", (btnCnKoPdf, "print", "In PDF"), (btnGopPdf, "layers", "Gộp PDF"));
            foreach (var b in new[] { opener, btnLeftSearch, excel, pdf }) { b.Margin = new Padding(8, 10, 0, 0); right.Controls.Add(b); }

            var kpis = BuildKpiCards();
            _filterPopup = BuildFilterPopup(withSearch: true, withNotes: true);

            // Thanh thao tác ở đáy khung bảng
            var actions = new Panel { Name = "sideActions", Dock = DockStyle.Bottom, Height = 48, BackColor = UiTheme.Surface };
            actions.Paint += (_, e) => e.Graphics.DrawLine(BorderPen, 0, 0, actions.Width, 0);
            var actionFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(12, 8, 8, 0), BackColor = UiTheme.Surface };
            _selectionInfo = new Label { Name = "selectionInfo", AutoSize = true, Font = UiTheme.BaseFont, ForeColor = UiTheme.MutedText, Margin = new Padding(0, 8, 12, 0) };
            actionFlow.Controls.AddRange(new Control[] { _selectionInfo, OutlineTool(btnXemHD, "eye", "Xem"), OutlineTool(btnTaiHDGoc, "download", "Tải HĐ gốc"), OutlineTool(btnKiemTraNCC, "shield", "Kiểm tra NCC") });
            actions.Controls.Add(actionFlow);
            actions.Controls.Add(lblStatusMessage);
            StatusMessageInline();
            lblStatusMessage.Margin = Padding.Empty;
            Card(actions, Array.Empty<Control>());

            _fitLayout = () => FitWrap(header, flow, 60);
            return new Control[] { header, kpis };
        }

        private Button Nav(Button b, string icon, string text, bool onDark = false)
        {
            ResetButton(b);
            SetCommandText(b, text);
            b.Image = WithGap(RibbonIcons.Get(icon, 16, onDark ? UiTheme.OnPrimary : UiTheme.Primary), 10);
            b.TextImageRelation = TextImageRelation.ImageBeforeText;
            b.ImageAlign = ContentAlignment.MiddleLeft;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Size = new Size(186, 36);
            b.Padding = new Padding(10, 0, 0, 0);
            b.Margin = new Padding(0, 1, 0, 1);
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = onDark ? UiTheme.Primary : UiTheme.Surface;
            if (onDark)
            {
                b.ForeColor = UiTheme.OnPrimary;
                b.FlatAppearance.MouseOverBackColor = b.FlatAppearance.MouseDownBackColor = UiTheme.PrimaryHover;
            }
            return b;
        }

        /// <summary>
        /// Thanh tiêu đề màu nhấn trên cùng (mẫu PA1): logo + "E-Tax · Tra cứu hóa đơn điện tử", bên phải tài khoản đang đăng nhập.
        /// Vẽ tay để không thêm control; tên tài khoản đọc từ thanh trạng thái.
        /// </summary>
        private Control BuildAppBar()
        {
            var bar = new DoubleBufferedPanel { Name = "appBar", Dock = DockStyle.Top, Height = 40, BackColor = UiTheme.Primary };
            var icon = RibbonIcons.Get("file", 20, UiTheme.OnPrimary);
            var brandFont = new Font("Segoe UI", 11F, FontStyle.Bold);
            bar.Disposed += (_, _) => { icon?.Dispose(); brandFont.Dispose(); };
            bar.Paint += (_, e) =>
            {
                var g = e.Graphics;
                if (icon != null) g.DrawImage(icon, 16, (bar.Height - 20) / 2);
                var brand = TextRenderer.MeasureText(g, "E-Tax", brandFont, Size.Empty, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, "E-Tax", brandFont, new Point(44, (bar.Height - brand.Height) / 2), UiTheme.OnPrimary, TextFormatFlags.NoPadding);
                var sub = TextRenderer.MeasureText(g, "Tra cứu hóa đơn điện tử", UiTheme.BaseFont, Size.Empty, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, "·  Tra cứu hóa đơn điện tử", UiTheme.BaseFont, new Point(44 + brand.Width + 10, (bar.Height - sub.Height) / 2 + 1), UiTheme.OnPrimaryMuted, TextFormatFlags.NoPadding);
                string user = (lblStatusUser.Text ?? "").Replace("👤", "").Trim();
                if (user.Length > 0)
                {
                    var size = TextRenderer.MeasureText(g, user, UiTheme.BoldFont, Size.Empty, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, user, UiTheme.BoldFont, new Point(bar.Width - size.Width - 16, (bar.Height - size.Height) / 2), UiTheme.OnPrimary, TextFormatFlags.NoPadding);
                    using var userIcon = RibbonIcons.Get("user", 16, UiTheme.OnPrimary);
                    if (userIcon != null) g.DrawImage(userIcon, bar.Width - size.Width - 38, (bar.Height - 16) / 2);
                }
            };
            return bar;
        }

        #endregion

        #region Kiểu 4 — Gọn

        /// <summary>PA5 nền sáng: một hàng tìm kiếm + số liệu nhỏ ở mép phải, một thanh nút, bảng sát mép.</summary>
        private Control[] BuildCompactLayout()
        {
            var header = HeaderBar("compactHeader", out var flow, out var right);
            flow.Controls.AddRange(SearchFields());
            Primary(btnLeftSearch, "search", "Tìm kiếm");
            btnLeftSearch.Margin = new Padding(6, FieldTop, 0, 0);
            flow.Controls.Add(btnLeftSearch);
            foreach (var (key, title) in new[] { ("Sold", "Bán ra"), ("Bought", "Mua vào"), ("Attention", "Cần chú ý") })
            {
                var box = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(18, 4, 0, 0), BackColor = UiTheme.Surface };
                box.Controls.Add(new Label { Text = title, AutoSize = true, Font = UiTheme.SmallFont, ForeColor = UiTheme.MutedText, Margin = Padding.Empty });
                var v = new Label { Name = "kpiValue_" + key, AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = UiTheme.Text, Margin = new Padding(0, 2, 0, 0) };
                box.Controls.Add(v);
                right.Controls.Add(box);
                _kpi[key] = (v, null);
            }

            var toolbar = ToolbarBar("compactToolbar", out var tools);
            toolbar.BackColor = tools.BackColor = UiTheme.FilterBack;
            tools.Controls.AddRange(new Control[]
            {
                Tool(btnXemHD, "eye", "Xem"), Tool(btnCnKoPdf, "print", "In PDF"), Tool(btnGopPdf, "layers", "Gộp PDF"), Tool(btnTaiHDGoc, "download", "Tải gốc"), Separator(),
                Tool(btnExportDS, "sheet", "Danh sách"), Tool(btnExportChiTiet, "list", "Chi tiết"), Tool(btnExportBangKeMua, "file", "Bảng kê mua"), Tool(btnExportBangKeBan, "file", "Bảng kê bán"), Separator(),
                Tool(btnKiemTraNCC, "shield", "Kiểm tra NCC"), Tool(btnKeKhai, "calc", "Tờ khai"), Tool(btnKhoHoaDon, "box", "Kho HĐ")
            });
            var opener = FilterOpener("Lọc kết quả");
            opener.Margin = new Padding(0, 4, 10, 0);
            var openerHost = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, BackColor = UiTheme.FilterBack };
            openerHost.Controls.Add(opener);
            toolbar.Controls.Add(openerHost);
            _filterPopup = BuildFilterPopup(withSearch: false, withNotes: true);
            _fitLayout = () => { FitWrap(header, flow, 72); FitWrap(toolbar, tools, 42); };
            return new Control[] { header, toolbar };
        }

        #endregion

        #region Phần dùng chung

        /// <summary>Khoảng từ đỉnh ô Field tới ô nhập (nhãn + khoảng cách): nút đặt cạnh ô nhập dùng làm lề trên.</summary>
        private const int FieldTop = 19;

        /// <summary>Khung bảng: nền xám quanh, khung trắng viền bo. ApplyLayout đặt dải tab, <paramref name="tops"/>, bảng vào trong.</summary>
        private void Card(params Control[] tops) => Card(null, tops);

        private void Card(Control? bottom, Control[] tops)
        {
            var outer = new Panel { Name = "contentCard", Dock = DockStyle.Fill, BackColor = UiTheme.AppBack, Padding = new Padding(16, 10, 16, 12) };
            var inner = new Panel { Name = "contentCardInner", Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(1) };
            inner.Paint += (_, e) => UiTheme.DrawRoundBorder(e.Graphics, new Rectangle(0, 0, inner.Width - 1, inner.Height - 1), 6, UiTheme.Border);
            inner.Resize += (_, _) => inner.Invalidate();
            outer.Controls.Add(inner);
            _cardInner = inner;
            _cardTops = tops;
            _cardBottom = bottom;
        }

        /// <summary>Thanh trên: nhóm trái tự xuống dòng khi hẹp, nhóm phải (AutoSize) bám mép phải.</summary>
        private static Panel HeaderBar(string name, out FlowLayoutPanel flow, out FlowLayoutPanel right, Color? back = null)
        {
            var color = back ?? UiTheme.Surface;
            var bar = new Panel { Name = name, Dock = DockStyle.Top, Height = 64, BackColor = color };
            flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(16, 8, 8, 10), BackColor = color };
            right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(0, 8, 16, 0), BackColor = color };
            bar.Controls.Add(flow);
            bar.Controls.Add(right);
            if (back == null) bar.Paint += (_, e) => e.Graphics.DrawLine(BorderPen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            return bar;
        }

        private Panel ToolbarBar(string name, out FlowLayoutPanel flow)
        {
            var bar = new Panel { Name = name, Dock = DockStyle.Top, Height = 40, BackColor = UiTheme.Surface };
            flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(10, 4, 8, 4), BackColor = UiTheme.Surface };
            bar.Controls.Add(flow);
            if (lblStatusMessage.Parent == null)
            {
                bar.Controls.Add(lblStatusMessage);
                StatusMessageInline();
            }
            bar.Paint += (_, e) => e.Graphics.DrawLine(BorderPen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            return bar;
        }

        private void StatusMessageInline()
        {
            lblStatusMessage.Dock = lblStatusMessage.Parent is FlowLayoutPanel ? DockStyle.None : DockStyle.Right;
            lblStatusMessage.AutoSize = false;
            lblStatusMessage.Size = new Size(300, 30);
            lblStatusMessage.Margin = new Padding(0, 14, 0, 0);
            lblStatusMessage.TextAlign = ContentAlignment.MiddleRight;
            lblStatusMessage.BackColor = UiTheme.Surface;
        }

        /// <summary>Thanh tự xuống dòng: cao theo nội dung ở chiều rộng hiện tại.</summary>
        private void FitWrap(Panel bar, FlowLayoutPanel flow, int minHeight)
        {
            int width = Math.Max(1, bar.ClientSize.Width - bar.Controls.Cast<Control>().Where(c => c != flow && c.Visible && c.Dock == DockStyle.Right).Sum(c => c.Width));
            int h = Math.Max(minHeight, flow.GetPreferredSize(new Size(width, 0)).Height);
            if (bar.Height != h) bar.Height = h;
        }

        /// <summary>Kỳ thuế, từ ngày, đến ngày, loại HĐ: nhãn đậm phía trên ô có viền bo (dùng cho Chuẩn, Gọn và popup của Menu trái).</summary>
        private Control[] SearchFields()
        {
            foreach (var dtp in new[] { dtpFromDate, dtpToDate }) { dtp.Format = DateTimePickerFormat.Custom; dtp.CustomFormat = "dd/MM/yyyy"; }
            var period = new FlowLayoutPanel { WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.Transparent };
            period.Controls.AddRange(new[] { Boxed(cbPeriodKind, 104), Boxed(cbPeriodNumber, 64), Boxed(nudPeriodYear, 78) });
            return new Control[]
            {
                Field("Kỳ thuế", period), Field("Từ ngày", Boxed(dtpFromDate, 128)), Field("Đến ngày", Boxed(dtpToDate, 128)),
                Field("Loại hóa đơn", Segmented())
            };
        }

        /// <summary>Bán ra | Mua vào | Tất cả dạng nút ghép (RadioButton Appearance.Button, vẫn là control và handler cũ).</summary>
        private Control Segmented()
        {
            var seg = new FlowLayoutPanel { Name = "segKinds", WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.Transparent };
            foreach (var (rb, text) in new[] { (rbSold, "Bán ra"), (rbBought, "Mua vào"), (rbAllInvoices, "Tất cả") })
            {
                rb.Appearance = Appearance.Button;
                rb.FlatStyle = FlatStyle.Flat;
                rb.UseVisualStyleBackColor = false;
                rb.AutoSize = false;
                rb.Dock = DockStyle.None;
                rb.Text = text;
                rb.Font = UiTheme.BaseFont;
                rb.TextAlign = ContentAlignment.MiddleCenter;
                rb.Size = new Size(TextRenderer.MeasureText(text, UiTheme.BoldFont).Width + 26, 32);
                rb.Margin = Padding.Empty;
                rb.Cursor = Cursors.Hand;
                rb.FlatAppearance.BorderSize = 1;
                rb.FlatAppearance.BorderColor = UiTheme.Border;
                seg.Controls.Add(rb);
                StyleSegment(rb);
            }
            return seg;
        }

        private static void StyleSegment(RadioButton rb)
        {
            if (rb.Appearance != Appearance.Button) return;
            rb.BackColor = rb.Checked ? UiTheme.Primary : UiTheme.Surface;
            rb.ForeColor = rb.Checked ? UiTheme.OnPrimary : UiTheme.Text;
            rb.FlatAppearance.CheckedBackColor = UiTheme.Primary;
            rb.FlatAppearance.MouseOverBackColor = rb.Checked ? UiTheme.PrimaryHover : UiTheme.HoverBack;
            rb.FlatAppearance.BorderColor = rb.Checked ? UiTheme.Primary : UiTheme.Border;
        }

        /// <summary>
        /// Ô nhập có viền bo cao 32px như mẫu HTML: control gốc nằm trong khung cắt để giấu viền 3D của Windows
        /// (ComboBox phẳng 1px, DateTimePicker 2px); TextBox/NumericUpDown bỏ viền. Viền host đổi màu nhấn khi đang gõ.
        /// </summary>
        private static Control Boxed(Control c, int width)
        {
            const int H = 32;
            var host = new InputHost { Name = "box_" + c.Name, Size = new Size(width, H), Margin = new Padding(0, 0, 6, 0), BackColor = UiTheme.Surface };
            c.Dock = DockStyle.None;
            c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            c.Margin = Padding.Empty;
            c.Font = UiTheme.BaseFont;
            c.ForeColor = UiTheme.Text;
            int border = 0, pad = 8;
            switch (c)
            {
                case TextBox t: t.BorderStyle = BorderStyle.None; break;
                case NumericUpDown n: n.BorderStyle = BorderStyle.None; pad = 6; break;
                case ComboBox cb: cb.FlatStyle = FlatStyle.Flat; cb.BackColor = UiTheme.Surface; border = 1; pad = 3; break;
                case DateTimePicker: border = 2; pad = 4; break;
            }
            int clipWidth = width - 2 - pad - 1;
            int h = c is TextBox tb ? tb.PreferredHeight : c.Height;
            var clip = new Panel { Size = new Size(clipWidth, Math.Max(1, h - 2 * border)), BackColor = UiTheme.Surface };
            clip.Location = new Point(1 + pad, (H - clip.Height) / 2);
            c.Location = new Point(-border, -border);
            c.Width = clipWidth + 2 * border;
            clip.Controls.Add(c);
            host.Controls.Add(clip);
            return host;
        }

        /// <summary>Khung ô nhập: viền bo 4px, màu nhấn khi control bên trong đang được focus.</summary>
        private sealed class InputHost : Panel
        {
            public InputHost() { DoubleBuffered = true; ResizeRedraw = true; }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                UiTheme.DrawRoundBorder(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), 4, ContainsFocus ? UiTheme.Primary : UiTheme.Border);
            }
        }

        private static Control Field(string label, Control input)
        {
            var box = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 12, 0), BackColor = Color.Transparent
            };
            var l = FieldLabel(label);
            l.Margin = new Padding(0, 0, 0, 3);
            box.Controls.Add(l);
            input.Margin = new Padding(0, 0, 4, 0);
            box.Controls.Add(input);
            return box;
        }

        private void ResetButton(Button b)
        {
            b.AutoSize = false;
            b.Dock = DockStyle.None;
            b.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            b.Image = null;
            b.MinimumSize = Size.Empty;
            b.MaximumSize = Size.Empty;
            b.Padding = Padding.Empty;
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.Font = UiTheme.BaseFont;
            b.ForeColor = UiTheme.Text;
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = UiTheme.Border;
            b.FlatAppearance.MouseOverBackColor = UiTheme.HoverBack;
            b.FlatAppearance.MouseDownBackColor = UiTheme.Selection;
        }

        /// <summary>Nút thanh công cụ: icon 16 + chữ, không viền, rộng theo chữ.</summary>
        private Button Tool(Button b, string icon, string text)
        {
            ResetButton(b);
            SetCommandText(b, text);
            b.Image = WithGap(RibbonIcons.Get(icon, 16, UiTheme.Primary), 5);
            b.TextImageRelation = TextImageRelation.ImageBeforeText;
            b.ImageAlign = ContentAlignment.MiddleLeft;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.Transparent;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(0, 30);
            b.MaximumSize = new Size(0, 30);
            b.Padding = new Padding(4, 0, 6, 0);
            b.Margin = new Padding(0, 0, 2, 0);
            return b;
        }

        private void Outline(Button b, string icon, string text)
        {
            ResetButton(b);
            SetCommandText(b, text);
            b.Image = WithGap(RibbonIcons.Get(icon, 16, UiTheme.Primary), 5);
            b.TextImageRelation = TextImageRelation.ImageBeforeText;
            b.BackColor = UiTheme.Surface;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(0, 32);
            b.MaximumSize = new Size(0, 32);
            b.TextAlign = b.ImageAlign = ContentAlignment.MiddleCenter;
            b.Padding = new Padding(6, 0, 8, 0);
        }

        /// <summary>Nút thanh công cụ có viền (mẫu PA1): icon + chữ, cao 32.</summary>
        private Button OutlineTool(Button b, string icon, string? text)
        {
            if (text != null) Outline(b, icon, text);
            b.Margin = new Padding(0, 0, 6, 0);
            return b;
        }

        /// <summary>
        /// Nút "Xuất Excel ▾": mở danh sách lệnh là chính các nút Designer (nằm trong Panel ẩn của panelSearch như popup lọc),
        /// nên handler và chữ "Đang xử lý..." vẫn là của nút gốc; khi lệnh đang chạy, nút mở menu hiện chữ đó.
        /// </summary>
        private Button MenuOpener(string name, string icon, string text, params (Button Button, string Icon, string Text)[] items)
        {
            var opener = new Button { Name = "btnMenu_" + name, Tag = text };
            Outline(opener, icon, text + "  ▾");
            _commandText.Remove(opener);
            var list = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(4), BackColor = UiTheme.Surface };
            foreach (var (b, ic, t) in items)
            {
                Nav(b, ic, t);
                b.Size = new Size(212, 34);
                list.Controls.Add(b);
            }
            var menu = new Panel { Name = "menuPopup_" + name, Visible = false, BorderStyle = BorderStyle.FixedSingle, BackColor = UiTheme.Surface, Size = new Size(224, items.Length * 36 + 12) };
            menu.Controls.Add(list);
            opener.Click += (_, _) => ToggleMenu(opener, menu);
            _menus.Add((opener, menu, items.Select(i => i.Button).ToArray()));
            return opener;
        }

        private void ToggleMenu(Button opener, Panel menu)
        {
            bool open = menu.Visible;
            CloseAllPopups();
            if (open) return;
            var at = panelSearch.PointToClient(opener.Parent!.PointToScreen(new Point(opener.Right, opener.Bottom + 4)));
            menu.Location = new Point(Math.Max(8, Math.Min(at.X - menu.Width, panelSearch.ClientSize.Width - menu.Width - 8)), Math.Max(0, at.Y));
            menu.Visible = true;
            menu.BringToFront();
        }

        private void CloseMenus()
        {
            foreach (var (_, menu, _) in _menus) menu.Visible = false;
        }

        private void CloseAllPopups()
        {
            CloseMenus();
            CloseFilterPopup();
        }

        /// <summary>Lệnh trong menu đang chạy ("Đang xử lý...") thì nút mở menu hiện chữ đó và tạm khóa.</summary>
        private void SyncMenuOpeners()
        {
            foreach (var (opener, _, items) in _menus)
            {
                var busy = items.FirstOrDefault(i => !i.Enabled || i.Text != LabelOf(i));
                opener.Text = busy != null && busy.Text != LabelOf(busy) ? busy.Text : (string)opener.Tag! + "  ▾";
                opener.Enabled = items.Any(i => i.Enabled);
            }
        }

        private void UpdateSelectionInfo()
        {
            if (_selectionInfo == null) return;
            int n = dgvMain.SelectedRows.Count;
            _selectionInfo.Text = n > 0 ? $"{n} hóa đơn đã chọn" : "Chưa chọn hóa đơn";
        }

        /// <summary>Thẻ đăng nhập dựng một lần trong constructor; đổi kiểu thì tô lại theo màu nhấn mới.</summary>
        private void RecolorLogin()
        {
            if (Controls.Find("loginHeader", true).FirstOrDefault() is Control header) header.BackColor = UiTheme.Primary;
            lblLoginTitle.BackColor = lblLoginSubtitle.BackColor = UiTheme.Primary;
            var font = btnLogin.Font;
            UiTheme.StyleButton(btnLogin, primary: true);
            btnLogin.Font = font;
        }

        private void Primary(Button b, string icon, string text)
        {
            ResetButton(b);
            SetCommandText(b, text);
            UiTheme.StyleButton(b, primary: true);
            b.Image = WithGap(RibbonIcons.Get(icon, 16, UiTheme.OnPrimary), 5);
            b.TextImageRelation = TextImageRelation.ImageBeforeText;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(0, 32);
            b.Padding = new Padding(10, 0, 12, 0);
        }

        /// <summary>Nút mở popup lọc; chữ kèm số điều kiện đang bật.</summary>
        private Button FilterOpener(string text)
        {
            var b = new Button { Name = "btnFilterPopup", Tag = text };
            Outline(b, "filter", text);
            _commandText.Remove(b);
            b.Click += (_, _) => ToggleFilterPopup();
            _filterButton = b;
            _ribbonTips.SetToolTip(b, "Lọc trong kết quả đã tìm");
            return b;
        }

        private void UpdateFilterBadge()
        {
            if (_filterButton == null) return;
            int n = InvoiceKpis.ActiveFilterCount(txtLocSoHD.Text, txtLocNCC.Text, cboLocLoai.SelectedIndex, chkChiCoGhiChu.Checked);
            _filterButton.Text = (string)_filterButton.Tag! + (n > 0 ? $" ({n})" : "");
            _filterButton.FlatAppearance.BorderColor = n > 0 ? UiTheme.Primary : UiTheme.Border;
        }

        /// <summary>
        /// Popup lọc nằm trong panelSearch (ẩn sẵn) nên control nghiệp vụ không rời form. Gồm: kỳ/ngày/loại (Menu trái),
        /// lọc trong kết quả, và ghi chú + DN mua lẻ (Chuẩn, Gọn).
        /// </summary>
        private Panel BuildFilterPopup(bool withSearch, bool withNotes)
        {
            var body = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
                Padding = new Padding(16, 12, 16, 8), BackColor = UiTheme.Surface
            };
            void Section(string text) => body.Controls.Add(new Label { Text = text, AutoSize = true, Font = UiTheme.BoldFont, ForeColor = UiTheme.Text, Margin = new Padding(0, 8, 0, 6) });
            FlowLayoutPanel Line(params Control[] c)
            {
                var r = new FlowLayoutPanel { WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 8), BackColor = UiTheme.Surface };
                r.Controls.AddRange(c);
                return r;
            }

            if (withSearch)
            {
                Section("Điều kiện tìm trên cổng thuế");
                var f = SearchFields();
                body.Controls.Add(Line(f[0]));
                body.Controls.Add(Line(f[1], f[2]));
                body.Controls.Add(Line(f[3]));
            }
            Section("Lọc trong kết quả");
            txtLocSoHD.PlaceholderText = "Số HĐ";
            txtLocNCC.PlaceholderText = "Tên hoặc MST";
            chkChiCoGhiChu.Text = "Chỉ HĐ có ghi chú"; chkChiCoGhiChu.AutoSize = true; chkChiCoGhiChu.Font = UiTheme.BaseFont;
            chkChiCoGhiChu.Margin = new Padding(0, FieldTop + 7, 0, 0);
            body.Controls.Add(Line(Field("Số HĐ", Boxed(txtLocSoHD, 130)), Field("MST / Tên người bán, mua", Boxed(txtLocNCC, 300))));
            body.Controls.Add(Line(Field("Loại", Boxed(cboLocLoai, 160)), chkChiCoGhiChu));
            if (withNotes)
            {
                Section("Ghi chú hóa đơn đang chọn");
                txtGhiChu.Multiline = false; txtGhiChu.PlaceholderText = "Ghi chú";
                Outline(btnMoGhiChu, "note", "Lưu ghi chú");
                body.Controls.Add(Line(Boxed(txtGhiChu, 300), btnMoGhiChu));
                Section("Kiểm tra doanh nghiệp mua lẻ");
                txtKtraMaDoanhNghiep.PlaceholderText = "Nhập MST";
                Outline(btnKtraDNMuaLe, "shield", "Kiểm tra DN mua lẻ");
                body.Controls.Add(Line(Boxed(txtKtraMaDoanhNghiep, 200), btnKtraDNMuaLe));
            }

            var title = new Label { Text = "Bộ lọc", Dock = DockStyle.Top, Height = 40, Padding = new Padding(16, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), BackColor = UiTheme.Surface };
            title.Paint += (_, e) => e.Graphics.DrawLine(BorderPen, 0, title.Height - 1, title.Width, title.Height - 1);
            var clear = new Button { Name = "btnClearFilter" };
            Outline(clear, "refresh", "Xóa lọc");
            clear.Click += (_, _) =>
            {
                txtLocSoHD.Clear(); txtLocNCC.Clear();
                if (cboLocLoai.Items.Count > 0) cboLocLoai.SelectedIndex = 0;
                chkChiCoGhiChu.Checked = false;
                btnRightSearch.PerformClick();
            };
            var close = new Button { Name = "btnClosePopup" };
            Outline(close, "logout", "Đóng");
            close.Click += (_, _) => CloseFilterPopup();
            ResetButton(btnRightSearch);
            Primary(btnRightSearch, "filter", "Lọc");
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 10, 12, 0), BackColor = UiTheme.Surface };
            footer.Controls.AddRange(new Control[] { btnRightSearch, close, clear });
            foreach (Control c in footer.Controls) c.Margin = new Padding(8, 0, 0, 0);
            footer.Paint += (_, e) => e.Graphics.DrawLine(BorderPen, 0, 0, footer.Width, 0);

            var popup = new Panel
            {
                Name = "filterPopup", Visible = false, BorderStyle = BorderStyle.FixedSingle, BackColor = UiTheme.Surface,
                Size = new Size(540, withSearch ? (withNotes ? 640 : 470) : (withNotes ? 420 : 280))
            };
            popup.Tag = body;
            popup.Controls.Add(body);
            popup.Controls.Add(title);
            popup.Controls.Add(footer);
            return popup;
        }

        private void ToggleFilterPopup()
        {
            if (_filterPopup == null || _filterButton == null) return;
            if (_filterPopup.Visible) { CloseFilterPopup(); return; }
            CloseMenus();
            var at = panelSearch.PointToClient(_filterButton.Parent!.PointToScreen(new Point(_filterButton.Left, _filterButton.Bottom + 4)));
            int maxX = Math.Max(0, panelSearch.ClientSize.Width - _filterPopup.Width - 8);
            _filterPopup.Location = new Point(Math.Min(Math.Max(8, at.X), maxX), Math.Max(0, at.Y));
            // Cao vừa nội dung (tiêu đề 40 + thân + chân 52), không vượt quá vùng làm việc
            int content = _filterPopup.Tag is Control body ? body.GetPreferredSize(new Size(_filterPopup.Width - 2, 0)).Height : _filterPopup.Height;
            _filterPopup.Height = Math.Min(40 + content + 52 + 2, Math.Max(200, panelSearch.ClientSize.Height - _filterPopup.Top - 8));
            _filterPopup.Visible = true;
            _filterPopup.BringToFront();
        }

        private void CloseFilterPopup()
        {
            if (_filterPopup != null) _filterPopup.Visible = false;
        }

        #endregion

        #region Thẻ số liệu, thanh trạng thái

        private Control BuildKpiCards()
        {
            var table = new TableLayoutPanel
            {
                Name = "kpiCards", Dock = DockStyle.Top, Height = 86, ColumnCount = 4, RowCount = 1,
                Padding = new Padding(16, 12, 6, 0), BackColor = UiTheme.AppBack
            };
            for (int i = 0; i < 4; i++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            foreach (var (key, title) in new[] { ("Sold", "Bán ra"), ("Bought", "Mua vào"), ("Vat", "Thuế GTGT tạm tính"), ("Attention", "Cần chú ý") })
            {
                var card = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), BackColor = UiTheme.Surface, Padding = new Padding(12, 6, 8, 4) };
                var stripe = UiTheme.KpiColor(key);
                card.Padding = new Padding(16, 6, 8, 4);
                card.Paint += (_, e) =>
                {
                    using (var b = new SolidBrush(stripe)) e.Graphics.FillRectangle(b, 1, 6, 4, card.Height - 12);
                    UiTheme.DrawRoundBorder(e.Graphics, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 6, UiTheme.Border);
                };
                card.Resize += (_, _) => card.Invalidate();
                var t = new Label { Text = title, Dock = DockStyle.Top, Height = 18, Font = UiTheme.LabelFont, ForeColor = UiTheme.MutedText };
                var v = new Label { Name = "kpiValue_" + key, Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = UiTheme.Text };
                var s = new Label { Name = "kpiSub_" + key, Dock = DockStyle.Top, Height = 18, Font = UiTheme.SmallFont, ForeColor = UiTheme.MutedText, AutoEllipsis = true };
                card.Controls.Add(s); card.Controls.Add(v); card.Controls.Add(t);
                table.Controls.Add(card);
                _kpi[key] = (v, s);
            }
            return table;
        }

        private static string Money(decimal v) => v.ToString("#,##0", Vi) + " đ";

        /// <summary>Cập nhật thẻ số liệu / dòng trạng thái kiểu Gọn. null = dùng số lần trước (khi vừa đổi kiểu).</summary>
        private void UpdateKpis(IEnumerable<SearchResult>? shown)
        {
            if (shown != null) _lastKpis = InvoiceKpis.Compute(shown);
            var k = _lastKpis;
            if (_kpi.TryGetValue("Sold", out var sold)) { sold.Value.Text = Money(k.SoldTotal); if (sold.Sub != null) sold.Sub.Text = $"{k.SoldCount} HĐ · thuế {k.SoldTax.ToString("#,##0", Vi)}"; }
            if (_kpi.TryGetValue("Bought", out var bought)) { bought.Value.Text = Money(k.BoughtTotal); if (bought.Sub != null) bought.Sub.Text = $"{k.BoughtCount} HĐ · thuế {k.BoughtTax.ToString("#,##0", Vi)}"; }
            if (_kpi.TryGetValue("Vat", out var vat))
            {
                vat.Value.Text = Money(k.VatEstimate);
                vat.Value.ForeColor = k.VatEstimate < 0 ? UiTheme.Primary : UiTheme.Text;
                if (vat.Sub != null) vat.Sub.Text = k.VatEstimate < 0 ? "Âm: còn được khấu trừ (chưa trừ kỳ trước)" : "Bán ra − mua vào, chưa trừ kỳ trước";
            }
            if (_kpi.TryGetValue("Attention", out var att))
            {
                att.Value.Text = k.Attention.ToString(Vi);
                att.Value.ForeColor = k.Attention > 0 ? UiTheme.ChipColors(ChipKind.Warning).Fore : UiTheme.Text;
                if (att.Sub != null) att.Sub.Text = "Thay thế, điều chỉnh, hủy, không rõ";
            }
            if (_sideSummary != null) _sideSummary.Text = $"{k.SoldCount + k.BoughtCount} hóa đơn · {dtpFromDate.Value:dd/MM/yyyy} – {dtpToDate.Value:dd/MM/yyyy}";
            if (_lblStatusKpi != null) _lblStatusKpi.Text = $"Thuế tạm tính: {Money(k.VatEstimate)}   ·   Cần chú ý: {k.Attention}";
        }

        /// <summary>Thanh trạng thái: chọn kiểu giao diện + dòng số liệu cho kiểu Gọn.</summary>
        private void BuildStatusExtras()
        {
            _lblStatusKpi = new ToolStripStatusLabel { Name = "lblStatusKpi", Visible = false, ForeColor = lblStatusUser.ForeColor };
            _ddLayout = new ToolStripDropDownButton { Name = "ddLayout", Text = "Giao diện: Ribbon", ShowDropDownArrow = true, ForeColor = lblStatusUser.ForeColor };
            foreach (var (kind, text) in LayoutNames)
            {
                var item = new ToolStripMenuItem(text) { Tag = kind };
                item.Click += (_, _) => ChooseLayout(kind);
                _ddLayout.DropDownItems.Add(item);
            }
            int at = statusBar.Items.IndexOf(lblStatusUser);
            statusBar.Items.Insert(Math.Max(0, at), _ddLayout);
            statusBar.Items.Insert(Math.Max(0, statusBar.Items.IndexOf(lblStatusInvoices) + 1), _lblStatusKpi);
        }

        private void ChooseLayout(LayoutKind kind)
        {
            if (kind == _layout) return;
            if (_gate.IsBusy)
            {
                MessageBox.Show(this, "Đang có tác vụ chạy, hãy chờ xong rồi đổi giao diện.", "Giao diện", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ApplyLayout(kind);
            _prefs.Layout = kind;
            SavePrefs();
        }

        private void SavePrefs()
        {
            try { _prefs.Save(); }
            catch (Exception ex) { AppendLog("⚠️ Không lưu được tùy chọn giao diện: " + ex.GetType().Name); }
        }

        #endregion

        #region Cột hiển thị (bánh răng)

        private IEnumerable<DataGridView> AllGrids() =>
            tabControlMain.TabPages.Cast<TabPage>().Select(GridOf).Where(g => g != null)!;

        /// <summary>Cuối constructor: ghi lại cột mặc định (như Designer/DetailGridManager tạo) để nút "Mặc định" trả về.</summary>
        private void CaptureColumnDefaults()
        {
            foreach (var g in AllGrids()) _columnDefaults[g.Name] = GridColumnLayout.Capture(g);
        }

        /// <summary>Form1_Load: áp kiểu giao diện và cột đã lưu.</summary>
        private void ApplySavedUiPreferences()
        {
            foreach (var g in AllGrids())
                if (_prefs.Columns.TryGetValue(g.Name, out var saved))
                {
                    try { GridColumnLayout.Apply(g, saved); }
                    catch (InvalidOperationException ex) { AppendLog("⚠️ Không áp được cột đã lưu cho " + g.Name + ": " + ex.Message); }
                }
            if (_prefs.Layout != _layout) ApplyLayout(_prefs.Layout);
        }

        private void OpenColumnChooser()
        {
            var page = tabControlMain.SelectedTab;
            if (page == null || GridOf(page) is not DataGridView grid) return;
            var current = GridColumnLayout.Capture(grid);
            var defaults = _columnDefaults.TryGetValue(grid.Name, out var d) ? d : current;
            var locked = new HashSet<string>(grid.Columns.Cast<DataGridViewColumn>().Where(GridColumnLayout.IsLocked).Select(c => c.Name));
            using var dlg = new ColumnChooserForm(page.Text, current, defaults, locked);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            GridColumnLayout.Apply(grid, dlg.Result);
            _prefs.Columns[grid.Name] = GridColumnLayout.Capture(grid).Select(c => new ColumnState(c.Name, c.Visible)).ToList();
            SavePrefs();
        }

        #endregion
    }
}
