using System.Text.Json;
using E_Tax;

namespace E_Tax.Tests
{
    public class InvoiceClassifierTests
    {
        private const string ValidMst = "0100109106";

        private static SearchResult Make(string loai, int? tthai, string? seller = ValidMst, string? buyer = null) =>
            new SearchResult { Loai_hoa_don = loai, Trang_thai_hoa_don = tthai, Ma_so_thue = seller!, Ma_so_thue_nguoi_mua = buyer };

        [Theory]
        [InlineData(1, InvoiceRelation.Goc)] [InlineData(2, InvoiceRelation.ThayThe)]
        [InlineData(3, InvoiceRelation.DieuChinh)] [InlineData(4, InvoiceRelation.BiThayThe)]
        [InlineData(5, InvoiceRelation.BiDieuChinh)] [InlineData(6, InvoiceRelation.BiHuy)]
        [InlineData(99, InvoiceRelation.KhongRo)] [InlineData(null, InvoiceRelation.KhongRo)]
        public void RelationOf_MapsPortalStatus(int? tthai, InvoiceRelation expected) =>
            Assert.Equal(expected, InvoiceClassifier.RelationOf(tthai));

        [Theory]
        [InlineData("0100109106", true)] [InlineData("0100109106-001", true)] [InlineData("001099012345", true)]
        [InlineData("0100109107", false)] [InlineData("abc", false)] [InlineData("", false)] [InlineData(null, false)]
        public void IsValidTaxCode_ChecksFormatAndCheckDigit(string? mst, bool expected) =>
            Assert.Equal(expected, InvoiceClassifier.IsValidTaxCode(mst));

        [Fact]
        public void Check_BoughtInvoiceForOtherBuyer_IsInvalid()
        {
            var r = InvoiceClassifier.Check(Make("Mua vào", 1, buyer: "0100109106-001"), "0100109106");
            Assert.False(r.IsValid);
            Assert.Contains(r.Issues, i => i.Contains("người mua"));
        }

        [Fact]
        public void Check_SoldInvoiceFromOtherSeller_IsInvalid()
        {
            var r = InvoiceClassifier.Check(Make("Bán ra", 1, seller: "0100109106"), "0101234567");
            Assert.False(r.IsValid);
            Assert.Contains(r.Issues, i => i.Contains("người bán"));
        }

        [Fact]
        public void Check_Cancelled_IsInvalid()
        {
            var r = InvoiceClassifier.Check(Make("Bán ra", 6), ValidMst);
            Assert.False(r.IsValid);
            Assert.Equal(InvoiceRelation.BiHuy, r.Relation);
        }

        [Fact]
        public void Check_UnknownStatus_IsNotValid()
        {
            var r = InvoiceClassifier.Check(Make("Bán ra", 99), ValidMst);
            Assert.False(r.IsValid);
            Assert.Equal(InvoiceRelation.KhongRo, r.Relation);
        }

        [Fact]
        public void Check_NoOwnTaxCode_SkipsPartyComparison()
        {
            var r = InvoiceClassifier.Check(Make("Bán ra", 1), null);
            Assert.True(r.IsValid);
            Assert.Empty(r.Issues);
        }

        [Theory]
        [InlineData("{\"khmshdgoc\":1,\"khhdgoc\":\"C26T\",\"shdgoc\":12}")]
        [InlineData("{\"khmshdgoc\":\"1\",\"khhdgoc\":\"C26T\",\"shdgoc\":\"12\"}")]
        public void Deserialize_OriginalFields_AcceptNumberOrString(string json)
        {
            var r = JsonSerializer.Deserialize<SearchResult>(json)!;
            Assert.Equal("1", r.Ky_hieu_ma_so_goc);
            Assert.Equal("C26T", r.Ky_hieu_hoa_don_goc);
            Assert.Equal("12", r.Hoa_don_lien_quan);
        }

        [Fact]
        public void Deserialize_ObjectInOptionalField_BecomesNull()
        {
            var r = JsonSerializer.Deserialize<SearchResult>("{\"nmmst\":{},\"nmten\":\"A\"}")!;
            Assert.Null(r.Ma_so_thue_nguoi_mua);
            Assert.Equal("A", r.Ten_nguoi_mua);
        }
    }
}
