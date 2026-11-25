namespace E_Tax.Tests
{
    public class GdtApiRequestHeaderTests
    {
        private static string H(HttpRequestMessage req, string name) =>
            req.Headers.TryGetValues(name, out var v) ? string.Join(",", v) : "<thiếu>";

        // Cổng thuế trả 403 nếu thiếu các header trang chính thức luôn gửi (2026-11-25)
        [Fact]
        public void CoHeaderRequestId_Action_EndPoint()
        {
            using var req = GdtApi.CreateRequest(HttpMethod.Post, "security-taxpayer/authenticate", "application/json");
            Assert.True(Guid.TryParse(H(req, "request-id"), out _));
            Assert.Equal("", H(req, "Action"));
            Assert.Equal("/", H(req, "End-Point"));
        }

        [Fact]
        public void RequestId_KhacNhauMoiLan()
        {
            using var a = GdtApi.CreateRequest(HttpMethod.Get, "captcha", "*/*");
            using var b = GdtApi.CreateRequest(HttpMethod.Get, "captcha", "*/*");
            Assert.NotEqual(H(a, "request-id"), H(b, "request-id"));
        }

        // Lệnh tra cứu gửi End-Point là trang tra cứu hoá đơn như trang chính thức
        [Theory]
        [InlineData("query/invoices/sold?size=50", "/tra-cuu/tra-cuu-hoa-don")]
        [InlineData("sco-query/invoices/purchase?size=50", "/tra-cuu/tra-cuu-hoa-don")]
        [InlineData("captcha", "/")]
        public void EndPoint_TheoLoaiLenh(string url, string expected)
        {
            using var req = GdtApi.CreateRequest(HttpMethod.Get, url, "application/json");
            Assert.Equal(expected, H(req, "End-Point"));
        }
    }
}
