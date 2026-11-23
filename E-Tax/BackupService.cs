using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace E_Tax
{
    /// <summary>
    /// Sao lưu/khôi phục kho HĐ cục bộ. Zip: store/{đường dẫn tương đối} và notes/notes.json (dấu / làm phân cách).
    /// Sao lưu được kiểm tra bằng đúng quy tắc của khôi phục. Khôi phục gộp file tháng theo khóa hóa đơn.
    /// </summary>
    public static class BackupService
    {
        private const string StorePrefix = "store";
        private const string NotesEntry = "notes/notes.json";
        private const string DesktopIni = "desktop.ini";
        private static readonly Regex CompanyPattern = new(@"^[0-9]+(-[0-9]+)?\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Kết quả kiểm tra zip: file ghi theo quy tắc ghi đè và nội dung file tháng theo từng doanh nghiệp
        private sealed record ArchivePlan(List<(ZipArchiveEntry Entry, string Target)> Files, Dictionary<string, Dictionary<string, string>> Months);

        /// <returns>Các cảnh báo cho người dùng (vd ghi chú hỏng nên không được đưa vào bản sao lưu).</returns>
        public static IReadOnlyList<string> Create(string storeRoot, string? notesPath, string zipPath)
        {
            string root = Path.GetFullPath(storeRoot);
            string zipFull = Path.GetFullPath(zipPath);
            if (IsInside(root, zipFull))
                throw new ArgumentException("File sao lưu không được nằm trong thư mục kho.", nameof(zipPath));

            Directory.CreateDirectory(Path.GetDirectoryName(zipFull)!);
            var warnings = new List<string>();
            string tmp = zipFull + ".tmp";
            try
            {
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var za = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    if (Directory.Exists(root))
                    {
                        // Chỉ lấy thư mục doanh nghiệp hợp lệ: desktop.ini hay thư mục lạ ở gốc kho không được làm hỏng bản sao lưu
                        foreach (string company in Directory.EnumerateDirectories(root).Where(d => CompanyPattern.IsMatch(Path.GetFileName(d))))
                        {
                            foreach (string file in Directory.EnumerateFiles(company, "*", SearchOption.AllDirectories))
                            {
                                if (file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                                string rel = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
                                za.CreateEntryFromFile(file, StorePrefix + "/" + rel, CompressionLevel.Optimal);
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(notesPath) && File.Exists(notesPath))
                    {
                        // Ghi chú hỏng không được chặn việc sao lưu hóa đơn; file gốc vẫn nằm nguyên trên đĩa
                        if (NotesStore.IsValidContent(File.ReadAllText(notesPath)))
                            za.CreateEntryFromFile(notesPath, NotesEntry, CompressionLevel.Optimal);
                        else
                            warnings.Add("File ghi chú bị hỏng nên không có trong bản sao lưu này.");
                    }
                }

                using (var check = ZipFile.OpenRead(tmp))
                    BuildPlan(check, root, notesPath, null);

                File.Move(tmp, zipFull, true);
                return warnings;
            }
            catch
            {
                try { File.Delete(tmp); } catch { }
                throw;
            }
        }

        /// <returns>Số file đã ghi.</returns>
        public static int Restore(string zipPath, string storeRoot, string? notesPath, string? onlyCompany, bool overwrite) =>
            Restore(zipPath, storeRoot, notesPath, onlyCompany, overwrite, out _);

        /// <summary>
        /// Kiểm tra toàn bộ zip và kho hiện tại, tạo bản sao lưu an toàn cạnh thư mục kho, rồi mới ghi. File tháng được gộp theo
        /// khóa hóa đơn: bản ghi chỉ có ở một bên đều được giữ; trùng khóa thì overwrite=true lấy bản sao lưu, false giữ kho.
        /// File đính kèm và ghi chú: overwrite=false không đè file đã có.
        /// </summary>
        /// <param name="safetyBackup">Đường dẫn bản sao lưu an toàn; null nếu không cần (không ghi gì hoặc kho đang trống).</param>
        /// <returns>Số file đã ghi.</returns>
        public static int Restore(string zipPath, string storeRoot, string? notesPath, string? onlyCompany, bool overwrite, out string? safetyBackup)
        {
            safetyBackup = null;
            if (onlyCompany != null && !CompanyPattern.IsMatch(onlyCompany))
                throw new ArgumentException("Mã doanh nghiệp không hợp lệ.", nameof(onlyCompany));

            string root = Path.GetFullPath(storeRoot);
            using var za = ZipFile.OpenRead(zipPath);

            // Kiểm tra và tính xong mọi thứ trước khi ghi bất cứ gì
            var plan = BuildPlan(za, root, notesPath, onlyCompany);
            var merges = new List<(string Path, string Json)>();
            foreach (var (company, months) in plan.Months)
                merges.AddRange(LocalInvoiceStore.PlanRestoreMerge(Path.Combine(root, company), months, overwrite));
            // Bản cách ly .corrupt trong kho là bằng chứng gốc: không bao giờ bị ghi đè
            var files = plan.Files.Where(f => !File.Exists(f.Target)
                || (overwrite && !f.Target.EndsWith(".corrupt", StringComparison.OrdinalIgnoreCase))).ToList();

            if ((merges.Count > 0 || files.Count > 0) && HasCurrentData(root, notesPath))
            {
                safetyBackup = SafetyBackupPath(root);
                Create(root, notesPath, safetyBackup);
            }

            foreach (var (path, json) in merges)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                LocalInvoiceStore.WriteAtomic(path, json);
            }

            foreach (var (entry, target) in files)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                string tmp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var src = entry.Open())
                    using (var dst = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        src.CopyTo(dst);
                        dst.Flush(true);
                    }
                    File.Move(tmp, target, true);
                }
                catch
                {
                    try { File.Delete(tmp); } catch { }
                    throw;
                }
            }
            return merges.Count + files.Count;
        }

        // Kiểm tra mọi entry và mọi file JSON của kho; dùng chung cho Create (kiểm tra zip vừa tạo) và Restore.
        private static ArchivePlan BuildPlan(ZipArchive za, string root, string? notesPath, string? onlyCompany)
        {
            var plan = new ArchivePlan(new List<(ZipArchiveEntry, string)>(), new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal));
            foreach (var entry in za.Entries)
            {
                string[] parts = SplitAndValidate(entry.FullName);
                bool isDir = entry.FullName.EndsWith('/') || entry.FullName.EndsWith(Path.DirectorySeparatorChar);
                if (parts[0] == StorePrefix)
                {
                    if (parts.Length == 1 && isDir) continue; // entry thư mục store/ trần
                    // Bản sao lưu cũ có thể chứa desktop.ini ở gốc kho: bỏ qua, không khôi phục
                    if (parts.Length == 2 && !isDir && parts[1].Equals(DesktopIni, StringComparison.OrdinalIgnoreCase)) continue;
                    if (parts.Length < 2 || !CompanyPattern.IsMatch(parts[1]))
                        throw new InvalidDataException($"Entry không hợp lệ: {entry.FullName}");
                    if (!isDir && parts.Length < 3)
                        throw new InvalidDataException($"Entry không hợp lệ: {entry.FullName}");
                    string target = Path.GetFullPath(Path.Combine(root, Path.Combine(parts.Skip(1).ToArray())));
                    if (!IsInside(root, target))
                        throw new InvalidDataException($"Entry ngoài thư mục kho: {entry.FullName}");
                    if (isDir) continue;

                    bool selected = onlyCompany == null || parts[1] == onlyCompany;
                    if (parts.Length == 3 && LocalInvoiceStore.IsDataFileName(parts[2]))
                    {
                        // File tháng của mọi doanh nghiệp đều phải đọc được, kể cả khi chỉ khôi phục một doanh nghiệp
                        string json = ReadEntry(entry);
                        LocalInvoiceStore.ValidateDataJson(json, parts[2]);
                        if (selected)
                        {
                            if (!plan.Months.TryGetValue(parts[1], out var months))
                                plan.Months[parts[1]] = months = new Dictionary<string, string>(StringComparer.Ordinal);
                            months[parts[2]] = json;
                        }
                        continue;
                    }
                    if (selected) plan.Files.Add((entry, target));
                }
                else if (!isDir && string.Join('/', parts) == NotesEntry)
                {
                    if (!NotesStore.IsValidContent(ReadEntry(entry)))
                        throw new InvalidDataException("File ghi chú trong bản sao lưu bị hỏng.");
                    if (onlyCompany != null || string.IsNullOrEmpty(notesPath)) continue;
                    plan.Files.Add((entry, Path.GetFullPath(notesPath)));
                }
                else if (!(isDir && parts.Length == 1 && parts[0] == "notes"))
                {
                    throw new InvalidDataException($"Entry không hợp lệ: {entry.FullName}");
                }
            }
            return plan;
        }

        private static string ReadEntry(ZipArchiveEntry entry)
        {
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static bool HasCurrentData(string root, string? notesPath) =>
            (Directory.Exists(root) && Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Any())
            || (!string.IsNullOrEmpty(notesPath) && File.Exists(notesPath));

        // Cạnh thư mục kho (không nằm trong kho); tên riêng từng lần nên không đè bản an toàn trước đó
        private static string SafetyBackupPath(string root)
        {
            string trimmed = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string parent = Path.GetDirectoryName(trimmed)
                ?? throw new InvalidOperationException("Không xác định được nơi lưu bản sao lưu an toàn.");
            return Path.Combine(parent, $"{Path.GetFileName(trimmed)}-safety-{DateTime.Now:yyyyMMdd_HHmmss}-{Guid.NewGuid():N}.zip");
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
