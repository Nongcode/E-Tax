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
        [InlineData("<html>502 Bad Gateway</html>")]
        [InlineData("")]
        [InlineData("{\"datas\":null,\"total\":null}")]
        [InlineData("{\"datas\":\"x\"}")]
        [InlineData("{\"message\":\"x\"}")]
        public void ParsePage_BadBody_IsFailedNotThrown(string json)
        {
            var (items, isEnd, failed) = InvoiceSearchResults.ParsePage(json);
            Assert.True(failed);
            Assert.False(isEnd);
            Assert.Null(items);
        }

        [Theory]
        [InlineData("{\"datas\":[],\"total\":0}")]
        [InlineData("{\"datas\":null,\"total\":0}")]
        public void ParsePage_EmptyPage_IsEnd(string json)
        {
            var (items, isEnd, failed) = InvoiceSearchResults.ParsePage(json);
            Assert.True(isEnd);
            Assert.False(failed);
            Assert.Null(items);
        }

        [Fact]
        public void ParsePage_WithItems_ReturnsItems()
        {
            var (items, isEnd, failed) = InvoiceSearchResults.ParsePage("{\"datas\":[{\"id\":\"a\"},{\"id\":\"b\"}],\"total\":2}");
            Assert.False(failed);
            Assert.False(isEnd);
            Assert.Equal(2, items!.Count);
        }

        [Fact]
        public void KeyOf_NoId_DiffersByKyHieuMaSo()
        {
            var a = new SearchResult { Ma_so_thue = "0101234567", Ky_hieu_ma_so = 1, Ky_hieu_hoa_don = "C26TAA", So_hoa_don = 1 };
            var b2 = new SearchResult { Ma_so_thue = "0101234567", Ky_hieu_ma_so = 2, Ky_hieu_hoa_don = "C26TAA", So_hoa_don = 1 };
            Assert.NotEqual(InvoiceSearchResults.KeyOf(a), InvoiceSearchResults.KeyOf(b2));
            Assert.Equal(2, InvoiceSearchResults.Deduplicate(new[] { a, b2 }).Count);
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
