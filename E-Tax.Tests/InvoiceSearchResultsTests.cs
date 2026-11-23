using System.Linq;
using E_Tax;
using Xunit;

namespace E_Tax.Tests
{
    public class InvoiceSearchResultsTests
    {
        private static SearchResult Inv(string? id, string loai, int so, string mst = "0101234567", string kh = "C26TAA") =>
            new SearchResult { Id = id!, Loai_hoa_don = loai, So_hoa_don = so, Ma_so_thue = mst, Ky_hieu_hoa_don = kh, Ky_hieu_ma_so = 1, Ngay_lap = "2026-05-01" };

        [Fact]
        public void Deduplicate_TwoInvoicesWithoutId_StaySeparate()
        {
            var r = InvoiceSearchResults.Deduplicate(new[] { Inv(null, "Mua vào", 1), Inv("", "Mua vào", 2) });
            Assert.Equal(2, r.Count);
        }

        [Fact]
        public void Deduplicate_SameIdInBothTypes_StaysAsTwo()
        {
            var r = InvoiceSearchResults.Deduplicate(new[] { Inv("X", "Bán ra", 1), Inv("X", "Mua vào", 1) });
            Assert.Equal(2, r.Count);
            Assert.Contains(r, x => x.Loai_hoa_don == "Mua vào");
        }

        [Fact]
        public void Deduplicate_TrueDuplicate_IsRemoved()
        {
            var r = InvoiceSearchResults.Deduplicate(new[] { Inv("X", "Mua vào", 1), Inv("X", "Mua vào", 1), Inv(null, "Bán ra", 5), Inv(null, "Bán ra", 5) });
            Assert.Equal(2, r.Count);
        }

        [Theory]
        [InlineData("{\"datas\":[]}", true)]
        [InlineData("{\"datas\":null,\"total\":0}", true)]
        [InlineData("{\"message\":\"x\"}", false)]
        [InlineData("null", false)]
        [InlineData("{\"datas\":null}", false)]
        [InlineData("{\"datas\":null,\"total\":5}", false)]
        [InlineData("not json", false)]
        public void IsEndOfResults(string json, bool expected) =>
            Assert.Equal(expected, InvoiceSearchResults.IsEndOfResults(json));
    }
}
