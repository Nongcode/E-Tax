using System.Windows.Forms;
using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Bánh răng "Cột": bật/tắt và sắp thứ tự cột, lưu theo tên bảng. STT (cột cố định) luôn hiện, luôn đứng đầu.</summary>
    public class GridColumnLayoutTests
    {
        private static DataGridView Grid(bool frozenStt = true)
        {
            var g = new DataGridView();
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "stt", HeaderText = "STT", Frozen = frozenStt });
            foreach (var n in new[] { "mst", "so", "ngay", "tien" })
                g.Columns.Add(new DataGridViewTextBoxColumn { Name = n, HeaderText = n.ToUpperInvariant() });
            return g;
        }

        private static string[] Order(DataGridView g) =>
            g.Columns.Cast<DataGridViewColumn>().OrderBy(c => c.DisplayIndex).Select(c => c.Name).ToArray();

        [Fact]
        public void Capture_ReturnsDisplayOrderAndVisibility()
        {
            using var g = Grid();
            g.Columns["so"]!.Visible = false;
            g.Columns["tien"]!.DisplayIndex = 1;

            var states = GridColumnLayout.Capture(g);

            Assert.Equal(new[] { "stt", "tien", "mst", "so", "ngay" }, states.Select(s => s.Name));
            Assert.False(states.Single(s => s.Name == "so").Visible);
            Assert.Equal("TIEN", states.Single(s => s.Name == "tien").Header);
        }

        [Fact]
        public void Apply_ReordersAndHides()
        {
            using var g = Grid();
            GridColumnLayout.Apply(g, new[] { C("stt"), C("tien"), C("ngay", false), C("mst"), C("so") });

            Assert.Equal(new[] { "stt", "tien", "ngay", "mst", "so" }, Order(g));
            Assert.False(g.Columns["ngay"]!.Visible);
            Assert.True(g.Columns["mst"]!.Visible);
        }

        [Fact]
        public void Apply_KeepsFrozenSttFirstAndVisible_EvenIfSavedOtherwise()
        {
            using var g = Grid();
            GridColumnLayout.Apply(g, new[] { C("tien"), C("stt", false), C("mst") });

            Assert.Equal("stt", Order(g)[0]);
            Assert.True(g.Columns["stt"]!.Visible);
        }

        [Fact]
        public void Apply_SttByHeader_WhenNotFrozen()
        {
            using var g = Grid(frozenStt: false);
            GridColumnLayout.Apply(g, new[] { C("mst"), C("stt", false) });
            Assert.True(g.Columns["stt"]!.Visible);
        }

        // Bản cũ lưu cột đã bị xóa, hoặc bản mới thêm cột chưa có trong file lưu
        [Fact]
        public void Apply_IgnoresUnknown_AndAppendsNewColumnsVisible()
        {
            using var g = Grid();
            GridColumnLayout.Apply(g, new[] { C("stt"), C("khong-con"), C("tien"), C("mst", false) });

            var order = Order(g);
            Assert.Equal(new[] { "stt", "tien", "mst" }, order.Take(3));
            Assert.Equal(new[] { "so", "ngay" }, order.Skip(3));
            Assert.True(g.Columns["so"]!.Visible);
        }

        [Fact]
        public void Apply_NeverHidesEveryColumn()
        {
            using var g = Grid(frozenStt: false);
            g.Columns["stt"]!.HeaderText = "Số TT khác";
            GridColumnLayout.Apply(g, g.Columns.Cast<DataGridViewColumn>().Select(c => C(c.Name, false)).ToList());
            Assert.Contains(g.Columns.Cast<DataGridViewColumn>(), c => c.Visible);
        }

        [Fact]
        public void CaptureThenApply_RoundTrips()
        {
            using var a = Grid();
            a.Columns["ngay"]!.DisplayIndex = 1;
            a.Columns["mst"]!.Visible = false;
            var saved = GridColumnLayout.Capture(a);

            using var b = Grid();
            GridColumnLayout.Apply(b, saved);

            Assert.Equal(Order(a), Order(b));
            Assert.False(b.Columns["mst"]!.Visible);
        }

        private static ColumnState C(string name, bool visible = true) => new(name, visible);
    }
}
