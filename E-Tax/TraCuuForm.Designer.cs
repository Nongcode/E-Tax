namespace E_Tax // Đảm bảo đúng namespace
{
    partial class TraCuuForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblHuongDan = new Label();
            lblMaTraCuuDesc = new Label();
            txtMaTraCuu = new TextBox();
            lblLink1Desc = new Label();
            llLinkTraCuu = new LinkLabel();
            lblLink2Desc = new Label();
            lblLink3Desc = new Label();
            lblLink4Desc = new Label();
            lblLink5Desc = new Label();
            gbNguoiBan = new GroupBox();
            txtDiaChiNguoiBan = new TextBox();
            lblDiaChiDesc = new Label();
            txtMSTNguoiBan = new TextBox();
            lblMSTDesc = new Label();
            txtTenNguoiBan = new TextBox();
            lblTenNguoiBanDesc = new Label();
            btnDong = new Button();
            gbNguoiBan.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(0, 117, 214);
            lblTitle.Location = new Point(12, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(79, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Tra cứu";
            // 
            // lblHuongDan
            // 
            lblHuongDan.AutoSize = true;
            lblHuongDan.Location = new Point(14, 44);
            lblHuongDan.Name = "lblHuongDan";
            lblHuongDan.Size = new Size(197, 15);
            lblHuongDan.TabIndex = 1;
            lblHuongDan.Text = "Hướng dẫn: bấm vào link để tra cứu";
            // 
            // lblMaTraCuuDesc
            // 
            lblMaTraCuuDesc.AutoSize = true;
            lblMaTraCuuDesc.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMaTraCuuDesc.ForeColor = SystemColors.Highlight;
            lblMaTraCuuDesc.Location = new Point(14, 78);
            lblMaTraCuuDesc.Name = "lblMaTraCuuDesc";
            lblMaTraCuuDesc.Size = new Size(93, 17);
            lblMaTraCuuDesc.TabIndex = 2;
            lblMaTraCuuDesc.Text = "MÃ TRA CỨU:";
            // 
            // txtMaTraCuu
            // 
            txtMaTraCuu.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtMaTraCuu.BackColor = SystemColors.Control;
            txtMaTraCuu.BorderStyle = BorderStyle.None;
            txtMaTraCuu.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            txtMaTraCuu.ForeColor = Color.Blue;
            txtMaTraCuu.Location = new Point(111, 78);
            txtMaTraCuu.Name = "txtMaTraCuu";
            txtMaTraCuu.ReadOnly = true;
            txtMaTraCuu.Size = new Size(461, 18);
            txtMaTraCuu.TabIndex = 100;
            txtMaTraCuu.TabStop = false;
            // 
            // lblLink1Desc
            // 
            lblLink1Desc.AutoSize = true;
            lblLink1Desc.Location = new Point(14, 110);
            lblLink1Desc.Name = "lblLink1Desc";
            lblLink1Desc.Size = new Size(81, 15);
            lblLink1Desc.TabIndex = 4;
            lblLink1Desc.Text = "Link tra cứu 1:";
            // 
            // llLinkTraCuu
            // 
            llLinkTraCuu.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            llLinkTraCuu.AutoSize = true;
            llLinkTraCuu.Location = new Point(108, 110);
            llLinkTraCuu.Name = "llLinkTraCuu";
            llLinkTraCuu.Size = new Size(0, 15);
            llLinkTraCuu.TabIndex = 0;
            llLinkTraCuu.TabStop = true;
            // 
            // lblLink2Desc
            // 
            lblLink2Desc.AutoSize = true;
            lblLink2Desc.Location = new Point(14, 135);
            lblLink2Desc.Name = "lblLink2Desc";
            lblLink2Desc.Size = new Size(81, 15);
            lblLink2Desc.TabIndex = 6;
            lblLink2Desc.Text = "Link tra cứu 2:";
            // 
            // lblLink3Desc
            // 
            lblLink3Desc.AutoSize = true;
            lblLink3Desc.Location = new Point(14, 160);
            lblLink3Desc.Name = "lblLink3Desc";
            lblLink3Desc.Size = new Size(81, 15);
            lblLink3Desc.TabIndex = 7;
            lblLink3Desc.Text = "Link tra cứu 3:";
            // 
            // lblLink4Desc
            // 
            lblLink4Desc.AutoSize = true;
            lblLink4Desc.Location = new Point(14, 185);
            lblLink4Desc.Name = "lblLink4Desc";
            lblLink4Desc.Size = new Size(81, 15);
            lblLink4Desc.TabIndex = 8;
            lblLink4Desc.Text = "Link tra cứu 4:";
            // 
            // lblLink5Desc
            // 
            lblLink5Desc.AutoSize = true;
            lblLink5Desc.Location = new Point(14, 210);
            lblLink5Desc.Name = "lblLink5Desc";
            lblLink5Desc.Size = new Size(81, 15);
            lblLink5Desc.TabIndex = 9;
            lblLink5Desc.Text = "Link tra cứu 5:";
            // 
            // gbNguoiBan
            // 
            gbNguoiBan.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbNguoiBan.Controls.Add(txtDiaChiNguoiBan);
            gbNguoiBan.Controls.Add(lblDiaChiDesc);
            gbNguoiBan.Controls.Add(txtMSTNguoiBan);
            gbNguoiBan.Controls.Add(lblMSTDesc);
            gbNguoiBan.Controls.Add(txtTenNguoiBan);
            gbNguoiBan.Controls.Add(lblTenNguoiBanDesc);
            gbNguoiBan.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            gbNguoiBan.Location = new Point(17, 245);
            gbNguoiBan.Name = "gbNguoiBan";
            gbNguoiBan.Size = new Size(555, 135);
            gbNguoiBan.TabIndex = 10;
            gbNguoiBan.TabStop = false;
            gbNguoiBan.Text = "Thông tin người bán";
            // 
            // txtDiaChiNguoiBan
            // 
            txtDiaChiNguoiBan.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtDiaChiNguoiBan.BackColor = SystemColors.Window;
            txtDiaChiNguoiBan.BorderStyle = BorderStyle.None;
            txtDiaChiNguoiBan.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtDiaChiNguoiBan.Location = new Point(94, 90);
            txtDiaChiNguoiBan.Multiline = true;
            txtDiaChiNguoiBan.Name = "txtDiaChiNguoiBan";
            txtDiaChiNguoiBan.ReadOnly = true;
            txtDiaChiNguoiBan.ScrollBars = ScrollBars.Vertical;
            txtDiaChiNguoiBan.Size = new Size(446, 35);
            txtDiaChiNguoiBan.TabIndex = 105;
            txtDiaChiNguoiBan.TabStop = false;
            // 
            // lblDiaChiDesc
            // 
            lblDiaChiDesc.AutoSize = true;
            lblDiaChiDesc.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDiaChiDesc.Location = new Point(12, 90);
            lblDiaChiDesc.Name = "lblDiaChiDesc";
            lblDiaChiDesc.Size = new Size(46, 15);
            lblDiaChiDesc.TabIndex = 14;
            lblDiaChiDesc.Text = "Địa chỉ:";
            // 
            // txtMSTNguoiBan
            // 
            txtMSTNguoiBan.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtMSTNguoiBan.BackColor = SystemColors.Window;
            txtMSTNguoiBan.BorderStyle = BorderStyle.None;
            txtMSTNguoiBan.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtMSTNguoiBan.Location = new Point(94, 62);
            txtMSTNguoiBan.Name = "txtMSTNguoiBan";
            txtMSTNguoiBan.ReadOnly = true;
            txtMSTNguoiBan.Size = new Size(446, 16);
            txtMSTNguoiBan.TabIndex = 104;
            txtMSTNguoiBan.TabStop = false;
            // 
            // lblMSTDesc
            // 
            lblMSTDesc.AutoSize = true;
            lblMSTDesc.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblMSTDesc.Location = new Point(12, 62);
            lblMSTDesc.Name = "lblMSTDesc";
            lblMSTDesc.Size = new Size(69, 15);
            lblMSTDesc.TabIndex = 12;
            lblMSTDesc.Text = "Mã số thuế:";
            // 
            // txtTenNguoiBan
            // 
            txtTenNguoiBan.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtTenNguoiBan.BackColor = SystemColors.Window;
            txtTenNguoiBan.BorderStyle = BorderStyle.None;
            txtTenNguoiBan.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtTenNguoiBan.Location = new Point(94, 34);
            txtTenNguoiBan.Name = "txtTenNguoiBan";
            txtTenNguoiBan.ReadOnly = true;
            txtTenNguoiBan.Size = new Size(446, 16);
            txtTenNguoiBan.TabIndex = 103;
            txtTenNguoiBan.TabStop = false;
            // 
            // lblTenNguoiBanDesc
            // 
            lblTenNguoiBanDesc.AutoSize = true;
            lblTenNguoiBanDesc.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTenNguoiBanDesc.Location = new Point(12, 34);
            lblTenNguoiBanDesc.Name = "lblTenNguoiBanDesc";
            lblTenNguoiBanDesc.Size = new Size(86, 15);
            lblTenNguoiBanDesc.TabIndex = 10;
            lblTenNguoiBanDesc.Text = "Tên người bán:";
            // 
            // btnDong
            // 
            btnDong.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDong.DialogResult = DialogResult.Cancel;
            btnDong.Location = new Point(497, 395);
            btnDong.Name = "btnDong";
            btnDong.Size = new Size(75, 28);
            btnDong.TabIndex = 1;
            btnDong.Text = "Đóng";
            btnDong.UseVisualStyleBackColor = true;
            // 
            // TraCuuForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            CancelButton = btnDong;
            ClientSize = new Size(584, 431);
            Controls.Add(btnDong);
            Controls.Add(gbNguoiBan);
            Controls.Add(lblLink5Desc);
            Controls.Add(lblLink4Desc);
            Controls.Add(lblLink3Desc);
            Controls.Add(lblLink2Desc);
            Controls.Add(llLinkTraCuu);
            Controls.Add(lblLink1Desc);
            Controls.Add(txtMaTraCuu);
            Controls.Add(lblMaTraCuuDesc);
            Controls.Add(lblHuongDan);
            Controls.Add(lblTitle);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "TraCuuForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Thông tin tra cứu hóa đơn";
            //Load += TraCuuForm_Load;
            gbNguoiBan.ResumeLayout(false);
            gbNguoiBan.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblHuongDan;
        private System.Windows.Forms.Label lblMaTraCuuDesc;
        private System.Windows.Forms.TextBox txtMaTraCuu;
        private System.Windows.Forms.Label lblLink1Desc;
        private System.Windows.Forms.LinkLabel llLinkTraCuu;
        private System.Windows.Forms.Label lblLink2Desc;
        private System.Windows.Forms.Label lblLink3Desc;
        private System.Windows.Forms.Label lblLink4Desc;
        private System.Windows.Forms.Label lblLink5Desc;
        private System.Windows.Forms.GroupBox gbNguoiBan;
        private System.Windows.Forms.Label lblTenNguoiBanDesc;
        private System.Windows.Forms.TextBox txtTenNguoiBan;
        private System.Windows.Forms.TextBox txtMSTNguoiBan;
        private System.Windows.Forms.Label lblMSTDesc;
        private System.Windows.Forms.TextBox txtDiaChiNguoiBan;
        private System.Windows.Forms.Label lblDiaChiDesc;
        private System.Windows.Forms.Button btnDong;
    }
}