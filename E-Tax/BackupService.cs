using System.IO.Compression;
using System.Text.RegularExpressions;

namespace E_Tax
{
    /// <summary>
    /// Sao lưu/khôi phục kho HĐ cục bộ. Zip: store/{đường dẫn tương đối} và notes/notes.json (dấu / làm phân cách).
    /// </summary>
    public static class BackupService
    {
        private const string StorePrefix = "store";
        private const string NotesEntry = "notes/notes.json";
        private static readonly Regex CompanyPattern = new(@"^[0-9]+(-[0-9]+)?\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static void Create(string storeRoot, string? notesPath, string zipPath)
        {
            string root = Path.GetFullPath(storeRoot);
            string zipFull = Path.GetFullPath(zipPath);
            if (IsInside(root, zipFull))
                throw new ArgumentException("File sao lưu không được nằm trong thư mục kho.", nameof(zipPath));

            Directory.CreateDirectory(Path.GetDirectoryName(zipFull)!);
            string tmp = zipFull + ".tmp";
            try
            {
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var za = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    if (Directory.Exists(root))
                    {
                        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                        {
                            if (file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                            string rel = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
                            za.CreateEntryFromFile(file, StorePrefix + "/" + rel, CompressionLevel.Optimal);
                        }
                    }
                    if (!string.IsNullOrEmpty(notesPath) && File.Exists(notesPath))
                        za.CreateEntryFromFile(notesPath, NotesEntry, CompressionLevel.Optimal);
                }
                File.Move(tmp, zipFull, true);
            }
            catch
            {
                try { File.Delete(tmp); } catch { }
                throw;
            }
        }

        /// <returns>Số file đã ghi.</returns>
        public static int Restore(string zipPath, string storeRoot, string? notesPath, string? onlyCompany, bool overwrite)
        {
            if (onlyCompany != null && !CompanyPattern.IsMatch(onlyCompany))
                throw new ArgumentException("Mã doanh nghiệp không hợp lệ.", nameof(onlyCompany));

            string root = Path.GetFullPath(storeRoot);
            using var za = ZipFile.OpenRead(zipPath);

            // Kiểm tra toàn bộ entry trước khi ghi bất cứ thứ gì.
            var plan = new List<(ZipArchiveEntry Entry, string Target)>();
            foreach (var entry in za.Entries)
            {
                string[] parts = SplitAndValidate(entry.FullName);
                bool isDir = entry.FullName.EndsWith('/') || entry.FullName.EndsWith(Path.DirectorySeparatorChar);
                if (parts[0] == StorePrefix)
                {
                    if (parts.Length < 2 || !CompanyPattern.IsMatch(parts[1]))
                        throw new InvalidDataException($"Entry không hợp lệ: {entry.FullName}");
                    if (!isDir && parts.Length < 3)
                        throw new InvalidDataException($"Entry không hợp lệ: {entry.FullName}");
                    string target = Path.GetFullPath(Path.Combine(root, Path.Combine(parts.Skip(1).ToArray())));
                    if (!IsInside(root, target))
                        throw new InvalidDataException($"Entry ngoài thư mục kho: {entry.FullName}");
                    if (isDir || (onlyCompany != null && parts[1] != onlyCompany)) continue;
                    plan.Add((entry, target));
                }
                else if (!isDir && string.Join('/', parts) == NotesEntry)
                {
                    if (onlyCompany != null || string.IsNullOrEmpty(notesPath)) continue;
                    plan.Add((entry, Path.GetFullPath(notesPath)));
                }
                else if (!(isDir && parts.Length == 1 && parts[0] == "notes"))
                {
                    throw new InvalidDataException($"Entry không hợp lệ: {entry.FullName}");
                }
            }

            int written = 0;
            foreach (var (entry, target) in plan)
            {
                if (!overwrite && File.Exists(target)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                string tmp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var src = entry.Open())
                    using (var dst = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        src.CopyTo(dst);
                    File.Move(tmp, target, true);
                    written++;
                }
                catch
                {
                    try { File.Delete(tmp); } catch { }
                    throw;
                }
            }
            return written;
        }

        private static string[] SplitAndValidate(string name)
        {
            string n = name.Replace(Path.DirectorySeparatorChar, '/');
            if (n.Length == 0 || n.StartsWith('/') || Path.IsPathRooted(n) || n.Contains(':'))
                throw new InvalidDataException($"Entry không hợp lệ: {name}");
            var parts = n.TrimEnd('/').Split('/');
            foreach (string p in parts)
                if (p.Length == 0 || p == "." || p == "..")
                    throw new InvalidDataException($"Entry không hợp lệ: {name}");
            return parts;
        }

        private static bool IsInside(string root, string path)
        {
            string r = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(r, StringComparison.OrdinalIgnoreCase);
        }
    }
}
