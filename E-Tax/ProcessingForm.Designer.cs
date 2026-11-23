namespace E_Tax
{
    partial class ProcessingForm
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
            label1 = new Label();
            lblSummary = new Label();
            lblElapsed = new Label();
            progressBar1 = new ProgressBar();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(20, 20);
            label1.Name = "label1";
            label1.Size = new Size(380, 44);
            label1.TabIndex = 0;
            label1.Text = "Đang xử lý dữ liệu, vui lòng chờ...";
            // 
            // lblSummary
            // 
            lblSummary.Location = new Point(20, 68);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(380, 20);
            lblSummary.TabIndex = 1;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(20, 94);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(380, 23);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 2;
            // 
            // lblElapsed
            // 
            lblElapsed.Location = new Point(20, 124);
            lblElapsed.Name = "lblElapsed";
            lblElapsed.Size = new Size(200, 20);
            lblElapsed.TabIndex = 3;
            lblElapsed.Text = "Đã chạy: 00:00";
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(300, 156);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 30);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Hủy";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // ProcessingForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(420, 204);
            ControlBox = false;
            Controls.Add(btnCancel);
            Controls.Add(lblElapsed);
            Controls.Add(progressBar1);
            Controls.Add(lblSummary);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ProcessingForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Đang xử lý";
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private ProgressBar progressBar1;
        private Label lblSummary;
        private Label lblElapsed;
        private Button btnCancel;
    }
}