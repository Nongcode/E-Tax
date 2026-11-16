using System.Drawing;
using E_Tax;

namespace E_Tax.Tests
{
    public class WindowSettingsTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-window-" + Guid.NewGuid());
        private string FilePath => Path.Combine(_dir, "sub", "window.json");

        private static readonly Rectangle[] OneScreen = { new Rectangle(0, 0, 1920, 1040) };
        private static readonly Size Minimum = new Size(1000, 725);

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Fact]
        public void Load_MissingFile_ReturnsNull()
        {
            Assert.Null(WindowSettings.Load(FilePath));
        }

        [Fact]
        public void Load_CorruptJson_ReturnsNull()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, "{oops");

            Assert.Null(WindowSettings.Load(FilePath));
        }

        [Fact]
        public void SaveThenLoad_RoundTrips()
        {
            var settings = new WindowSettings(120, 80, 1280, 800, true);

            settings.Save(FilePath);

            Assert.Equal(settings, WindowSettings.Load(FilePath));
        }

        [Fact]
        public void Fit_OffScreen_ReturnsNull()
        {
            var settings = new WindowSettings(5000, 5000, 1280, 800, false);

            Assert.Null(WindowSettings.Fit(settings, OneScreen, Minimum));
        }

        [Fact]
        public void Fit_TooSmall_ClampsToMinimum()
        {
            var settings = new WindowSettings(100, 50, 400, 300, false);

            Assert.Equal(new Rectangle(100, 50, 1000, 725), WindowSettings.Fit(settings, OneScreen, Minimum));
        }

        [Fact]
        public void Fit_TitleBarAboveScreen_ReturnsNull()
        {
            // Phần thân còn chạm màn hình nhưng thanh tiêu đề ở ngoài → không kéo được cửa sổ
            var settings = new WindowSettings(100, -600, 1280, 800, false);

            Assert.Null(WindowSettings.Fit(settings, OneScreen, Minimum));
        }

        [Fact]
        public void Fit_LargerThanScreen_ShrinksToWorkingArea()
        {
            var settings = new WindowSettings(0, 0, 3840, 2100, false);

            Assert.Equal(new Rectangle(0, 0, 1920, 1040), WindowSettings.Fit(settings, OneScreen, Minimum));
        }

        [Fact]
        public void ClampMinimum_SmallScreen_FitsWorkingArea()
        {
            // Laptop 1366x768: vùng làm việc cao ~728px
            Assert.Equal(new Size(1000, 728), WindowSettings.ClampMinimum(new Size(1000, 747), new Rectangle(0, 0, 1366, 728)));
            Assert.Equal(new Size(1000, 725), WindowSettings.ClampMinimum(new Size(1000, 725), new Rectangle(0, 0, 1920, 1040)));
        }

        [Fact]
        public void Fit_OnSecondScreen_KeepsBounds()
        {
            var screens = new[] { new Rectangle(0, 0, 1920, 1040), new Rectangle(1920, 0, 1920, 1040) };
            var settings = new WindowSettings(2000, 100, 1280, 800, false);

            Assert.Equal(new Rectangle(2000, 100, 1280, 800), WindowSettings.Fit(settings, screens, Minimum));
        }
    }
}
