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
        public void Get_NullOrEmptyId_ReturnsEmpty()
        {
            var store = new NotesStore(NewPath, null);
            Assert.Equal("", store.Get(null));
            Assert.Equal("", store.Get(""));
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
        public void Load_CorruptJson_KeepsCopy_AndSaveIsRefused()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(NewPath)!);
            File.WriteAllText(NewPath, "{oops");

            var store = new NotesStore(NewPath, null);
            store.Load();
            store.Set("a", "mới");

            Assert.NotNull(store.LoadProblem);
            Assert.Throws<InvalidOperationException>(() => store.Save());
            Assert.Equal("{oops", File.ReadAllText(NewPath));
            Assert.Equal("{oops", File.ReadAllText(NewPath + ".corrupt"));
        }

        [Fact]
        public void Load_NullJson_IsCorrupt()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(NewPath)!);
            File.WriteAllText(NewPath, "null");

            var store = new NotesStore(NewPath, null);
            store.Load();

            Assert.Throws<InvalidOperationException>(() => store.Save());
        }

        [Fact]
        public void Load_CorruptLegacy_DoesNotCreateEmptyNewFile()
        {
            File.WriteAllText(LegacyPath, "{oops");

            var store = new NotesStore(NewPath, LegacyPath);
            store.Load();

            Assert.False(File.Exists(NewPath));
            Assert.Throws<InvalidOperationException>(() => store.Save());
            Assert.Equal("{oops", File.ReadAllText(LegacyPath));
        }

        [Fact]
        public void Load_AfterUserFixesFile_SaveWorksAgain()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(NewPath)!);
            File.WriteAllText(NewPath, "{oops");
            var store = new NotesStore(NewPath, null);
            store.Load();

            File.WriteAllText(NewPath, "{\"a\":\"x\"}");
            store.Load();
            store.Set("b", "y");
            store.Save();

            Assert.Null(store.LoadProblem);
            var reloaded = new NotesStore(NewPath, null);
            reloaded.Load();
            Assert.Equal("x", reloaded.Get("a"));
            Assert.Equal("y", reloaded.Get("b"));
        }

        [Fact]
        public void Load_SecondDifferentCorruption_KeepsFirstCopy()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(NewPath)!);
            File.WriteAllText(NewPath, "{hong1");
            new NotesStore(NewPath, null).Load();
            File.WriteAllText(NewPath, "{hong2");
            new NotesStore(NewPath, null).Load();

            Assert.Equal("{hong1", File.ReadAllText(NewPath + ".corrupt"));
            Assert.Contains(Directory.GetFiles(Path.GetDirectoryName(NewPath)!, "*.corrupt"), f => File.ReadAllText(f) == "{hong2");
        }

        [Fact]
        public void Save_LeavesNoTempFile()
        {
            var store = new NotesStore(NewPath, null);
            store.Load();
            store.Set("a", "x");
            store.Save();

            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(NewPath)!, "*.tmp"));
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
