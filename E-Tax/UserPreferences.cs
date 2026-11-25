using System.Text.Json;

namespace E_Tax
{
    /// <summary>
    /// Tùy chọn người dùng tại %AppData%\E-Tax\preferences.json. Chỉ lưu LastUsername,
    /// tuyệt đối không lưu mật khẩu, captcha hay JWT.
    /// </summary>
    internal sealed class UserPreferences
    {
        public static string DefaultPath { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "preferences.json");

        private sealed class Data
        {
            public string? LastUsername { get; set; }
        }

        private readonly string _path;

        public UserPreferences(string path)
        {
            _path = path;
        }

        public string LastUsername { get; set; } = "";

        /// <summary>File thiếu hoặc hỏng thì để rỗng, không ném exception.</summary>
        public void Load()
        {
            LastUsername = "";
            try
            {
                if (!File.Exists(_path)) return;
                var data = JsonSerializer.Deserialize<Data>(File.ReadAllText(_path));
                LastUsername = data?.LastUsername ?? "";
            }
            catch (Exception)
            {
                LastUsername = "";
            }
        }

        /// <summary>Ghi nguyên tử (file tạm rồi thay thế); ném exception nếu lỗi để nơi gọi quyết định.</summary>
        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(new Data { LastUsername = LastUsername }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, _path, overwrite: true);
        }
    }
}
