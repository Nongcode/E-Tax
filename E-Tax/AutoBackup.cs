using System.Globalization;

namespace E_Tax
{
    /// <summary>
    /// Sao lưu tự động kho hóa đơn và ghi chú, tối đa một lần mỗi ngày, giữ các bản mới nhất.
    /// Chỉ xóa file do chính cơ chế này tạo (auto-yyyyMMdd_HHmmss.zip).
    /// </summary>
    public static class AutoBackup
    {
        public const string Prefix = "auto-";
        private const string StampFormat = "yyyyMMdd_HHmmss";
        public const int DefaultKeep = 7;
        private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

        public static string DefaultDir { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "backups");

        public static string FileNameFor(DateTime at) => Prefix + at.ToString(StampFormat, CultureInfo.InvariantCulture) + ".zip";

        /// <returns>Đường dẫn bản vừa tạo; null nếu chưa tới hạn hoặc kho trống.</returns>
        public static string? RunIfDue(string storeRoot, string? notesPath, string backupDir, DateTime now, int keep = DefaultKeep)
        {
            if (!Directory.Exists(storeRoot) || !Directory.EnumerateFiles(storeRoot, "*.json", SearchOption.AllDirectories).Any())
                return null;

            var existing = List(backupDir);
            if (existing.Count > 0 && now - existing[^1].At < Interval) return null;

            string path = Path.Combine(backupDir, FileNameFor(now));
            BackupService.Create(storeRoot, notesPath, path);

            foreach (var old in List(backupDir).SkipLast(Math.Max(1, keep)))
            {
                try { File.Delete(old.Path); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
            return path;
        }

        // Các bản tự động, cũ nhất trước; bỏ qua file không đúng tên
        private static List<(string Path, DateTime At)> List(string backupDir)
        {
            if (!Directory.Exists(backupDir)) return new();
            var list = new List<(string, DateTime)>();
            foreach (string file in Directory.EnumerateFiles(backupDir, Prefix + "*.zip"))
            {
                string stamp = Path.GetFileNameWithoutExtension(file).Substring(Prefix.Length);
                if (DateTime.TryParseExact(stamp, StampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at))
                    list.Add((file, at));
            }
            return list.OrderBy(x => x.Item2).ToList();
        }
    }
}
