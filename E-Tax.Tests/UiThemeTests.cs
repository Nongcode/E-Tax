using System.Drawing;
using System.Windows.Forms;
using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Bảng màu Ribbon (Phương án 3): chữ phải đủ tương phản WCAG AA (≥ 4.5:1), nhãn trạng thái đúng nhóm.</summary>
    public class UiThemeTests
    {
        public static readonly TheoryData<string, string, string> TextPairs = new()
        {
            { "Text on Surface", nameof(UiTheme.Text), nameof(UiTheme.Surface) },
            { "Muted on Surface", nameof(UiTheme.MutedText), nameof(UiTheme.Surface) },
            { "Text on RibbonBack", nameof(UiTheme.Text), nameof(UiTheme.RibbonBack) },
            { "Muted on RibbonBack", nameof(UiTheme.MutedText), nameof(UiTheme.RibbonBack) },
            { "Text on header", nameof(UiTheme.Text), nameof(UiTheme.GridHeaderBack) },
            { "Text on selection", nameof(UiTheme.Text), nameof(UiTheme.Selection) },
            { "OnPrimary on Primary", nameof(UiTheme.OnPrimary), nameof(UiTheme.Primary) },
            { "Primary on Surface", nameof(UiTheme.Primary), nameof(UiTheme.Surface) },
        };

        public static readonly TheoryData<string> Accents = new() { nameof(UiTheme.AccentOffice), nameof(UiTheme.AccentNavy), nameof(UiTheme.AccentTeal), nameof(UiTheme.AccentBlue) };

        // Mỗi kiểu giao diện một màu nhấn: chữ trắng trên nút chính, màu nhấn trên nền trắng, chữ trên dòng chọn đều ≥ 4.5:1
        [Theory]
        [MemberData(nameof(Accents))]
        public void Accent_HasReadableContrast(string name)
        {
            var a = ((Color Primary, Color Hover, Color HoverBack, Color Selection))typeof(UiTheme).GetField(name)!.GetValue(null)!;
            Assert.True(UiTheme.ContrastRatio(Color.White, a.Primary) >= 4.5, "chữ trắng trên màu chính");
            Assert.True(UiTheme.ContrastRatio(Color.White, a.Hover) >= 4.5, "chữ trắng khi hover");
            Assert.True(UiTheme.ContrastRatio(a.Primary, a.HoverBack) >= 4.5, "màu nhấn trên nền hover");
            Assert.True(UiTheme.ContrastRatio(UiTheme.Text, a.Selection) >= 4.5, "chữ trên dòng chọn");
            Assert.True(UiTheme.ContrastRatio(UiTheme.OnPrimaryMuted, a.Primary) >= 4.5, "chữ phụ trên thanh tiêu đề");
            Assert.True(UiTheme.ContrastRatio(Color.White, a.Hover) >= 4.5, "menu trái: mục đang chọn");
            Assert.True(UiTheme.ContrastRatio(UiTheme.Text, a.HoverBack) >= 4.5, "tiêu đề bảng tô nhạt");
        }

        [Fact]
        public void UseAccent_ChangesPrimary_OnlyOnCallingThread()
        {
            Color other = default;
            var t = new Thread(() => { UiTheme.UseAccent(UiTheme.AccentTeal); other = UiTheme.Primary; });
            t.Start(); t.Join();
            Assert.Equal(UiTheme.AccentTeal.Primary, other);
            Assert.Equal(UiTheme.AccentOffice.Primary, UiTheme.Primary);
        }

        private static Color Get(string name) => (Color)(typeof(UiTheme).GetField(name)?.GetValue(null) ?? typeof(UiTheme).GetProperty(name)!.GetValue(null))!;

        // Bảng kiểu Office: tiêu đề nền xám nhạt chữ đậm tối (thay nền xanh đặc cũ)
        [Fact]
        public void StyleGrid_AppliesTheme()
        {
            using var dgv = new DataGridView();

            UiTheme.StyleGrid(dgv);

            Assert.False(dgv.EnableHeadersVisualStyles);
            Assert.Equal(Color.FromArgb(0xF3, 0xF3, 0xF3), dgv.ColumnHeadersDefaultCellStyle.BackColor);
            Assert.Equal(UiTheme.Text, dgv.ColumnHeadersDefaultCellStyle.ForeColor);
            Assert.True(dgv.ColumnHeadersDefaultCellStyle.Font!.Bold);
            Assert.Equal(34, dgv.ColumnHeadersHeight);
            Assert.Equal(DataGridViewColumnHeadersHeightSizeMode.DisableResizing, dgv.ColumnHeadersHeightSizeMode);
            Assert.Equal(28, dgv.RowTemplate.Height);
            Assert.Equal(DataGridViewCellBorderStyle.SingleHorizontal, dgv.CellBorderStyle);
            Assert.Equal(BorderStyle.None, dgv.BorderStyle);
            Assert.Equal(Color.White, dgv.BackgroundColor);
            Assert.Equal(Color.FromArgb(0xE6, 0xE6, 0xE6), dgv.GridColor);
            Assert.Equal(Color.FromArgb(0xFA, 0xFA, 0xFA), dgv.AlternatingRowsDefaultCellStyle.BackColor);
            Assert.Equal(Color.FromArgb(0xCF, 0xE3, 0xFF), dgv.DefaultCellStyle.SelectionBackColor);
            Assert.Equal(UiTheme.Text, dgv.DefaultCellStyle.SelectionForeColor);
            Assert.Equal("Segoe UI", dgv.DefaultCellStyle.Font!.Name);
            Assert.Equal(new Padding(6, 0, 6, 0), dgv.DefaultCellStyle.Padding);
        }

        // Máy cài vùng tiếng Anh vẫn phải hiện 33.420.000
        [Fact]
        public void StyleGrid_FormatsMoneyVietnamese_RegardlessOfWindowsRegion()
        {
            using var dgv = new DataGridView();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "c", DefaultCellStyle = new DataGridViewCellStyle { Format = "#,##0" } });
            dgv.Columns.Add(new DataGridViewButtonColumn { Name = "b" });

            UiTheme.StyleGrid(dgv);

            var style = dgv.Columns["c"]!.InheritedStyle;
            Assert.Equal("33.420.000", 33420000m.ToString(style.Format, style.FormatProvider));
            Assert.Equal(FlatStyle.Flat, ((DataGridViewButtonColumn)dgv.Columns["b"]!).FlatStyle);
        }

        [Fact]
        public void ApplyDefaultStyle_UsesTheme()
        {
            using var dgv = new DataGridView();

            GridColumnHelper.ApplyDefaultStyle(dgv);

            Assert.False(dgv.EnableHeadersVisualStyles);
            Assert.Equal(28, dgv.RowTemplate.Height);
        }

        // Form phụ (Kê khai, Kho HĐ, Tra cứu, Tiến trình): áp màu/nút/bảng chung, nút mặc định (AcceptButton) là primary
        [Fact]
        public void ApplyTo_StylesNestedButtonsAndGrids()
        {
            using var form = new Form();
            var ok = new Button();
            var cancel = new Button();
            var grid = new DataGridView();
            var panel = new Panel();
            panel.Controls.AddRange(new Control[] { cancel, grid });
            form.Controls.AddRange(new Control[] { ok, panel });
            form.AcceptButton = ok;

            UiTheme.ApplyTo(form);

            Assert.Equal(UiTheme.Primary, ok.BackColor);
            Assert.Equal(UiTheme.Surface, cancel.BackColor);
            Assert.Equal(FlatStyle.Flat, cancel.FlatStyle);
            Assert.Equal(UiTheme.GridHeaderBack, grid.ColumnHeadersDefaultCellStyle.BackColor);
            Assert.Equal("Segoe UI", form.Font.Name);
        }

        [Fact]
        public void StyleButton_PrimaryAndSecondary()
        {
            using var primary = new Button();
            using var secondary = new Button();

            UiTheme.StyleButton(primary, primary: true);
            UiTheme.StyleButton(secondary, primary: false);

            Assert.Equal(FlatStyle.Flat, primary.FlatStyle);
            Assert.Equal(UiTheme.Primary, primary.BackColor);
            Assert.Equal(UiTheme.OnPrimary, primary.ForeColor);
            Assert.Equal(UiTheme.Surface, secondary.BackColor);
            Assert.Equal(UiTheme.Text, secondary.ForeColor);
            Assert.Equal(UiTheme.Border, secondary.FlatAppearance.BorderColor);
            Assert.Equal("Segoe UI", secondary.Font.Name);
        }

        [Theory]
        [MemberData(nameof(TextPairs))]
        public void TextColors_MeetContrastAA(string pair, string fg, string bg)
        {
            double ratio = UiTheme.ContrastRatio(Get(fg), Get(bg));
            Assert.True(ratio >= 4.5, $"{pair}: {ratio:0.00}");
        }

        [Fact]
        public void ContrastRatio_BlackOnWhite_Is21()
        {
            Assert.Equal(21.0, UiTheme.ContrastRatio(Color.Black, Color.White), 1);
            Assert.Equal(1.0, UiTheme.ContrastRatio(Color.White, Color.White), 3);
        }

        [Theory]
        [InlineData(InvoiceRelation.Goc, ChipKind.Ok)]
        [InlineData(InvoiceRelation.DieuChinh, ChipKind.Warning)]
        [InlineData(InvoiceRelation.BiDieuChinh, ChipKind.Warning)]
        [InlineData(InvoiceRelation.ThayThe, ChipKind.Info)]
        [InlineData(InvoiceRelation.BiThayThe, ChipKind.Danger)]
        [InlineData(InvoiceRelation.BiHuy, ChipKind.Danger)]
        [InlineData(InvoiceRelation.KhongRo, ChipKind.Danger)]
        public void StatusChip_ByRelation(InvoiceRelation relation, ChipKind expected)
        {
            Assert.Equal(expected, UiTheme.ChipKindOf(relation));
        }

        [Theory]
        [InlineData("Safe", ChipKind.Ok)]
        [InlineData("Risky", ChipKind.Danger)]
        [InlineData("Unknown", ChipKind.Warning)]
        public void RiskChip_ByStatus(string status, ChipKind expected)
        {
            Assert.Equal(expected, UiTheme.ChipKindOf(Enum.Parse<RiskStatus>(status)));
        }

        [Theory]
        [InlineData(ChipKind.Ok)]
        [InlineData(ChipKind.Warning)]
        [InlineData(ChipKind.Info)]
        [InlineData(ChipKind.Danger)]
        public void ChipColors_AreReadable_AndDistinct(ChipKind kind)
        {
            var (fore, back) = UiTheme.ChipColors(kind);
            Assert.True(UiTheme.ContrastRatio(fore, back) >= 4.5, $"{kind}: {UiTheme.ContrastRatio(fore, back):0.00}");
            var others = Enum.GetValues<ChipKind>().Where(k => k != kind).Select(k => UiTheme.ChipColors(k).Back);
            Assert.DoesNotContain(back, others);
        }
    }
}
