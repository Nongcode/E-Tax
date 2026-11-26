using System.Drawing;
using System.Windows.Forms;

namespace E_Tax
{
    public enum ChipKind { Ok, Warning, Info, Danger }

    /// <summary>
    /// Bảng màu và style dùng chung — giao diện Ribbon kiểu Office (docs/features/giao-dien/thiet-ke-lai/design.md).
    /// Chữ trên nền đều đạt tương phản ≥ 4.5:1 (UiThemeTests).
    /// </summary>
    internal static class UiTheme
    {
        /// <summary>Màu nhấn đổi theo kiểu giao diện (UseAccent): Chuẩn navy, Menu trái xanh ngọc, Ribbon/Gọn xanh dương.</summary>
        /// Gắn theo luồng: app chỉ có một luồng UI; test dựng form trên luồng riêng nên không giẫm màu của nhau khi chạy song song.
        [ThreadStatic] private static (Color Primary, Color Hover, Color HoverBack, Color Selection)? _accent;
        private static (Color Primary, Color Hover, Color HoverBack, Color Selection) Accent => _accent ?? AccentOffice;
        public static Color Primary => Accent.Primary;
        public static Color PrimaryHover => Accent.Hover;
        public static Color HoverBack => Accent.HoverBack;
        public static Color Selection => Accent.Selection;

        /// <summary>Bộ màu nhấn: (chính, hover, nền hover nhạt, nền dòng chọn). Mọi màu chính đạt ≥ 4.5:1 với chữ trắng.</summary>
        public static readonly (Color Primary, Color Hover, Color HoverBack, Color Selection) AccentOffice =
            (Color.FromArgb(0x18, 0x5A, 0xBD), Color.FromArgb(0x10, 0x4A, 0xA0), Color.FromArgb(0xE8, 0xF0, 0xFB), Color.FromArgb(0xCF, 0xE3, 0xFF));
        public static readonly (Color Primary, Color Hover, Color HoverBack, Color Selection) AccentNavy =
            (Color.FromArgb(0x1E, 0x3A, 0x5F), Color.FromArgb(0x15, 0x2B, 0x47), Color.FromArgb(0xEC, 0xF1, 0xF7), Color.FromArgb(0xDC, 0xE6, 0xF2));
        public static readonly (Color Primary, Color Hover, Color HoverBack, Color Selection) AccentTeal =
            (Color.FromArgb(0x0F, 0x76, 0x6E), Color.FromArgb(0x0B, 0x5E, 0x58), Color.FromArgb(0xE6, 0xF5, 0xF3), Color.FromArgb(0xCC, 0xF1, 0xEA));
        public static readonly (Color Primary, Color Hover, Color HoverBack, Color Selection) AccentBlue =
            (Color.FromArgb(0x1D, 0x4E, 0xD8), Color.FromArgb(0x1E, 0x40, 0xAF), Color.FromArgb(0xEB, 0xF2, 0xFE), Color.FromArgb(0xDB, 0xEA, 0xFE));

        /// <summary>Chữ phụ trên nền màu nhấn (thanh tiêu đề, menu trái): trắng mờ, vẫn ≥ 4.5:1 với mọi màu nhấn.</summary>
        public static readonly Color OnPrimaryMuted = Color.FromArgb(0xE6, 0xEE, 0xF6);

        /// <summary>Vạch màu bên trái thẻ số liệu: bán ra xanh lá, mua vào xanh dương, thuế cam, cần chú ý đỏ.</summary>
        public static Color KpiColor(string key) => key switch
        {
            "Sold" => Color.FromArgb(0x16, 0xA3, 0x4A),
            "Bought" => Color.FromArgb(0x25, 0x63, 0xEB),
            "Vat" => Color.FromArgb(0xD9, 0x77, 0x06),
            _ => Color.FromArgb(0xDC, 0x26, 0x26)
        };

        public static void UseAccent((Color Primary, Color Hover, Color HoverBack, Color Selection) accent)
        {
            _accent = accent;
        }

        public static readonly Color OnPrimary = Color.White;
        public static readonly Color Text = Color.FromArgb(0x1F, 0x1F, 0x1F);
        public static readonly Color MutedText = Color.FromArgb(0x5C, 0x5C, 0x5C);
        public static readonly Color Border = Color.FromArgb(0xD1, 0xD1, 0xD1);
        public static readonly Color Divider = Color.FromArgb(0xE1, 0xE1, 0xE1);
        public static readonly Color Surface = Color.White;
        public static readonly Color AppBack = Color.FromArgb(0xEC, 0xF0, 0xF5); // xám ngả xanh: thẻ trắng nổi lên, đỡ "trắng toát"
        public static readonly Color RibbonBack = Color.White;
        public static readonly Color FilterBack = Color.FromArgb(0xFA, 0xFA, 0xFA);
        public static readonly Color GridHeaderBack = Color.FromArgb(0xF3, 0xF3, 0xF3);
        public static readonly Color GridLine = Color.FromArgb(0xE6, 0xE6, 0xE6);
        public static readonly Color RowAlt = Color.FromArgb(0xFA, 0xFA, 0xFA);

        public static readonly Font BaseFont = new("Segoe UI", 9F);
        public static readonly Font BoldFont = new("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font SmallFont = new("Segoe UI", 8F);
        public static readonly Font LabelFont = new("Segoe UI", 8.25F, FontStyle.Bold);
        public static readonly Font TitleFont = new("Segoe UI", 15F, FontStyle.Bold);

        private static readonly System.Globalization.CultureInfo Vietnamese = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");

        public static void StyleGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = Surface;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = GridLine;

            // Phải tắt visual styles thì màu tiêu đề mới có hiệu lực.
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 34;
            var header = dgv.ColumnHeadersDefaultCellStyle;
            header.BackColor = GridHeaderBack;
            header.ForeColor = Text;
            header.SelectionBackColor = GridHeaderBack;
            header.SelectionForeColor = Text;
            header.Font = BoldFont;
            header.Padding = new Padding(6, 0, 6, 0);

            dgv.RowHeadersVisible = false;
            dgv.RowTemplate.Height = 28;
            var cell = dgv.DefaultCellStyle;
            cell.Font = BaseFont;
            cell.BackColor = Surface;
            cell.ForeColor = Text;
            cell.SelectionBackColor = Selection;
            cell.SelectionForeColor = Text;
            cell.Padding = new Padding(6, 0, 6, 0);
            // Số tiền kiểu Việt Nam (33.420.000) bất kể cài đặt vùng của Windows; cột kế thừa từ style mặc định
            cell.FormatProvider = Vietnamese;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = RowAlt;
            // Tiêu đề một dòng ("Tiền chưa thuế" không gãy đôi); cột hẹp hơn tiêu đề thì nới ra
            header.WrapMode = DataGridViewTriState.False;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill || string.IsNullOrEmpty(col.HeaderText)) continue;
                int need = TextRenderer.MeasureText(col.HeaderText, BoldFont).Width + 20;
                if (col.Width < need) col.Width = need;
            }
            foreach (var button in dgv.Columns.OfType<DataGridViewButtonColumn>())
                button.FlatStyle = FlatStyle.Flat;
        }

        /// <summary>
        /// Áp giao diện chung cho form phụ: font Segoe UI, nút phẳng (AcceptButton là primary), bảng kiểu Office.
        /// Không đổi bố cục; gọi ở cuối constructor sau khi đã tạo control.
        /// </summary>
        public static void ApplyTo(Form form)
        {
            form.Font = BaseFont;
            if (form.BackColor == SystemColors.Control) form.BackColor = AppBack;
            var accept = form.AcceptButton as Button;
            void Walk(Control parent)
            {
                foreach (Control c in parent.Controls)
                {
                    switch (c)
                    {
                        case Button b: StyleButton(b, primary: b == accept); break;
                        case DataGridView g: StyleGrid(g); break;
                    }
                    Walk(c);
                }
            }
            Walk(form);
        }

        /// <summary>Nút phẳng: primary nền xanh chữ trắng; secondary nền trắng viền xám.</summary>
        public static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.Font = primary ? BoldFont : BaseFont;
            button.Cursor = Cursors.Hand;
            button.BackColor = primary ? Primary : Surface;
            button.ForeColor = primary ? OnPrimary : Text;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? Primary : Border;
            button.FlatAppearance.MouseOverBackColor = primary ? PrimaryHover : HoverBack;
            button.FlatAppearance.MouseDownBackColor = primary ? PrimaryHover : Selection;
        }

        /// <summary>Nhóm màu của nhãn trạng thái hóa đơn. Không rõ trạng thái coi là nguy hiểm (không được tính thuế).</summary>
        public static ChipKind ChipKindOf(InvoiceRelation relation) => relation switch
        {
            InvoiceRelation.Goc => ChipKind.Ok,
            InvoiceRelation.DieuChinh or InvoiceRelation.BiDieuChinh => ChipKind.Warning,
            InvoiceRelation.ThayThe => ChipKind.Info,
            _ => ChipKind.Danger
        };

        /// <summary>Nhãn rủi ro NCC: không kiểm tra được là cảnh báo, không bao giờ coi là an toàn.</summary>
        public static ChipKind ChipKindOf(RiskStatus status) => status switch
        {
            RiskStatus.Safe => ChipKind.Ok,
            RiskStatus.Risky => ChipKind.Danger,
            _ => ChipKind.Warning
        };

        /// <summary>Vẽ nhãn bo tròn (chip) căn trái giữa ô, chữ đậm nhỏ.</summary>
        public static void DrawChip(Graphics g, Rectangle cell, string text, ChipKind kind)
        {
            var (fore, back) = ChipColors(kind);
            var size = TextRenderer.MeasureText(g, text, ChipFont, Size.Empty, TextFormatFlags.NoPadding);
            int h = Math.Min(20, cell.Height - 6);
            var rect = new Rectangle(cell.X + 6, cell.Y + (cell.Height - h) / 2, Math.Min(size.Width + 16, cell.Width - 10), h);
            if (rect.Width <= 0 || rect.Height <= 0) return;
            var old = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = RoundRect(rect, h / 2))
            using (var brush = new SolidBrush(back))
                g.FillPath(brush, path);
            g.SmoothingMode = old;
            TextRenderer.DrawText(g, text, ChipFont, rect, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private static readonly Font ChipFont = new("Segoe UI", 8.25F, FontStyle.Bold);

        private static System.Drawing.Drawing2D.GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>Viền bo góc 1px (ô nhập, thẻ số liệu, khung bảng).</summary>
        public static void DrawRoundBorder(Graphics g, Rectangle r, int radius, Color color)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            var old = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = RoundRect(r, radius))
            using (var pen = new Pen(color))
                g.DrawPath(pen, path);
            g.SmoothingMode = old;
        }

        public static (Color Fore, Color Back) ChipColors(ChipKind kind) => kind switch
        {
            ChipKind.Ok => (Color.FromArgb(0x0E, 0x6B, 0x0E), Color.FromArgb(0xDF, 0xF6, 0xDD)),
            ChipKind.Warning => (Color.FromArgb(0x7A, 0x4F, 0x00), Color.FromArgb(0xFF, 0xF4, 0xCE)),
            ChipKind.Info => (Primary, Color.FromArgb(0xDE, 0xEC, 0xF9)),
            _ => (Color.FromArgb(0xA8, 0x00, 0x00), Color.FromArgb(0xFD, 0xE7, 0xE9))
        };

        /// <summary>Tỉ lệ tương phản WCAG 2.x giữa hai màu (1–21).</summary>
        public static double ContrastRatio(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color c)
        {
            static double Channel(int v)
            {
                double s = v / 255.0;
                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }
    }
}
