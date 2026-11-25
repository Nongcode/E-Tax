namespace E_Tax.Tests
{
    public class AppErrorHandlerTests
    {
        private static Exception Thrown(Exception ex)
        {
            try { throw ex; } catch (Exception e) { return e; }
        }

        [Fact]
        public void BuildLogEntry_CoLoaiLoiVaStack_KhongCoMessage()
        {
            var ex = Thrown(new HttpRequestException("https://x.gov.vn/api?token=SECRET123"));
            string entry = AppErrorHandler.BuildLogEntry("ThreadException", ex);

            Assert.Contains("ThreadException", entry);
            Assert.Contains(typeof(HttpRequestException).FullName!, entry);
            Assert.Contains(nameof(Thrown), entry);
            Assert.DoesNotContain("SECRET123", entry);
            Assert.DoesNotContain("token=", entry);
        }

        [Fact]
        public void BuildLogEntry_LoiLong_KhongCoMessageCuaInner()
        {
            var ex = Thrown(new InvalidOperationException("outer-msg", new IOException("inner-secret")));
            string entry = AppErrorHandler.BuildLogEntry("X", ex);
            Assert.Contains(typeof(IOException).FullName!, entry);
            Assert.DoesNotContain("outer-msg", entry);
            Assert.DoesNotContain("inner-secret", entry);
        }

        [Fact]
        public void BuildLogEntry_KhongNemVoiNull()
        {
            string entry = AppErrorHandler.BuildLogEntry("X", null!);
            Assert.False(string.IsNullOrEmpty(entry));
        }
    }
}
