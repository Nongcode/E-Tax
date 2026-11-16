using System.Drawing;
using System.Text.Json;

namespace E_Tax
{
    /// <summary>
    /// Vị trí, kích thước và trạng thái phóng to của cửa sổ chính, lưu tại %AppData%\E-Tax\window.json.
    /// </summary>
    internal sealed record WindowSettings(int X, int Y, int Width, int Height, bool Maximized)
    {
        public static string DefaultPath { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "window.json");

        /// <summary>
        /// File thiếu hoặc hỏng → null, không ném exception.
        /// </summary>
        public static WindowSettings? Load(string path)
        {
            try
            {
                if (File.Exists(path))
                    return JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Logger.Write($"⚠️ Không đọc được file vị trí cửa sổ {path}: {ex.Message}");
            }
            return null;
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this));
        }

        /// <summary>
        /// Vùng cửa sổ để khôi phục (không nhỏ hơn minimum), hoặc null nếu không giao với màn hình nào
        /// (vd: màn hình phụ đã rút) — khi đó để cửa sổ mở giữa màn hình chính.
        /// </summary>
        public static Rectangle? Fit(WindowSettings settings, IReadOnlyList<Rectangle> workingAreas, Size minimum)
        {
            var bounds = new Rectangle(settings.X, settings.Y,
                Math.Max(settings.Width, minimum.Width), Math.Max(settings.Height, minimum.Height));

            return workingAreas.Any(area => area.IntersectsWith(bounds)) ? bounds : null;
        }
    }
}
