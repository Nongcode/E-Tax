using System;
using System.Windows.Forms;

namespace E_Tax // Đảm bảo đây là namespace của bạn
{
    public partial class ProcessingForm : Form
    {
        public ProcessingForm()
        {
            InitializeComponent();
        }

        // Tạo một phương thức public để Form1 có thể cập nhật chữ
        public void UpdateStatus(string message)
        {
            // Kiểm tra xem có cần gọi Invoke không (để đảm bảo an toàn thread)
            if (label1.InvokeRequired)
            {
                label1.Invoke(new Action(() => label1.Text = message));
            }
            else
            {
                label1.Text = message;
            }
        }
    }
}