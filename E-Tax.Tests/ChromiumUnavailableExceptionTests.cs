namespace E_Tax.Tests
{
    public class ChromiumUnavailableExceptionTests
    {
        public static IEnumerable<object[]> Failures() => new[]
        {
            new object[] { new InvalidDataException("html") },
            new object[] { new UnauthorizedAccessException() },
            new object[] { new HttpRequestException("x") },
            new object[] { new IOException() },
            new object[] { new TaskCanceledException() }, // timeout, không phải hủy của người dùng
        };

        [Theory]
        [MemberData(nameof(Failures))]
        public async Task LoiBuocTai_ThanhChromiumUnavailable(Exception inner)
        {
            var ex = await Assert.ThrowsAsync<ChromiumUnavailableException>(() =>
                ChromiumUnavailableException.RunAsync(() => throw inner, CancellationToken.None));
            Assert.Same(inner, ex.InnerException);
            Assert.Equal(ChromiumUnavailableException.UserMessage, ex.Message);
        }

        [Fact]
        public async Task DuongDanRong_CungMotLoai()
        {
            await Assert.ThrowsAsync<ChromiumUnavailableException>(() =>
                ChromiumUnavailableException.RunAsync(() => Task.FromResult<string?>(""), CancellationToken.None));
        }

        [Fact]
        public async Task NguoiDungHuy_KhongBiDoiLoai()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ChromiumUnavailableException.RunAsync(() => throw new OperationCanceledException(cts.Token), cts.Token));
        }

        [Fact]
        public async Task ThanhCong_TraDuongDan()
        {
            Assert.Equal("c:\\chrome.exe",
                await ChromiumUnavailableException.RunAsync(() => Task.FromResult<string?>("c:\\chrome.exe"), CancellationToken.None));
        }

        [Fact]
        public void ThongBao_KhongCoUrl()
        {
            Assert.DoesNotContain("http", ChromiumUnavailableException.UserMessage, StringComparison.OrdinalIgnoreCase);
        }
    }
}
