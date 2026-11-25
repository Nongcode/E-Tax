using System.Text.Json;

namespace E_Tax
{
    /// <summary>
    /// Ghi chú theo Id hóa đơn, lưu JSON tại %AppData%\E-Tax\notes.json.
    /// Lần đầu tự chép file notes.json cũ (cạnh exe) sang, file cũ giữ nguyên.
    /// </summary>
    internal sealed class NotesStore
    {
        public static string DefaultPath { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "notes.json");

        private readonly string _filePath;
        private readonly string? _legacyFilePath;
        private Dictionary<string, string> _notes = new Dictionary<string, string>();

        public NotesStore(string filePath, string? legacyFilePath)
        {
            _filePath = filePath;
            _legacyFilePath = legacyFilePath;
        }

        /// <summary>Lý do chưa đọc được ghi chú; khác null thì Save bị từ chối để không ghi đè ghi chú cũ bằng tập rỗng.</summary>
        public string? LoadProblem { get; private set; }

        public void Load()
        {
            _notes = new Dictionary<string, string>();
            LoadProblem = null;

            bool migrate = !File.Exists(_filePath) && _legacyFilePath != null && File.Exists(_legacyFilePath);
            string source = migrate ? _legacyFilePath! : _filePath;
            if (!File.Exists(source)) return;

            try
            {
                _notes = ReadFile(source);
            }
            catch (JsonException)
            {
                string kept = KeepCorruptCopy(source);
                LoadProblem = $"File ghi chú bị hỏng: {Path.GetFileName(source)}. Bản sao được giữ tại {kept}. " +
                              "Chưa thể lưu ghi chú cho đến khi sửa file này hoặc khôi phục từ bản sao lưu.";
                Logger.Write("❌ " + LoadProblem);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LoadProblem = $"Không đọc được file ghi chú: {ex.Message}. Chưa thể lưu ghi chú cho đến khi đọc lại được file này.";
                Logger.Write("❌ " + LoadProblem);
                return;
            }

            if (!migrate) return;
            try
            {
                Save();
                Logger.Write($"ℹ️ Đã chuyển ghi chú từ {_legacyFilePath} sang {_filePath}");
            }
            catch (Exception ex)
            {
                Logger.Write($"❌ Lỗi khi chuyển file ghi chú: {ex.Message}");
            }
        }

        public string Get(string? invoiceId) =>
            !string.IsNullOrEmpty(invoiceId) && _notes.TryGetValue(invoiceId, out string? note) ? note : "";

        public void Set(string invoiceId, string note) => _notes[invoiceId] = note;

        /// <summary>
        /// Ghi ra file; ném exception nếu lỗi để nơi gọi thông báo cho người dùng.
        /// </summary>
        public void Save()
        {
            if (LoadProblem != null) throw new InvalidOperationException(LoadProblem);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            LocalInvoiceStore.WriteAtomic(_filePath, JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>Nội dung có phải file ghi chú hợp lệ (object chuỗi-chuỗi).</summary>
        internal static bool IsValidContent(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static Dictionary<string, string> ReadFile(string path) =>
            JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
            ?? throw new JsonException("File ghi chú không có dữ liệu.");

        // Chép (không di chuyển) sang .corrupt: file gốc giữ nguyên để lần mở sau vẫn nhận ra lỗi. Không đè bản giữ trước đó.
        private static string KeepCorruptCopy(string path)
        {
            string target = path + ".corrupt";
            try
            {
                if (File.Exists(target))
                {
                    if (File.ReadAllBytes(target).AsSpan().SequenceEqual(File.ReadAllBytes(path))) return target;
                    target = path + "." + Guid.NewGuid().ToString("N") + ".corrupt";
                }
                File.Copy(path, target);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Logger.Write($"❌ Không tạo được bản sao file ghi chú hỏng: {ex.Message}");
                return path;
            }
            return target;
        }
    }
}
