using System;
using System.Collections.Generic;
using E_Tax;

namespace E_Tax.Tests
{
    public class InvoiceLinkerTests
    {
        private const string Seller = "0100109106";

        private static SearchResult Orig(string id, string seller = Seller, int ms = 1, string kh = "C26TAA", int so = 10, string? ngay = "2026-01-15T00:00:00") =>
            new SearchResult { Id = id, Ma_so_thue = seller, Ky_hieu_ma_so = ms, Ky_hieu_hoa_don = kh, So_hoa_don = so, Trang_thai_hoa_don = 5, Ngay_lap = ngay! };

        private static SearchResult Child(string id, int tthai, string seller = Seller, string? gocNgay = null, string gocSo = "10") =>
            new SearchResult
            {
                Id = id, Ma_so_thue = seller, Ky_hieu_ma_so = 1, Ky_hieu_hoa_don = "C26TAA", So_hoa_don = 20,
                Trang_thai_hoa_don = tthai, Ngay_lap = "2026-02-10T00:00:00",
                Ky_hieu_ma_so_goc = " 1 ", Ky_hieu_hoa_don_goc = "C26TAA ", Hoa_don_lien_quan = gocSo, Ngay_lap_goc = gocNgay,
            };

        [Fact]
        public void Link_AdjustmentFindsOriginal_SameSellerAndKeys()
        {
            var map = InvoiceLinker.Link(new[] { Orig("o1"), Child("c1", 3, gocNgay: "15/01/2026") });
            Assert.Equal("o1", map["c1"].OriginalId);
        }

        [Fact]
        public void Link_SameNumberDifferentSeller_NotLinked()
        {
            var map = InvoiceLinker.Link(new[] { Orig("o1", seller: "0100109107"), Child("c1", 3) });
            Assert.Null(map["c1"].OriginalId);
        }

        [Fact]
        public void Link_OriginalOutsideResults_KeepsOriginalDate()
        {
            var map = InvoiceLinker.Link(new[] { Child("c1", 2, gocNgay: "15/01/2026") });
            Assert.Null(map["c1"].OriginalId);
            Assert.Equal(new DateTime(2026, 1, 15), map["c1"].OriginalDate);
        }

        [Fact]
        public void Link_OnlyReplacementAndAdjustment_AreLinked()
        {
            var map = InvoiceLinker.Link(new[] { Orig("o1"), Child("c1", 1), Child("c2", 2), Child("c3", 3) });
            Assert.False(map.ContainsKey("c1"));
            Assert.False(map.ContainsKey("o1"));
            Assert.True(map.ContainsKey("c2"));
            Assert.True(map.ContainsKey("c3"));
        }

        [Fact]
        public void EffectiveDate_UsesOriginalPeriod_ForAdjustment()
        {
            var child = Child("c1", 3, gocNgay: "15/01/2026");
            var map = InvoiceLinker.Link(new[] { child });
            Assert.Equal(new DateTime(2026, 1, 15), InvoiceLinker.EffectiveDate(child, map["c1"]));
        }

        [Fact]
        public void EffectiveDate_FallsBackToOwnDate_WhenNoOriginalDate()
        {
            var child = Child("c1", 3);
            var map = InvoiceLinker.Link(new[] { child });
            Assert.Equal(new DateTime(2026, 2, 10), InvoiceLinker.EffectiveDate(child, map["c1"]));
            Assert.Equal(new DateTime(2026, 2, 10), InvoiceLinker.EffectiveDate(child, null));
        }

        [Fact]
        public void Link_OriginalNumberWithLeadingZeros_IsLinked()
        {
            var map = InvoiceLinker.Link(new[] { Orig("o1"), Child("c1", 3, gocSo: "0000010") });
            Assert.Equal("o1", map["c1"].OriginalId);
        }

        [Fact]
        public void Link_OriginalInList_NoOriginalDate_UsesOriginalInvoiceDate()
        {
            var map = InvoiceLinker.Link(new[] { Orig("o1"), Child("c1", 3) });
            Assert.Equal(new DateTime(2026, 1, 15), map["c1"].OriginalDate);
        }

        [Fact]
        public void Link_OriginalDateField_TakesPriorityOverListedInvoice()
        {
            var map = InvoiceLinker.Link(new[] { Orig("o1"), Child("c1", 3, gocNgay: "05/01/2026") });
            Assert.Equal(new DateTime(2026, 1, 5), map["c1"].OriginalDate);
        }
    }
}
