using System;
using System.Diagnostics; // Cần thêm để mở link
using System.Windows.Forms;

namespace E_Tax // Đảm bảo đúng namespace
{
    public partial class TraCuuForm : Form
    {
        // Lưu lại link để mở khi click
        private string _linkUrl;

        // Constructor nhận dữ liệu
        public TraCuuForm(string maTraCuu, string linkTraCuu, string tenNguoiBan, string mstNguoiBan, string diaChiNguoiBan)
        {
            InitializeComponent();

            // Hiển thị dữ liệu lên các control
            txtMaTraCuu.Text = maTraCuu;
            llLinkTraCuu.Text = linkTraCuu;
            _linkUrl = linkTraCuu; // Lưu lại URL

            // Hiển thị thông tin người bán (giả sử có các TextBox tương ứng)
            txtTenNguoiBan.Text = tenNguoiBan;
            txtMSTNguoiBan.Text = mstNguoiBan;
            txtDiaChiNguoiBan.Text = diaChiNguoiBan;

            // Gắn sự kiện click cho LinkLabel
            llLinkTraCuu.LinkClicked += LlLinkTraCuu_LinkClicked;
            // Gắn sự kiện click cho nút Đóng
            btnDong.Click += BtnDong_Click;
        }

        // Sự kiện khi click vào LinkLabel
        private void LlLinkTraCuu_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                // Mở link bằng trình duyệt mặc định
                Process.Start(new ProcessStartInfo(_linkUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở link: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Sự kiện khi click nút Đóng
        private void BtnDong_Click(object sender, EventArgs e)
        {
            this.Close(); // Đóng form
        }
    }
}