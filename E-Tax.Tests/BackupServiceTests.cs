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

        // File tháng hợp lệ: mỗi cặp (số HĐ, tên người bán) là một hóa đơn
        private static string Month(params (int No, string Name)[] invoices) =>
            "[" + string.Join(",", invoices.Select(i =>
                $"{{\"loai\":\"Mua vào\",\"hd\":{{\"id\":\"id-{i.No}\",\"nbmst\":\"0300000001\",\"khmshdon\":1,\"khhdon\":\"C26TAA\"," +
                $"\"shdon\":{i.No},\"tdlap\":\"2026-03-15T00:00:00\",\"nbten\":\"{i.Name}\"}}}}")) + "]";

        private static Dictionary<int, string> Names(string root, string company) =>
            new LocalInvoiceStore(root).Query(company, new StoreQuery(null, null, null, null, null, null))
                .ToDictionary(r => r.So_hoa_don!.Value, r => r.Thong_tin_hoa_don);

        private const string NotesJson = "{\"a\":\"notes-data\"}";

        private void Seed()
        {
            Put(Path.Combine(Store, "0101234567", "2026-03.json"), Month((1, "A-data")));
            Put(Path.Combine(Store, "0101234567", "files", "2026-03", "ncc", "x.xml"), "xml-data");
            Put(Path.Combine(Store, "0101234567-001", "2026-04.json"), Month((2, "B-data")));
            Put(Path.Combine(Store, "0101234567", "partial.json.tmp"), "junk");
            Put(Notes, NotesJson);
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
            Assert.Equal("A-data", Names(store2, "0101234567")[1]);
            Assert.Equal("xml-data", File.ReadAllText(Path.Combine(store2, "0101234567", "files", "2026-03", "ncc", "x.xml")));
            Assert.Equal("B-data", Names(store2, "0101234567-001")[2]);
            Assert.Equal(NotesJson, File.ReadAllText(notes2));
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
            Put(Path.Combine(Store, "0101234567", "2026-03.json"), Month((1, "LOCAL-NEW")));
            Put(Notes, "{\"a\":\"LOCAL-NOTES\"}");
            File.Delete(Path.Combine(Store, "0101234567-001", "2026-04.json"));

            int n = BackupService.Restore(Zip, Store, Notes, null, false);

            Assert.Equal(1, n); // chỉ 2026-04.json bị mất; bản ghi trùng và file đính kèm đã có nên giữ nguyên
            Assert.Equal("LOCAL-NEW", Names(Store, "0101234567")[1]);
            Assert.Equal("{\"a\":\"LOCAL-NOTES\"}", File.ReadAllText(Notes));
            Assert.Equal("B-data", Names(Store, "0101234567-001")[2]);
        }

        [Fact]
        public void Restore_Overwrite_ReplacesExisting()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);
            Put(Path.Combine(Store, "0101234567", "2026-03.json"), Month((1, "LOCAL-NEW")));

            BackupService.Restore(Zip, Store, Notes, null, true);

            Assert.Equal("A-data", Names(Store, "0101234567")[1]);
        }

        [Theory]
        [InlineData(true, "BACKUP")]
        [InlineData(false, "LOCAL")]
        public void Restore_MergesRecords_BothSidesSurvive_ConflictFollowsFlag(bool overwrite, string expected)
        {
            string file = Path.Combine(Store, "0101234567", "2026-03.json");
            Put(file, Month((1, "BACKUP"), (2, "only-backup")));
            BackupService.Create(Store, null, Zip);
            Put(file, Month((1, "LOCAL"), (3, "only-store")));

            BackupService.Restore(Zip, Store, null, null, overwrite);

            var names = Names(Store, "0101234567");
            Assert.Equal(3, names.Count);
            Assert.Equal(expected, names[1]);
            Assert.Equal("only-backup", names[2]);
            Assert.Equal("only-store", names[3]);
        }

        [Fact]
        public void Restore_Overwrite_RecordInOtherMonth_IsMovedNotDuplicated()
        {
            Put(Path.Combine(Store, "0101234567", "2026-03.json"), Month((1, "BACKUP")));
            BackupService.Create(Store, null, Zip);
            File.Delete(Path.Combine(Store, "0101234567", "2026-03.json"));
            Put(Path.Combine(Store, "0101234567", "2026-05.json"), Month((1, "LOCAL")));

            BackupService.Restore(Zip, Store, null, null, true);

            string raw = File.ReadAllText(Path.Combine(Store, "0101234567", "2026-05.json"));
            Assert.DoesNotContain("LOCAL", raw);
            Assert.Contains("BACKUP", File.ReadAllText(Path.Combine(Store, "0101234567", "2026-03.json")));
        }

        [Theory]
        [InlineData("store/0101234567/2026-04.json")]
        [InlineData("store/0109999999/2026-04.json")]
        [InlineData("notes/notes.json")]
        public void Restore_CorruptJsonInZip_WritesNothing(string corruptEntry)
        {
            string file = Path.Combine(Store, "0101234567", "2026-03.json");
            Put(file, Month((1, "LOCAL")));
            string before = File.ReadAllText(file);
            string z = MakeZip(("store/0101234567/2026-03.json", Month((1, "BACKUP"), (2, "new"))), (corruptEntry, "{ hỏng"));

            Assert.Throws<InvalidDataException>(() => BackupService.Restore(z, Store, Notes, "0101234567", true));

            Assert.Equal(before, File.ReadAllText(file));
            Assert.False(File.Exists(Path.Combine(Store, "0101234567", "2026-04.json")));
            Assert.False(File.Exists(Notes));
            Assert.Empty(Directory.GetFiles(_dir, "*-safety-*"));
        }

        [Fact]
        public void Restore_CorruptMonthInStore_WritesNothing()
        {
            string file = Path.Combine(Store, "0101234567", "2026-03.json");
            Put(file, "{ hỏng");
            string z = MakeZip(("store/0101234567/2026-04.json", Month((2, "new"))));

            var ex = Assert.Throws<InvalidDataException>(() => BackupService.Restore(z, Store, null, null, true));

            Assert.Contains("2026-03.json", ex.Message);
            Assert.Equal("{ hỏng", File.ReadAllText(file));
            Assert.False(File.Exists(Path.Combine(Store, "0101234567", "2026-04.json")));
        }

        [Fact]
        public void Restore_CreatesSafetyBackupOfCurrentStore_BesideRoot()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);
            Put(Path.Combine(Store, "0101234567", "2026-03.json"), Month((1, "BEFORE-RESTORE")));

            BackupService.Restore(Zip, Store, Notes, null, true, out string? safety);

            Assert.NotNull(safety);
            Assert.Equal(Path.GetFullPath(_dir), Path.GetDirectoryName(safety));
            string check = Path.Combine(_dir, "check");
            BackupService.Restore(safety!, check, null, null, true);
            Assert.Equal("BEFORE-RESTORE", Names(check, "0101234567")[1]);
        }

        [Fact]
        public void Restore_EmptyStore_NeedsNoSafetyBackup()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);

            BackupService.Restore(Zip, Path.Combine(_dir, "fresh"), Path.Combine(_dir, "fresh-notes.json"), null, true, out string? safety);

            Assert.Null(safety);
        }

        [Fact]
        public void Create_SkipsDesktopIniAndNonCompanyFolders_AndIsRestorable()
        {
            Seed();
            Put(Path.Combine(Store, "desktop.ini"), "[.ShellClassInfo]");
            Put(Path.Combine(Store, "Thu muc la", "a.txt"), "x");

            BackupService.Create(Store, Notes, Zip);

            using (var za = ZipFile.OpenRead(Zip))
            {
                Assert.DoesNotContain(za.Entries, e => e.FullName.Contains("desktop.ini"));
                Assert.DoesNotContain(za.Entries, e => e.FullName.Contains("Thu muc la"));
            }
            string store2 = Path.Combine(_dir, "store2");
            BackupService.Restore(Zip, store2, null, null, true);
            Assert.Equal("A-data", Names(store2, "0101234567")[1]);
        }

        [Fact]
        public void Create_CorruptMonthFile_FailsNamingFile_KeepsPreviousBackup()
        {
            Seed();
            BackupService.Create(Store, Notes, Zip);
            byte[] before = File.ReadAllBytes(Zip);
            Put(Path.Combine(Store, "0101234567", "2026-05.json"), "{ hỏng");

            var ex = Assert.Throws<InvalidDataException>(() => BackupService.Create(Store, Notes, Zip));

            Assert.Contains("2026-05.json", ex.Message);
            Assert.Equal(before, File.ReadAllBytes(Zip));
            Assert.False(File.Exists(Zip + ".tmp"));
        }

        [Fact]
        public void Create_CorruptNotes_StillBacksUpStore_AndReportsIt()
        {
            Seed();
            Put(Notes, "{ hỏng");

            var warnings = BackupService.Create(Store, Notes, Zip);

            Assert.Contains(warnings, w => w.Contains("ghi chú"));
            using var za = ZipFile.OpenRead(Zip);
            Assert.DoesNotContain(za.Entries, e => e.FullName == "notes/notes.json");
            Assert.Contains(za.Entries, e => e.FullName == "store/0101234567/2026-03.json");
        }

        [Fact]
        public void Restore_Overwrite_NeverReplacesQuarantinedFile()
        {
            string quarantined = Path.Combine(Store, "0101234567", "2026-03.json.corrupt");
            Put(quarantined, "bản cách ly cũ");
            Put(Path.Combine(Store, "0101234567", "2026-04.json"), Month((2, "A")));
            BackupService.Create(Store, null, Zip);
            Put(quarantined, "bản cách ly mới");

            BackupService.Restore(Zip, Store, null, null, true);

            Assert.Equal("bản cách ly mới", File.ReadAllText(quarantined));
        }

        [Fact]
        public void Restore_AcceptsBareStoreDirectoryEntry_AndOldDesktopIni()
        {
            string z = MakeZip(("store/", ""), ("store/desktop.ini", "x"), ("store/0101234567/2026-03.json", Month((1, "A"))));
            string store2 = Path.Combine(_dir, "store2");

            Assert.Equal(1, BackupService.Restore(z, store2, null, null, true));

            Assert.Equal("A", Names(store2, "0101234567")[1]);
            Assert.False(File.Exists(Path.Combine(store2, "desktop.ini")));
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
