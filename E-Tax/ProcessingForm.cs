using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace E_Tax // Đảm bảo đây là namespace của bạn
{
    /// <summary>
    /// Cửa sổ tiến trình có nút Hủy. Là cửa sổ top-level thuộc form chủ (Show(owner)), nên form chủ có thể
    /// bị vô hiệu hóa trong khi nút Hủy vẫn bấm được. Report/UpdateStatus an toàn từ mọi thread.
    /// </summary>
    public partial class ProcessingForm : Form
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly CancellationToken _token;
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer { Interval = 500 };
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private volatile bool _closing;

        public ProcessingForm()
        {
            InitializeComponent();
            _token = _cts.Token;
            _timer.Tick += (_, _) => lblElapsed.Text = $"Đã chạy: {(int)_stopwatch.Elapsed.TotalMinutes:00}:{_stopwatch.Elapsed.Seconds:00}";
            Disposed += (_, _) =>
            {
                _timer.Dispose();
                _cts.Dispose();
            };
        }

        /// <summary>Token bị hủy khi người dùng bấm "Hủy".</summary>
        public CancellationToken CancellationToken => _token;

        /// <summary>
        /// Hiện cửa sổ như một cửa sổ con của owner rồi vô hiệu hóa owner. Chính cửa sổ này luôn Enabled
        /// để nút Hủy bấm được.
        /// </summary>
        internal static ProcessingForm ShowFor(Form owner)
        {
            var form = new ProcessingForm();
            form.Show(owner);
            owner.Enabled = false;
            form.Enabled = true;
            return form;
        }

        // Cập nhật dòng trạng thái
        public void UpdateStatus(string message) => RunOnUi(() => label1.Text = message);

        internal void Report(BatchProgress p) => RunOnUi(() =>
        {
            lblSummary.Text = p.Summary;
            if (p.Total <= 0)
            {
                progressBar1.Style = ProgressBarStyle.Marquee;
                return;
            }
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.Maximum = p.Total;
            progressBar1.Value = Math.Max(0, Math.Min(p.Done, p.Total));
        });

        /// <summary>
        /// Chạy action trên UI thread; bỏ qua nếu form đang đóng/đã hủy (callback tiến trình có thể đến trễ).
        /// </summary>
        private void RunOnUi(Action action)
        {
            if (_closing || IsDisposed || !IsHandleCreated) return;
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (!_closing && !IsDisposed) action();
                    }));
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { } // handle đã bị hủy giữa chừng
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _stopwatch.Restart();
            _timer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _closing = true;
            _timer.Stop();
            base.OnFormClosing(e);
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            if (_closing || _cts.IsCancellationRequested) return;
            _cts.Cancel();
            btnCancel.Enabled = false;
            btnCancel.Text = "Đang hủy...";
        }
    }
}
