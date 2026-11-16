using System.Drawing;
using System.Windows.Forms;

namespace E_Tax
{
    /// <summary>
    /// Tạo cột và style chung cho các DataGridView cấu hình bằng code.
    /// </summary>
    internal static class GridColumnHelper
    {
        public static void ApplyDefaultStyle(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgv.Columns.Clear();
        }

        public static void AddTextColumn(DataGridView dgv, string name, string header, string? dataProperty, int width,
            bool frozen = false, DataGridViewContentAlignment? alignment = null)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProperty,
                Width = width,
                ReadOnly = true,
                Frozen = frozen
            };
            if (alignment.HasValue)
                column.DefaultCellStyle = new DataGridViewCellStyle { Alignment = alignment.Value };
            dgv.Columns.Add(column);
        }

        public static void AddNumericColumn(DataGridView dgv, string name, string header, string? dataProperty, int width, string format)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProperty,
                Width = width,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = format, Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dgv.Columns.Add(column);
        }
    }
}
