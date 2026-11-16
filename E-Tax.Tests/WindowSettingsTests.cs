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
        public void Fit_OnSecondScreen_KeepsBounds()
        {
            var screens = new[] { new Rectangle(0, 0, 1920, 1040), new Rectangle(1920, 0, 1920, 1040) };
            var settings = new WindowSettings(2000, 100, 1280, 800, false);

            Assert.Equal(new Rectangle(2000, 100, 1280, 800), WindowSettings.Fit(settings, screens, Minimum));
        }
    }
}
