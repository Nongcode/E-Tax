using System.Reflection;
using System.Windows.Forms;
using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>
    /// 4 kiểu giao diện (Chuẩn, Menu trái, Ribbon, Gọn) dùng chung control Designer: đổi kiểu không được làm mất control,
    /// mất handler, hay giấu lệnh (trừ các ô nằm trong popup bộ lọc).
    /// </summary>
    public class Form1LayoutKindsTests
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static readonly TheoryData<LayoutKind> Kinds = new() { LayoutKind.Classic, LayoutKind.SideMenu, LayoutKind.Ribbon, LayoutKind.Compact };

        private static readonly string[] Commands =
        {
            "btnLeftSearch", "btnXemHD", "btnCnKoPdf", "btnGopPdf", "btnTaiHDGoc", "btnExportDS", "btnExportChiTiet",
            "btnExportBangKeMua", "btnExportBangKeBan", "btnKeKhai", "btnKhoHoaDon", "btnKiemTraNCC"
        };

        private static readonly string[] AllBusiness = Commands.Concat(new[]
        {
            "cbPeriodKind", "cbPeriodNumber", "nudPeriodYear", "dtpFromDate", "dtpToDate", "rbSold", "rbBought", "rbAllInvoices",
            "txtKtraMaDoanhNghiep", "btnKtraDNMuaLe", "txtLocSoHD", "txtLocNCC", "cboLocLoai", "chkChiCoGhiChu", "btnRightSearch",
            "txtGhiChu", "btnMoGhiChu", "tabControlMain", "dgvMain", "dgvDetails", "dgvMua", "dgvBan", "dgvVatNop", "dgvGiamThue",
            "downloadProgressBar", "lblDownloadStatus", "lblStatusMessage"
        }).ToArray();

        private static T OnForm<T>(Func<Form1, T> probe)
        {
            T result = default!;
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var form = new Form1();
                    form.ClientSize = new System.Drawing.Size(1264, 760);
                    ((Control)Field(form, "panelLogin")).Visible = false;
                    ((Control)Field(form, "panelSearch")).Visible = true;
                    result = probe(form);
                }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) throw new Xunit.Sdk.XunitException("Lỗi khi dựng/đổi bố cục Form1: " + failure);
            return result;
        }

        private static object Field(Form1 form, string name) =>
            typeof(Form1).GetField(name, Any)?.GetValue(form) ?? throw new Xunit.Sdk.XunitException($"Form1 không còn '{name}'.");

        private static void Apply(Form1 f, LayoutKind kind)
        {
            typeof(Form1).GetMethod("ApplyLayout", Any)!.Invoke(f, new object[] { kind });
            f.PerformLayout();
        }

        private static bool IsInside(Control c, Control ancestor)
        {
            for (var p = c.Parent; p != null; p = p.Parent) if (p == ancestor) return true;
            return false;
        }

        private static Control? Named(Control root, string name) => root.Controls.Find(name, true).FirstOrDefault();

        /// <summary>Hiện được trên màn hình: mọi tổ tiên tới panelSearch đều Visible (đọc cờ riêng, không phụ thuộc form đã hiện chưa).</summary>
        private static bool Shown(Control c, Control root)
        {
            for (var x = c; x != null && x != root; x = x.Parent) if (!OwnVisible(x)) return false;
            return true;
        }

        /// <summary>Cờ Visible riêng của control (Control.Visible trả false khi form chưa hiện).</summary>
        private static bool OwnVisible(Control x)
        {
            var m = typeof(Control).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(mi => mi.Name == "GetState" && mi.GetParameters().Length == 1);
            var flag = m.GetParameters()[0].ParameterType;
            object arg = flag.IsEnum ? Enum.ToObject(flag, 0x00000002) : 0x00000002; // STATE_VISIBLE
            return (bool)m.Invoke(x, new[] { arg })!;
        }

        [Theory]
        [MemberData(nameof(Kinds))]
        public void Layout_KeepsEveryBusinessControl_InsideSearchPanel(LayoutKind kind)
        {
            var missing = OnForm(f =>
            {
                Apply(f, kind);
                var search = (Control)Field(f, "panelSearch");
                return AllBusiness.Where(n => Field(f, n) is not Control c || c.IsDisposed || !IsInside(c, search)).ToList();
            });
            Assert.Empty(missing);
        }

        [Theory]
        [MemberData(nameof(Kinds))]
        public void Layout_ShowsEveryCommand_WithoutOpeningPopup(LayoutKind kind)
        {
            var hidden = OnForm(f =>
            {
                Apply(f, kind);
                var search = (Control)Field(f, "panelSearch");
                return Commands.Where(n => !Reachable((Control)Field(f, n), search)).ToList();
            });
            Assert.Empty(hidden);
        }

        /// <summary>Hiện sẵn, hoặc nằm trong menu thả "menuPopup_X" mà nút mở "btnMenu_X" đang hiện.</summary>
        private static bool Reachable(Control c, Control search)
        {
            if (Shown(c, search)) return true;
            for (var p = c.Parent; p != null && p != search; p = p.Parent)
                if (p.Name.StartsWith("menuPopup_") && Named(search, "btnMenu_" + p.Name["menuPopup_".Length..]) is Control opener)
                    return Shown(opener, search);
            return false;
        }

        [Fact]
        public void SideMenu_ExcelAndPdfMenus_HoldRealButtons_ClosedByDefault()
        {
            var (excel, pdf, closed) = OnForm(f =>
            {
                Apply(f, LayoutKind.SideMenu);
                var search = (Control)Field(f, "panelSearch");
                var em = Named(search, "menuPopup_Excel")!;
                var pm = Named(search, "menuPopup_Pdf")!;
                return (new[] { "btnExportDS", "btnExportChiTiet", "btnExportBangKeMua", "btnExportBangKeBan" }.All(n => IsInside((Control)Field(f, n), em)),
                        new[] { "btnCnKoPdf", "btnGopPdf" }.All(n => IsInside((Control)Field(f, n), pm)),
                        !OwnVisible(em) && !OwnVisible(pm));
            });
            Assert.True(excel);
            Assert.True(pdf);
            Assert.True(closed);
        }

        // Lệnh trong menu đang chạy: nút mở menu phải cho thấy, nếu không người dùng tưởng app treo
        [Fact]
        public void SideMenu_BusyCommandInMenu_ShowsOnOpener()
        {
            var (busy, done) = OnForm(f =>
            {
                Apply(f, LayoutKind.SideMenu);
                var search = (Control)Field(f, "panelSearch");
                var opener = Named(search, "btnMenu_Excel")!;
                var b = (Button)Field(f, "btnExportDS");
                string label = b.Text;
                b.Text = "Đang xử lý...";
                string whileBusy = opener.Text;
                b.Text = label;
                return (whileBusy, opener.Text);
            });
            Assert.Equal("Đang xử lý...", busy);
            Assert.StartsWith("Xuất Excel", done);
        }

        [Theory]
        [InlineData(LayoutKind.Classic, true)]
        [InlineData(LayoutKind.Compact, true)]
        [InlineData(LayoutKind.Ribbon, false)]
        public void InvoiceKinds_AreSegmentedButtons_ExceptRibbon(LayoutKind kind, bool segmented)
        {
            var (appearance, checkedBack) = OnForm(f =>
            {
                Apply(f, kind);
                var rb = (RadioButton)Field(f, "rbSold");
                rb.Checked = true;
                return (rb.Appearance, rb.BackColor);
            });
            Assert.Equal(segmented ? Appearance.Button : Appearance.Normal, appearance);
            if (segmented) Assert.NotEqual(System.Drawing.Color.White, checkedBack);
        }

        [Theory]
        [InlineData(LayoutKind.Classic, nameof(UiTheme.AccentNavy))]
        [InlineData(LayoutKind.SideMenu, nameof(UiTheme.AccentTeal))]
        [InlineData(LayoutKind.Ribbon, nameof(UiTheme.AccentOffice))]
        [InlineData(LayoutKind.Compact, nameof(UiTheme.AccentBlue))]
        public void EachLayout_UsesItsAccent(LayoutKind kind, string accent)
        {
            var (search, expected) = OnForm(f =>
            {
                Apply(f, kind);
                var a = ((System.Drawing.Color Primary, System.Drawing.Color, System.Drawing.Color, System.Drawing.Color))typeof(UiTheme).GetField(accent)!.GetValue(null)!;
                return (((Button)Field(f, "btnLeftSearch")).BackColor, a.Primary);
            });
            if (kind == LayoutKind.Ribbon) return; // Ribbon: nút Tìm kiếm là nút lớn nền trắng
            Assert.Equal(expected, search);
        }

        // "Trắng quá": kiểu nào cũng có mảng màu nhấn — thanh tiêu đề (Menu trái: menu bên trái tô màu)
        [Theory]
        [MemberData(nameof(Kinds))]
        public void EachLayout_HasAccentBand(LayoutKind kind)
        {
            var (band, accent) = OnForm(f =>
            {
                Apply(f, kind);
                var search = (Control)Field(f, "panelSearch");
                var c = Named(search, kind == LayoutKind.SideMenu ? "sideNav" : "appBar");
                return (c?.BackColor, UiTheme.Primary);
            });
            Assert.Equal(accent, band);
        }

        // Bảng nằm trong khung trắng (mẫu PA1/PA2); kiểu Gọn sát mép như PA5
        [Theory]
        [InlineData(LayoutKind.Classic, true)]
        [InlineData(LayoutKind.SideMenu, true)]
        [InlineData(LayoutKind.Compact, false)]
        public void Tables_SitInCard_WhereMockupHasOne(LayoutKind kind, bool card)
        {
            var inCard = OnForm(f =>
            {
                Apply(f, kind);
                var search = (Control)Field(f, "panelSearch");
                return Named(search, "contentCardInner") is Control inner && IsInside((Control)Field(f, "tabControlMain"), inner);
            });
            Assert.Equal(card, inCard);
        }

        [Theory]
        [MemberData(nameof(Kinds))]
        public void Layout_TablesFill_AndTabStripHasGear(LayoutKind kind)
        {
            var (dock, strip) = OnForm(f =>
            {
                Apply(f, kind);
                var search = (Control)Field(f, "panelSearch");
                return (((Control)Field(f, "tabControlMain")).Dock, Named(search, "tabStrip") != null);
            });
            Assert.Equal(DockStyle.Fill, dock);
            Assert.True(strip);
        }

        // Kiểu dùng popup: ô lọc trong kết quả nằm trong popup (ẩn sẵn), mở bằng nút "Bộ lọc"/"Lọc kết quả"
        [Theory]
        [InlineData(LayoutKind.Classic)]
        [InlineData(LayoutKind.SideMenu)]
        [InlineData(LayoutKind.Compact)]
        public void PopupLayouts_PutResultFiltersInClosedPopup(LayoutKind kind)
        {
            var (popupVisible, inside, hasButton) = OnForm(f =>
            {
                Apply(f, kind);
                var search = (Control)Field(f, "panelSearch");
                var popup = Named(search, "filterPopup")!;
                return (OwnVisible(popup),
                        new[] { "txtLocSoHD", "txtLocNCC", "cboLocLoai", "chkChiCoGhiChu", "btnRightSearch" }.All(n => IsInside((Control)Field(f, n), popup)),
                        Named(search, "btnFilterPopup") != null);
            });
            Assert.False(popupVisible);
            Assert.True(inside);
            Assert.True(hasButton);
        }

        [Fact]
        public void SideMenu_HasNavigationOnLeft()
        {
            var dock = OnForm(f =>
            {
                Apply(f, LayoutKind.SideMenu);
                return Named((Control)Field(f, "panelSearch"), "sideNav")?.Dock;
            });
            Assert.Equal(DockStyle.Left, dock);
        }

        [Fact]
        public void SwitchingLayouts_Repeatedly_KeepsControlsAndHandlers()
        {
            var (lost, wired) = OnForm(f =>
            {
                foreach (var k in new[] { LayoutKind.Classic, LayoutKind.Compact, LayoutKind.SideMenu, LayoutKind.Ribbon, LayoutKind.SideMenu, LayoutKind.Classic })
                    Apply(f, k);
                var search = (Control)Field(f, "panelSearch");
                var gone = AllBusiness.Where(n => ((Control)Field(f, n)).IsDisposed || !IsInside((Control)Field(f, n), search)).ToList();
                var events = (System.ComponentModel.EventHandlerList)typeof(System.ComponentModel.Component).GetProperty("Events", Any)!.GetValue(Field(f, "btnExportDS"))!;
                var key = typeof(Control).GetField("s_clickEvent", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                var names = (events[key] as Delegate)?.GetInvocationList().Select(d => d.Method.Name).ToList() ?? new List<string>();
                return (gone, names);
            });
            Assert.Empty(lost);
            Assert.Single(wired, "btnExportDS_Click"); // không bị gắn trùng sau nhiều lần đổi
        }

        [Fact]
        public void LayoutMenu_OnStatusBar_ListsFourKinds()
        {
            var items = OnForm(f =>
            {
                var bar = (StatusStrip)Field(f, "statusBar");
                var menu = bar.Items.OfType<ToolStripDropDownButton>().Single(i => i.Name == "ddLayout");
                return menu.DropDownItems.Cast<ToolStripItem>().Select(i => i.Text).ToList();
            });
            Assert.Equal(new[] { "Chuẩn", "Menu trái", "Ribbon", "Gọn" }, items);
        }

        [Fact]
        public void KpiCards_ShowCurrentTotals()
        {
            var texts = OnForm(f =>
            {
                Apply(f, LayoutKind.Classic);
                var results = (List<SearchResult>)Field(f, "_latestResults");
                results.Add(new SearchResult { Loai_hoa_don = "Bán ra", Tong_tien_thanh_toan = 1080, Tong_tien_thue = 80, Trang_thai_hoa_don = 1 });
                typeof(Form1).GetMethod("UpdateInvoiceSummary", Any)!.Invoke(f, new object?[] { null });
                return Named((Control)Field(f, "panelSearch"), "kpiCards")!.Controls.Find("kpiValue_Sold", true).Single().Text;
            });
            Assert.Equal("1.080 đ", texts);
        }
    }
}
