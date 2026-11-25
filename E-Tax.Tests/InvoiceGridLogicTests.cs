using System.Drawing;
using E_Tax;

namespace E_Tax.Tests
{
    public class InvoiceGridLogicTests
    {
        [Theory]
        [InlineData(0, 200, 1)]
        [InlineData(1, 200, 1)]
        [InlineData(200, 200, 1)]
        [InlineData(201, 200, 2)]
        [InlineData(1000, 200, 5)]
        public void PageCount_RoundsUpAndIsAtLeastOne(int total, int size, int expected) =>
            Assert.Equal(expected, InvoiceGridLogic.PageCount(total, size));

        [Theory]
        [InlineData(-3, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(99, 2)]
        public void ClampPage_StaysInRange(int page, int expected) =>
            Assert.Equal(expected, InvoiceGridLogic.ClampPage(page, total: 450, pageSize: 200));

        [Fact]
        public void Slice_ReturnsRequestedPageAndShortLastPage()
        {
            var items = Enumerable.Range(1, 450).ToList();
            Assert.Equal(200, InvoiceGridLogic.Slice(items, 0, 200).Count);
            Assert.Equal(201, InvoiceGridLogic.Slice(items, 1, 200)[0]);
            var last = InvoiceGridLogic.Slice(items, 2, 200);
            Assert.Equal(50, last.Count);
            Assert.Equal(450, last[^1]);
        }

        [Fact]
        public void Slice_PageBeyondEnd_ClampsToLastPage_EmptyListGivesEmpty()
        {
            var items = Enumerable.Range(1, 450).ToList();
            Assert.Equal(401, InvoiceGridLogic.Slice(items, 50, 200)[0]);
            Assert.Empty(InvoiceGridLogic.Slice(new List<int>(), 3, 200));
        }

        [Theory]
        [InlineData(InvoiceRelation.DieuChinh)]
        [InlineData(InvoiceRelation.BiDieuChinh)]
        public void ClassificationColor_Adjustment_IsLightYellow(InvoiceRelation r) =>
            Assert.Equal(Color.FromArgb(0xFF, 0xF9, 0xC4), InvoiceGridLogic.ClassificationColor(r));

        [Theory]
        [InlineData(InvoiceRelation.ThayThe)]
        [InlineData(InvoiceRelation.BiThayThe)]
        public void ClassificationColor_Replacement_IsOrange(InvoiceRelation r) =>
            Assert.Equal(Color.FromArgb(0xFF, 0xCC, 0x80), InvoiceGridLogic.ClassificationColor(r));

        [Theory]
        [InlineData(InvoiceRelation.Goc)]
        [InlineData(InvoiceRelation.BiHuy)]
        [InlineData(InvoiceRelation.KhongRo)]
        public void ClassificationColor_OtherRelations_NoColor(InvoiceRelation r) =>
            Assert.Null(InvoiceGridLogic.ClassificationColor(r));

        [Fact]
        public void RowColor_RiskBeatsUnknownBeatsClassification()
        {
            var risky = Color.Red;
            var unknown = Color.Gray;
            Assert.Equal(risky, InvoiceGridLogic.RowColor(RiskStatus.Risky, InvoiceRelation.DieuChinh, risky, unknown));
            Assert.Equal(unknown, InvoiceGridLogic.RowColor(RiskStatus.Unknown, InvoiceRelation.ThayThe, risky, unknown));
            Assert.Equal(Color.FromArgb(0xFF, 0xCC, 0x80), InvoiceGridLogic.RowColor(RiskStatus.Safe, InvoiceRelation.ThayThe, risky, unknown));
            Assert.Equal(Color.FromArgb(0xFF, 0xF9, 0xC4), InvoiceGridLogic.RowColor(null, InvoiceRelation.BiDieuChinh, risky, unknown));
            Assert.Null(InvoiceGridLogic.RowColor(null, InvoiceRelation.Goc, risky, unknown));
        }

        [Fact]
        public void ClassificationText_AddsOriginalDateWhenLinked()
        {
            var inv = new SearchResult { Id = "a", Trang_thai_hoa_don = 3 };
            Assert.Equal("Hóa đơn điều chỉnh", InvoiceGridLogic.ClassificationText(inv, null));
            var link = new InvoiceLink("a", "b", new DateTime(2026, 3, 5));
            Assert.Equal("Hóa đơn điều chỉnh (gốc 05/03/2026)", InvoiceGridLogic.ClassificationText(inv, link));
            Assert.Equal("Hóa đơn điều chỉnh", InvoiceGridLogic.ClassificationText(inv, new InvoiceLink("a", null, null)));
        }

        [Theory]
        [InlineData("0100109106", "0100109106")]
        [InlineData("  0100109106  ", "0100109106")]
        [InlineData("0100109106-001", "0100109106-001")]
        [InlineData("admin", null)]
        [InlineData("0100109107", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void CurrentTaxCode_OnlyValidUsernames(string? username, string? expected) =>
            Assert.Equal(expected, InvoiceGridLogic.CurrentTaxCode(username));

        private const string Own = "0100109106";

        private static SearchResult Bought(string? buyer) => new SearchResult
        {
            Id = "id1", Loai_hoa_don = "Mua vào", Trang_thai_hoa_don = 1, Ma_so_thue = Own, Ky_hieu_ma_so = 1,
            Ky_hieu_hoa_don = "C26TAA", So_hoa_don = 7, Ngay_lap = "2026-03-05T00:00:00", Thong_tin_hoa_don = "Cty A",
            Tong_tien_thanh_toan = 1100m, Ma_so_thue_nguoi_mua = buyer,
        };

        [Fact]
        public void ToRow_MapsDisplayFields()
        {
            var row = InvoiceGridLogic.ToRow(Bought(Own), null, Own, "ghi chú", new[] { @"C:\x\a.pdf", @"C:\x\a.xml" });
            Assert.Equal("Mua vào", row.Loai);
            Assert.Equal(Own, row.Mst);
            Assert.Equal("1C26TAA", row.KyHieu);
            Assert.Equal(7, row.SoHd);
            Assert.Equal("Cty A", row.NguoiBan);
            Assert.Equal(1100m, row.TongTien);
            Assert.Equal("Hóa đơn gốc", row.PhanLoai);
            Assert.Equal("", row.CanhBao);
            Assert.Equal("PDF, XML", row.Files);
            Assert.Equal("ghi chú", row.GhiChu);
        }

        [Fact]
        public void ToRow_FlagsRealMismatch_ButNotWhenOwnTaxCodeIsNullOrBuyerMissing()
        {
            Assert.Contains("người mua", InvoiceGridLogic.ToRow(Bought("0101234567"), null, Own, "", Array.Empty<string>()).CanhBao);
            Assert.Equal("", InvoiceGridLogic.ToRow(Bought("0101234567"), null, null, "", Array.Empty<string>()).CanhBao);
            Assert.Equal("", InvoiceGridLogic.ToRow(Bought(null), null, Own, "", Array.Empty<string>()).CanhBao);
            Assert.Equal("", InvoiceGridLogic.ToRow(Bought(Own), null, Own, "", Array.Empty<string>()).Files);
        }

        [Theory]
        [InlineData("0100109106", "0100109106", "0100109106")]
        [InlineData("0100109106", "0101234567", null)]
        [InlineData(null, "0100109106", null)]
        public void PartyCheckTaxCode_OnlyWhenLoggedInAsViewedCompany(string? current, string viewed, string? expected) =>
            Assert.Equal(expected, InvoiceGridLogic.PartyCheckTaxCode(current, viewed));

        [Fact]
        public void ExistingNonEmptyFiles_SkipsMissingAndEmpty()
        {
            string dir = Path.Combine(Path.GetTempPath(), "etax-grid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string ok = Path.Combine(dir, "a.pdf");
                string empty = Path.Combine(dir, "b.xml");
                File.WriteAllText(ok, "x");
                File.WriteAllText(empty, "");
                var got = InvoiceGridLogic.ExistingNonEmptyFiles(ok, empty, Path.Combine(dir, "missing.xml"));
                Assert.Equal(new[] { ok }, got);
            }
            finally { Directory.Delete(dir, true); }
        }
    }
}
