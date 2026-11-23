using System.Net;

namespace E_Tax.Tests
{
    public class UpdateCheckerTests
    {
        private const string Valid = "{\"version\":\"1.0.1\",\"downloadUrl\":\"https://example.com/etax.zip\",\"notes\":\"Sửa lỗi\"}";
        private static readonly Version Current = new Version(1, 0, 0);

        [Fact]
        public void Parse_ManifestHopLe()
        {
            var info = UpdateChecker.Parse(Valid);
            Assert.NotNull(info);
            Assert.Equal(new Version(1, 0, 1), info!.Version);
            Assert.Equal("https://example.com/etax.zip", info.DownloadUrl);
            Assert.Equal("Sửa lỗi", info.Notes);
        }

        [Theory]
        [InlineData("{\"version\":\"1.0.1\",\"downloadUrl\":\"http://example.com/a.zip\"}")]
        [InlineData("{\"version\":\"1.0.1\",\"downloadUrl\":\"/a.zip\"}")]
        [InlineData("{\"version\":\"1.0.1\"}")]
        [InlineData("{\"version\":\"abc\",\"downloadUrl\":\"https://example.com/a.zip\"}")]
        [InlineData("{\"downloadUrl\":\"https://example.com/a.zip\"}")]
        [InlineData("{hỏng")]
        [InlineData("")]
        [InlineData("[]")]
        public void Parse_TuChoiDuLieuSai(string json)
        {
            Assert.Null(UpdateChecker.Parse(json));
        }

        [Theory]
        [InlineData("notes")]
        [InlineData("version")]
        [InlineData("downloadUrl")]
        public void Parse_SurrogateLe_TraNullKhongNem(string field)
        {
            string ok = "{\"version\":\"1.0.1\",\"downloadUrl\":\"https://example.com/a.zip\",\"notes\":\"n\"}";
            string json = ok.Replace("\"" + field + "\":\"", "\"" + field + "\":\"\\uD800");
            Assert.Null(UpdateChecker.Parse(json));
        }

        [Fact]
        public void IsNewer_SoSanhVersion()
        {
            var info = UpdateChecker.Parse(Valid)!;
            Assert.True(UpdateChecker.IsNewer(info, new Version(1, 0, 0)));
            Assert.False(UpdateChecker.IsNewer(info, new Version(1, 0, 1)));
            Assert.False(UpdateChecker.IsNewer(info, new Version(2, 0, 0)));
        }

        [Fact]
        public async Task CheckAsync_CoBanMoi_TraThongTin()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json(Valid));
            using var http = new HttpClient(handler);
            var info = await UpdateChecker.CheckAsync(http, "https://example.com/m.json", Current, CancellationToken.None);
            Assert.NotNull(info);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task CheckAsync_KhongMoiHon_TraNull()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json(Valid));
            using var http = new HttpClient(handler);
            Assert.Null(await UpdateChecker.CheckAsync(http, "https://example.com/m.json", new Version(1, 0, 1), CancellationToken.None));
        }

        [Fact]
        public async Task CheckAsync_LoiMang_TraNull()
        {
            var handler = new FakeHttpHandler((_, _) => throw new HttpRequestException("lỗi"));
            using var http = new HttpClient(handler);
            Assert.Null(await UpdateChecker.CheckAsync(http, "https://example.com/m.json", Current, CancellationToken.None));
        }

        [Fact]
        public async Task CheckAsync_Http500_TraNull()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json(Valid, HttpStatusCode.InternalServerError));
            using var http = new HttpClient(handler);
            Assert.Null(await UpdateChecker.CheckAsync(http, "https://example.com/m.json", Current, CancellationToken.None));
        }

        [Fact]
        public async Task CheckAsync_BiHuy_TraNullKhongNem()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json(Valid), TimeSpan.FromSeconds(5));
            using var http = new HttpClient(handler);
            using var cts = new CancellationTokenSource(50);
            Assert.Null(await UpdateChecker.CheckAsync(http, "https://example.com/m.json", Current, cts.Token));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CheckAsync_UrlRong_KhongGoiMang(string url)
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json(Valid));
            using var http = new HttpClient(handler);
            Assert.Null(await UpdateChecker.CheckAsync(http, url, Current, CancellationToken.None));
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void ManifestUrl_MacDinhRong()
        {
            Assert.Equal("", UpdateChecker.ManifestUrl);
        }
    }
}
