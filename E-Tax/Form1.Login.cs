using System.Drawing;
using System.Windows.Forms;

namespace E_Tax
{
    /// <summary>
    /// Màn đăng nhập dạng thẻ (Phương án 3): dải xanh trên cùng, nhãn trên mỗi ô, ô viền mảnh cùng chiều rộng với nút.
    /// Chỉ chuyển control Designer sang khung mới và áp style; luồng đăng nhập/kích hoạt giữ nguyên.
    /// </summary>
    public partial class Form1
    {
        private const int LoginCardWidth = 440;
        private const int LoginContentWidth = 392;

        private Panel? _loginHeader;
        private FlowLayoutPanel? _loginBody;

        private void ApplyLoginLayout()
        {
            panelLogin.SuspendLayout();

            var header = new Panel { Name = "loginHeader", Dock = DockStyle.Top, Height = 72, BackColor = UiTheme.Primary, Padding = new Padding(24, 14, 24, 0) };
            lblLoginTitle.Text = "E-Tax";
            lblLoginTitle.Font = UiTheme.TitleFont;
            lblLoginTitle.ForeColor = UiTheme.OnPrimary;
            lblLoginTitle.BackColor = UiTheme.Primary;
            lblLoginTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblLoginTitle.AutoSize = true;
            lblLoginTitle.Location = new Point(24, 12);
            lblLoginSubtitle.Text = "Đăng nhập cổng hóa đơn điện tử (hoadondientu.gdt.gov.vn)";
            lblLoginSubtitle.Font = UiTheme.BaseFont;
            lblLoginSubtitle.ForeColor = Color.FromArgb(0xDE, 0xEC, 0xF9);
            lblLoginSubtitle.BackColor = UiTheme.Primary;
            lblLoginSubtitle.TextAlign = ContentAlignment.MiddleLeft;
            lblLoginSubtitle.AutoSize = true;
            lblLoginSubtitle.Location = new Point(26, 44);
            header.Controls.Add(lblLoginTitle);
            header.Controls.Add(lblLoginSubtitle);

            var body = new FlowLayoutPanel
            {
                Name = "loginBody",
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(24, 18, 24, 20),
                BackColor = UiTheme.Surface
            };

            LoginInput(txtUser, "Nhập mã số thuế hoặc tài khoản");
            LoginInput(txtPass, "Nhập mật khẩu");
            LoginInput(txtCaptcha, "Nhập các ký tự trong ảnh");

            picCaptcha.Size = new Size(LoginContentWidth - 56, 52);
            picCaptcha.BorderStyle = BorderStyle.FixedSingle;
            picCaptcha.Margin = Padding.Empty;
            UiTheme.StyleButton(btnRefreshCaptcha, primary: false);
            btnRefreshCaptcha.Text = "";
            btnRefreshCaptcha.Image = RibbonIcons.Get("refresh", 20, UiTheme.Primary);
            btnRefreshCaptcha.Size = new Size(52, 52);
            btnRefreshCaptcha.Margin = new Padding(4, 0, 0, 0);
            _ribbonTips.SetToolTip(btnRefreshCaptcha, "Lấy mã xác nhận khác");
            var captchaRow = new FlowLayoutPanel
            {
                Name = "captchaRow",
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 6),
                BackColor = UiTheme.Surface
            };
            captchaRow.Controls.Add(picCaptcha);
            captchaRow.Controls.Add(btnRefreshCaptcha);

            chkKeepLogin.Font = UiTheme.BaseFont;
            chkKeepLogin.ForeColor = UiTheme.Text;
            chkKeepLogin.Margin = new Padding(0, 4, 0, 12);

            UiTheme.StyleButton(btnLogin, primary: true);
            btnLogin.Text = "Đăng nhập";
            btnLogin.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnLogin.Size = new Size(LoginContentWidth, 40);
            btnLogin.Margin = Padding.Empty;

            var hint = new Label
            {
                Name = "lblLoginHint",
                Text = "App không lưu mật khẩu.",
                AutoSize = false,
                Size = new Size(LoginContentWidth, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = UiTheme.SmallFont,
                ForeColor = UiTheme.MutedText,
                Margin = new Padding(0, 8, 0, 0)
            };

            StyleActivation();

            body.Controls.AddRange(new Control[]
            {
                InputLabel("Mã số thuế / Tài khoản"), txtUser,
                InputLabel("Mật khẩu"), txtPass,
                InputLabel("Mã xác nhận"), captchaRow, txtCaptcha,
                chkKeepLogin, btnLogin, hint, panelActivation
            });

            // Bỏ các gạch chân kiểu cũ (panel1–3) và vị trí tuyệt đối; thẻ = dải tiêu đề + thân
            foreach (var old in new Control[] { panel1, panel2, panel3 })
            {
                panelLogin.Controls.Remove(old);
                old.Dispose();
            }
            panelLogin.Controls.Clear();
            panelLogin.Controls.Add(body);
            panelLogin.Controls.Add(header); // thêm sau → dock trước → nằm trên cùng
            panelLogin.Width = LoginCardWidth;
            panelLogin.BackColor = UiTheme.Surface;
            panelLogin.BorderStyle = BorderStyle.FixedSingle;
            panelLogin.Anchor = AnchorStyles.None;
            _loginHeader = header;
            _loginBody = body;

            BackColor = UiTheme.AppBack;
            lblVersion.BackColor = UiTheme.AppBack;
            lblVersion.ForeColor = UiTheme.MutedText;
            lblVersion.Font = UiTheme.BaseFont;

            // Enter để đăng nhập, chỉ khi đang ở màn đăng nhập (ô nhập giờ là một dòng)
            panelLogin.VisibleChanged += (_, _) => AcceptButton = panelLogin.Visible ? btnLogin : null;
            AcceptButton = btnLogin;

            panelLogin.ResumeLayout(true);
        }

        /// <summary>Chiều cao thẻ đăng nhập theo nội dung hiện có (khối kích hoạt ẩn/hiện).</summary>
        private int LoginCardHeight() =>
            (_loginHeader?.Height ?? 0) + (_loginBody?.GetPreferredSize(new Size(LoginCardWidth, 0)).Height ?? 0) + 2;

        private static void LoginInput(TextBox box, string placeholder)
        {
            box.Multiline = false;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.Font = new Font("Segoe UI", 10.5F);
            box.ForeColor = UiTheme.Text;
            box.PlaceholderText = placeholder;
            box.Width = LoginContentWidth;
            box.Margin = new Padding(0, 0, 0, 12);
        }

        private static Label InputLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 0, 0, 4)
        };

        /// <summary>Khối kích hoạt bản quyền: giữ bố cục bên trong, chỉ đổi màu/nút cho cùng kiểu với thẻ.</summary>
        private void StyleActivation()
        {
            const int pad = 12, labelW = 92, buttonW = 84, rowH = 28;
            int inputW = LoginContentWidth - pad * 2 - labelW - buttonW - 8;
            panelActivation.Width = LoginContentWidth;
            panelActivation.Height = 124;
            panelActivation.Margin = new Padding(0, 12, 0, 0);
            panelActivation.BackColor = Color.FromArgb(0xFD, 0xE7, 0xE9);
            panelActivation.Padding = Padding.Empty;
            panelActivation.Controls.Remove(panel4); // gạch chân kiểu cũ
            panel4.Dispose();

            label5.Text = "Bản dùng thử đã hết hạn. Nhập mã kích hoạt hoặc gửi mã máy cho người bán để nhận mã.";
            label5.AutoSize = false;
            label5.Font = UiTheme.BoldFont;
            label5.ForeColor = Color.FromArgb(0xA8, 0x00, 0x00);
            label5.Bounds = new Rectangle(pad, 10, LoginContentWidth - pad * 2, 34);

            foreach (var (label, text, row) in new[] { (label6, "Mã kích hoạt", 0), (lblMachineId, "Mã máy", 1) })
            {
                label.Text = text;
                label.AutoSize = false;
                label.Font = UiTheme.LabelFont;
                label.ForeColor = UiTheme.Text;
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.Bounds = new Rectangle(pad, 50 + row * (rowH + 8), labelW, rowH);
            }
            foreach (var (box, row) in new[] { (txtActivationKey, 0), (txtMachineId, 1) })
            {
                box.BorderStyle = BorderStyle.FixedSingle;
                box.Font = row == 1 ? new Font("Consolas", 10F) : UiTheme.BaseFont;
                box.Bounds = new Rectangle(pad + labelW, 50 + row * (rowH + 8) + 2, inputW, 24);
            }
            foreach (var (button, primary, row) in new[] { (btnActivate, true, 0), (btnCopyMachineId, false, 1) })
            {
                UiTheme.StyleButton(button, primary);
                button.Bounds = new Rectangle(LoginContentWidth - pad - buttonW, 50 + row * (rowH + 8), buttonW, rowH);
            }
        }
    }
}
