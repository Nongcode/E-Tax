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
        /// Kích thước tối thiểu không vượt vùng làm việc của màn hình (vd laptop 1366x768).
        /// </summary>
        public static Size ClampMinimum(Size design, Rectangle workingArea) =>
            new(Math.Min(design.Width, workingArea.Width), Math.Min(design.Height, workingArea.Height));

        /// <summary>
        /// Vùng cửa sổ để khôi phục (không nhỏ hơn minimum, không lớn hơn màn hình chứa nó), hoặc null nếu
        /// thanh tiêu đề không nằm trên màn hình nào (vd: màn hình phụ đã rút) — khi đó để cửa sổ mở giữa màn hình chính.
        /// </summary>
        public static Rectangle? Fit(WindowSettings settings, IReadOnlyList<Rectangle> workingAreas, Size minimum)
        {
            int width = Math.Max(settings.Width, minimum.Width);
            int height = Math.Max(settings.Height, minimum.Height);

            // Thanh tiêu đề phải thấy được thì mới kéo cửa sổ bằng chuột được
            var titleBar = new Rectangle(settings.X, settings.Y, width, TitleBarHeight);
            foreach (var area in workingAreas)
            {
                if (area.IntersectsWith(titleBar))
                    return new Rectangle(settings.X, settings.Y, Math.Min(width, area.Width), Math.Min(height, area.Height));
            }
            return null;
        }

        private const int TitleBarHeight = 30;
    }
}
