using System.Drawing;
using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Icon ribbon vẽ từ SVG nhúng (thư viện Svg). Tên lạ trả null để nút chỉ hiện chữ.</summary>
    public class RibbonIconsTests
    {
        public static readonly TheoryData<string> Names = new();

        static RibbonIconsTests()
        {
            foreach (var n in new[] { "search", "eye", "print", "layers", "download", "sheet", "list", "file", "calc", "box",
                                      "shield", "note", "refresh", "filter", "user", "lock", "logout" })
                Names.Add(n);
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void KnownIcon_RendersAtRequestedSize_WithInk(string name)
        {
            using var bmp = RibbonIcons.Get(name, 24, UiTheme.Primary);

            Assert.NotNull(bmp);
            Assert.Equal(new Size(24, 24), bmp!.Size);
            int inked = 0;
            for (int x = 0; x < 24; x++)
                for (int y = 0; y < 24; y++)
                    if (bmp.GetPixel(x, y).A > 64) inked++;
            Assert.InRange(inked, 10, 24 * 24 - 10); // có nét vẽ nhưng không tô kín
        }

        [Fact]
        public void Icon_UsesRequestedColor()
        {
            using var bmp = RibbonIcons.Get("search", 32, Color.FromArgb(0x18, 0x5A, 0xBD))!;
            var solid = Enumerable.Range(0, 32).SelectMany(x => Enumerable.Range(0, 32).Select(y => bmp.GetPixel(x, y)))
                .Where(p => p.A == 255).ToList();
            Assert.NotEmpty(solid);
            Assert.All(solid, p => Assert.True(p.B > p.R && p.B > p.G, $"Màu lạ {p}"));
        }

        [Theory]
        [InlineData("khong-co")]
        [InlineData("")]
        public void UnknownIcon_ReturnsNull(string name)
        {
            Assert.Null(RibbonIcons.Get(name, 24, Color.Black));
        }

        [Fact]
        public void ReturnsIndependentBitmaps_SoDisposingOneIsSafe()
        {
            var a = RibbonIcons.Get("eye", 16, Color.Black)!;
            a.Dispose();
            using var b = RibbonIcons.Get("eye", 16, Color.Black)!;
            Assert.Equal(16, b.Width);
        }
    }
}
