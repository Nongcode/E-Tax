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
        private System.Windows.Forms.Panel panelLogin;
        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.DataGridView dgvDetails;
        private System.Windows.Forms.DataGridView dgvMua; // Lưới BK Mua
        private System.Windows.Forms.DataGridView dgvBan; // Lưới BK Bán

        /// <summary>
        /// Khởi tạo các control trên form.
        /// </summary>
        /// <summary>
        /// Khởi tạo các control trên form.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            txtUser = new TextBox();
            txtPass = new TextBox();
            txtCaptcha = new TextBox();
            btnRefreshCaptcha = new Button();
            btnLogin = new Button();
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
            mainSplitContainer = new SplitContainer();
            pictureBox3 = new PictureBox();
            lblKyBaoCao = new Label();
            txtThang = new TextBox();
            lblNam = new Label();
            txtNam = new TextBox();
            label1 = new Label();
            dtpFromDate = new DateTimePicker();
            label2 = new Label();
            dtpToDate = new DateTimePicker();
            btnTaiHDGoc = new Button();
            btnNenZip = new Button();
            btnCnKoPdf = new Button();
            btnXemHD = new Button();
            gbInvoiceType = new GroupBox();
            rbAllInvoices = new RadioButton();
            rbBought = new RadioButton();
            rbSold = new RadioButton();
            btnLeftSearch = new Button();
            btnExportChiTiet = new Button();
            btnExportDS = new Button();
            tabControlMain = new TabControl();
            tabTongHop = new TabPage();
            dgvMain = new DataGridView();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn11 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn13 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn14 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn15 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn16 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn17 = new DataGridViewTextBoxColumn();
            Tra_Cuu = new DataGridViewButtonColumn();
            dataGridViewTextBoxColumn18 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn19 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn20 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn21 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn22 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn23 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn24 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn25 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn26 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn27 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn28 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn29 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn30 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn31 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn32 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn33 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn34 = new DataGridViewTextBoxColumn();
            tabChiTiet = new TabPage();
            dgvDetails = new DataGridView();
            tabDkMua = new TabPage();
            dgvMua = new DataGridView();
            tabBKBan = new TabPage();
            dgvBan = new DataGridView();
            tabVATNop = new TabPage();
            tabGiamThue = new TabPage();
            gbNguoiBan = new GroupBox();
            btnKtraDNMuaLe = new Button();
            lblGhiChu = new Label();
            txtGhiChu = new TextBox();
            btnMoGhiChu = new Button();
            lblTimMST = new Label();
            txtTimMST = new TextBox();
            btnRightSearch = new Button();
            downloadProgressBar = new ProgressBar();
            lblDownloadStatus = new Label();
            pictureBox2 = new PictureBox();
            lblVersion = new Label();
            lblStatusMessage = new Label();
            statusTimer = new System.Windows.Forms.Timer(components);
            panelLogin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panelActivation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picCaptcha).BeginInit();
            panelSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            gbInvoiceType.SuspendLayout();
            tabControlMain.SuspendLayout();
            tabTongHop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMain).BeginInit();
            tabChiTiet.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDetails).BeginInit();
            tabDkMua.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMua).BeginInit();
            tabBKBan.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvBan).BeginInit();
            gbNguoiBan.SuspendLayout();
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
            txtUser.PlaceholderText = "Nhập tài khoản...";
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
            txtCaptcha.PlaceholderText = "Nhập mã bên dưới...";
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
            panelLogin.Location = new Point(289, 26);
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
            panelSearch.Controls.Add(mainSplitContainer);
            panelSearch.Controls.Add(downloadProgressBar);
            panelSearch.Controls.Add(lblDownloadStatus);
            panelSearch.Controls.Add(pictureBox2);
            panelSearch.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            panelSearch.ForeColor = Color.FromArgb(0, 117, 214);
            panelSearch.Location = new Point(10, 29);
            panelSearch.Name = "panelSearch";
            panelSearch.Size = new Size(962, 649);
            panelSearch.TabIndex = 1;
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.Location = new Point(0, 0);
            mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.Controls.Add(pictureBox3);
            mainSplitContainer.Panel1.Controls.Add(lblKyBaoCao);
            mainSplitContainer.Panel1.Controls.Add(txtThang);
            mainSplitContainer.Panel1.Controls.Add(lblNam);
            mainSplitContainer.Panel1.Controls.Add(txtNam);
            mainSplitContainer.Panel1.Controls.Add(label1);
            mainSplitContainer.Panel1.Controls.Add(dtpFromDate);
            mainSplitContainer.Panel1.Controls.Add(label2);
            mainSplitContainer.Panel1.Controls.Add(dtpToDate);
            mainSplitContainer.Panel1.Controls.Add(btnTaiHDGoc);
            mainSplitContainer.Panel1.Controls.Add(btnNenZip);
            mainSplitContainer.Panel1.Controls.Add(btnCnKoPdf);
            mainSplitContainer.Panel1.Controls.Add(btnXemHD);
            mainSplitContainer.Panel1.Controls.Add(gbInvoiceType);
            mainSplitContainer.Panel1.Controls.Add(btnLeftSearch);
            mainSplitContainer.Panel1.Controls.Add(btnExportChiTiet);
            mainSplitContainer.Panel1.Controls.Add(btnExportDS);
            mainSplitContainer.Panel1.Controls.Add(tabControlMain);
            mainSplitContainer.Panel1.Controls.Add(gbNguoiBan);
            mainSplitContainer.Panel1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            mainSplitContainer.Panel1.ForeColor = SystemColors.ControlText;
            mainSplitContainer.Panel2Collapsed = true;
            mainSplitContainer.Size = new Size(962, 649);
            mainSplitContainer.SplitterDistance = 650;
            mainSplitContainer.TabIndex = 30;
            // 
            // pictureBox3
            // 
            pictureBox3.Image = (Image)resources.GetObject("pictureBox3.Image");
            pictureBox3.Location = new Point(499, 10);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(76, 81);
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.TabIndex = 40;
            pictureBox3.TabStop = false;
            // 
            // lblKyBaoCao
            // 
            lblKyBaoCao.AutoSize = true;
            lblKyBaoCao.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblKyBaoCao.Location = new Point(12, 16);
            lblKyBaoCao.Name = "lblKyBaoCao";
            lblKyBaoCao.Size = new Size(73, 17);
            lblKyBaoCao.TabIndex = 30;
            lblKyBaoCao.Text = "Kỳ báo cáo";
            // 
            // txtThang
            // 
            txtThang.Location = new Point(12, 40);
            txtThang.Name = "txtThang";
            txtThang.PlaceholderText = "Tháng ";
            txtThang.Size = new Size(75, 23);
            txtThang.TabIndex = 31;
            txtThang.Leave += txtMonthYear_Leave;
            // 
            // lblNam
            // 
            lblNam.AutoSize = true;
            lblNam.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNam.Location = new Point(119, 16);
            lblNam.Name = "lblNam";
            lblNam.Size = new Size(36, 17);
            lblNam.TabIndex = 34;
            lblNam.Text = "Năm";
            // 
            // txtNam
            // 
            txtNam.Location = new Point(119, 40);
            txtNam.Name = "txtNam";
            txtNam.PlaceholderText = "Năm";
            txtNam.Size = new Size(77, 23);
            txtNam.TabIndex = 33;
            txtNam.Leave += txtMonthYear_Leave;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(12, 74);
            label1.Name = "label1";
            label1.Size = new Size(55, 17);
            label1.TabIndex = 22;
            label1.Text = "Từ ngày";
            // 
            // dtpFromDate
            // 
            dtpFromDate.CustomFormat = "dd/MM/yyyy";
            dtpFromDate.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dtpFromDate.Format = DateTimePickerFormat.Custom;
            dtpFromDate.Location = new Point(12, 94);
            dtpFromDate.Name = "dtpFromDate";
            dtpFromDate.Size = new Size(103, 23);
            dtpFromDate.TabIndex = 24;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.Location = new Point(133, 74);
            label2.Name = "label2";
            label2.Size = new Size(63, 17);
            label2.TabIndex = 23;
            label2.Text = "Đến ngày";
            // 
            // dtpToDate
            // 
            dtpToDate.CustomFormat = "dd/MM/yyyy";
            dtpToDate.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dtpToDate.Format = DateTimePickerFormat.Custom;
            dtpToDate.Location = new Point(133, 94);
            dtpToDate.Name = "dtpToDate";
            dtpToDate.Size = new Size(96, 23);
            dtpToDate.TabIndex = 25;
            // 
            // btnTaiHDGoc
            // 
            btnTaiHDGoc.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnTaiHDGoc.Location = new Point(508, 189);
            btnTaiHDGoc.Name = "btnTaiHDGoc";
            btnTaiHDGoc.Size = new Size(130, 30);
            btnTaiHDGoc.TabIndex = 10;
            btnTaiHDGoc.Text = "Tải tất cả HĐ Gốc";
            btnTaiHDGoc.UseVisualStyleBackColor = true;
            btnTaiHDGoc.Click += btnTaiHDGoc_Click;
            // 
            // btnNenZip
            // 
            btnNenZip.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnNenZip.Location = new Point(341, 189);
            btnNenZip.Name = "btnNenZip";
            btnNenZip.Size = new Size(76, 30);
            btnNenZip.TabIndex = 9;
            btnNenZip.Text = "Mở file zip";
            btnNenZip.UseVisualStyleBackColor = true;
            // 
            // btnCnKoPdf
            // 
            btnCnKoPdf.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnCnKoPdf.Location = new Point(423, 189);
            btnCnKoPdf.Name = "btnCnKoPdf";
            btnCnKoPdf.Size = new Size(76, 30);
            btnCnKoPdf.TabIndex = 8;
            btnCnKoPdf.Text = "In pdf";
            btnCnKoPdf.UseVisualStyleBackColor = true;
            // 
            // btnXemHD
            // 
            btnXemHD.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnXemHD.Location = new Point(16, 189);
            btnXemHD.Name = "btnXemHD";
            btnXemHD.Size = new Size(95, 30);
            btnXemHD.TabIndex = 7;
            btnXemHD.Text = "Xem HĐ";
            btnXemHD.UseVisualStyleBackColor = true;
            btnXemHD.Click += btnXemHD_Click;
            // 
            // gbInvoiceType
            // 
            gbInvoiceType.Controls.Add(rbAllInvoices);
            gbInvoiceType.Controls.Add(rbBought);
            gbInvoiceType.Controls.Add(rbSold);
            gbInvoiceType.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            gbInvoiceType.Location = new Point(16, 123);
            gbInvoiceType.Name = "gbInvoiceType";
            gbInvoiceType.Size = new Size(430, 60);
            gbInvoiceType.TabIndex = 29;
            gbInvoiceType.TabStop = false;
            gbInvoiceType.Text = "Chọn loại hóa đơn";
            // 
            // rbAllInvoices
            // 
            rbAllInvoices.AutoSize = true;
            rbAllInvoices.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbAllInvoices.Location = new Point(290, 25);
            rbAllInvoices.Name = "rbAllInvoices";
            rbAllInvoices.Size = new Size(104, 19);
            rbAllInvoices.TabIndex = 2;
            rbAllInvoices.Text = "Tất cả hoá đơn";
            rbAllInvoices.UseVisualStyleBackColor = true;
            // 
            // rbBought
            // 
            rbBought.AutoSize = true;
            rbBought.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbBought.Location = new Point(155, 25);
            rbBought.Name = "rbBought";
            rbBought.Size = new Size(120, 19);
            rbBought.TabIndex = 1;
            rbBought.Text = "Hóa đơn mua vào";
            rbBought.UseVisualStyleBackColor = true;
            // 
            // rbSold
            // 
            rbSold.AutoSize = true;
            rbSold.Checked = true;
            rbSold.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbSold.Location = new Point(20, 25);
            rbSold.Name = "rbSold";
            rbSold.Size = new Size(107, 19);
            rbSold.TabIndex = 0;
            rbSold.TabStop = true;
            rbSold.Text = "Hóa đơn bán ra";
            rbSold.UseVisualStyleBackColor = true;
            // 
            // btnLeftSearch
            // 
            btnLeftSearch.Location = new Point(452, 134);
            btnLeftSearch.Name = "btnLeftSearch";
            btnLeftSearch.Size = new Size(96, 49);
            btnLeftSearch.TabIndex = 35;
            btnLeftSearch.Text = "Tìm kiếm";
            btnLeftSearch.UseVisualStyleBackColor = true;
            btnLeftSearch.Click += btnLeftSearch_Click;
            // 
            // btnExportChiTiet
            // 
            btnExportChiTiet.Location = new Point(113, 189);
            btnExportChiTiet.Name = "btnExportChiTiet";
            btnExportChiTiet.Size = new Size(102, 30);
            btnExportChiTiet.TabIndex = 36;
            btnExportChiTiet.Text = "Tải chi tiết HĐ";
            btnExportChiTiet.UseVisualStyleBackColor = true;
            btnExportChiTiet.Click += btnExportChiTiet_Click;
            // 
            // btnExportDS
            // 
            btnExportDS.Location = new Point(221, 189);
            btnExportDS.Name = "btnExportDS";
            btnExportDS.Size = new Size(114, 30);
            btnExportDS.TabIndex = 37;
            btnExportDS.Text = "Tải danh sách HĐ";
            btnExportDS.UseVisualStyleBackColor = true;
            btnExportDS.Click += btnExportDS_Click;
            // 
            // tabControlMain
            // 
            tabControlMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControlMain.Controls.Add(tabTongHop);
            tabControlMain.Controls.Add(tabChiTiet);
            tabControlMain.Controls.Add(tabDkMua);
            tabControlMain.Controls.Add(tabBKBan);
            tabControlMain.Controls.Add(tabVATNop);
            tabControlMain.Controls.Add(tabGiamThue);
            tabControlMain.Location = new Point(12, 225);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(950, 390);
            tabControlMain.TabIndex = 38;
            // 
            // tabTongHop
            // 
            tabTongHop.Controls.Add(dgvMain);
            tabTongHop.Location = new Point(4, 24);
            tabTongHop.Name = "tabTongHop";
            tabTongHop.Padding = new Padding(3);
            tabTongHop.Size = new Size(942, 362);
            tabTongHop.TabIndex = 0;
            tabTongHop.Text = "Tổng hợp";
            tabTongHop.UseVisualStyleBackColor = true;
            // 
            // dgvMain
            // 
            dgvMain.AllowUserToAddRows = false;
            dgvMain.AllowUserToDeleteRows = false;
            dgvMain.BackgroundColor = SystemColors.ButtonHighlight;
            dgvMain.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMain.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn18, dataGridViewTextBoxColumn19, dataGridViewTextBoxColumn20, dataGridViewTextBoxColumn21, dataGridViewTextBoxColumn22, dataGridViewTextBoxColumn23, dataGridViewTextBoxColumn24, dataGridViewTextBoxColumn25, dataGridViewTextBoxColumn26, dataGridViewTextBoxColumn27, dataGridViewTextBoxColumn28, dataGridViewTextBoxColumn29, dataGridViewTextBoxColumn30, dataGridViewTextBoxColumn31, dataGridViewTextBoxColumn32, dataGridViewTextBoxColumn33, dataGridViewTextBoxColumn34, Tra_Cuu });
            dgvMain.Dock = DockStyle.Fill;
            dgvMain.Location = new Point(3, 3);
            dgvMain.Name = "dgvMain";
            dgvMain.ReadOnly = true;
            dgvMain.RowHeadersVisible = false;
            dgvMain.Size = new Size(936, 356);
            dgvMain.TabIndex = 0;
            dgvMain.CellContentClick += dgvMain_CellContentClick;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            // 
            // Tra_Cuu
            // 
            Tra_Cuu.Name = "Tra_Cuu";
            Tra_Cuu.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn19
            // 
            dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            dataGridViewTextBoxColumn19.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn20
            // 
            dataGridViewTextBoxColumn20.Name = "dataGridViewTextBoxColumn20";
            dataGridViewTextBoxColumn20.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn21
            // 
            dataGridViewTextBoxColumn21.Name = "dataGridViewTextBoxColumn21";
            dataGridViewTextBoxColumn21.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn22
            // 
            dataGridViewTextBoxColumn22.Name = "dataGridViewTextBoxColumn22";
            dataGridViewTextBoxColumn22.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn23
            // 
            dataGridViewTextBoxColumn23.Name = "dataGridViewTextBoxColumn23";
            dataGridViewTextBoxColumn23.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn24
            // 
            dataGridViewTextBoxColumn24.Name = "dataGridViewTextBoxColumn24";
            dataGridViewTextBoxColumn24.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn25
            // 
            dataGridViewTextBoxColumn25.Name = "dataGridViewTextBoxColumn25";
            dataGridViewTextBoxColumn25.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn26
            // 
            dataGridViewTextBoxColumn26.Name = "dataGridViewTextBoxColumn26";
            dataGridViewTextBoxColumn26.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn27
            // 
            dataGridViewTextBoxColumn27.Name = "dataGridViewTextBoxColumn27";
            dataGridViewTextBoxColumn27.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn28
            // 
            dataGridViewTextBoxColumn28.Name = "dataGridViewTextBoxColumn28";
            dataGridViewTextBoxColumn28.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn29
            // 
            dataGridViewTextBoxColumn29.Name = "dataGridViewTextBoxColumn29";
            dataGridViewTextBoxColumn29.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn30
            // 
            dataGridViewTextBoxColumn30.Name = "dataGridViewTextBoxColumn30";
            dataGridViewTextBoxColumn30.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn31
            // 
            dataGridViewTextBoxColumn31.Name = "dataGridViewTextBoxColumn31";
            dataGridViewTextBoxColumn31.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn32
            // 
            dataGridViewTextBoxColumn32.Name = "dataGridViewTextBoxColumn32";
            dataGridViewTextBoxColumn32.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn33
            // 
            dataGridViewTextBoxColumn33.Name = "dataGridViewTextBoxColumn33";
            dataGridViewTextBoxColumn33.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn34
            // 
            dataGridViewTextBoxColumn34.Name = "dataGridViewTextBoxColumn34";
            dataGridViewTextBoxColumn34.ReadOnly = true;
            // 
            // tabChiTiet
            // 
            tabChiTiet.Controls.Add(dgvDetails);
            tabChiTiet.Location = new Point(4, 24);
            tabChiTiet.Name = "tabChiTiet";
            tabChiTiet.Padding = new Padding(3);
            tabChiTiet.Size = new Size(942, 362);
            tabChiTiet.TabIndex = 1;
            tabChiTiet.Text = "Chi tiết";
            tabChiTiet.UseVisualStyleBackColor = true;
            // 
            // dgvDetails
            // 
            dgvDetails.AllowUserToAddRows = false;
            dgvDetails.AllowUserToDeleteRows = false;
            dgvDetails.BackgroundColor = SystemColors.Window;
            dgvDetails.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDetails.Dock = DockStyle.Fill;
            dgvDetails.Location = new Point(3, 3);
            dgvDetails.Name = "dgvDetails";
            dgvDetails.ReadOnly = true;
            dgvDetails.RowHeadersVisible = false;
            dgvDetails.Size = new Size(936, 356);
            dgvDetails.TabIndex = 0;
            // 
            // tabDkMua
            // 
            tabDkMua.Controls.Add(dgvMua);
            tabDkMua.Location = new Point(4, 24);
            tabDkMua.Name = "tabDkMua";
            tabDkMua.Size = new Size(942, 362);
            tabDkMua.TabIndex = 2;
            tabDkMua.Text = "Bk mua";
            tabDkMua.UseVisualStyleBackColor = true;
            // 
            // dgvMua
            // 
            dgvMua.AllowUserToAddRows = false;
            dgvMua.AllowUserToDeleteRows = false;
            dgvMua.BackgroundColor = SystemColors.Window;
            dgvMua.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMua.Dock = DockStyle.Fill;
            dgvMua.Location = new Point(0, 0);
            dgvMua.Name = "dgvMua";
            dgvMua.ReadOnly = true;
            dgvMua.RowHeadersVisible = false;
            dgvMua.Size = new Size(942, 362);
            dgvMua.TabIndex = 0;
            // 
            // tabBKBan
            // 
            tabBKBan.Controls.Add(dgvBan);
            tabBKBan.Location = new Point(4, 24);
            tabBKBan.Name = "tabBKBan";
            tabBKBan.Size = new Size(942, 362);
            tabBKBan.TabIndex = 3;
            tabBKBan.Text = "BK bán";
            tabBKBan.UseVisualStyleBackColor = true;
            // 
            // dgvBan
            // 
            dgvBan.AllowUserToAddRows = false;
            dgvBan.AllowUserToDeleteRows = false;
            dgvBan.BackgroundColor = SystemColors.Window;
            dgvBan.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvBan.Dock = DockStyle.Fill;
            dgvBan.Location = new Point(0, 0);
            dgvBan.Name = "dgvBan";
            dgvBan.ReadOnly = true;
            dgvBan.RowHeadersVisible = false;
            dgvBan.Size = new Size(942, 362);
            dgvBan.TabIndex = 0;
            // 
            // tabVATNop
            // 
            tabVATNop.Location = new Point(4, 24);
            tabVATNop.Name = "tabVATNop";
            tabVATNop.Size = new Size(942, 362);
            tabVATNop.TabIndex = 4;
            tabVATNop.Text = "VAT nộp";
            tabVATNop.UseVisualStyleBackColor = true;
            // 
            // tabGiamThue
            // 
            tabGiamThue.Location = new Point(4, 24);
            tabGiamThue.Name = "tabGiamThue";
            tabGiamThue.Size = new Size(942, 362);
            tabGiamThue.TabIndex = 6;
            tabGiamThue.Text = "Bảng kê giảm thuế";
            tabGiamThue.UseVisualStyleBackColor = true;
            // 
            // gbNguoiBan
            // 
            gbNguoiBan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            gbNguoiBan.Controls.Add(btnKtraDNMuaLe);
            gbNguoiBan.Controls.Add(lblGhiChu);
            gbNguoiBan.Controls.Add(txtGhiChu);
            gbNguoiBan.Controls.Add(btnMoGhiChu);
            gbNguoiBan.Controls.Add(lblTimMST);
            gbNguoiBan.Controls.Add(txtTimMST);
            gbNguoiBan.Controls.Add(btnRightSearch);
            gbNguoiBan.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            gbNguoiBan.Location = new Point(642, 10);
            gbNguoiBan.Name = "gbNguoiBan";
            gbNguoiBan.Size = new Size(290, 221);
            gbNguoiBan.TabIndex = 0;
            gbNguoiBan.TabStop = false;
            gbNguoiBan.Text = "Thông tin ng bán";
            // 
            // btnKtraDNMuaLe
            // 
            btnKtraDNMuaLe.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnKtraDNMuaLe.Location = new Point(15, 30);
            btnKtraDNMuaLe.Name = "btnKtraDNMuaLe";
            btnKtraDNMuaLe.Size = new Size(260, 30);
            btnKtraDNMuaLe.TabIndex = 0;
            btnKtraDNMuaLe.Text = "Ktra DN Mua Lẻ";
            btnKtraDNMuaLe.UseVisualStyleBackColor = true;
            // 
            // lblGhiChu
            // 
            lblGhiChu.AutoSize = true;
            lblGhiChu.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblGhiChu.Location = new Point(15, 75);
            lblGhiChu.Name = "lblGhiChu";
            lblGhiChu.Size = new Size(102, 15);
            lblGhiChu.TabIndex = 1;
            lblGhiChu.Text = "Thông tin ghi chú";
            // 
            // txtGhiChu
            // 
            txtGhiChu.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtGhiChu.Location = new Point(15, 95);
            txtGhiChu.Multiline = true;
            txtGhiChu.Name = "txtGhiChu";
            txtGhiChu.PlaceholderText = "Nhập ghi chú";
            txtGhiChu.Size = new Size(210, 60);
            txtGhiChu.TabIndex = 2;
            // 
            // btnMoGhiChu
            // 
            btnMoGhiChu.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnMoGhiChu.Location = new Point(230, 95);
            btnMoGhiChu.Name = "btnMoGhiChu";
            btnMoGhiChu.Size = new Size(45, 60);
            btnMoGhiChu.TabIndex = 3;
            btnMoGhiChu.Text = "Ghi chú";
            btnMoGhiChu.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnMoGhiChu.UseVisualStyleBackColor = true;
            // 
            // lblTimMST
            // 
            lblTimMST.AutoSize = true;
            lblTimMST.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTimMST.Location = new Point(15, 170);
            lblTimMST.Name = "lblTimMST";
            lblTimMST.Size = new Size(124, 15);
            lblTimMST.TabIndex = 4;
            lblTimMST.Text = "Tìm kiếm MST ng bán";
            // 
            // txtTimMST
            // 
            txtTimMST.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtTimMST.Location = new Point(15, 190);
            txtTimMST.Name = "txtTimMST";
            txtTimMST.Size = new Size(170, 23);
            txtTimMST.TabIndex = 5;
            // 
            // btnRightSearch
            // 
            btnRightSearch.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnRightSearch.Location = new Point(190, 188);
            btnRightSearch.Name = "btnRightSearch";
            btnRightSearch.Size = new Size(85, 27);
            btnRightSearch.TabIndex = 6;
            btnRightSearch.Text = "Tìm kiếm";
            btnRightSearch.UseVisualStyleBackColor = true;
            btnRightSearch.Click += btnRightSearch_Click;
            // 
            // downloadProgressBar
            // 
            downloadProgressBar.Location = new Point(54, 443);
            downloadProgressBar.Name = "downloadProgressBar";
            downloadProgressBar.Size = new Size(350, 23);
            downloadProgressBar.TabIndex = 27;
            downloadProgressBar.Visible = false;
            // 
            // lblDownloadStatus
            // 
            lblDownloadStatus.AutoSize = true;
            lblDownloadStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDownloadStatus.Location = new Point(54, 469);
            lblDownloadStatus.Name = "lblDownloadStatus";
            lblDownloadStatus.Size = new Size(80, 15);
            lblDownloadStatus.TabIndex = 28;
            lblDownloadStatus.Text = "Đang tải 0/0...";
            lblDownloadStatus.Visible = false;
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
            pictureBox2.Visible = false;
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
            // lblStatusMessage
            // 
            lblStatusMessage.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatusMessage.Location = new Point(54, 359);
            lblStatusMessage.Name = "lblStatusMessage";
            lblStatusMessage.Size = new Size(350, 21);
            lblStatusMessage.TabIndex = 30;
            lblStatusMessage.Text = "Status message here";
            lblStatusMessage.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusMessage.Visible = false;
            // 
            // statusTimer
            // 
            statusTimer.Interval = 2000;
            statusTimer.Tick += statusTimer_Tick;
            // 
            // Form1
            // 
            BackColor = Color.White;
            ClientSize = new Size(984, 680);
            Controls.Add(lblVersion);
            Controls.Add(panelSearch);
            Controls.Add(panelLogin);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
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
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            gbInvoiceType.ResumeLayout(false);
            gbInvoiceType.PerformLayout();
            tabControlMain.ResumeLayout(false);
            tabTongHop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvMain).EndInit();
            tabChiTiet.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDetails).EndInit();
            tabDkMua.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvMua).EndInit();
            tabBKBan.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvBan).EndInit();
            gbNguoiBan.ResumeLayout(false);
            gbNguoiBan.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        private void txtMonthYear_Leave(object sender, EventArgs e)
        {
            // Gọi phương thức cập nhật chung khi rời khỏi ô tháng hoặc năm
            UpdateDatePickersFromMonthYear();
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
        private GroupBox gbInvoiceType;
        private RadioButton rbBought;
        private RadioButton rbSold;
        private System.Windows.Forms.Label lblStatusMessage;
        private System.Windows.Forms.Timer statusTimer;


        private System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.Label lblKyBaoCao;
        private System.Windows.Forms.TextBox txtThang;
        private System.Windows.Forms.Label lblNam;
        private System.Windows.Forms.TextBox txtNam;
        private System.Windows.Forms.RadioButton rbAllInvoices;
        private System.Windows.Forms.Button btnLeftSearch;
        private System.Windows.Forms.Button btnExportChiTiet;
        private System.Windows.Forms.Button btnExportDS;
        private System.Windows.Forms.GroupBox gbNguoiBan;
        private System.Windows.Forms.Button btnKtraDNMuaLe;
        private System.Windows.Forms.Label lblGhiChu;
        private System.Windows.Forms.TextBox txtGhiChu;
        private System.Windows.Forms.Button btnMoGhiChu;
        private System.Windows.Forms.Label lblTimMST;
        private System.Windows.Forms.Label lblThang;
        private System.Windows.Forms.TextBox txtTimMST;
        private System.Windows.Forms.Button btnRightSearch;
        private System.Windows.Forms.Button btnXemHD;
        private System.Windows.Forms.Button btnCnKoPdf;
        private System.Windows.Forms.Button btnNenZip;
        private System.Windows.Forms.Button btnTaiHDGoc;
        private PictureBox pictureBox3;
        private TabControl tabControlMain;
        private TabPage tabTongHop;
        private DataGridView dgvMain;
        private TabPage tabChiTiet;
        private TabPage tabDkMua;
        private TabPage tabBKBan;
        private TabPage tabVATNop;
        private TabPage tabGiamThue;

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn15;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn16;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn17;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn18;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn19;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn20;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn21;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn22;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn23;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn24;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn25;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn26;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn27;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn28;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn29;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn30;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn31;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn32;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn33;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn34;
        private DataGridViewButtonColumn Tra_Cuu;
    }
}