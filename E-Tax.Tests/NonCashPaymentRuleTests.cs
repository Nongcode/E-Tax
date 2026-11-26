using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Mua vào từ 5 triệu (gộp cùng người bán, cùng ngày) phải có chứng từ thanh toán không dùng tiền mặt mới được khấu trừ.</summary>
    public class NonCashPaymentRuleTests
    {
        private const string Mua = "Mua vào";
        private const string Ban = "Bán ra";

        private static SearchResult H(string id, decimal total, string seller = "0300000001", string date = "2026-10-10T17:00:00Z",
            string loai = Mua, int status = 1, int mau = 1) => new()
        {
            Id = id, Loai_hoa_don = loai, Trang_thai_hoa_don = status, Ky_hieu_ma_so = mau, Ma_so_thue = seller,
            Ky_hieu_hoa_don = "C26TAA", So_hoa_don = int.Parse(id), Ngay_lap = date, Tong_tien_thanh_toan = total
        };

        [Fact]
        public void SingleInvoiceFromFiveMillion_IsFlagged()
        {
            var groups = NonCashPaymentRule.Find(new[] { H("1", 5_000_000m), H("2", 4_999_999m, seller: "0300000002") });
            var g = Assert.Single(groups);
            Assert.Equal(5_000_000m, g.Total);
            Assert.Single(g.Invoices);
        }

        // Tách 20tr thành nhiều HĐ dưới 5tr cùng ngày vẫn phải chuyển khoản
        [Fact]
        public void SplitInvoicesSameSellerSameDay_AreSummed()
        {
            var parts = Enumerable.Range(1, 5).Select(i => H(i.ToString(), 4_000_000m)).ToArray();
            var g = Assert.Single(NonCashPaymentRule.Find(parts));
            Assert.Equal(20_000_000m, g.Total);
            Assert.Equal(5, g.Invoices.Count);
        }

        // Ngày theo giờ Việt Nam: 17:00Z ngày 10 là ngày 11 ở VN
        [Fact]
        public void DifferentDaysOrSellers_AreNotSummed()
        {
            var list = new[]
            {
                H("1", 3_000_000m, date: "2026-10-10T17:00:00Z"),
                H("2", 3_000_000m, date: "2026-10-11T17:00:00Z"),
                H("3", 3_000_000m, seller: "0300000002", date: "2026-10-10T17:00:00Z")
            };
            Assert.Empty(NonCashPaymentRule.Find(list));
        }

        [Fact]
        public void SalesCancelledReplacedAndSalesInvoiceForm2_AreIgnored()
        {
            var list = new[]
            {
                H("1", 9_000_000m, loai: Ban),
                H("2", 9_000_000m, status: 4),   // bị thay thế
                H("3", 9_000_000m, status: 6),   // đã hủy
                H("4", 9_000_000m, mau: 2)       // hóa đơn bán hàng: không có thuế GTGT để khấu trừ
            };
            Assert.Empty(NonCashPaymentRule.Find(list));
        }

        [Fact]
        public void Note_ListsInvoicesAndRule()
        {
            var note = NonCashPaymentRule.Note(new[] { H("1", 6_000_000m), H("2", 100_000m, seller: "0300000002") });
            Assert.NotNull(note);
            Assert.Contains("1 hóa đơn", note);
            Assert.Contains("5.000.000", note);
            Assert.Contains("không dùng tiền mặt", note);
            Assert.Contains("C26TAA-1", note);
            Assert.Null(NonCashPaymentRule.Note(new[] { H("2", 100_000m) }));
        }
    }
}
