using E_Tax;

namespace E_Tax.Tests
{
    public class InvoiceFilterTests
    {
        private static readonly List<SearchResult> Data = new()
        {
            new() { Id = "1", So_hoa_don = 123, Ma_so_thue = "0101234567", Thong_tin_hoa_don = "Công ty Đại Phát", Loai_hoa_don = "Mua vào" },
            new() { Id = "2", So_hoa_don = 1234, Ma_so_thue = "0309999999", Thong_tin_hoa_don = "Cty An Bình", Loai_hoa_don = "Bán ra" },
        };

        private static string Note(string id) => id == "2" ? "đã kiểm tra" : "";

        private static List<string?> Ids(InvoiceFilterCriteria c) =>
            InvoiceFilter.Apply(Data, c, Note).Select(x => x.Id).ToList<string?>();

        [Fact] public void InvoiceNumber_IsExact() => Assert.Equal(new[] { "1" }, Ids(new("123", null, null, false)));
        [Fact] public void InvoiceNumber_NotAnInteger_MatchesNothing() => Assert.Empty(Ids(new("abc", null, null, false)));
        [Fact] public void Supplier_IgnoresCaseAndDiacritics() => Assert.Equal(new[] { "1" }, Ids(new(null, "dai phat", null, false)));
        [Fact] public void Supplier_MatchesTaxCode() => Assert.Equal(new[] { "2" }, Ids(new(null, "030999", null, false)));
        [Fact] public void Type_AndNotes_Combine() => Assert.Equal(new[] { "2" }, Ids(new(null, null, "Bán ra", true)));
        [Fact] public void EmptyCriteria_ReturnsAll() => Assert.Equal(2, Ids(new(" ", "", null, false)).Count);
    }
}
