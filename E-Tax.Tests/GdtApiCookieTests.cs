using System.Net;

namespace E_Tax.Tests
{
    public class GdtApiCookieTests
    {
        private static readonly Uri Portal = new Uri("https://hoadondientu.gdt.gov.vn/");

        private static Cookie C(string name, string domain, string path) =>
            new Cookie(name, "v", path, domain);

        private static List<string> Remaining(CookieContainer c) =>
            c.GetAllCookies().Select(x => $"{x.Name}|{x.Domain}|{x.Path}").ToList();

        [Fact]
        public void XoaJwtOGoc_VaOPathCon()
        {
            var c = new CookieContainer();
            c.Add(C("jwt", "hoadondientu.gdt.gov.vn", "/"));
            c.Add(C("jwt", "hoadondientu.gdt.gov.vn", "/security-taxpayer"));
            c.Add(C("jwtToken", "hoadondientu.gdt.gov.vn", "/query"));

            int n = GdtApi.ExpireSessionCookies(c, Portal);

            Assert.Equal(3, n);
            Assert.Empty(c.GetAllCookies());
        }

        [Fact]
        public void GiuCookieCungTenODomainKhac_VaCookieKhacCuaCong()
        {
            var c = new CookieContainer();
            c.Add(C("jwt", "hoadondientu.gdt.gov.vn", "/"));
            c.Add(C("jwt", "example.com", "/"));
            c.Add(C("lang", "hoadondientu.gdt.gov.vn", "/"));

            int n = GdtApi.ExpireSessionCookies(c, Portal);

            Assert.Equal(1, n);
            var left = Remaining(c);
            Assert.Contains("jwt|example.com|/", left);
            Assert.Contains("lang|hoadondientu.gdt.gov.vn|/", left);
            Assert.Equal(2, left.Count);
        }

        [Fact]
        public void CookieDomainCha_CungBiXoa()
        {
            var c = new CookieContainer();
            c.Add(C("jwt", ".gdt.gov.vn", "/"));
            Assert.Equal(1, GdtApi.ExpireSessionCookies(c, Portal));
            Assert.Empty(c.GetAllCookies());
        }
    }
}
