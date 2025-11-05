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
        private System.Windows.Forms.DataGridView dgvMua;
        private System.Windows.Forms.DataGridView dgvBan;
        private System.Windows.Forms.DataGridView dgvVatNop;
        private System.Windows.Forms.DataGridView dgvGiamThue;

        private DataGridViewTextBoxColumn colDgvSTT;
        private DataGridViewTextBoxColumn colDgvLoaiHD;
        private DataGridViewTextBoxColumn colDgvMST;
        private DataGridViewTextBoxColumn colDgvKHMauSo;
        private DataGridViewTextBoxColumn colDgvKHHoaDon;
        private DataGridViewTextBoxColumn colDgvSoHoaDon;
        private DataGridViewTextBoxColumn colDgvNgayLap;
        private DataGridViewTextBoxColumn colDgvThongTinHD;
        private DataGridViewTextBoxColumn colDgvTienChuaThue;
        private DataGridViewTextBoxColumn colDgvTienThue;
        private DataGridViewTextBoxColumn colDgvTongTien;
        private DataGridViewButtonColumn colDgvTraCuu;

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
            label1 = new Label();
            dtpFromDate = new DateTimePicker();
            label2 = new Label();
            dtpToDate = new DateTimePicker();
            btnTaiHDGoc = new Button();
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
            colDgvSTT = new DataGridViewTextBoxColumn();
            colDgvTraCuu = new DataGridViewButtonColumn();
            colDgvMST = new DataGridViewTextBoxColumn();
            colDgvKHMauSo = new DataGridViewTextBoxColumn();
            colDgvKHHoaDon = new DataGridViewTextBoxColumn();
            colDgvSoHoaDon = new DataGridViewTextBoxColumn();
            colDgvNgayLap = new DataGridViewTextBoxColumn();
            colDgvThongTinHD = new DataGridViewTextBoxColumn();
            colDgvTienChuaThue = new DataGridViewTextBoxColumn();
            colDgvTienThue = new DataGridViewTextBoxColumn();
            colDgvTongTien = new DataGridViewTextBoxColumn();
            tabChiTiet = new TabPage();
            dgvDetails = new DataGridView();
            tabDkMua = new TabPage();
            dgvMua = new DataGridView();
            tabBKBan = new TabPage();
            dgvBan = new DataGridView();
            tabVATNop = new TabPage();
            dgvVatNop = new DataGridView();
            tabGiamThue = new TabPage();
            dgvGiamThue = new DataGridView();
            gbNguoiBan = new GroupBox();
            txtKtraMaDoanhNghiep = new TextBox();
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
            colDgvLoaiHD = new DataGridViewTextBoxColumn();
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
            tabVATNop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvVatNop).BeginInit();
            tabGiamThue.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvGiamThue).BeginInit();
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
            mainSplitContainer.Panel1.Controls.Add(label1);
            mainSplitContainer.Panel1.Controls.Add(dtpFromDate);
            mainSplitContainer.Panel1.Controls.Add(label2);
            mainSplitContainer.Panel1.Controls.Add(dtpToDate);
            mainSplitContainer.Panel1.Controls.Add(btnTaiHDGoc);
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
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 11.25F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(0, 117, 214);
            label1.Location = new Point(10, 27);
            label1.Name = "label1";
            label1.Size = new Size(61, 20);
            label1.TabIndex = 22;
            label1.Text = "Từ ngày";
            label1.Click += label1_Click;
            // 
            // dtpFromDate
            // 
            dtpFromDate.CustomFormat = "dd/MM/yyyy";
            dtpFromDate.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dtpFromDate.Format = DateTimePickerFormat.Custom;
            dtpFromDate.Location = new Point(10, 63);
            dtpFromDate.Name = "dtpFromDate";
            dtpFromDate.Size = new Size(156, 23);
            dtpFromDate.TabIndex = 24;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 11.25F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(0, 117, 214);
            label2.Location = new Point(225, 27);
            label2.Name = "label2";
            label2.Size = new Size(70, 20);
            label2.TabIndex = 23;
            label2.Text = "Đến ngày";
            // 
            // dtpToDate
            // 
            dtpToDate.CustomFormat = "dd/MM/yyyy";
            dtpToDate.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dtpToDate.Format = DateTimePickerFormat.Custom;
            dtpToDate.Location = new Point(225, 63);
            dtpToDate.Name = "dtpToDate";
            dtpToDate.Size = new Size(156, 23);
            dtpToDate.TabIndex = 25;
            // 
            // btnTaiHDGoc
            // 
            btnTaiHDGoc.BackColor = SystemColors.GradientActiveCaption;
            btnTaiHDGoc.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnTaiHDGoc.Location = new Point(506, 180);
            btnTaiHDGoc.Name = "btnTaiHDGoc";
            btnTaiHDGoc.Size = new Size(130, 30);
            btnTaiHDGoc.TabIndex = 10;
            btnTaiHDGoc.Text = "Tải tất cả HĐ Gốc";
            btnTaiHDGoc.UseVisualStyleBackColor = false;
            btnTaiHDGoc.Click += btnTaiHDGoc_Click;
            // 
            // btnCnKoPdf
            // 
            btnCnKoPdf.BackColor = SystemColors.GradientActiveCaption;
            btnCnKoPdf.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnCnKoPdf.Location = new Point(126, 181);
            btnCnKoPdf.Name = "btnCnKoPdf";
            btnCnKoPdf.Size = new Size(76, 30);
            btnCnKoPdf.TabIndex = 8;
            btnCnKoPdf.Text = "In pdf";
            btnCnKoPdf.UseVisualStyleBackColor = false;
            btnCnKoPdf.Click += btnCnKoPdf_Click;
            // 
            // btnXemHD
            // 
            btnXemHD.BackColor = SystemColors.GradientActiveCaption;
            btnXemHD.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnXemHD.Location = new Point(10, 181);
            btnXemHD.Name = "btnXemHD";
            btnXemHD.Size = new Size(97, 30);
            btnXemHD.TabIndex = 7;
            btnXemHD.Text = "Xem HĐ";
            btnXemHD.UseVisualStyleBackColor = false;
            btnXemHD.Click += btnXemHD_Click;
            // 
            // gbInvoiceType
            // 
            gbInvoiceType.Controls.Add(rbAllInvoices);
            gbInvoiceType.Controls.Add(rbBought);
            gbInvoiceType.Controls.Add(rbSold);
            gbInvoiceType.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            gbInvoiceType.Location = new Point(7, 105);
            gbInvoiceType.Name = "gbInvoiceType";
            gbInvoiceType.Size = new Size(522, 60);
            gbInvoiceType.TabIndex = 29;
            gbInvoiceType.TabStop = false;
            gbInvoiceType.Text = "Chọn loại hóa đơn";
            // 
            // rbAllInvoices
            // 
            rbAllInvoices.AutoSize = true;
            rbAllInvoices.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbAllInvoices.Location = new Point(374, 25);
            rbAllInvoices.Name = "rbAllInvoices";
            rbAllInvoices.Size = new Size(104, 19);
            rbAllInvoices.TabIndex = 2;
            rbAllInvoices.Text = "Tất cả hoá đơn";
            rbAllInvoices.UseVisualStyleBackColor = true;
            rbAllInvoices.CheckedChanged += rbAllInvoices_CheckedChanged;
            // 
            // rbBought
            // 
            rbBought.AutoSize = true;
            rbBought.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbBought.Location = new Point(198, 24);
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
            btnLeftSearch.BackColor = SystemColors.ScrollBar;
            btnLeftSearch.Location = new Point(535, 112);
            btnLeftSearch.Name = "btnLeftSearch";
            btnLeftSearch.Size = new Size(96, 54);
            btnLeftSearch.TabIndex = 35;
            btnLeftSearch.Text = "Tìm kiếm";
            btnLeftSearch.UseVisualStyleBackColor = false;
            btnLeftSearch.Click += btnLeftSearch_Click;
            // 
            // btnExportChiTiet
            // 
            btnExportChiTiet.BackColor = SystemColors.GradientActiveCaption;
            btnExportChiTiet.Location = new Point(231, 180);
            btnExportChiTiet.Name = "btnExportChiTiet";
            btnExportChiTiet.Size = new Size(102, 30);
            btnExportChiTiet.TabIndex = 36;
            btnExportChiTiet.Text = "Tải chi tiết HĐ";
            btnExportChiTiet.UseVisualStyleBackColor = false;
            btnExportChiTiet.Click += btnExportChiTiet_Click;
            // 
            // btnExportDS
            // 
            btnExportDS.BackColor = SystemColors.GradientActiveCaption;
            btnExportDS.Location = new Point(359, 180);
            btnExportDS.Name = "btnExportDS";
            btnExportDS.Size = new Size(114, 30);
            btnExportDS.TabIndex = 37;
            btnExportDS.Text = "Tải danh sách HĐ";
            btnExportDS.UseVisualStyleBackColor = false;
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
            tabControlMain.ItemSize = new Size(300, 30);
            tabControlMain.Location = new Point(10, 231);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(943, 412);
            tabControlMain.TabIndex = 38;
            // 
            // tabTongHop
            // 
            tabTongHop.Controls.Add(dgvMain);
            tabTongHop.Location = new Point(4, 34);
            tabTongHop.Name = "tabTongHop";
            tabTongHop.Padding = new Padding(3);
            tabTongHop.Size = new Size(935, 374);
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
            dgvMain.Columns.AddRange(new DataGridViewColumn[] { colDgvSTT, colDgvTraCuu, colDgvMST, colDgvKHMauSo, colDgvKHHoaDon, colDgvSoHoaDon, colDgvNgayLap, colDgvThongTinHD, colDgvTienChuaThue, colDgvTienThue, colDgvTongTien });
            dgvMain.Dock = DockStyle.Fill;
            dgvMain.Location = new Point(3, 3);
            dgvMain.Name = "dgvMain";
            dgvMain.ReadOnly = true;
            dgvMain.RowHeadersVisible = false;
            dgvMain.Size = new Size(929, 368);
            dgvMain.TabIndex = 0;
            dgvMain.CellContentClick += dgvMain_CellContentClick;
            dgvMain.SelectionChanged += dgvMain_SelectionChanged;
            // 
            // colDgvSTT
            // 
            colDgvSTT.Frozen = true;
            colDgvSTT.HeaderText = "STT";
            colDgvSTT.Name = "colDgvSTT";
            colDgvSTT.ReadOnly = true;
            colDgvSTT.Width = 40;
            // 
            // colDgvTraCuu
            // 
            colDgvTraCuu.HeaderText = "Tra cứu";
            colDgvTraCuu.Name = "colDgvTraCuu";
            colDgvTraCuu.ReadOnly = true;
            colDgvTraCuu.Text = "Tải";
            colDgvTraCuu.UseColumnTextForButtonValue = true;
            colDgvTraCuu.Width = 90;
            // 
            // colDgvMST
            // 
            colDgvMST.DataPropertyName = "Ma_so_thue";
            colDgvMST.HeaderText = "MST";
            colDgvMST.Name = "colDgvMST";
            colDgvMST.ReadOnly = true;
            // 
            // colDgvKHMauSo
            // 
            colDgvKHMauSo.DataPropertyName = "Ky_hieu_ma_so";
            colDgvKHMauSo.HeaderText = "Ký hiệu Mẫu số";
            colDgvKHMauSo.Name = "colDgvKHMauSo";
            colDgvKHMauSo.ReadOnly = true;
            colDgvKHMauSo.Width = 80;
            // 
            // colDgvKHHoaDon
            // 
            colDgvKHHoaDon.DataPropertyName = "Ky_hieu_hoa_don";
            colDgvKHHoaDon.HeaderText = "Ký hiệu HĐ";
            colDgvKHHoaDon.Name = "colDgvKHHoaDon";
            colDgvKHHoaDon.ReadOnly = true;
            colDgvKHHoaDon.Width = 80;
            // 
            // colDgvSoHoaDon
            // 
            colDgvSoHoaDon.DataPropertyName = "So_hoa_don";
            colDgvSoHoaDon.HeaderText = "Số HĐ";
            colDgvSoHoaDon.Name = "colDgvSoHoaDon";
            colDgvSoHoaDon.ReadOnly = true;
            colDgvSoHoaDon.Width = 80;
            // 
            // colDgvNgayLap
            // 
            colDgvNgayLap.DataPropertyName = "Ngay_lap";
            colDgvNgayLap.HeaderText = "Ngày lập";
            colDgvNgayLap.Name = "colDgvNgayLap";
            colDgvNgayLap.ReadOnly = true;
            // 
            // colDgvThongTinHD
            // 
            colDgvThongTinHD.DataPropertyName = "Thong_tin_hoa_don";
            colDgvThongTinHD.HeaderText = "Thông tin HĐ";
            colDgvThongTinHD.Name = "colDgvThongTinHD";
            colDgvThongTinHD.ReadOnly = true;
            colDgvThongTinHD.Width = 200;
            // 
            // colDgvTienChuaThue
            // 
            colDgvTienChuaThue.DataPropertyName = "Tong_tien_chua_thue";
            colDgvTienChuaThue.HeaderText = "Tiền chưa thuế";
            colDgvTienChuaThue.Name = "colDgvTienChuaThue";
            colDgvTienChuaThue.ReadOnly = true;
            colDgvTienChuaThue.Width = 120;
            // 
            // colDgvTienThue
            // 
            colDgvTienThue.DataPropertyName = "Tong_tien_thue";
            colDgvTienThue.HeaderText = "Tiền thuế";
            colDgvTienThue.Name = "colDgvTienThue";
            colDgvTienThue.ReadOnly = true;
            // 
            // colDgvTongTien
            // 
            colDgvTongTien.DataPropertyName = "Tong_tien_thanh_toan";
            colDgvTongTien.HeaderText = "Tổng tiền";
            colDgvTongTien.Name = "colDgvTongTien";
            colDgvTongTien.ReadOnly = true;
            colDgvTongTien.Width = 120;
            // 
            // tabChiTiet
            // 
            tabChiTiet.Controls.Add(dgvDetails);
            tabChiTiet.Location = new Point(4, 34);
            tabChiTiet.Name = "tabChiTiet";
            tabChiTiet.Padding = new Padding(3);
            tabChiTiet.Size = new Size(935, 374);
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
            dgvDetails.Size = new Size(929, 368);
            dgvDetails.TabIndex = 0;
            // 
            // tabDkMua
            // 
            tabDkMua.Controls.Add(dgvMua);
            tabDkMua.Location = new Point(4, 34);
            tabDkMua.Name = "tabDkMua";
            tabDkMua.Size = new Size(935, 374);
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
            dgvMua.Size = new Size(935, 374);
            dgvMua.TabIndex = 0;
            // 
            // tabBKBan
            // 
            tabBKBan.Controls.Add(dgvBan);
            tabBKBan.Location = new Point(4, 34);
            tabBKBan.Name = "tabBKBan";
            tabBKBan.Size = new Size(935, 374);
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
            dgvBan.Size = new Size(935, 374);
            dgvBan.TabIndex = 0;
            // 
            // tabVATNop
            // 
            tabVATNop.Controls.Add(dgvVatNop);
            tabVATNop.Location = new Point(4, 34);
            tabVATNop.Name = "tabVATNop";
            tabVATNop.Size = new Size(935, 374);
            tabVATNop.TabIndex = 5;
            tabVATNop.Text = "Bảng kê Thuế";
            tabVATNop.UseVisualStyleBackColor = true;
            // 
            // dgvVatNop
            // 
            dgvVatNop.AllowUserToAddRows = false;
            dgvVatNop.AllowUserToDeleteRows = false;
            dgvVatNop.BackgroundColor = SystemColors.Window;
            dgvVatNop.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvVatNop.Dock = DockStyle.Fill;
            dgvVatNop.Location = new Point(0, 0);
            dgvVatNop.Name = "dgvVatNop";
            dgvVatNop.ReadOnly = true;
            dgvVatNop.RowHeadersVisible = false;
            dgvVatNop.Size = new Size(935, 374);
            dgvVatNop.TabIndex = 0;
            // 
            // tabGiamThue
            // 
            tabGiamThue.Controls.Add(dgvGiamThue);
            tabGiamThue.Location = new Point(4, 34);
            tabGiamThue.Name = "tabGiamThue";
            tabGiamThue.Size = new Size(935, 374);
            tabGiamThue.TabIndex = 6;
            tabGiamThue.Text = "VAT nộp";
            tabGiamThue.UseVisualStyleBackColor = true;
            // 
            // dgvGiamThue
            // 
            dgvGiamThue.AllowUserToAddRows = false;
            dgvGiamThue.AllowUserToDeleteRows = false;
            dgvGiamThue.BackgroundColor = SystemColors.Window;
            dgvGiamThue.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvGiamThue.Dock = DockStyle.Fill;
            dgvGiamThue.Location = new Point(0, 0);
            dgvGiamThue.Name = "dgvGiamThue";
            dgvGiamThue.ReadOnly = true;
            dgvGiamThue.RowHeadersVisible = false;
            dgvGiamThue.Size = new Size(935, 374);
            dgvGiamThue.TabIndex = 0;
            // 
            // gbNguoiBan
            // 
            gbNguoiBan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            gbNguoiBan.Controls.Add(txtKtraMaDoanhNghiep);
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
            gbNguoiBan.Size = new Size(290, 249);
            gbNguoiBan.TabIndex = 0;
            gbNguoiBan.TabStop = false;
            gbNguoiBan.Text = "Thông tin ng bán";
            // 
            // txtKtraMaDoanhNghiep
            // 
            txtKtraMaDoanhNghiep.BackColor = SystemColors.Window;
            txtKtraMaDoanhNghiep.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            txtKtraMaDoanhNghiep.ForeColor = Color.FromArgb(0, 117, 214);
            txtKtraMaDoanhNghiep.Location = new Point(15, 27);
            txtKtraMaDoanhNghiep.Name = "txtKtraMaDoanhNghiep";
            txtKtraMaDoanhNghiep.PlaceholderText = "Nhập mã doanh nghiệp";
            txtKtraMaDoanhNghiep.Size = new Size(260, 24);
            txtKtraMaDoanhNghiep.TabIndex = 7;
            // 
            // btnKtraDNMuaLe
            // 
            btnKtraDNMuaLe.BackColor = Color.LightGray;
            btnKtraDNMuaLe.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnKtraDNMuaLe.Location = new Point(14, 58);
            btnKtraDNMuaLe.Name = "btnKtraDNMuaLe";
            btnKtraDNMuaLe.Size = new Size(260, 30);
            btnKtraDNMuaLe.TabIndex = 0;
            btnKtraDNMuaLe.Text = "Ktra DN Mua Lẻ";
            btnKtraDNMuaLe.UseVisualStyleBackColor = false;
            btnKtraDNMuaLe.Click += btnKtraDNMuaLe_Click;
            // 
            // lblGhiChu
            // 
            lblGhiChu.AutoSize = true;
            lblGhiChu.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblGhiChu.Location = new Point(14, 91);
            lblGhiChu.Name = "lblGhiChu";
            lblGhiChu.Size = new Size(102, 15);
            lblGhiChu.TabIndex = 1;
            lblGhiChu.Text = "Thông tin ghi chú";
            // 
            // txtGhiChu
            // 
            txtGhiChu.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtGhiChu.Location = new Point(14, 109);
            txtGhiChu.Multiline = true;
            txtGhiChu.Name = "txtGhiChu";
            txtGhiChu.PlaceholderText = "Nhập ghi chú";
            txtGhiChu.Size = new Size(210, 47);
            txtGhiChu.TabIndex = 2;
            // 
            // btnMoGhiChu
            // 
            btnMoGhiChu.BackColor = Color.LightGray;
            btnMoGhiChu.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnMoGhiChu.Location = new Point(230, 109);
            btnMoGhiChu.Name = "btnMoGhiChu";
            btnMoGhiChu.Size = new Size(54, 50);
            btnMoGhiChu.TabIndex = 3;
            btnMoGhiChu.Text = "Ghi chú";
            btnMoGhiChu.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnMoGhiChu.UseVisualStyleBackColor = false;
            btnMoGhiChu.Click += btnMoGhiChu_Click;
            // 
            // lblTimMST
            // 
            lblTimMST.AutoSize = true;
            lblTimMST.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTimMST.Location = new Point(14, 170);
            lblTimMST.Name = "lblTimMST";
            lblTimMST.Size = new Size(124, 15);
            lblTimMST.TabIndex = 4;
            lblTimMST.Text = "Tìm kiếm MST ng bán";
            // 
            // txtTimMST
            // 
            txtTimMST.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtTimMST.Location = new Point(14, 192);
            txtTimMST.Name = "txtTimMST";
            txtTimMST.Size = new Size(170, 23);
            txtTimMST.TabIndex = 5;
            // 
            // btnRightSearch
            // 
            btnRightSearch.BackColor = Color.LightGray;
            btnRightSearch.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnRightSearch.Location = new Point(199, 188);
            btnRightSearch.Name = "btnRightSearch";
            btnRightSearch.Size = new Size(85, 27);
            btnRightSearch.TabIndex = 6;
            btnRightSearch.Text = "Tìm kiếm";
            btnRightSearch.UseVisualStyleBackColor = false;
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
            // colDgvLoaiHD
            // 
            colDgvLoaiHD.Name = "colDgvLoaiHD";
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
            ClientSize = new Size(984, 686);
            Controls.Add(lblVersion);
            Controls.Add(panelSearch);
            Controls.Add(panelLogin);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
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
            tabVATNop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvVatNop).EndInit();
            tabGiamThue.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvGiamThue).EndInit();
            gbNguoiBan.ResumeLayout(false);
            gbNguoiBan.PerformLayout();
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
        private GroupBox gbInvoiceType;
        private RadioButton rbBought;
        private RadioButton rbSold;
        private System.Windows.Forms.Label lblStatusMessage;
        private System.Windows.Forms.Timer statusTimer;


        private System.Windows.Forms.SplitContainer mainSplitContainer;
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
        private System.Windows.Forms.Button btnTaiHDGoc;
        private TabControl tabControlMain;
        private TabPage tabTongHop;
        private DataGridView dgvMain;
        private TabPage tabChiTiet;
        private TabPage tabDkMua;
        private TabPage tabBKBan;
        private TabPage tabVATNop;
        private TabPage tabGiamThue;

        private System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1;
        private System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2;
        private System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3;
        private TextBox txtKtraMaDoanhNghiep;
    }
}