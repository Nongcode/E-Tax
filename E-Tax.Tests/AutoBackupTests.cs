using E_Tax;

namespace E_Tax.Tests
{
    public class AutoBackupTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-auto-" + Guid.NewGuid());
        private string Store => Path.Combine(_dir, "store");
        private string Backups => Path.Combine(_dir, "backups");
        private static readonly DateTime Now = new(2026, 11, 25, 9, 0, 0);

        public AutoBackupTests()
        {
            Directory.CreateDirectory(Path.Combine(Store, "0101234567"));
            File.WriteAllText(Path.Combine(Store, "0101234567", "2026-03.json"), "[]");
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private void Existing(DateTime at)
        {
            Directory.CreateDirectory(Backups);
            BackupService.Create(Store, null, Path.Combine(Backups, AutoBackup.FileNameFor(at)));
        }

        [Fact]
        public void NoBackupYet_CreatesOne()
        {
            string? path = AutoBackup.RunIfDue(Store, null, Backups, Now);

            Assert.NotNull(path);
            Assert.True(File.Exists(path));
            Assert.Equal(Backups, Path.GetDirectoryName(path));
        }

        [Fact]
        public void RecentBackup_SkipsUntilADayHasPassed()
        {
            Existing(Now.AddHours(-23));

            Assert.Null(AutoBackup.RunIfDue(Store, null, Backups, Now));
            Assert.NotNull(AutoBackup.RunIfDue(Store, null, Backups, Now.AddHours(2)));
        }

        [Fact]
        public void EmptyStore_CreatesNothing()
        {
            Directory.Delete(Store, true);

            Assert.Null(AutoBackup.RunIfDue(Store, null, Backups, Now));
            Assert.False(Directory.Exists(Backups) && Directory.EnumerateFiles(Backups).Any());
        }

        [Fact]
        public void KeepsOnlyNewestAutomaticBackups_AndLeavesOtherFiles()
        {
            for (int i = 10; i >= 1; i--) Existing(Now.AddDays(-i));
            File.WriteAllText(Path.Combine(Backups, "thu-cong.zip"), "x");

            AutoBackup.RunIfDue(Store, null, Backups, Now, keep: 3);

            var autos = Directory.GetFiles(Backups, AutoBackup.Prefix + "*.zip").Select(Path.GetFileName).OrderBy(n => n).ToList();
            Assert.Equal(new[] { AutoBackup.FileNameFor(Now.AddDays(-2)), AutoBackup.FileNameFor(Now.AddDays(-1)), AutoBackup.FileNameFor(Now) }, autos);
            Assert.True(File.Exists(Path.Combine(Backups, "thu-cong.zip")));
        }

        [Fact]
        public void BackupContainsStoreFiles()
        {
            string path = AutoBackup.RunIfDue(Store, null, Backups, Now)!;

            using var za = System.IO.Compression.ZipFile.OpenRead(path);
            Assert.Contains(za.Entries, e => e.FullName == "store/0101234567/2026-03.json");
        }
    }
}
