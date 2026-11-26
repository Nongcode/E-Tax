using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>
    /// Lưới an toàn khi đổi bố cục Form1: mọi control nghiệp vụ còn tồn tại, nằm đúng khu (đăng nhập / tìm kiếm)
    /// và còn nối đúng handler. Dựng Form1 thật trên luồng STA, không hiển thị, không gọi mạng.
    /// </summary>
    public class Form1LayoutTests
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // (control, sự kiện, handler) — đúng như Designer gốc trước khi làm lại giao diện
        public static readonly TheoryData<string, string, string> Wiring = new()
        {
            { "btnRefreshCaptcha", "Click", "btnRefreshCaptcha_Click" },
            { "btnLogin", "Click", "btnLogin_Click" },
            { "btnActivate", "Click", "btnActivate_Click" },
            { "btnCopyMachineId", "Click", "btnCopyMachineId_Click" },
            { "cbPeriodKind", "SelectedIndexChanged", "cbPeriodKind_SelectedIndexChanged" },
            { "cbPeriodNumber", "SelectedIndexChanged", "PeriodValue_Changed" },
            { "nudPeriodYear", "ValueChanged", "PeriodValue_Changed" },
            { "btnTaiHDGoc", "Click", "btnTaiHDGoc_Click" },
            { "btnCnKoPdf", "Click", "btnCnKoPdf_Click" },
            { "btnGopPdf", "Click", "btnGopPdf_Click" },
            { "btnXemHD", "Click", "btnXemHD_Click" },
            { "btnLeftSearch", "Click", "btnLeftSearch_Click" },
            { "btnExportChiTiet", "Click", "btnExportChiTiet_Click" },
            { "btnExportDS", "Click", "btnExportDS_Click" },
            { "btnExportBangKeMua", "Click", "btnExportBangKeMua_Click" },
            { "btnExportBangKeBan", "Click", "btnExportBangKeBan_Click" },
            { "dgvMain", "CellContentClick", "dgvMain_CellContentClick" },
            { "dgvMain", "SelectionChanged", "dgvMain_SelectionChanged" },
            { "btnKtraDNMuaLe", "Click", "btnKtraDNMuaLe_Click" },
            { "btnMoGhiChu", "Click", "btnMoGhiChu_Click" },
            { "btnRightSearch", "Click", "btnRightSearch_Click" },
            { "btnKhoHoaDon", "Click", "btnKhoHoaDon_Click" },
            { "btnKiemTraNCC", "Click", "btnKiemTraNCC_Click" },
            { "btnKeKhai", "Click", "btnKeKhai_Click" },
            { "lblLogout", "Click", "lblLogout_Click" },
        };

        private static readonly string[] LoginControls =
        {
            "txtUser", "txtPass", "txtCaptcha", "picCaptcha", "btnRefreshCaptcha", "chkKeepLogin", "btnLogin",
            "panelActivation", "txtActivationKey", "btnActivate", "txtMachineId", "btnCopyMachineId"
        };

        private static readonly string[] SearchControls =
        {
            "cbPeriodKind", "cbPeriodNumber", "nudPeriodYear", "dtpFromDate", "dtpToDate", "rbSold", "rbBought", "rbAllInvoices",
            "btnLeftSearch", "btnXemHD", "btnCnKoPdf", "btnGopPdf", "btnTaiHDGoc", "btnExportDS", "btnExportChiTiet",
            "btnExportBangKeMua", "btnExportBangKeBan", "btnKeKhai", "btnKiemTraNCC", "txtKtraMaDoanhNghiep", "btnKtraDNMuaLe",
            "txtLocSoHD", "txtLocNCC", "cboLocLoai", "chkChiCoGhiChu", "btnRightSearch", "txtGhiChu", "btnMoGhiChu",
            "tabControlMain", "dgvMain", "dgvDetails", "dgvMua", "dgvBan", "dgvVatNop", "dgvGiamThue",
            "downloadProgressBar", "lblDownloadStatus", "btnKhoHoaDon", "lblStatusMessage"
        };

        // Ribbon (Phương án 3): nhóm → các lệnh theo thứ tự hiển thị
        public static readonly TheoryData<string, string, string[]> RibbonGroups = new()
        {
            { "grpTraCuu", "Tra cứu", new[] { "btnLeftSearch" } },
            { "grpHoaDon", "Hóa đơn", new[] { "btnXemHD", "btnCnKoPdf", "btnGopPdf", "btnTaiHDGoc" } },
            { "grpXuat", "Xuất Excel", new[] { "btnExportDS", "btnExportChiTiet", "btnExportBangKeMua", "btnExportBangKeBan" } },
            { "grpKeKhai", "Kê khai", new[] { "btnKeKhai", "btnKhoHoaDon" } },
            { "grpKiemTra", "Kiểm tra", new[] { "btnKiemTraNCC", "btnKtraDNMuaLe" } },
        };

        private static Control? FindByName(Control root, string name) =>
            root.Controls.Find(name, searchAllChildren: true).FirstOrDefault();

        private static Form1 LaidOut(Form1 f)
        {
            f.ClientSize = new System.Drawing.Size(1000, 700);
            ((Control)Field(f, "panelLogin")).Visible = false;
            ((Control)Field(f, "panelSearch")).Visible = true;
            f.PerformLayout();
            return f;
        }

        [Theory]
        [MemberData(nameof(RibbonGroups))]
        public void RibbonGroup_HoldsCommands_WithCaption(string group, string caption, string[] buttons)
        {
            var (captions, members) = OnForm(f =>
            {
                var g = FindByName((Control)Field(f, "panelSearch"), group) ?? throw new Xunit.Sdk.XunitException($"Không có nhóm {group}");
                var texts = g.Controls.Find("lblCaption_" + group, true).Select(c => c.Text).ToList();
                var inside = buttons.Where(b => IsInside((Control)Field(f, b), g)).ToList();
                return (texts, inside);
            });
            Assert.Equal(new[] { caption }, captions);
            Assert.Equal(buttons, members);
        }

        [Fact]
        public void BigRibbonButtons_HaveIconAboveText()
        {
            var bad = OnForm(f => new[] { "btnLeftSearch", "btnXemHD", "btnExportDS", "btnKeKhai", "btnKhoHoaDon", "btnKiemTraNCC" }
                .Where(n => Field(f, n) is not Button b || b.Image == null || b.TextImageRelation != TextImageRelation.ImageAboveText)
                .ToList());
            Assert.Empty(bad);
        }

        // Lệnh phụ: nút nhỏ icon bên trái, xếp chồng 3 hàng như Office để ribbon vừa cửa sổ 1280px
        [Fact]
        public void SmallRibbonButtons_HaveIconBeforeText_AndStack()
        {
            var (bad, columns) = OnForm(f =>
            {
                string[][] stacks = { new[] { "btnCnKoPdf", "btnGopPdf", "btnTaiHDGoc" }, new[] { "btnExportChiTiet", "btnExportBangKeMua", "btnExportBangKeBan" } };
                var wrong = stacks.SelectMany(s => s)
                    .Where(n => Field(f, n) is not Button b || b.Image == null || b.TextImageRelation != TextImageRelation.ImageBeforeText || b.Height > 26)
                    .ToList();
                var sameParent = stacks.Select(s => s.Select(n => ((Control)Field(f, n)).Parent).Distinct().Count()).ToList();
                return (wrong, sameParent);
            });
            Assert.Empty(bad);
            Assert.All(columns, c => Assert.Equal(1, c));
        }

        // Cửa sổ tối thiểu 1000px: ribbon cuộn ngang nhưng nhãn nhóm không bị thanh cuộn che; thanh lọc xuống dòng, không mất nút
        [Fact]
        public void NarrowWindow_KeepsFilterControlsAndGroupCaptionsVisible()
        {
            var (hiddenFilter, ribbonRoom) = OnForm(f =>
            {
                f.ClientSize = new System.Drawing.Size(984, 686);
                ((Control)Field(f, "panelLogin")).Visible = false;
                ((Control)Field(f, "panelSearch")).Visible = true;
                f.PerformLayout();
                var search = (Control)Field(f, "panelSearch");
                var bar = FindByName(search, "filterBar")!;
                var hidden = new[] { "txtLocSoHD", "txtLocNCC", "cboLocLoai", "chkChiCoGhiChu", "btnRightSearch", "txtGhiChu", "btnMoGhiChu" }
                    .Where(n =>
                    {
                        var c = (Control)Field(f, n);
                        var r = bar.RectangleToClient(c.Parent!.RectangleToScreen(c.Bounds));
                        return r.Bottom > bar.Height || r.Right > bar.Width;
                    }).ToList();
                var ribbon = FindByName(search, "ribbon")!;
                int captionBottom = 4 + 80 + 16; // padding trên + nội dung + nhãn nhóm
                return (hidden, ribbon.Height - SystemInformation.HorizontalScrollBarHeight - captionBottom);
            });
            Assert.Empty(hiddenFilter);
            Assert.True(ribbonRoom >= 0, $"Nhãn nhóm bị che {-ribbonRoom}px");
        }

        // Nhãn trạng thái (Phân loại) và rủi ro NCC hiện ngay sau Tổng tiền, không bị đẩy ra cuối bảng
        [Fact]
        public void StatusColumns_FollowTotalColumn()
        {
            var order = OnForm(f =>
            {
                var grid = (DataGridView)Field(f, "dgvMain");
                return grid.Columns.Cast<DataGridViewColumn>().OrderBy(c => c.DisplayIndex).Select(c => c.Name).ToList();
            });
            int total = order.IndexOf("colDgvTongTien");
            Assert.Equal("colDgvPhanLoai", order[total + 1]);
            Assert.Equal("colDgvRuiRo", order[total + 2]);
        }

        [Fact]
        public void Ribbon_FitsWithoutScrolling_At1280()
        {
            bool scrolls = OnForm(f =>
            {
                f.ClientSize = new System.Drawing.Size(1264, 760);
                ((Control)Field(f, "panelLogin")).Visible = false;
                ((Control)Field(f, "panelSearch")).Visible = true;
                f.PerformLayout();
                var groups = FindByName((Control)Field(f, "panelSearch"), "ribbonGroups")!;
                groups.PerformLayout();
                int needed = groups.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Horizontal) + groups.Padding.Horizontal;
                return needed > groups.ClientSize.Width;
            });
            Assert.False(scrolls);
        }

        [Fact]
        public void TablesFillRemainingSpace_BelowRibbonAndFilterBar()
        {
            var (tabsTop, ribbonBottom, filterBottom, dock) = OnForm(f =>
            {
                LaidOut(f);
                var search = (Control)Field(f, "panelSearch");
                var tabs = (Control)Field(f, "tabControlMain");
                var ribbon = FindByName(search, "ribbon")!;
                var filter = FindByName(search, "filterBar")!;
                Point Abs(Control c) => search.PointToClient(c.Parent!.PointToScreen(c.Location));
                return (Abs(tabs).Y, Abs(ribbon).Y + ribbon.Height, Abs(filter).Y + filter.Height, tabs.Dock);
            });
            Assert.Equal(DockStyle.Fill, dock);
            Assert.True(filterBottom > ribbonBottom - 1, "Thanh lọc phải nằm dưới ribbon");
            Assert.True(tabsTop >= filterBottom, "Bảng phải nằm dưới thanh lọc");
        }

        [Fact]
        public void RibbonControls_DoNotOverlap()
        {
            var overlaps = OnForm(f =>
            {
                LaidOut(f);
                var ribbon = FindByName((Control)Field(f, "panelSearch"), "ribbon")!;
                var found = new List<string>();
                void Check(Control parent)
                {
                    var kids = parent.Controls.Cast<Control>().Where(c => c.Visible || !parent.Visible).ToList();
                    for (int i = 0; i < kids.Count; i++)
                        for (int j = i + 1; j < kids.Count; j++)
                            if (kids[i].Bounds.IntersectsWith(kids[j].Bounds)) found.Add($"{kids[i].Name} × {kids[j].Name}");
                    foreach (var k in kids) Check(k);
                }
                Check(ribbon);
                return found;
            });
            Assert.Empty(overlaps);
        }

        private static T OnForm<T>(Func<Form1, T> probe)
        {
            T result = default!;
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var form = new Form1();
                    result = probe(form);
                }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) throw new Xunit.Sdk.XunitException("Không dựng được Form1: " + failure);
            return result;
        }

        private static object Field(Form1 form, string name) =>
            typeof(Form1).GetField(name, Any)?.GetValue(form) ?? throw new Xunit.Sdk.XunitException($"Form1 không còn control '{name}'.");

        private static bool IsInside(Control control, Control ancestor)
        {
            for (var c = control.Parent; c != null; c = c.Parent)
                if (c == ancestor) return true;
            return false;
        }

        /// <summary>Handler đang gắn vào sự kiện (EventHandlerList với khóa tĩnh, hoặc field delegate riêng của control).</summary>
        private static IEnumerable<string> Handlers(object target, string eventName)
        {
            var events = (EventHandlerList)typeof(Component).GetProperty("Events", Any)!.GetValue(target)!;
            string[] keyNames = { "s_" + char.ToLowerInvariant(eventName[0]) + eventName[1..] + "Event", "Event" + eventName, "EVENT_" + eventName.ToUpperInvariant() };
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                foreach (var f in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (keyNames.Any(k => string.Equals(f.Name, k, StringComparison.OrdinalIgnoreCase)) && f.GetValue(null) is object key && events[key] is Delegate d)
                        return d.GetInvocationList().Select(x => x.Method.Name);
                foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (typeof(Delegate).IsAssignableFrom(f.FieldType) && f.Name.Contains(eventName, StringComparison.OrdinalIgnoreCase) && f.GetValue(target) is Delegate d)
                        return d.GetInvocationList().Select(x => x.Method.Name);
            }
            return Array.Empty<string>();
        }

        [Theory]
        [MemberData(nameof(Wiring))]
        public void BusinessHandler_IsStillWired(string control, string eventName, string handler)
        {
            var wired = OnForm(f => Handlers(Field(f, control), eventName).ToList());
            Assert.Contains(handler, wired);
        }

        [Fact]
        public void LoginControls_StayInsideLoginPanel()
        {
            var outside = OnForm(f =>
            {
                var panel = (Control)Field(f, "panelLogin");
                return LoginControls.Where(n => !IsInside((Control)Field(f, n), panel)).ToList();
            });
            Assert.Empty(outside);
        }

        [Fact]
        public void SearchControls_StayInsideSearchPanel()
        {
            var outside = OnForm(f =>
            {
                var panel = (Control)Field(f, "panelSearch");
                return SearchControls.Where(n => !IsInside((Control)Field(f, n), panel)).ToList();
            });
            Assert.Empty(outside);
        }

        // Đăng nhập: mỗi ô có nhãn riêng ngay phía trên (không chỉ dùng chữ mờ trong ô), cùng chiều rộng với nút
        [Theory]
        [InlineData("txtUser", "Mã số thuế / Tài khoản")]
        [InlineData("txtPass", "Mật khẩu")]
        [InlineData("txtCaptcha", "Mã xác nhận")]
        public void LoginInput_HasLabelAbove_AndSameWidthAsButton(string input, string label)
        {
            var (labelText, labelAbove, inputWidth, buttonWidth, multiline) = OnForm(f =>
            {
                var box = (TextBox)Field(f, input);
                var parent = box.Parent!;
                var lbl = parent.Controls.OfType<Label>().FirstOrDefault(l => l.Text == label);
                bool above = lbl != null && parent.Controls.GetChildIndex(lbl) < parent.Controls.GetChildIndex(box);
                return (lbl?.Text, above, box.Width, ((Control)Field(f, "btnLogin")).Width, box.Multiline);
            });
            Assert.Equal(label, labelText);
            Assert.True(labelAbove, "Nhãn phải nằm trước ô nhập");
            Assert.Equal(buttonWidth, inputWidth);
            Assert.False(multiline);
        }

        [Fact]
        public void LoginCard_HasPrimaryHeader_AndFitsContent()
        {
            var (headerColor, cardHeight, contentBottom) = OnForm(f =>
            {
                var panel = (Control)Field(f, "panelLogin");
                var header = FindByName(panel, "loginHeader")!;
                f.ClientSize = new System.Drawing.Size(1000, 700);
                f.PerformLayout();
                var btn = (Control)Field(f, "btnLogin");
                int bottom = panel.RectangleToClient(btn.Parent!.RectangleToScreen(btn.Bounds)).Bottom;
                return (header.BackColor, panel.Height, bottom);
            });
            Assert.Equal(UiTheme.Primary, headerColor);
            Assert.True(cardHeight >= contentBottom, $"Thẻ đăng nhập cao {cardHeight} nhưng nút đăng nhập ở {contentBottom}");
        }

        // Hết hạn dùng thử: khóa ô đăng nhập nhưng khối kích hoạt vẫn dùng được, tiêu đề thẻ không bị xám
        [Fact]
        public void ExpiredTrial_LocksLoginInputs_ButActivationStaysUsable()
        {
            var state = OnForm(f =>
            {
                typeof(Form1).GetMethod("SetLoginInputsEnabled", Any)!.Invoke(f, new object[] { false });
                bool E(string n) => ((Control)Field(f, n)).Enabled;
                return new
                {
                    Locked = new[] { "txtUser", "txtPass", "txtCaptcha", "btnRefreshCaptcha", "chkKeepLogin", "btnLogin" }.Where(E).ToList(),
                    Activation = new[] { "txtActivationKey", "btnActivate", "txtMachineId", "btnCopyMachineId" }.Where(n => !E(n)).ToList(),
                    Title = E("lblLoginTitle")
                };
            });
            Assert.Empty(state.Locked);
            Assert.Empty(state.Activation);
            Assert.True(state.Title);
        }

        [Fact]
        public void LogoutLink_IsOnStatusBar()
        {
            bool onBar = OnForm(f => ((ToolStripItem)Field(f, "lblLogout")).Owner == Field(f, "statusBar"));
            Assert.True(onBar);
        }
    }
}
