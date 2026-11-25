using E_Tax;

namespace E_Tax.Tests
{
    public class InvoiceDownloadPlanTests
    {
        private static SearchResult Inv(string? mst, int? khms, string? kh, int? so, string loai = "Mua vào", string? id = null) => new SearchResult
        {
            Id = id!, Ma_so_thue = mst!, Ky_hieu_ma_so = khms, Ky_hieu_hoa_don = kh!, So_hoa_don = so, Loai_hoa_don = loai
        };

        [Fact]
        public void FileIdentifier_TwoSuppliersSameKyHieuAndSo_Differ() =>
            Assert.NotEqual(Inv("0101111111", 1, "C26TAA", 5).FileIdentifier, Inv("0102222222", 1, "C26TAA", 5).FileIdentifier);

        [Fact]
        public void FileIdentifier_DifferentKyHieuMaSo_Differ() =>
            Assert.NotEqual(Inv("0101111111", 1, "C26TAA", 5).FileIdentifier, Inv("0101111111", 2, "C26TAA", 5).FileIdentifier);

        [Fact]
        public void FileIdentifier_HasAllParts() =>
            Assert.Equal("0101234567-001_1_C26TAA_5", Inv("0101234567-001", 1, "C26TAA", 5).FileIdentifier);

        [Fact]
        public void FileIdentifier_ReplacesInvalidCharacters()
        {
            string id = Inv("01/23:45", 1, "C2*6?T\"A<B>|\\", 5).FileIdentifier;
            Assert.Empty(id.Where(c => Path.GetInvalidFileNameChars().Contains(c)));
            Assert.Equal("01_23_45_1_C2_6_T_A_B____5", id);
        }

        [Fact]
        public void FileIdentifier_MissingParts_DoesNotThrow() =>
            Assert.Equal("MST_NA_KHMS_NA_KH_NA_SHD_NA", new SearchResult().FileIdentifier);

        [Fact]
        public void Build_SameInvoiceInBothTypes_DownloadsOnce()
        {
            var plan = InvoiceDownloadPlan.Build(new[]
            {
                Inv("0101111111", 1, "C26TAA", 5, "Bán ra", "x"),
                Inv("0101111111", 1, "C26TAA", 5, "Mua vào", "x")
            });
            Assert.Single(plan);
            Assert.Equal("Bán ra", plan[0].Invoice.Loai_hoa_don);
        }

        [Fact]
        public void Build_TwoSuppliers_KeepsBothInOrder()
        {
            var a = Inv("0101111111", 1, "C26TAA", 5);
            var b = Inv("0102222222", 1, "C26TAA", 5);
            var plan = InvoiceDownloadPlan.Build(new[] { a, b });
            Assert.Equal(new[] { a, b }, plan.Select(p => p.Invoice));
            Assert.Equal(2, plan.Select(p => p.FileId).Distinct().Count());
        }

        [Fact]
        public void Build_InvoicesMissingKeys_AreNotMergedAndGetOwnNames()
        {
            var plan = InvoiceDownloadPlan.Build(new[] { Inv(null, null, null, null), Inv(null, null, null, null) });
            Assert.Equal(2, plan.Count);
            Assert.Equal(2, plan.Select(p => p.FileId).Distinct().Count());
        }

        [Fact]
        public void Build_DifferentInvoicesWithSameSanitizedName_GetUniqueNames()
        {
            // "A/B" và "A:B" cùng thành "A_B"; "ab" và "AB" trùng tên trên Windows
            var plan = InvoiceDownloadPlan.Build(new[]
            {
                Inv("01", 1, "A/B", 5), Inv("01", 1, "A:B", 5), Inv("01", 1, "A_B", 5),
                Inv("02", 1, "ab", 5), Inv("02", 1, "AB", 5)
            });
            Assert.Equal(5, plan.Count);
            Assert.Equal(5, plan.Select(p => p.FileId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void OrderForMerge_SortsByKyHieuThenNumber_NoDuplicates_OnlyProduced()
        {
            var plan = InvoiceDownloadPlan.Build(new[]
            {
                Inv("02", 1, "B", 1), Inv("01", 1, "A", 10), Inv("01", 1, "A", 2), Inv("03", 1, "A", 2)
            });
            var produced = new[]
            {
                (plan[0], "b1.pdf"), (plan[1], "a10.pdf"), (plan[2], "a2-01.pdf"), (plan[3], "a2-03.pdf"), (plan[2], "a2-01.pdf")
            };
            Assert.Equal(new[] { "a2-01.pdf", "a2-03.pdf", "a10.pdf", "b1.pdf" }, InvoiceDownloadPlan.OrderForMerge(produced));
            Assert.Equal(new[] { "a10.pdf" }, InvoiceDownloadPlan.OrderForMerge(new[] { (plan[1], "a10.pdf") }));
        }
    }
}
