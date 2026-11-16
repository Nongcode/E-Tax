using E_Tax;

namespace E_Tax.Tests
{
    public class NotesStoreTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-notes-" + Guid.NewGuid());
        private string NewPath => Path.Combine(_dir, "appdata", "notes.json");
        private string LegacyPath => Path.Combine(_dir, "legacy", "notes.json");

        public NotesStoreTests()
        {
            Directory.CreateDirectory(Path.Combine(_dir, "legacy"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Fact]
        public void Load_MigratesLegacyFile()
        {
            File.WriteAllText(LegacyPath, "{\"a\":\"x\"}");

            var store = new NotesStore(NewPath, LegacyPath);
            store.Load();

            Assert.Equal("x", store.Get("a"));
            Assert.True(File.Exists(NewPath));
        }

        [Fact]
        public void Load_PrefersNewFile()
        {
            File.WriteAllText(LegacyPath, "{\"a\":\"old\"}");
            Directory.CreateDirectory(Path.GetDirectoryName(NewPath)!);
            File.WriteAllText(NewPath, "{\"a\":\"new\"}");

            var store = new NotesStore(NewPath, LegacyPath);
            store.Load();

            Assert.Equal("new", store.Get("a"));
        }

        [Fact]
        public void Load_CorruptJson_ReturnsEmpty()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(NewPath)!);
            File.WriteAllText(NewPath, "{oops");

            var store = new NotesStore(NewPath, null);
            store.Load();

            Assert.Equal("", store.Get("a"));
        }

        [Fact]
        public void Save_CreatesDirectory()
        {
            var store = new NotesStore(NewPath, null);
            store.Load();
            store.Set("a", "ghi chú");
            store.Save();

            var reloaded = new NotesStore(NewPath, null);
            reloaded.Load();
            Assert.Equal("ghi chú", reloaded.Get("a"));
        }
    }
}
