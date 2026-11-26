using System.Text;
using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Duy trì đăng nhập: JWT lưu mã hóa DPAPI, chỉ khôi phục khi còn hạn theo exp trong token.</summary>
    public sealed class SessionStoreTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 11, 26, 8, 0, 0, DateTimeKind.Utc);
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-session-" + Guid.NewGuid().ToString("N"));
        private string FilePath => Path.Combine(_dir, "session.dat");

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static string Jwt(string payloadJson) => $"{B64("{\"alg\":\"HS256\"}")}.{B64(payloadJson)}.c2ln";

        private static string JwtExpiring(DateTime utc) => Jwt($"{{\"sub\":\"0110443702\",\"exp\":{new DateTimeOffset(utc).ToUnixTimeSeconds()}}}");

        private SessionStore Store(DateTime? now = null) => new(FilePath, () => now ?? Now);

        [Fact]
        public void ReadExpiry_FromPayload()
        {
            Assert.Equal(Now.AddHours(8), JwtExpiry.ReadUtc(JwtExpiring(Now.AddHours(8))));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("khong-phai-jwt")]
        [InlineData("a.%%%.c")]
        [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ4In0.c2ln")] // không có exp
        public void ReadExpiry_InvalidOrMissing_IsNull(string? token)
        {
            Assert.Null(JwtExpiry.ReadUtc(token));
        }

        [Fact]
        public void SaveThenLoad_ReturnsSession_WhileValid()
        {
            string token = JwtExpiring(Now.AddHours(8));
            Assert.True(Store().Save("0110443702", token));

            var s = Store(Now.AddHours(7)).Load();

            Assert.NotNull(s);
            Assert.Equal("0110443702", s!.Username);
            Assert.Equal(token, s.Token);
            Assert.Equal(Now.AddHours(8), s.ExpiresUtc);
        }

        [Fact]
        public void SavedFile_DoesNotContainTokenInPlainText()
        {
            string token = JwtExpiring(Now.AddHours(8));
            Store().Save("0110443702", token);

            byte[] raw = File.ReadAllBytes(FilePath);
            string asText = Encoding.UTF8.GetString(raw);
            Assert.DoesNotContain(token, asText);
            Assert.DoesNotContain("0110443702", asText);
        }

        // Còn dưới 5 phút coi như hết hạn: tránh vào màn hình tìm kiếm rồi bị cổng từ chối ngay
        [Fact]
        public void Load_ExpiredOrAlmostExpired_ReturnsNull_AndDeletesFile()
        {
            Store().Save("u", JwtExpiring(Now.AddHours(1)));

            Assert.Null(Store(Now.AddHours(1).AddMinutes(-4)).Load());
            Assert.False(File.Exists(FilePath));
        }

        [Fact]
        public void Save_TokenWithoutExpiry_IsRefused_AndRemovesOldSession()
        {
            Store().Save("u", JwtExpiring(Now.AddHours(8)));

            Assert.False(Store().Save("u", Jwt("{\"sub\":\"x\"}")));
            Assert.False(File.Exists(FilePath));
        }

        [Fact]
        public void Load_CorruptFile_ReturnsNull_AndDeletesFile()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllBytes(FilePath, new byte[] { 1, 2, 3, 4 });

            Assert.Null(Store().Load());
            Assert.False(File.Exists(FilePath));
        }

        [Fact]
        public void Load_MissingFile_ReturnsNull()
        {
            Assert.Null(Store().Load());
        }

        [Fact]
        public void Clear_RemovesSession()
        {
            Store().Save("u", JwtExpiring(Now.AddHours(8)));
            Store().Clear();
            Assert.Null(Store().Load());
            Store().Clear(); // không có file vẫn không lỗi
        }
    }
}
