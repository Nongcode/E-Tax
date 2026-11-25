using E_Tax;

namespace E_Tax.Tests
{
    public class InvoiceSummaryTests
    {
        private static SearchResult Invoice(string type, decimal? total) =>
            new SearchResult { Loai_hoa_don = type, Tong_tien_thanh_toan = total };

        [Fact]
        public void Compute_Empty()
        {
            var summary = InvoiceSummary.Compute(Array.Empty<SearchResult>());

            Assert.Equal(new InvoiceSummary(0, 0m, 0, 0m), summary);
            Assert.Equal("Chưa có dữ liệu", summary.Format());
        }

        [Fact]
        public void Compute_SplitsByType()
        {
            var summary = InvoiceSummary.Compute(new[]
            {
                Invoice("Bán ra", 100m),
                Invoice("Bán ra", 250m),
                Invoice("Mua vào", 40m),
            });

            Assert.Equal(new InvoiceSummary(2, 350m, 1, 40m), summary);
        }

        [Fact]
        public void Compute_NullTotalsCountAsZero()
        {
            var summary = InvoiceSummary.Compute(new[] { Invoice("Bán ra", null), Invoice("Mua vào", null) });

            Assert.Equal(new InvoiceSummary(1, 0m, 1, 0m), summary);
        }

        [Fact]
        public void Format_UsesThousandSeparators()
        {
            var summary = new InvoiceSummary(80, 1_250_000_000m, 43, 640_000_000m);

            Assert.Equal("Bán ra: 80 HĐ · 1.250.000.000 ₫   |   Mua vào: 43 HĐ · 640.000.000 ₫", summary.Format());
        }
    }
}
