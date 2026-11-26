namespace E_Tax.Tests
{
    public class UserPreferencesTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-prefs-" + Guid.NewGuid().ToString("N"));
        private string PrefsPath => Path.Combine(_dir, "sub", "preferences.json");

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        [Fact]
        public void RoundTrip_LastUsername()
        {
            new UserPreferences(PrefsPath) { LastUsername = "0100109106" }.Save();

            var loaded = new UserPreferences(PrefsPath);
            loaded.Load();
            Assert.Equal("0100109106", loaded.LastUsername);
        }

        [Fact]
        public void FileKhongCoTruongMatKhau()
        {
            new UserPreferences(PrefsPath) { LastUsername = "abc" }.Save();
            string text = File.ReadAllText(PrefsPath).ToLowerInvariant();
            Assert.Contains("lastusername", text);
            Assert.DoesNotContain("pass", text);
            Assert.DoesNotContain("captcha", text);
            Assert.DoesNotContain("jwt", text);
            Assert.DoesNotContain("token", text);
        }

        [Fact]
        public void FileHong_NapThanhRong()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefsPath)!);
            File.WriteAllText(PrefsPath, "{hỏng");
            var p = new UserPreferences(PrefsPath);
            p.Load();
            Assert.Equal("", p.LastUsername);
        }

        [Fact]
        public void KhongCoFile_NapThanhRong()
        {
            var p = new UserPreferences(PrefsPath);
            p.Load();
            Assert.Equal("", p.LastUsername);
        }

        [Fact]
        public void RoundTrip_LayoutAndColumns_KeepsUsername()
        {
            var p = new UserPreferences(PrefsPath) { LastUsername = "0100109106", Layout = LayoutKind.SideMenu };
            p.Columns["dgvMain"] = new List<ColumnState> { new("colDgvSTT", true), new("colDgvMST", false) };
            p.Save();

            var loaded = new UserPreferences(PrefsPath);
            loaded.Load();
            Assert.Equal("0100109106", loaded.LastUsername);
            Assert.Equal(LayoutKind.SideMenu, loaded.Layout);
            Assert.Equal(new[] { "colDgvSTT", "colDgvMST" }, loaded.Columns["dgvMain"].Select(c => c.Name));
            Assert.False(loaded.Columns["dgvMain"][1].Visible);
        }

        [Fact]
        public void LayoutLa_HoacThieu_DungRibbon()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefsPath)!);
            File.WriteAllText(PrefsPath, "{\"LastUsername\":\"x\",\"Layout\":\"KieuLa\"}");
            var p = new UserPreferences(PrefsPath);
            p.Load();
            Assert.Equal("x", p.LastUsername);
            Assert.Equal(LayoutKind.Ribbon, p.Layout);
            Assert.Empty(p.Columns);
        }

        [Fact]
        public void Save_KhongDeLaiFileTam()
        {
            new UserPreferences(PrefsPath) { LastUsername = "x" }.Save();
            Assert.Single(Directory.GetFiles(Path.GetDirectoryName(PrefsPath)!));
        }
    }
}
