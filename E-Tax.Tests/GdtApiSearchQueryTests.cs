namespace E_Tax.Tests
{
    public class GdtApiSearchQueryTests
    {
        private static readonly DateTime From = new(2025, 8, 1);
        private static readonly DateTime To = new(2025, 8, 30, 23, 59, 59);

        // Cổng trả 500 "Không hỗ trợ sắp xếp theo nhiều trường" (2026-11-25)
        [Fact]
        public void SapXepMotTruong()
        {
            string q = GdtApi.BuildSearchQuery(From, To, InvoiceType.Sold, 0, 50);
            Assert.Contains("&sort=tdlap:desc&", q);
        }

        [Fact]
        public void BanRa_KhongLocTrangThai()
        {
            Assert.Equal("size=50&page=2&sort=tdlap:desc&search=tdlap=ge=01/08/2025T00:00:00;tdlap=le=30/08/2025T23:59:59",
                GdtApi.BuildSearchQuery(From, To, InvoiceType.Sold, 2, 50));
        }

        [Fact]
        public void MuaVao_ChiLayTrangThai5()
        {
            Assert.EndsWith(";ttxly==5", GdtApi.BuildSearchQuery(From, To, InvoiceType.Bought, 0, 50));
        }
    }
}
