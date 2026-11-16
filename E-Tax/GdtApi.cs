using System.Net.Http;
using System.Net.Http.Headers;

namespace E_Tax
{
    /// <summary>
    /// Hằng số và hàm tạo request cho API cổng hóa đơn điện tử (hoadondientu.gdt.gov.vn).
    /// </summary>
    internal static class GdtApi
    {
        public const string BaseUrl = "https://hoadondientu.gdt.gov.vn:30000/";
        public const string PortalUrl = "https://hoadondientu.gdt.gov.vn/";
        public const string PortalOrigin = "https://hoadondientu.gdt.gov.vn";

        public const string BrowserUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36";

        /// <summary>
        /// Tạo request có User-Agent, Accept; thêm Bearer token nếu có và Referer/Origin của cổng nếu cần.
        /// </summary>
        public static HttpRequestMessage CreateRequest(HttpMethod method, string url, string accept,
            string? jwtToken = null, bool withPortalHeaders = false)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.UserAgent.ParseAdd(BrowserUserAgent);
            req.Headers.Add("Accept", accept);
            if (withPortalHeaders)
            {
                req.Headers.Add("Referer", PortalUrl);
                req.Headers.Add("Origin", PortalOrigin);
            }
            if (!string.IsNullOrEmpty(jwtToken))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            }
            return req;
        }

        public static string BuildDetailUrl(string nbmst, string khhdon, int shdon, int khmshdon, int? page = null, int? size = null)
        {
            string url = $"query/invoices/detail?" +
                         $"nbmst={Uri.EscapeDataString(nbmst)}&" +
                         $"khhdon={Uri.EscapeDataString(khhdon)}&" +
                         $"shdon={shdon}&" +
                         $"khmshdon={khmshdon}";
            if (page.HasValue) url += $"&page={page.Value}";
            if (size.HasValue) url += $"&size={size.Value}";
            return url;
        }
    }
}
