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

        public void Load()
        {
            _notes = new Dictionary<string, string>();
            try
            {
                if (!File.Exists(_filePath) && _legacyFilePath != null && File.Exists(_legacyFilePath))
                {
                    _notes = ReadFile(_legacyFilePath);
                    Save();
                    Logger.Write($"ℹ️ Đã chuyển ghi chú từ {_legacyFilePath} sang {_filePath}");
                    return;
                }

                if (File.Exists(_filePath))
                {
                    _notes = ReadFile(_filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"❌ Lỗi khi tải file ghi chú: {ex.Message}");
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
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static Dictionary<string, string> ReadFile(string path)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new Dictionary<string, string>();
            }
            catch (JsonException)
            {
                return new Dictionary<string, string>();
            }
        }
    }
}
