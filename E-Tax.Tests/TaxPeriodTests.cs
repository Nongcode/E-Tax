using System;
using E_Tax;
using Xunit;

namespace E_Tax.Tests
{
    public class TaxPeriodTests
    {
        [Fact] public void Quarter2_CoversAprilToJune()
        {
            var p = TaxPeriod.Quarter(2026, 2);
            Assert.Equal(new DateTime(2026, 4, 1), p.From);
            Assert.Equal(new DateTime(2026, 6, 30), p.To);
            Assert.Equal("Quý 2/2026", p.Label);
        }
        [Fact] public void Month_LeapFebruary() => Assert.Equal(new DateTime(2028, 2, 29), TaxPeriod.Month(2028, 2).To);
        [Theory] [InlineData(0)] [InlineData(13)]
        public void Month_Invalid_Throws(int m) => Assert.Throws<ArgumentOutOfRangeException>(() => TaxPeriod.Month(2026, m));
        [Fact] public void Year_And_Custom_Labels()
        {
            Assert.Equal("Năm 2026", TaxPeriod.Year(2026).Label);
            Assert.Equal("Tháng 2/2026", TaxPeriod.Month(2026, 2).Label);
            Assert.Equal("01/01/2026 - 15/01/2026", TaxPeriod.Custom(new DateTime(2026, 1, 1), new DateTime(2026, 1, 15)).Label);
        }

        [Theory]
        [InlineData("2026-01-31T17:30:00Z", 2026, 2, 1)]
        [InlineData("2026-03-01T00:00:00", 2026, 3, 1)]
        [InlineData("31/12/2026", 2026, 12, 31)]
        [InlineData("20261231235959", 2026, 12, 31)]
        public void ToVietnamDate_Parses(string raw, int y, int m, int d) =>
            Assert.Equal(new DateTime(y, m, d), InvoiceDate.ToVietnamDate(raw));
        [Theory] [InlineData(null)] [InlineData("")] [InlineData("abc")]
        public void ToVietnamDate_Invalid_ReturnsNull(string? raw) => Assert.Null(InvoiceDate.ToVietnamDate(raw));
    }
}
