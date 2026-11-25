using E_Tax;

namespace E_Tax.Tests
{
    public class BusyGateTests
    {
        [Fact]
        public void TryEnter_WhenIdle_Succeeds_AndReportsFlow()
        {
            var gate = new BusyGate();
            Assert.False(gate.IsBusy);
            Assert.Null(gate.CurrentFlow);
            Assert.True(gate.TryEnter("Tìm kiếm"));
            Assert.True(gate.IsBusy);
            Assert.Equal("Tìm kiếm", gate.CurrentFlow);
        }

        [Fact]
        public void SecondEnter_IsRefused_AndKeepsFirstFlow()
        {
            var gate = new BusyGate();
            gate.TryEnter("Tải");
            Assert.False(gate.TryEnter("Kho hóa đơn"));
            Assert.Equal("Tải", gate.CurrentFlow);
        }

        [Fact]
        public void ExitThenEnter_Works()
        {
            var gate = new BusyGate();
            gate.TryEnter("A");
            gate.Exit();
            Assert.False(gate.IsBusy);
            Assert.Null(gate.CurrentFlow);
            Assert.True(gate.TryEnter("B"));
            Assert.Equal("B", gate.CurrentFlow);
        }

        [Fact]
        public void Changed_FiresOnEnterAndExit_ButNotOnRefusedEnter()
        {
            var gate = new BusyGate();
            int count = 0;
            var busyStates = new List<bool>();
            gate.Changed += () => { count++; busyStates.Add(gate.IsBusy); };
            gate.TryEnter("A");
            gate.TryEnter("B");
            gate.Exit();
            Assert.Equal(2, count);
            Assert.Equal(new[] { true, false }, busyStates);
        }

        [Fact]
        public void Exit_WithoutEnter_Throws()
        {
            var gate = new BusyGate();
            Assert.Throws<InvalidOperationException>(() => gate.Exit());
            gate.TryEnter("A");
            gate.Exit();
            Assert.Throws<InvalidOperationException>(() => gate.Exit());
        }
    }
}
