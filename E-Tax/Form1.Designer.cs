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
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnExportList;
        private System.Windows.Forms.Button btnExportDetails;
        private System.Windows.Forms.Button btnSaveOriginal;
        private System.Windows.Forms.Panel panelLogin;
        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.Label lblVersion;

        /// <summary>
        /// Khởi tạo các control trên form.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            txtUser = new TextBox();
            txtPass = new TextBox();
            txtCaptcha = new TextBox();
            btnRefreshCaptcha = new Button();
            btnLogin = new Button();
            btnSearch = new Button();
            btnSaveOriginal = new Button();
            panelLogin = new Panel();
            pictureBox1 = new PictureBox();
            panel3 = new Panel();
            panelActivation = new Panel();
            panel4 = new Panel();
            btnActivate = new Button();
            label6 = new Label();
            txtActivationKey = new TextBox();
            label5 = new Label();
            panel2 = new Panel();
            panel1 = new Panel();
            picCaptcha = new PictureBox();
            panelSearch = new Panel();
            searchProgressBar = new ProgressBar();
            downloadProgressBar = new ProgressBar();
            lblDownloadStatus = new Label();
            dtpToDate = new DateTimePicker();
            dtpFromDate = new DateTimePicker();
            label2 = new Label();
            label1 = new Label();
            pictureBox2 = new PictureBox();
            lblVersion = new Label();
            txtResult = new TextBox();
            panelLogin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panelActivation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picCaptcha).BeginInit();
            panelSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            SuspendLayout();
            // 
            // txtUser
            // 
            txtUser.BorderStyle = BorderStyle.None;
            txtUser.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            txtUser.ForeColor = Color.FromArgb(0, 117, 214);
            txtUser.Location = new Point(60, 151);
            txtUser.Multiline = true;
            txtUser.Name = "txtUser";
            txtUser.PlaceholderText = "Nhập tên đăng nhập ...";
            txtUser.Size = new Size(344, 34);
            txtUser.TabIndex = 3;
            // 
            // txtPass
            // 
            txtPass.BorderStyle = BorderStyle.None;
            txtPass.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            txtPass.ForeColor = Color.FromArgb(0, 117, 214);
            txtPass.Location = new Point(61, 205);
            txtPass.Multiline = true;
            txtPass.Name = "txtPass";
            txtPass.PasswordChar = '*';
            txtPass.PlaceholderText = "Nhập mật khẩu...";
            txtPass.Size = new Size(350, 36);
            txtPass.TabIndex = 4;
            // 
            // txtCaptcha
            // 
            txtCaptcha.BorderStyle = BorderStyle.None;
            txtCaptcha.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            txtCaptcha.ForeColor = Color.FromArgb(0, 117, 214);
            txtCaptcha.Location = new Point(61, 261);
            txtCaptcha.Multiline = true;
            txtCaptcha.Name = "txtCaptcha";
            txtCaptcha.PlaceholderText = "Nhập mã Capcha...";
            txtCaptcha.Size = new Size(356, 30);
            txtCaptcha.TabIndex = 5;
            // 
            // btnRefreshCaptcha
            // 
            btnRefreshCaptcha.BackColor = Color.Transparent;
            btnRefreshCaptcha.Font = new Font("Segoe UI", 27.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRefreshCaptcha.ForeColor = Color.FromArgb(0, 117, 214);
            btnRefreshCaptcha.Location = new Point(350, 311);
            btnRefreshCaptcha.Name = "btnRefreshCaptcha";
            btnRefreshCaptcha.Size = new Size(67, 66);
            btnRefreshCaptcha.TabIndex = 7;
            btnRefreshCaptcha.Text = "↻";
            btnRefreshCaptcha.UseVisualStyleBackColor = false;
            btnRefreshCaptcha.Click += btnRefreshCaptcha_Click;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(0, 117, 214);
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI Emoji", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(67, 397);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(350, 54);
            btnLogin.TabIndex = 8;
            btnLogin.Text = "ĐĂNG NHẬP";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            // 
            // btnSearch
            // 
            btnSearch.BackColor = SystemColors.GradientInactiveCaption;
            btnSearch.Location = new Point(54, 311);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(350, 48);
            btnSearch.TabIndex = 10;
            btnSearch.Text = "Tìm kiếm hóa đơn";
            btnSearch.UseVisualStyleBackColor = false;
            btnSearch.Click += btnSearch_Click;
            // 
            // btnSaveOriginal
            // 
            btnSaveOriginal.BackColor = SystemColors.GradientInactiveCaption;
            btnSaveOriginal.Location = new Point(54, 407);
            btnSaveOriginal.Name = "btnSaveOriginal";
            btnSaveOriginal.Size = new Size(350, 44);
            btnSaveOriginal.TabIndex = 2;
            btnSaveOriginal.Text = "Lưu Hóa đơn Tổng Hợp";
            btnSaveOriginal.UseVisualStyleBackColor = false;
            btnSaveOriginal.Click += btnSaveOriginal_Click;
            // 
            // panelLogin
            // 
            panelLogin.Controls.Add(pictureBox1);
            panelLogin.Controls.Add(panel3);
            panelLogin.Controls.Add(panelActivation);
            panelLogin.Controls.Add(panel2);
            panelLogin.Controls.Add(panel1);
            panelLogin.Controls.Add(txtUser);
            panelLogin.Controls.Add(txtPass);
            panelLogin.Controls.Add(txtCaptcha);
            panelLogin.Controls.Add(picCaptcha);
            panelLogin.Controls.Add(btnRefreshCaptcha);
            panelLogin.Controls.Add(btnLogin);
            panelLogin.Location = new Point(10, 29);
            panelLogin.Name = "panelLogin";
            panelLogin.Size = new Size(471, 649);
            panelLogin.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(162, 18);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(134, 107);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 20;
            pictureBox1.TabStop = false;
            // 
            // panel3
            // 
            panel3.BackColor = Color.FromArgb(0, 117, 214);
            panel3.Location = new Point(67, 290);
            panel3.Name = "panel3";
            panel3.Size = new Size(350, 1);
            panel3.TabIndex = 19;
            // 
            // panelActivation
            // 
            panelActivation.Controls.Add(panel4);
            panelActivation.Controls.Add(btnActivate);
            panelActivation.Controls.Add(label6);
            panelActivation.Controls.Add(txtActivationKey);
            panelActivation.Controls.Add(label5);
            panelActivation.Font = new Font("Segoe UI", 9.75F, FontStyle.Italic, GraphicsUnit.Point, 0);
            panelActivation.Location = new Point(17, 467);
            panelActivation.Name = "panelActivation";
            panelActivation.Size = new Size(439, 142);
            panelActivation.TabIndex = 17;
            panelActivation.Visible = false;
            // 
            // panel4
            // 
            panel4.BackColor = Color.FromArgb(0, 117, 214);
            panel4.Location = new Point(64, 67);
            panel4.Name = "panel4";
            panel4.Size = new Size(236, 1);
            panel4.TabIndex = 19;
            // 
            // btnActivate
            // 
            btnActivate.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnActivate.ForeColor = Color.FromArgb(0, 117, 214);
            btnActivate.Location = new Point(320, 34);
            btnActivate.Name = "btnActivate";
            btnActivate.Size = new Size(75, 38);
            btnActivate.TabIndex = 3;
            btnActivate.Text = "Kích hoạt";
            btnActivate.UseVisualStyleBackColor = true;
            btnActivate.Click += btnActivate_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.ForeColor = Color.FromArgb(0, 117, 214);
            label6.Location = new Point(21, 41);
            label6.Name = "label6";
            label6.Size = new Size(34, 21);
            label6.TabIndex = 2;
            label6.Text = "Mã";
            // 
            // txtActivationKey
            // 
            txtActivationKey.Location = new Point(64, 42);
            txtActivationKey.Multiline = true;
            txtActivationKey.Name = "txtActivationKey";
            txtActivationKey.PlaceholderText = "Nhập mã kích hoạt";
            txtActivationKey.Size = new Size(232, 23);
            txtActivationKey.TabIndex = 1;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label5.Location = new Point(3, 0);
            label5.Name = "label5";
            label5.Size = new Size(377, 21);
            label5.TabIndex = 0;
            label5.Text = "Bản dùng thử đã hết hạn. Vui lòng liên hệ chủ sở hữu";
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(0, 117, 214);
            panel2.Location = new Point(61, 240);
            panel2.Name = "panel2";
            panel2.Size = new Size(350, 1);
            panel2.TabIndex = 18;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(0, 117, 214);
            panel1.Location = new Point(60, 184);
            panel1.Name = "panel1";
            panel1.Size = new Size(350, 1);
            panel1.TabIndex = 17;
            // 
            // picCaptcha
            // 
            picCaptcha.BorderStyle = BorderStyle.FixedSingle;
            picCaptcha.Location = new Point(67, 311);
            picCaptcha.Name = "picCaptcha";
            picCaptcha.Size = new Size(280, 66);
            picCaptcha.SizeMode = PictureBoxSizeMode.StretchImage;
            picCaptcha.TabIndex = 6;
            picCaptcha.TabStop = false;
            // 
            // panelSearch
            // 
            panelSearch.Controls.Add(txtResult);
            panelSearch.Controls.Add(searchProgressBar);
            panelSearch.Controls.Add(downloadProgressBar);
            panelSearch.Controls.Add(lblDownloadStatus);
            panelSearch.Controls.Add(dtpToDate);
            panelSearch.Controls.Add(dtpFromDate);
            panelSearch.Controls.Add(label2);
            panelSearch.Controls.Add(label1);
            panelSearch.Controls.Add(pictureBox2);
            panelSearch.Controls.Add(btnSearch);
            panelSearch.Controls.Add(btnSaveOriginal);
            panelSearch.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            panelSearch.ForeColor = Color.FromArgb(0, 117, 214);
            panelSearch.Location = new Point(10, 29);
            panelSearch.Name = "panelSearch";
            panelSearch.Size = new Size(462, 655);
            panelSearch.TabIndex = 1;
            // 
            // searchProgressBar
            // 
            searchProgressBar.Location = new Point(54, 360);
            searchProgressBar.MarqueeAnimationSpeed = 30;
            searchProgressBar.Name = "searchProgressBar";
            searchProgressBar.Size = new Size(350, 15);
            searchProgressBar.Style = ProgressBarStyle.Marquee;
            searchProgressBar.TabIndex = 26;
            searchProgressBar.Visible = false;
            // 
            // downloadProgressBar
            // 
            downloadProgressBar.Location = new Point(54, 457);
            downloadProgressBar.Name = "downloadProgressBar";
            downloadProgressBar.Size = new Size(350, 23);
            downloadProgressBar.TabIndex = 27;
            downloadProgressBar.Visible = false;
            // 
            // lblDownloadStatus
            // 
            lblDownloadStatus.AutoSize = true;
            lblDownloadStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDownloadStatus.Location = new Point(54, 493);
            lblDownloadStatus.Name = "lblDownloadStatus";
            lblDownloadStatus.Size = new Size(80, 15);
            lblDownloadStatus.TabIndex = 28;
            lblDownloadStatus.Text = "Đang tải 0/0...";
            lblDownloadStatus.Visible = false;
            // 
            // dtpToDate
            // 
            dtpToDate.CustomFormat = "dd/MM/yyyy";
            dtpToDate.Font = new Font("Segoe UI", 11.25F, FontStyle.Italic, GraphicsUnit.Point, 0);
            dtpToDate.Format = DateTimePickerFormat.Custom;
            dtpToDate.Location = new Point(123, 240);
            dtpToDate.Name = "dtpToDate";
            dtpToDate.Size = new Size(281, 27);
            dtpToDate.TabIndex = 25;
            // 
            // dtpFromDate
            // 
            dtpFromDate.CustomFormat = "dd/MM/yyyy";
            dtpFromDate.Font = new Font("Segoe UI", 11.25F, FontStyle.Italic, GraphicsUnit.Point, 0);
            dtpFromDate.Format = DateTimePickerFormat.Custom;
            dtpFromDate.Location = new Point(123, 160);
            dtpFromDate.Name = "dtpFromDate";
            dtpFromDate.Size = new Size(281, 27);
            dtpFromDate.TabIndex = 24;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(20, 246);
            label2.Name = "label2";
            label2.Size = new Size(77, 21);
            label2.TabIndex = 23;
            label2.Text = "Đến ngày";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(31, 168);
            label1.Name = "label1";
            label1.Size = new Size(66, 21);
            label1.TabIndex = 22;
            label1.Text = "Từ ngày";
            // 
            // pictureBox2
            // 
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(161, 16);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(134, 107);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 21;
            pictureBox2.TabStop = false;
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblVersion.ForeColor = Color.FromArgb(0, 117, 214);
            lblVersion.Location = new Point(10, 5);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(286, 21);
            lblVersion.TabIndex = 17;
            lblVersion.Text = "Phiên bản dùng thử trong vòng 1 tháng";
            // 
            // txtResult
            // 
            txtResult.Dock = DockStyle.Bottom;
            txtResult.Location = new Point(0, 558);
            txtResult.Multiline = true;
            txtResult.Name = "txtResult";
            txtResult.ReadOnly = true;
            txtResult.ScrollBars = ScrollBars.Vertical;
            txtResult.Size = new Size(462, 97);
            txtResult.TabIndex = 29;
            // 
            // Form1
            // 
            BackColor = Color.White;
            ClientSize = new Size(484, 690);
            Controls.Add(panelSearch);
            Controls.Add(lblVersion);
            Controls.Add(panelLogin);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "Form1";
            Text = "E-Tax Invoice Downloader";
            Load += Form1_Load;
            panelLogin.ResumeLayout(false);
            panelLogin.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panelActivation.ResumeLayout(false);
            panelActivation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picCaptcha).EndInit();
            panelSearch.ResumeLayout(false);
            panelSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Panel panelActivation;
        private TextBox txtActivationKey;
        private Label label5;
        private Label label6;
        private Button btnActivate;
        private Panel panel1;
        private Panel panel2;
        private Panel panel3;
        private Panel panel4;
        private PictureBox pictureBox1;
        private PictureBox pictureBox2;
        private Label label1;
        private Label label2;
        private DateTimePicker dtpToDate;
        private DateTimePicker dtpFromDate;
        private System.Windows.Forms.ProgressBar searchProgressBar;
        private System.Windows.Forms.ProgressBar downloadProgressBar;
        private System.Windows.Forms.Label lblDownloadStatus;
        private TextBox txtResult;
    }
}