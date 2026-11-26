using System.Windows.Forms;
using E_Tax;

namespace E_Tax.Tests
{
    /// <summary>Hộp thoại "Cột hiển thị": tick ẩn/hiện, ↑↓ hoặc kéo để đổi thứ tự, "Mặc định". Cột khóa (STT) không ẩn, không dời.</summary>
    public class ColumnChooserFormTests
    {
        private static readonly ColumnState[] Current =
        {
            new("stt", true) { Header = "STT" }, new("a", true) { Header = "A" }, new("b", false) { Header = "B" }, new("c", true) { Header = "C" }
        };
        private static readonly ColumnState[] Defaults =
        {
            new("stt", true) { Header = "STT" }, new("a", true) { Header = "A" }, new("b", true) { Header = "B" }, new("c", true) { Header = "C" }
        };

        private static T OnDialog<T>(Func<ColumnChooserForm, T> probe)
        {
            T result = default!;
            Exception? failure = null;
            var t = new Thread(() =>
            {
                try
                {
                    using var dlg = new ColumnChooserForm("Tổng hợp", Current, Defaults, new HashSet<string> { "stt" });
                    result = probe(dlg);
                }
                catch (Exception ex) { failure = ex; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (failure != null) throw new Xunit.Sdk.XunitException(failure.ToString());
            return result;
        }

        private static string Describe(IEnumerable<ColumnState> s) => string.Join(",", s.Select(x => x.Name + (x.Visible ? "" : "-")));

        [Fact]
        public void StartsWithCurrentState()
        {
            Assert.Equal("stt,a,b-,c", OnDialog(d => Describe(d.Result)));
        }

        [Fact]
        public void MoveDown_ThenUp_ReordersSelected()
        {
            Assert.Equal("stt,b-,c,a", OnDialog(d =>
            {
                d.Select(1); d.MoveSelected(+1); d.MoveSelected(+1); // a xuống cuối
                return Describe(d.Result);
            }));
            Assert.Equal("stt,c,a,b-", OnDialog(d =>
            {
                d.Select(3); d.MoveSelected(-1); d.MoveSelected(-1);
                return Describe(d.Result);
            }));
        }

        [Fact]
        public void LockedColumn_CannotBeHiddenOrMoved_AndNothingMovesAboveIt()
        {
            Assert.Equal("stt,a,b-,c", OnDialog(d =>
            {
                d.SetChecked(0, false);
                d.Select(0); d.MoveSelected(+1);
                d.Select(1); d.MoveSelected(-1);
                return Describe(d.Result);
            }));
        }

        [Fact]
        public void Toggle_AndResetToDefault()
        {
            Assert.Equal("stt,a-,b,c", OnDialog(d => { d.SetChecked(1, false); d.SetChecked(2, true); return Describe(d.Result); }));
            Assert.Equal("stt,a,b,c", OnDialog(d => { d.SetChecked(1, false); d.ResetToDefault(); return Describe(d.Result); }));
        }
    }
}
