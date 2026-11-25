namespace E_Tax.Tests
{
    public class GdtApiUrlTests
    {
        // Cổng hoá đơn bỏ cổng :30000, API chuyển sang /api/ (2026-11-25)
        [Theory]
        [InlineData("captcha", "https://hoadondientu.gdt.gov.vn/api/captcha")]
        [InlineData("security-taxpayer/authenticate", "https://hoadondientu.gdt.gov.vn/api/security-taxpayer/authenticate")]
        [InlineData("query/invoices/purchase?sort=tdlap", "https://hoadondientu.gdt.gov.vn/api/query/invoices/purchase?sort=tdlap")]
        public void DuongDanTuongDoi_GhepSauApi(string path, string expected)
        {
            Assert.Equal(expected, new Uri(new Uri(GdtApi.BaseUrl), path).ToString());
        }
    }
}
