using System.IO.Compression;
using E_Tax;

namespace E_Tax.Tests
{
    public class BackupServiceTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-backup-" + Guid.NewGuid());
        private string Store => Path.Combine(_dir, "store");
        private string Notes => Path.Combine(_dir, "notes.json");
        private string Zip => Path.Combine(_dir, "out", "backup.zip");

        public BackupServiceTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static void Put(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        private void Seed()
        {
            Put(Path.Combine(Store, "0101234567", "2026-03.json"), "A-data");
            Put(Path.Combine(Store, "0101234567", "files", "2026-03", "ncc", "x.xml"), "xml-data");
            Put(Path.Combine(Store, "0101234567-001", "2026-04.json"), "B-data");
            Put(Path.Combine(Store, "0101234567", "partial.json.tmp"), "junk");
            Put(Notes, "notes-data");
        }

        private string MakeZip(params (string name, string content)[] entries)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Zip)!);
            using var fs = File.Create(Zip);
            using var za = new ZipArchive(fs, ZipArchiveMode.Create);
            foreach (var (name, content) in entries)
            {
                using var w = new StreamWriter(za.CreateEntry(name).Open());
                w.Write(content);
            }
            return Zip;
        }

        [Fact]
        public void BackupThenRestore_MatchesContent_AndSkipsTmp()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);
            using (var za = ZipFile.OpenRead(Zip))
                Assert.DoesNotContain(za.Entries, e => e.FullName.EndsWith(".tmp"));

            string store2 = Path.Combine(_dir, "store2"), notes2 = Path.Combine(_dir, "notes2.json");
            int n = BackupService.Restore(Zip, store2, notes2, null, true);

            Assert.Equal(4, n);
            Assert.Equal("A-data", File.ReadAllText(Path.Combine(store2, "0101234567", "2026-03.json")));
            Assert.Equal("xml-data", File.ReadAllText(Path.Combine(store2, "0101234567", "files", "2026-03", "ncc", "x.xml")));
            Assert.Equal("B-data", File.ReadAllText(Path.Combine(store2, "0101234567-001", "2026-04.json")));
            Assert.Equal("notes-data", File.ReadAllText(notes2));
            Assert.False(File.Exists(Path.Combine(store2, "0101234567", "partial.json.tmp")));
        }

        [Fact]
        public void Restore_OnlyCompany_RestoresOneCompanyAndNoNotes()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);
            string store2 = Path.Combine(_dir, "store2"), notes2 = Path.Combine(_dir, "notes2.json");

            int n = BackupService.Restore(Zip, store2, notes2, "0101234567-001", true);

            Assert.Equal(1, n);
            Assert.True(File.Exists(Path.Combine(store2, "0101234567-001", "2026-04.json")));
            Assert.False(Directory.Exists(Path.Combine(store2, "0101234567")));
            Assert.False(File.Exists(notes2));
        }

        [Fact]
        public void Restore_NoOverwrite_KeepsExistingFiles()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);
            Put(Path.Combine(Store, "0101234567", "2026-03.json"), "LOCAL-NEW");
            Put(Notes, "LOCAL-NOTES");
            File.Delete(Path.Combine(Store, "0101234567-001", "2026-04.json"));

            int n = BackupService.Restore(Zip, Store, Notes, null, false);

            Assert.Equal(1, n); // chỉ 2026-04.json bị mất; các file còn lại đã tồn tại nên bỏ qua
            Assert.Equal("LOCAL-NEW", File.ReadAllText(Path.Combine(Store, "0101234567", "2026-03.json")));
            Assert.Equal("LOCAL-NOTES", File.ReadAllText(Notes));
            Assert.Equal("B-data", File.ReadAllText(Path.Combine(Store, "0101234567-001", "2026-04.json")));
        }

        [Fact]
        public void Restore_Overwrite_ReplacesExisting()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);
            Put(Path.Combine(Store, "0101234567", "2026-03.json"), "LOCAL-NEW");

            BackupService.Restore(Zip, Store, Notes, null, true);

            Assert.Equal("A-data", File.ReadAllText(Path.Combine(Store, "0101234567", "2026-03.json")));
        }

        [Theory]
        [InlineData(@"store\..\evil.txt")]
        [InlineData("store/0101234567/../../evil.txt")]
        [InlineData(@"..\evil.txt")]
        [InlineData("evil.txt")]
        [InlineData("/store/0101234567/a.json")]
        [InlineData("C:/evil.txt")]
        [InlineData("store/notdigits/a.json")]
        [InlineData("store/a.json")]
        [InlineData("notes/other.json")]
        public void Restore_BadEntry_ThrowsAndWritesNothing(string bad)
        {
            string z = MakeZip(("store/0101234567/ok.json", "ok"), (bad, "evil"));
            string store2 = Path.Combine(_dir, "store2");

            Assert.Throws<InvalidDataException>(() => BackupService.Restore(z, store2, Path.Combine(_dir, "n2.json"), null, true));

            Assert.False(Directory.Exists(store2));
            Assert.False(File.Exists(Path.Combine(_dir, "evil.txt")));
            Assert.False(File.Exists(Path.Combine(_dir, "n2.json")));
        }

        [Fact]
        public void Create_ZipInsideStore_Throws()
        {
            Seed();
            Assert.Throws<ArgumentException>(() => BackupService.Create(Store, Notes, Path.Combine(Store, "b.zip")));
        }

        [Fact]
        public void Create_Failure_LeavesNoTargetOrTmp()
        {
            Seed();
            // thư mục đích không tạo được: một file chiếm chỗ tên thư mục cha
            Put(Path.Combine(_dir, "blocker"), "x");
            string z = Path.Combine(_dir, "blocker", "b.zip");

            Assert.ThrowsAny<Exception>(() => BackupService.Create(Store, Notes, z));
            Assert.False(File.Exists(z));
            Assert.False(File.Exists(z + ".tmp"));
        }

        [Fact]
        public void Create_FailureMidway_KeepsPreviousBackupIntact()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);
            byte[] before = File.ReadAllBytes(Zip);
            // khóa độc quyền một file để việc đọc giữa chừng thất bại
            using var lockFile = new FileStream(Path.Combine(Store, "0101234567", "2026-03.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Assert.ThrowsAny<Exception>(() => BackupService.Create(Store, Notes, Zip));

            Assert.Equal(before, File.ReadAllBytes(Zip));
            Assert.False(File.Exists(Zip + ".tmp"));
        }
    }
}
