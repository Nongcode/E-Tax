using E_Tax;

namespace E_Tax.Tests
{
    public class AppSettingsTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-settings-" + Guid.NewGuid());
        private string FilePath => Path.Combine(_dir, "appsettings.json");

        public AppSettingsTests()
        {
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Fact]
        public void Load_MissingFile_ReturnsNulls()
        {
            var settings = AppSettings.Load(FilePath);

            Assert.Null(settings.BlacklistApiToken);
        }

        [Fact]
        public void Load_ReadsToken()
        {
            File.WriteAllText(FilePath, "{\"BlacklistApiToken\":\"abc123\"}");

            var settings = AppSettings.Load(FilePath);

            Assert.Equal("abc123", settings.BlacklistApiToken);
        }

        [Fact]
        public void Load_CorruptJson_ReturnsNulls()
        {
            File.WriteAllText(FilePath, "{oops");

            var settings = AppSettings.Load(FilePath);

            Assert.Null(settings.BlacklistApiToken);
        }
    }
}
