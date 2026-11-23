using E_Tax;

namespace E_Tax.Tests
{
    public class BatchProgressTests
    {
        [Fact]
        public void Summary_ShowsCounts() =>
            Assert.Equal("Tải HĐ: 3/10, lỗi 1", new BatchProgress("Tải HĐ", 3, 10, 1).Summary);

        [Fact]
        public void Summary_HidesFailedWhenZero() =>
            Assert.Equal("Tải HĐ: 3/10", new BatchProgress("Tải HĐ", 3, 10, 0).Summary);
    }
}
