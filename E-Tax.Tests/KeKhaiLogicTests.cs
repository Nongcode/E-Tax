using E_Tax;

namespace E_Tax.Tests
{
    public class KeKhaiLogicTests
    {
        [Theory]
        [InlineData("1.234.567,5", "1234567.5")]
        [InlineData("1234567.5", "1234567.5")]
        [InlineData("1,234,567.5", "1234567.5")]
        [InlineData("1.234", "1234")]            // mơ hồ: đọc theo kiểu Việt (phân cách nghìn)
        [InlineData("1.234.567", "1234567")]
        [InlineData("1,234,567", "1234567")]
        [InlineData("1,5", "1.5")]
        [InlineData("1,234", "1234")]
        [InlineData("12,345", "12345")]
        [InlineData("250,000", "250000")]
        [InlineData("0,123", "0.123")]
        [InlineData("1234,567", "1234.567")]
        [InlineData("999999999999999999", "999999999999999999")]
        [InlineData("-999999999999999999", "-999999999999999999")]
        [InlineData("0.123", "0.123")]
        [InlineData("1234.567", "1234.567")]
        [InlineData("1234567", "1234567")]
        [InlineData("  500  ", "500")]
        [InlineData("-1.000", "-1000")]
        [InlineData("-2,5", "-2.5")]
        [InlineData(".5", "0.5")]
        [InlineData("0", "0")]
        [InlineData("", "0")]
        [InlineData("   ", "0")]
        public void TryParseAmount_Accepts(string text, string expected)
        {
            Assert.True(KeKhaiLogic.TryParseAmount(text, out var value));
            Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), value);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("12a")]
        [InlineData("1.23.4")]
        [InlineData("1.234,567.8")]
        [InlineData("1,5,5")]
        [InlineData("1,234.567,8")]
        [InlineData("-")]
        [InlineData(",")]
        [InlineData("5,")]
        [InlineData("1 234")]
        [InlineData("--5")]
        [InlineData("1e5")]
        [InlineData("1000000000000000000")]
        [InlineData("-1000000000000000000")]
        [InlineData("999999999999999999,5")]
        [InlineData("79228162514264337593543950335")]
        public void TryParseAmount_Rejects(string text)
        {
            Assert.False(KeKhaiLogic.TryParseAmount(text, out _));
        }

        [Fact]
        public void TryParseAmount_Null_IsZero()
        {
            Assert.True(KeKhaiLogic.TryParseAmount(null, out var v));
            Assert.Equal(0m, v);
        }

        [Theory]
        [InlineData(null, false, true, true, true)]
        [InlineData("khoa", false, true, true, false)]
        [InlineData(null, true, true, true, false)]
        [InlineData(null, false, false, true, false)]
        [InlineData(null, false, true, false, false)]
        public void CanExport_RequiresAllConditions(string? block, bool exporting, bool inputValid, bool hasCurrent, bool expected)
        {
            Assert.Equal(expected, KeKhaiLogic.CanExport(block, exporting, inputValid, hasCurrent));
        }

        [Fact]
        public void ComposeWarnings_KeepsOrder_BlockThenListingThenReturn()
        {
            var list = KeKhaiLogic.ComposeWarnings("khoa", new[] { "b1", "b2" }, new[] { "c1" });
            Assert.Equal(new[] { "khoa", "b1", "b2", "c1" }, list);
        }

        [Fact]
        public void ComposeWarnings_NoBlock_OmitsIt()
        {
            var list = KeKhaiLogic.ComposeWarnings(null, new[] { "b1" }, new string[0]);
            Assert.Equal(new[] { "b1" }, list);
        }
    }
}
