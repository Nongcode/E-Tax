using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace E_Tax
{
    /// <summary>
    /// Hằng số và hàm tạo request cho API cổng hóa đơn điện tử (hoadondientu.gdt.gov.vn).
    /// </summary>
    internal static class GdtApi
    {
        public const string BaseUrl = "https://hoadondientu.gdt.gov.vn/api/";
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
            // Trang chính thức gắn 3 header này vào mọi request; thiếu thì đăng nhập bị 403
            req.Headers.TryAddWithoutValidation("request-id", Guid.NewGuid().ToString());
            req.Headers.TryAddWithoutValidation("Action", "");
            req.Headers.TryAddWithoutValidation("End-Point", EndPointFor(url));
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

        /// <summary>
        /// Tham số tra cứu danh sách hóa đơn trong khoảng ngày. Cổng chỉ cho sắp xếp theo một trường
        /// (nhiều trường trả 500); các trang có thể trùng hóa đơn, kết quả được loại trùng sau.
        /// </summary>
        public static string BuildSearchQuery(DateTime from, DateTime to, InvoiceType type, int page, int size)
        {
            string search = $"tdlap=ge={from:dd/MM/yyyyTHH:mm:ss};tdlap=le={to:dd/MM/yyyyTHH:mm:ss}";

            // Hóa đơn mua vào: chỉ lấy trạng thái xử lý 5
            if (type == InvoiceType.Bought)
            {
                search += ";ttxly==5";
            }

            return $"size={size}&page={page}&sort=tdlap:desc&search={search}";
        }

        /// <summary>Trang của cổng mà request giả lập đang đứng (header End-Point).</summary>
        private static string EndPointFor(string url) =>
            url.StartsWith("query/") || url.StartsWith("sco-query/") ? "/tra-cuu/tra-cuu-hoa-don" : "/";

        /// <summary>
        /// Hết hạn mọi cookie phiên cũ ("jwt", "jwtToken") thuộc host của cổng, bất kể path.
        /// Không đụng cookie cùng tên của domain khác hoặc cookie khác. Trả về số cookie đã hết hạn; không log giá trị cookie.
        /// </summary>
        public static int ExpireSessionCookies(CookieContainer container, Uri portalUri)
        {
            string host = portalUri.Host;
            int count = 0;
            foreach (Cookie cookie in container.GetAllCookies().ToList())
            {
                if (cookie.Name != "jwt" && cookie.Name != "jwtToken") continue;
                string domain = cookie.Domain.TrimStart('.');
                if (!host.Equals(domain, StringComparison.OrdinalIgnoreCase) &&
                    !host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase)) continue;
                // Cookie từ GetAllCookies có thể là bản sao: ghi đè bằng cookie hết hạn cùng tên/domain/path
                container.Add(new Cookie(cookie.Name, "", cookie.Path, cookie.Domain) { Expires = DateTime.UtcNow.AddDays(-1) });
                count++;
            }
            return count;
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
