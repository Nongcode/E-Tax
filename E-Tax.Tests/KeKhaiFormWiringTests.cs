using System.Reflection;
using System.Windows.Forms;
using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Dựng KeKhaiForm thật (không hiển thị, không gọi mạng) để kiểm tra lý do khóa đi đúng vào từng nút xuất.</summary>
    public class KeKhaiFormWiringTests
    {
        private static InvoiceDetailItem Line(string key, string loai) => new()
        {
            InvoiceKey = key, LoaiHoaDon = loai, TrangThaiHD = 1, DoanhSoChuaThue = 100m, TienThueGTGT = 8m, ThueSuat = "8%",
            KyHieuHD = "C26TAA", SoHD = 1, NgayHD = "01/05/2026", TenSanPham = "Hàng"
        };

        private static SearchResult Header(string id, string loai, int? status = 1) => new()
        {
            Id = id, Loai_hoa_don = loai, Trang_thai_hoa_don = status, Ngay_lap = "2026-05-10T00:00:00", Ma_so_thue = "0300000001"
        };

        private static (bool Vat, bool Appendix, bool Accounting) ButtonStates(SearchScope scope, int nonVnd, params SearchResult[] extraHeaders)
        {
            (bool, bool, bool) result = default;
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var items = new[] { Line("m", "Mua vào"), Line("b", "Bán ra") };
                    var headers = new List<SearchResult> { Header("m", "Mua vào"), Header("b", "Bán ra") };
                    headers.AddRange(extraHeaders);
                    using var form = new KeKhaiForm(items, TaxPeriod.Month(2026, 5), "0101234567", 0, 0, nonVnd, headers, scope, Array.Empty<string>());
                    bool Enabled(string field) =>
                        ((Button)typeof(KeKhaiForm).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!).Enabled;
                    result = (Enabled("_btnVat"), Enabled("_btnReduction"), Enabled("_btnAccounting"));
                }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) throw new Xunit.Sdk.XunitException("Không dựng được KeKhaiForm: " + failure);
            return result;
        }

        [Fact]
        public void BothDirections_AllButtonsEnabled() =>
            Assert.Equal((true, true, true), ButtonStates(new SearchScope(true, true), 0));

        [Fact]
        public void BoughtOnly_OnlyAccountingEnabled() =>
            Assert.Equal((false, false, true), ButtonStates(new SearchScope(true, false), 0));

        [Fact]
        public void SoldOnly_AppendixAndAccountingEnabled() =>
            Assert.Equal((false, true, true), ButtonStates(new SearchScope(false, true), 0));

        [Fact]
        public void ForeignCurrency_AllButtonsDisabled() =>
            Assert.Equal((false, false, false), ButtonStates(new SearchScope(true, true), 1));

        [Fact]
        public void UnknownStatusWithoutLines_AllButtonsDisabled() =>
            Assert.Equal((false, false, false), ButtonStates(new SearchScope(true, true), 0, Header("x", "Mua vào", null)));
    }
}
