using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Thẻ số liệu (kiểu Chuẩn / Menu trái) và thanh trạng thái (kiểu Gọn).</summary>
    public class InvoiceKpisTests
    {
        private static SearchResult H(string loai, decimal total, decimal tax, int status = 1, int mau = 1) => new()
        {
            Loai_hoa_don = loai, Tong_tien_thanh_toan = total, Tong_tien_thue = tax, Trang_thai_hoa_don = status, Ky_hieu_ma_so = mau
        };

        [Fact]
        public void Totals_ByDirection()
        {
            var k = InvoiceKpis.Compute(new[] { H("Bán ra", 1080, 80), H("Bán ra", 540, 40), H("Mua vào", 216, 16) });

            Assert.Equal(2, k.SoldCount);
            Assert.Equal(1620m, k.SoldTotal);
            Assert.Equal(120m, k.SoldTax);
            Assert.Equal(1, k.BoughtCount);
            Assert.Equal(216m, k.BoughtTotal);
            Assert.Equal(16m, k.BoughtTax);
            Assert.Equal(104m, k.VatEstimate);
        }

        // Thuế tạm tính chỉ lấy HĐ được tính thuế; HĐ bán hàng mẫu 2 mua vào không có thuế GTGT được khấu trừ
        [Fact]
        public void VatEstimate_UsesOnlyTaxableInvoices()
        {
            var k = InvoiceKpis.Compute(new[]
            {
                H("Bán ra", 1080, 80),
                H("Bán ra", 1080, 80, status: 6),   // đã hủy
                H("Mua vào", 216, 16, status: 4),   // bị thay thế
                H("Mua vào", 500, 30, mau: 2)
            });
            Assert.Equal(80m, k.VatEstimate);
        }

        [Fact]
        public void Attention_CountsEveryNonOriginalInvoice()
        {
            var k = InvoiceKpis.Compute(new[] { H("Bán ra", 1, 0), H("Bán ra", 1, 0, status: 3), H("Mua vào", 1, 0, status: 6), H("Mua vào", 1, 0, status: 99) });
            Assert.Equal(3, k.Attention);
        }

        [Fact]
        public void Empty_IsZero()
        {
            var k = InvoiceKpis.Compute(Array.Empty<SearchResult>());
            Assert.Equal(0, k.SoldCount + k.BoughtCount + k.Attention);
            Assert.Equal(0m, k.VatEstimate);
        }

        [Theory]
        [InlineData("", "", 0, false, 0)]
        [InlineData("12", "", 0, false, 1)]
        [InlineData(" ", "abc", 2, true, 3)]
        [InlineData("1", "x", 1, true, 4)]
        public void FilterBadge_CountsActiveConditions(string soHd, string ncc, int loai, bool ghiChu, int expected)
        {
            Assert.Equal(expected, InvoiceKpis.ActiveFilterCount(soHd, ncc, loai, ghiChu));
        }
    }
}
