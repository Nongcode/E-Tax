using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace E_Tax
{
    public enum DownloadItemState { Pending, Done, Failed }

    public sealed record DownloadJobSummary(string JobId, string Title, DateTime CreatedUtc, int Pending, int Done, int Failed);

    /// <summary>
    /// Lưu tiến độ tải theo từng khóa HĐ: {directory}\{jobId}.json, ghi nguyên tử (.tmp rồi Move).
    /// Trạng thái nằm trong bộ nhớ đến khi Flush.
    /// </summary>
    public sealed class DownloadJobStore
    {
        private const int MaxErrorLength = 200;
        private static readonly Regex JobIdPattern = new(@"^[0-9a-f]{32}\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex TokenPattern = new("token=[^&\\s\"']*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private sealed class JobItem
        {
            [JsonPropertyName("s")] public DownloadItemState State { get; set; }
            [JsonPropertyName("e")] public string? Error { get; set; }
        }

        private sealed class JobFile
        {
            [JsonPropertyName("title")] public string Title { get; set; } = "";
            [JsonPropertyName("created")] public DateTime CreatedUtc { get; set; }
            [JsonPropertyName("items")] public Dictionary<string, JobItem> Items { get; set; } = new(StringComparer.Ordinal);
        }

        private readonly string _directory;
        private readonly object _lock = new();
        private readonly Dictionary<string, JobFile> _jobs = new(StringComparer.Ordinal);

        public DownloadJobStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Thư mục không hợp lệ.", nameof(directory));
            _directory = Path.GetFullPath(directory);
        }

        public static string DefaultDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "jobs");

        public string Create(string title, IEnumerable<string> itemKeys)
        {
            var job = new JobFile { Title = title ?? "", CreatedUtc = DateTime.UtcNow };
            foreach (string key in itemKeys)
                if (key != null) job.Items.TryAdd(key, new JobItem { State = DownloadItemState.Pending });

            string id = Guid.NewGuid().ToString("N");
            lock (_lock)
            {
                _jobs[id] = job;
                Save(id, job);
            }
            return id;
        }

        public void Mark(string jobId, string key, DownloadItemState state, string? error)
        {
            lock (_lock)
            {
                var job = Get(jobId);
                if (!job.Items.TryGetValue(key, out var item))
                    throw new KeyNotFoundException("Khóa không thuộc job: " + key);
                item.State = state;
                item.Error = state == DownloadItemState.Failed ? Sanitize(error) : null;
            }
        }

        public void Flush(string jobId)
        {
            lock (_lock) Save(jobId, Get(jobId));
        }

        public IReadOnlyList<string> PendingKeys(string jobId)
        {
            lock (_lock)
                return Get(jobId).Items.Where(p => p.Value.State == DownloadItemState.Pending).Select(p => p.Key).ToList();
        }

        public IReadOnlySet<string> DoneKeys(string jobId)
        {
            lock (_lock)
                return Get(jobId).Items.Where(p => p.Value.State == DownloadItemState.Done).Select(p => p.Key).ToHashSet(StringComparer.Ordinal);
        }

        public int RetryFailed(string jobId)
        {
            lock (_lock)
            {
                var job = Get(jobId);
                int n = 0;
                foreach (var item in job.Items.Values.Where(i => i.State == DownloadItemState.Failed))
                {
                    item.State = DownloadItemState.Pending;
                    item.Error = null;
                    n++;
                }
                if (n > 0) Save(jobId, job);
                return n;
            }
        }

        /// <summary>Job mới nhất cùng title và đúng cùng tập khóa mà còn mục Pending/Failed; không có thì null.</summary>
        public string? FindResumable(string title, IEnumerable<string> keys) => Find(title, keys, requireUnfinished: true);

        /// <summary>Job mới nhất cùng title và đúng cùng tập khóa, ở mọi trạng thái; không có thì null.</summary>
        public string? FindLatest(string title, IEnumerable<string> keys) => Find(title, keys, requireUnfinished: false);

        /// <summary>Xóa job có title là đường dẫn nằm dưới workRoot mà thư mục đó không còn; không bao giờ đụng job có title ngoài workRoot.</summary>
        public int DeleteOrphans(string workRoot)
        {
            string root = Path.GetFullPath(workRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            int removed = 0;
            lock (_lock)
            {
                foreach (string id in JobIds())
                {
                    try
                    {
                        JobFile? job = TryLoad(id);
                        if (job == null || !Path.IsPathFullyQualified(job.Title)) continue;
                        string full = Path.GetFullPath(job.Title);
                        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || full.Length == root.Length) continue;
                        if (Directory.Exists(full)) continue;
                        Delete(id);
                        removed++;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { }
                }
            }
            return removed;
        }

        private string? Find(string title, IEnumerable<string> keys, bool requireUnfinished)
        {
            var wanted = new HashSet<string>(keys.Where(k => k != null), StringComparer.Ordinal);
            lock (_lock)
            {
                string? bestId = null;
                DateTime bestCreated = DateTime.MinValue;
                foreach (string id in JobIds())
                {
                    JobFile? job = TryLoad(id);
                    if (job == null || job.Title != (title ?? "")) continue;
                    if (!wanted.SetEquals(job.Items.Keys)) continue;
                    if (requireUnfinished && !job.Items.Values.Any(i => i.State != DownloadItemState.Done)) continue;
                    if (bestId == null || job.CreatedUtc > bestCreated)
                    {
                        bestId = id;
                        bestCreated = job.CreatedUtc;
                    }
                }
                return bestId;
            }
        }

        public IReadOnlyList<DownloadJobSummary> List()
        {
            lock (_lock)
            {
                var result = new List<DownloadJobSummary>();
                foreach (string id in JobIds())
                {
                    JobFile? job = TryLoad(id);
                    if (job == null) continue;
                    result.Add(new DownloadJobSummary(id, job.Title, job.CreatedUtc,
                        job.Items.Values.Count(i => i.State == DownloadItemState.Pending),
                        job.Items.Values.Count(i => i.State == DownloadItemState.Done),
                        job.Items.Values.Count(i => i.State == DownloadItemState.Failed)));
                }
                return result.OrderByDescending(s => s.CreatedUtc).ToList();
            }
        }

        public void Delete(string jobId)
        {
            lock (_lock)
            {
                string path = PathOf(jobId);
                _jobs.Remove(jobId);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static string? Sanitize(string? error)
        {
            if (string.IsNullOrEmpty(error)) return error;
            string clean = TokenPattern.Replace(error, "token=***");
            return clean.Length > MaxErrorLength ? clean.Substring(0, MaxErrorLength) : clean;
        }

        private string PathOf(string jobId)
        {
            if (!JobIdPattern.IsMatch(jobId ?? "")) throw new ArgumentException("Mã job không hợp lệ.", nameof(jobId));
            return Path.Combine(_directory, jobId + ".json");
        }

        private JobFile Get(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var cached)) return cached;
            var job = TryLoad(jobId) ?? throw new KeyNotFoundException("Không tìm thấy job: " + jobId);
            _jobs[jobId] = job;
            return job;
        }

        private IEnumerable<string> JobIds()
        {
            if (!Directory.Exists(_directory)) return Array.Empty<string>();
            return Directory.GetFiles(_directory, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => JobIdPattern.IsMatch(n))
                .ToList();
        }

        // Ưu tiên bản trong bộ nhớ (chưa Flush) rồi mới đọc file; file hỏng thì bỏ qua, không ném lỗi.
        private JobFile? TryLoad(string id)
        {
            if (_jobs.TryGetValue(id, out var cached)) return cached;
            try
            {
                string path = PathOf(id);
                if (!File.Exists(path)) return null;
                var job = JsonSerializer.Deserialize<JobFile>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
                if (job == null || job.Items == null || job.Items.Values.Any(i => i == null)) return null;
                return job;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private void Save(string jobId, JobFile job)
        {
            string path = PathOf(jobId);
            Directory.CreateDirectory(_directory);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(job, JsonOptions), new UTF8Encoding(false));
            File.Move(temp, path, true);
        }
    }

    /// <summary>Thư mục làm việc bền vững cho một lượt tải: tên là SHA-256 của tập khóa HĐ.</summary>
    public static class DownloadWorkFolder
    {
        private static readonly Regex HashName = new(@"^[0-9a-f]{64}\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string DefaultRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "E-Tax", "work");

        public static string PathFor(string root, IEnumerable<string> keys)
        {
            var distinct = keys.Where(k => k != null).Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal);
            byte[] hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', distinct)));
            return Path.Combine(root, Convert.ToHexString(hash).ToLowerInvariant());
        }

        /// <summary>Xóa thư mục con trực tiếp tên đúng 64 ký tự hex thường, quá hạn; lỗi một thư mục không dừng các thư mục khác.</summary>
        public static int CleanupStale(string root, DateTime utcNow, TimeSpan maxAge)
        {
            if (!Directory.Exists(root)) return 0;
            int removed = 0;
            IEnumerable<string> dirs;
            try { dirs = Directory.GetDirectories(root).ToList(); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return 0; }

            foreach (string dir in dirs)
            {
                try
                {
                    var info = new DirectoryInfo(dir);
                    if (!HashName.IsMatch(info.Name)) continue;
                    if (info.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
                    if (utcNow - info.LastWriteTimeUtc <= maxAge) continue;
                    info.Delete(true);
                    removed++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
            return removed;
        }

        /// <summary>Xóa file gộp cũ và file tạm của PdfMerger (.{name}.*.tmp) để bản cũ không lọt vào kết quả; không xóa được thì ném lỗi.</summary>
        public static void RemoveMergedOutputs(string pdfFolder, string mergedName)
        {
            if (!Directory.Exists(pdfFolder)) return;
            string merged = Path.Combine(pdfFolder, mergedName);
            if (File.Exists(merged)) File.Delete(merged);
            foreach (string tmp in Directory.GetFiles(pdfFolder, "." + mergedName + ".*.tmp"))
                File.Delete(tmp);
        }

        /// <summary>
        /// Quyết định dùng lại một HĐ đã Done: ZIP và PDF phải có và không rỗng; ZIP đọc được; nếu ZIP có entry .xml
        /// thì file XML kết quả cũng phải có và không rỗng. ZIP hỏng thì false.
        /// </summary>
        public static bool CanSkipInvoice(string zipPath, string pdfPath, string xmlPath)
        {
            if (!CanSkip(DownloadItemState.Done, new[] { zipPath, pdfPath })) return false;
            try
            {
                using var zip = System.IO.Compression.ZipFile.OpenRead(zipPath);
                bool hasXml = zip.Entries.Any(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                return !hasXml || CanSkip(DownloadItemState.Done, new[] { xmlPath });
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>Chỉ bỏ qua mục đã Done khi mọi file kết quả của nó còn đủ và không rỗng.</summary>
        public static bool CanSkip(DownloadItemState state, IEnumerable<string> requiredFiles)
        {
            if (state != DownloadItemState.Done) return false;
            foreach (string f in requiredFiles)
            {
                try
                {
                    var fi = new FileInfo(f);
                    if (!fi.Exists || fi.Length == 0) return false;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
            }
            return true;
        }
    }
}
