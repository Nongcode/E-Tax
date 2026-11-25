using System.Drawing;
using System.Windows.Forms;
using E_Tax;

namespace E_Tax.Tests
{
    public class UiThemeTests
    {
        [Fact]
        public void StyleGrid_AppliesTheme()
        {
            using var dgv = new DataGridView();

            UiTheme.StyleGrid(dgv);

            Assert.False(dgv.EnableHeadersVisualStyles);
            Assert.Equal(Color.FromArgb(0x00, 0x75, 0xD6), dgv.ColumnHeadersDefaultCellStyle.BackColor);
            Assert.Equal(Color.White, dgv.ColumnHeadersDefaultCellStyle.ForeColor);
            Assert.Equal(34, dgv.ColumnHeadersHeight);
            Assert.Equal(DataGridViewColumnHeadersHeightSizeMode.DisableResizing, dgv.ColumnHeadersHeightSizeMode);
            Assert.Equal(28, dgv.RowTemplate.Height);
            Assert.Equal(DataGridViewCellBorderStyle.SingleHorizontal, dgv.CellBorderStyle);
            Assert.Equal(BorderStyle.None, dgv.BorderStyle);
            Assert.Equal(Color.White, dgv.BackgroundColor);
            Assert.Equal(Color.FromArgb(0xE5, 0xE7, 0xEB), dgv.GridColor);
            Assert.Equal(Color.FromArgb(0xF8, 0xFA, 0xFC), dgv.AlternatingRowsDefaultCellStyle.BackColor);
            Assert.Equal(Color.FromArgb(0xDB, 0xEA, 0xFE), dgv.DefaultCellStyle.SelectionBackColor);
            Assert.Equal(Color.FromArgb(0x1D, 0x23, 0x30), dgv.DefaultCellStyle.SelectionForeColor);
            Assert.Equal("Segoe UI", dgv.DefaultCellStyle.Font!.Name);
            Assert.Equal(new Padding(6, 0, 6, 0), dgv.DefaultCellStyle.Padding);
        }

        [Fact]
        public void ApplyDefaultStyle_UsesTheme()
        {
            using var dgv = new DataGridView();

            GridColumnHelper.ApplyDefaultStyle(dgv);

            Assert.False(dgv.EnableHeadersVisualStyles);
            Assert.Equal(28, dgv.RowTemplate.Height);
        }
    }
}
