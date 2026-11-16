using System.Text;

namespace E_Tax
{
    /// <summary>
    /// Ghi log ra file theo ngày: %LocalAppData%\E-Tax\logs\etax_yyyyMMdd.log (giữ 30 ngày).
    /// Không bao giờ ném exception ra ngoài.
    /// </summary>
    internal static class Logger
    {
        private const int RetentionDays = 30;

        private static readonly object _lock = new object();
        private static bool _cleanedUp;

        public static string LogDirectory { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "E-Tax", "logs");

        public static void Write(string message)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(LogDirectory);
                    if (!_cleanedUp)
                    {
                        _cleanedUp = true;
                        DeleteOldLogs();
                    }

                    string filePath = Path.Combine(LogDirectory, $"etax_{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(filePath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch
            {
                // Lỗi ghi log không được làm hỏng chức năng chính
            }
        }

        private static void DeleteOldLogs()
        {
            DateTime threshold = DateTime.Now.AddDays(-RetentionDays);
            foreach (string file in Directory.GetFiles(LogDirectory, "etax_*.log"))
            {
                if (File.GetLastWriteTime(file) < threshold)
                {
                    File.Delete(file);
                }
            }
        }
    }
}
