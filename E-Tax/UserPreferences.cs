using System.Text.Json;

namespace E_Tax
{
    /// <summary>Kiểu giao diện người dùng chọn (thanh trạng thái → "Giao diện").</summary>
    public enum LayoutKind { Ribbon, Classic, SideMenu, Compact }

    /// <summary>
    /// Tùy chọn người dùng tại %AppData%\E-Tax\preferences.json: tên đăng nhập gần nhất, kiểu giao diện, cột hiển thị
    /// từng bảng. Tuyệt đối không lưu mật khẩu, captcha hay JWT (phiên "Duy trì đăng nhập" nằm ở SessionStore, mã hóa riêng).
    /// </summary>
    internal sealed class UserPreferences
    {
        public static string DefaultPath { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "preferences.json");

        private sealed class Data
        {
            public string? LastUsername { get; set; }
            public string? Layout { get; set; }
            public Dictionary<string, List<ColumnState>>? Columns { get; set; }
        }

        private readonly string _path;

        public UserPreferences(string path)
        {
            _path = path;
        }

        public string LastUsername { get; set; } = "";
        public LayoutKind Layout { get; set; } = LayoutKind.Ribbon;
        /// <summary>Cột hiển thị theo tên bảng (Name của DataGridView).</summary>
        public Dictionary<string, List<ColumnState>> Columns { get; private set; } = new();

        /// <summary>File thiếu hoặc hỏng thì về mặc định, không ném exception. Kiểu giao diện lạ → Ribbon.</summary>
        public void Load()
        {
            LastUsername = "";
            Layout = LayoutKind.Ribbon;
            Columns = new();
            try
            {
                if (!File.Exists(_path)) return;
                var data = JsonSerializer.Deserialize<Data>(File.ReadAllText(_path));
                LastUsername = data?.LastUsername ?? "";
                if (Enum.TryParse(data?.Layout, ignoreCase: false, out LayoutKind layout) && Enum.IsDefined(layout)) Layout = layout;
                if (data?.Columns != null)
                    foreach (var (grid, cols) in data.Columns)
                        if (!string.IsNullOrEmpty(grid) && cols != null)
                            Columns[grid] = cols.Where(c => c != null && !string.IsNullOrEmpty(c.Name)).ToList();
            }
            catch (Exception)
            {
                LastUsername = "";
                Layout = LayoutKind.Ribbon;
                Columns = new();
            }
        }

        /// <summary>Ghi nguyên tử (file tạm rồi thay thế); ném exception nếu lỗi để nơi gọi quyết định.</summary>
        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string tmp = _path + ".tmp";
            var data = new Data { LastUsername = LastUsername, Layout = Layout.ToString(), Columns = Columns };
            File.WriteAllText(tmp, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, _path, overwrite: true);
        }
    }
}
