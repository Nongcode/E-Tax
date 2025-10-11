using System.Drawing;
using System.Windows.Forms;

namespace E_Tax
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // ==== Các control được khai báo ở đây ====
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.TextBox txtPass;
        private System.Windows.Forms.TextBox txtCaptcha;
        private System.Windows.Forms.PictureBox picCaptcha;
        private System.Windows.Forms.Button btnRefreshCaptcha;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnExportList;
        private System.Windows.Forms.Button btnExportDetails;
        private System.Windows.Forms.Button btnSaveOriginal;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Panel panelLogin;
        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.Label lblVersion;

        /// <summary>
        /// Khởi tạo các control trên form.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolTip = new ToolTip(components);
            txtUser = new TextBox();
            txtPass = new TextBox();
            txtCaptcha = new TextBox();
            btnRefreshCaptcha = new Button();
            btnLogin = new Button();
            txtSearch = new TextBox();
            btnSearch = new Button();
            btnExportList = new Button();
            btnExportDetails = new Button();
            btnSaveOriginal = new Button();
            panelLogin = new Panel();
            picCaptcha = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            label4 = new Label();
            panelSearch = new Panel();
            panelActivation = new Panel();
            label6 = new Label();
            txtActivationKey = new TextBox();
            label5 = new Label();
            label3 = new Label();
            lblVersion = new Label();
            btnActivate = new Button();
            panelLogin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picCaptcha).BeginInit();
            panelSearch.SuspendLayout();
            panelActivation.SuspendLayout();
            SuspendLayout();
            // 
            // txtUser
            // 
            txtUser.Location = new Point(109, 29);
            txtUser.Name = "txtUser";
            txtUser.PlaceholderText = "Nhập tên đăng nhập ...";
            txtUser.Size = new Size(200, 23);
            txtUser.TabIndex = 3;
            toolTip.SetToolTip(txtUser, "Nhập tên đăng nhập.");
            // 
            // txtPass
            // 
            txtPass.Location = new Point(109, 77);
            txtPass.Name = "txtPass";
            txtPass.PasswordChar = '*';
            txtPass.PlaceholderText = "Nhập mật khẩu...";
            txtPass.Size = new Size(200, 23);
            txtPass.TabIndex = 4;
            toolTip.SetToolTip(txtPass, "Nhập mật khẩu.");
            // 
            // txtCaptcha
            // 
            txtCaptcha.Location = new Point(109, 125);
            txtCaptcha.Name = "txtCaptcha";
            txtCaptcha.PlaceholderText = "Nhập mã bên cạnh...";
            txtCaptcha.Size = new Size(120, 23);
            txtCaptcha.TabIndex = 5;
            toolTip.SetToolTip(txtCaptcha, "Nhập mã captcha hiển thị bên cạnh.");
            // 
            // btnRefreshCaptcha
            // 
            btnRefreshCaptcha.BackColor = SystemColors.ButtonShadow;
            btnRefreshCaptcha.Location = new Point(269, 167);
            btnRefreshCaptcha.Name = "btnRefreshCaptcha";
            btnRefreshCaptcha.Size = new Size(60, 23);
            btnRefreshCaptcha.TabIndex = 7;
            btnRefreshCaptcha.Text = "↻";
            toolTip.SetToolTip(btnRefreshCaptcha, "Làm mới mã captcha.");
            btnRefreshCaptcha.UseVisualStyleBackColor = false;
            btnRefreshCaptcha.Click += btnRefreshCaptcha_Click;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = SystemColors.ActiveCaption;
            btnLogin.Location = new Point(29, 167);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(200, 30);
            btnLogin.TabIndex = 8;
            btnLogin.Text = "Login";
            toolTip.SetToolTip(btnLogin, "Đăng nhập vào hệ thống.");
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(106, 43);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Nhập từ khóa...";
            txtSearch.Size = new Size(200, 23);
            txtSearch.TabIndex = 9;
            toolTip.SetToolTip(txtSearch, "Nhập từ khóa hoặc ngày (dd/MM/yyyy) để tìm kiếm.");
            // 
            // btnSearch
            // 
            btnSearch.BackColor = SystemColors.ActiveCaption;
            btnSearch.Location = new Point(106, 95);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(200, 30);
            btnSearch.TabIndex = 10;
            btnSearch.Text = "Search Product";
            toolTip.SetToolTip(btnSearch, "Tìm kiếm hóa đơn theo từ khóa hoặc ngày.");
            btnSearch.UseVisualStyleBackColor = false;
            btnSearch.Click += btnSearch_Click;
            // 
            // btnExportList
            // 
            btnExportList.BackColor = SystemColors.GradientInactiveCaption;
            btnExportList.Location = new Point(106, 191);
            btnExportList.Name = "btnExportList";
            btnExportList.Size = new Size(200, 30);
            btnExportList.TabIndex = 0;
            btnExportList.Text = "Xuất DS Hóa đơn";
            toolTip.SetToolTip(btnExportList, "Xuất danh sách hóa đơn ra file Excel.");
            btnExportList.UseVisualStyleBackColor = false;
            btnExportList.Click += btnExportList_Click;
            // 
            // btnExportDetails
            // 
            btnExportDetails.BackColor = SystemColors.GradientInactiveCaption;
            btnExportDetails.Location = new Point(106, 142);
            btnExportDetails.Name = "btnExportDetails";
            btnExportDetails.Size = new Size(200, 30);
            btnExportDetails.TabIndex = 1;
            btnExportDetails.Text = "Xuất Chi tiết Hóa đơn";
            toolTip.SetToolTip(btnExportDetails, "Xuất chi tiết hóa đơn ra file Excel.");
            btnExportDetails.UseVisualStyleBackColor = false;
            btnExportDetails.Click += btnExportDetails_Click;
            // 
            // btnSaveOriginal
            // 
            btnSaveOriginal.BackColor = SystemColors.GradientInactiveCaption;
            btnSaveOriginal.Location = new Point(106, 240);
            btnSaveOriginal.Name = "btnSaveOriginal";
            btnSaveOriginal.Size = new Size(200, 30);
            btnSaveOriginal.TabIndex = 2;
            btnSaveOriginal.Text = "Lưu Hóa đơn Gốc";
            toolTip.SetToolTip(btnSaveOriginal, "Lưu hóa đơn gốc dưới dạng HTML và XML.");
            btnSaveOriginal.UseVisualStyleBackColor = false;
            btnSaveOriginal.Click += btnSaveOriginal_Click;
            // 
            // panelLogin
            // 
            panelLogin.Controls.Add(txtUser);
            panelLogin.Controls.Add(txtPass);
            panelLogin.Controls.Add(txtCaptcha);
            panelLogin.Controls.Add(picCaptcha);
            panelLogin.Controls.Add(btnRefreshCaptcha);
            panelLogin.Controls.Add(btnLogin);
            panelLogin.Controls.Add(label1);
            panelLogin.Controls.Add(label2);
            panelLogin.Controls.Add(label4);
            panelLogin.Location = new Point(0, 0);
            panelLogin.Name = "panelLogin";
            panelLogin.Size = new Size(460, 293);
            panelLogin.TabIndex = 0;
            // 
            // picCaptcha
            // 
            picCaptcha.BorderStyle = BorderStyle.FixedSingle;
            picCaptcha.Location = new Point(250, 111);
            picCaptcha.Name = "picCaptcha";
            picCaptcha.Size = new Size(100, 40);
            picCaptcha.SizeMode = PictureBoxSizeMode.StretchImage;
            picCaptcha.TabIndex = 6;
            picCaptcha.TabStop = false;
            // 
            // label1
            // 
            label1.BackColor = SystemColors.Control;
            label1.Location = new Point(3, 29);
            label1.Name = "label1";
            label1.Size = new Size(100, 23);
            label1.TabIndex = 13;
            label1.Text = "Username:";
            // 
            // label2
            // 
            label2.Location = new Point(3, 80);
            label2.Name = "label2";
            label2.Size = new Size(100, 23);
            label2.TabIndex = 14;
            label2.Text = "Password:";
            // 
            // label4
            // 
            label4.Location = new Point(3, 128);
            label4.Name = "label4";
            label4.Size = new Size(100, 23);
            label4.TabIndex = 16;
            label4.Text = "Captcha:";
            // 
            // panelSearch
            // 
            panelSearch.Controls.Add(panelActivation);
            panelSearch.Controls.Add(panelLogin);
            panelSearch.Controls.Add(txtSearch);
            panelSearch.Controls.Add(btnSearch);
            panelSearch.Controls.Add(btnExportList);
            panelSearch.Controls.Add(btnExportDetails);
            panelSearch.Controls.Add(btnSaveOriginal);
            panelSearch.Controls.Add(label3);
            panelSearch.Location = new Point(20, 20);
            panelSearch.Name = "panelSearch";
            panelSearch.Size = new Size(460, 560);
            panelSearch.TabIndex = 1;
            // 
            // panelActivation
            // 
            panelActivation.Controls.Add(btnActivate);
            panelActivation.Controls.Add(label6);
            panelActivation.Controls.Add(txtActivationKey);
            panelActivation.Controls.Add(label5);
            panelActivation.Location = new Point(29, 310);
            panelActivation.Name = "panelActivation";
            panelActivation.Size = new Size(394, 100);
            panelActivation.TabIndex = 17;
            panelActivation.Visible = false;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(21, 41);
            label6.Name = "label6";
            label6.Size = new Size(29, 15);
            label6.TabIndex = 2;
            label6.Text = "OTP";
            // 
            // txtActivationKey
            // 
            txtActivationKey.Location = new Point(77, 38);
            txtActivationKey.Name = "txtActivationKey";
            txtActivationKey.PlaceholderText = "Nhập mã kích hoạt";
            txtActivationKey.Size = new Size(200, 23);
            txtActivationKey.TabIndex = 1;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(21, 10);
            label5.Name = "label5";
            label5.Size = new Size(356, 15);
            label5.TabIndex = 0;
            label5.Text = "Bản dùng thử đã hết hạn. Vui lòng liên hệ chủ sở hữu để kích hoạt";
            // 
            // label3
            // 
            label3.Location = new Point(3, 46);
            label3.Name = "label3";
            label3.Size = new Size(100, 23);
            label3.TabIndex = 15;
            label3.Text = "Search (Keyword/Date):";
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Location = new Point(10, 5);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(217, 15);
            lblVersion.TabIndex = 17;
            lblVersion.Text = "Phiên bản dùng thử trong vòng 1 tháng";
            // 
            // btnActivate
            // 
            btnActivate.Location = new Point(302, 38);
            btnActivate.Name = "btnActivate";
            btnActivate.Size = new Size(75, 23);
            btnActivate.TabIndex = 3;
            btnActivate.Text = "Add OTP";
            btnActivate.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            ClientSize = new Size(500, 600);
            Controls.Add(panelSearch);
            Controls.Add(lblVersion);
            Name = "Form1";
            Text = "WinForms API Demo";
            Load += Form1_Load;
            panelLogin.ResumeLayout(false);
            panelLogin.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picCaptcha).EndInit();
            panelSearch.ResumeLayout(false);
            panelSearch.PerformLayout();
            panelActivation.ResumeLayout(false);
            panelActivation.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private Panel panelActivation;
        private TextBox txtActivationKey;
        private Label label5;
        private Label label6;
        private Button btnActivate;
    }
}