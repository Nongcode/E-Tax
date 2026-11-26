using System.Collections.Concurrent;
using System.Drawing;
using Svg;

namespace E_Tax
{
    /// <summary>
    /// Icon nét mảnh kiểu Lucide (lưới 24×24, stroke 2) nhúng sẵn, vẽ ra Bitmap bằng thư viện Svg theo kích thước và màu.
    /// Cache theo (tên, cỡ, màu); mỗi lần gọi trả bản sao để nơi nhận tự Dispose. Tên lạ hoặc lỗi vẽ → null (nút chỉ hiện chữ).
    /// </summary>
    internal static class RibbonIcons
    {
        private static readonly Dictionary<string, string> Paths = new(StringComparer.Ordinal)
        {
            ["search"] = "<circle cx='11' cy='11' r='7'/><path d='m21 21-4.3-4.3'/>",
            ["eye"] = "<path d='M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z'/><circle cx='12' cy='12' r='3'/>",
            ["print"] = "<path d='M6 9V3h12v6'/><rect x='6' y='14' width='12' height='7'/><path d='M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2'/>",
            ["layers"] = "<path d='m12 2 10 5-10 5L2 7z'/><path d='m2 17 10 5 10-5'/><path d='m2 12 10 5 10-5'/>",
            ["download"] = "<path d='M12 3v12'/><path d='m7 10 5 5 5-5'/><path d='M5 21h14'/>",
            ["sheet"] = "<rect x='3' y='3' width='18' height='18' rx='2'/><path d='M3 9h18M3 15h18M9 3v18'/>",
            ["list"] = "<path d='M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01'/>",
            ["file"] = "<path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><path d='M14 2v6h6M8 13h8M8 17h5'/>",
            ["calc"] = "<rect x='4' y='2' width='16' height='20' rx='2'/><path d='M8 6h8M8 11h.01M12 11h.01M16 11h.01M8 15h.01M12 15h.01M16 15h.01M8 19h8'/>",
            ["box"] = "<path d='M21 8 12 3 3 8v8l9 5 9-5z'/><path d='M3 8l9 5 9-5M12 13v8'/>",
            ["shield"] = "<path d='M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z'/><path d='m9 12 2 2 4-4'/>",
            ["note"] = "<path d='M4 4h16v12H8l-4 4z'/>",
            ["refresh"] = "<path d='M21 12a9 9 0 1 1-3-6.7L21 8'/><path d='M21 3v5h-5'/>",
            ["filter"] = "<path d='M3 5h18l-7 8v6l-4 2v-8z'/>",
            ["user"] = "<circle cx='12' cy='8' r='4'/><path d='M4 21a8 8 0 0 1 16 0'/>",
            ["lock"] = "<rect x='4' y='11' width='16' height='10' rx='2'/><path d='M8 11V7a4 4 0 0 1 8 0v4'/>",
            ["gear"] = "<circle cx='12' cy='12' r='3'/><path d='M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z'/>",
            ["logout"] = "<path d='M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4'/><path d='m16 17 5-5-5-5M21 12H9'/>",
        };

        private static readonly ConcurrentDictionary<(string, int, int), Bitmap?> Cache = new();

        public static Bitmap? Get(string name, int size, Color color)
        {
            if (string.IsNullOrEmpty(name) || !Paths.ContainsKey(name) || size <= 0) return null;
            var master = Cache.GetOrAdd((name, size, color.ToArgb()), _ => Render(name, size, color));
            if (master == null) return null;
            lock (master) return new Bitmap(master);
        }

        private static Bitmap? Render(string name, int size, Color color)
        {
            try
            {
                string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                string svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' " +
                             $"stroke='{hex}' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>{Paths[name]}</svg>";
                var doc = SvgDocument.FromSvg<SvgDocument>(svg);
                var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    doc.Draw(g, new SizeF(size, size));
                }
                return bmp;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
