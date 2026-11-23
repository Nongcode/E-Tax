using System.Text;

namespace E_Tax
{
    /// <summary>
    /// Bắt lỗi chưa xử lý toàn ứng dụng. Chỉ log loại lỗi và stack trace, không log Message
    /// (có thể chứa URL hoặc token). Handler không bao giờ ném exception.
    /// </summary>
    internal static class AppErrorHandler
    {
        public static void Install()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => Handle("ThreadException", e.Exception, showDialog: true);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Handle("UnhandledException", e.ExceptionObject as Exception, showDialog: true);
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Handle("UnobservedTaskException", e.Exception, showDialog: false);
                e.SetObserved();
            };
        }

        /// <summary>Nội dung log: nguồn, loại lỗi và stack trace của lỗi và các lỗi lồng; không có Message.</summary>
        internal static string BuildLogEntry(string source, Exception? ex)
        {
            var sb = new StringBuilder();
            sb.Append("❌ Lỗi chưa xử lý (").Append(source).Append(')');
            if (ex == null)
            {
                sb.Append(": không có thông tin lỗi");
                return sb.ToString();
            }

            int depth = 0;
            for (Exception? current = ex; current != null && depth < 10; current = current.InnerException, depth++)
            {
                sb.AppendLine();
                sb.Append(depth == 0 ? "Loại: " : "Lỗi trong: ").Append(current.GetType().FullName);
                if (current is AggregateException agg)
                {
                    foreach (var inner in agg.InnerExceptions)
                        sb.AppendLine().Append("  - ").Append(inner.GetType().FullName).Append(' ').Append(inner.StackTrace);
                }
                sb.AppendLine().Append(current.StackTrace);
            }
            return sb.ToString();
        }

        internal static string BuildDialogText() =>
            "Ứng dụng gặp lỗi không mong muốn." + Environment.NewLine + Environment.NewLine +
            "Chi tiết kỹ thuật đã được ghi vào thư mục:" + Environment.NewLine + Logger.LogDirectory + Environment.NewLine + Environment.NewLine +
            "Bạn có thể gửi file log mới nhất để được hỗ trợ.";

        private static void Handle(string source, Exception? ex, bool showDialog)
        {
            try
            {
                Logger.Write(BuildLogEntry(source, ex));
                if (showDialog)
                {
                    MessageBox.Show(BuildDialogText(), "E-Tax", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {
                // Handler lỗi không được làm sập ứng dụng thêm lần nữa
            }
        }
    }
}
