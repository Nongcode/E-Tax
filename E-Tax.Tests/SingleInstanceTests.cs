using E_Tax;

namespace E_Tax.Tests
{
    public class SingleInstanceTests
    {
        [Fact]
        public void MutexName_IsPerUser()
        {
            Assert.NotEqual(SingleInstance.MutexName("S-1-5-21-1"), SingleInstance.MutexName("S-1-5-21-2"));
            Assert.StartsWith(@"Global\", SingleInstance.MutexName("S-1-5-21-1"));
        }

        [Fact]
        public void TryAcquire_SecondHolderIsRefused_UntilFirstIsReleased()
        {
            string name = @"Local\E-Tax.Test." + Guid.NewGuid().ToString("N");

            var first = SingleInstance.TryAcquire(name);
            Assert.NotNull(first);
            Assert.Null(SingleInstance.TryAcquire(name));

            first!.Dispose();
            using var again = SingleInstance.TryAcquire(name);
            Assert.NotNull(again);
        }
    }
}
