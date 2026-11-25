using System.Drawing;
using System.Windows.Forms;

namespace E_Tax
{
    /// <summary>
    /// Bảng màu và style dùng chung cho giao diện (CR-001).
    /// </summary>
    internal static class UiTheme
    {
        public static readonly Color Accent = Color.FromArgb(0x00, 0x75, 0xD6);
        public static readonly Color HeaderText = Color.White;
        public static readonly Color GridLine = Color.FromArgb(0xE5, 0xE7, 0xEB);
        public static readonly Color RowAlt = Color.FromArgb(0xF8, 0xFA, 0xFC);
        public static readonly Color Selection = Color.FromArgb(0xDB, 0xEA, 0xFE);
        public static readonly Color SelectionText = Color.FromArgb(0x1D, 0x23, 0x30);

        private static readonly Font GridFont = new("Segoe UI", 9);
        private static readonly Font GridHeaderFont = new("Segoe UI", 9, FontStyle.Bold);

        public static void StyleGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = GridLine;

            // Phải tắt visual styles thì màu tiêu đề mới có hiệu lực.
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 34;
            var header = dgv.ColumnHeadersDefaultCellStyle;
            header.BackColor = Accent;
            header.ForeColor = HeaderText;
            header.SelectionBackColor = Accent;
            header.SelectionForeColor = HeaderText;
            header.Font = GridHeaderFont;
            header.Padding = new Padding(6, 0, 6, 0);

            dgv.RowTemplate.Height = 28;
            var cell = dgv.DefaultCellStyle;
            cell.Font = GridFont;
            cell.BackColor = Color.White;
            cell.SelectionBackColor = Selection;
            cell.SelectionForeColor = SelectionText;
            cell.Padding = new Padding(6, 0, 6, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = RowAlt;
        }
    }
}
