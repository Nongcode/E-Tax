using System.Net;
using E_Tax;

namespace E_Tax.Tests
{
    public class InvoiceDetailServiceTests
    {
        private static readonly TimeSpan[] NoDelay = { TimeSpan.Zero, TimeSpan.Zero };

        private static SearchResult Invoice(string id, int so = 1) => new SearchResult
        {
            Id = id,
            Ma_so_thue = "0101234567",
            Ky_hieu_hoa_don = "C24TAA",
            So_hoa_don = so,
            Ky_hieu_ma_so = 1
        };

        private static InvoiceDetailService CreateService(FakeHttpHandler handler) =>
            new InvoiceDetailService(
                new HttpClient(handler) { BaseAddress = new Uri(GdtApi.BaseUrl) },
                () => "test-token",
                _ => { },
                NoDelay);

        [Fact]
        public async Task GetDetailPages_CachesById()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json(FakeHttpHandler.DetailJson(3)));
            var service = CreateService(handler);

            var first = await service.GetDetailPagesAsync(Invoice("a"));
            var second = await service.GetDetailPagesAsync(Invoice("a"));

            Assert.NotNull(first);
            Assert.Same(first, second);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task GetDetailPages_FollowsPaging()
        {
            var handler = new FakeHttpHandler((req, _) =>
                FakeHttpHandler.Json(FakeHttpHandler.DetailJson(req.RequestUri!.Query.Contains("page=0") ? 50 : 3)));
            var service = CreateService(handler);

            var pages = await service.GetDetailPagesAsync(Invoice("a"));

            Assert.NotNull(pages);
            Assert.Equal(2, pages!.Count);
            Assert.Equal(2, handler.RequestCount);
            Assert.Contains(handler.Urls, u => u.Contains("page=1") && u.Contains("size=50"));
        }

        [Fact]
        public async Task GetDetailPages_ErrorNotCached()
        {
            var handler = new FakeHttpHandler((_, n) => n == 1
                ? FakeHttpHandler.Json("{\"message\":\"Unauthorized\"}", HttpStatusCode.Unauthorized)
                : FakeHttpHandler.Json(FakeHttpHandler.DetailJson(2)));
            var service = CreateService(handler);

            var failed = await service.GetDetailPagesAsync(Invoice("a"));
            Assert.Null(failed);
            Assert.Equal(1, handler.RequestCount); // 401 không thử lại

            var ok = await service.GetDetailPagesAsync(Invoice("a"));
            Assert.NotNull(ok);
            Assert.Single(ok!);
            Assert.Equal(2, handler.RequestCount);
        }

        [Fact]
        public async Task GetDetailPages_PartialPagesReturnNull()
        {
            // Trang 1 đủ 50 dòng, trang 2 lỗi 401 → coi là lỗi (không trả dữ liệu thiếu), không cache
            var handler = new FakeHttpHandler((req, n) => n == 2
                ? FakeHttpHandler.Json("{\"message\":\"Unauthorized\"}", HttpStatusCode.Unauthorized)
                : FakeHttpHandler.Json(FakeHttpHandler.DetailJson(req.RequestUri!.Query.Contains("page=0") ? 50 : 3)));
            var service = CreateService(handler);

            var partial = await service.GetDetailPagesAsync(Invoice("a"));
            Assert.Null(partial);

            var full = await service.GetDetailPagesAsync(Invoice("a"));
            Assert.NotNull(full);
            Assert.Equal(2, full!.Count);
        }

        [Fact]
        public async Task Retries_On429()
        {
            var handler = new FakeHttpHandler((_, n) => n <= 2
                ? FakeHttpHandler.Json("{}", (HttpStatusCode)429)
                : FakeHttpHandler.Json(FakeHttpHandler.DetailJson(1)));
            var service = CreateService(handler);

            var pages = await service.GetDetailPagesAsync(Invoice("a"));

            Assert.NotNull(pages);
            Assert.Equal(3, handler.RequestCount);
        }

        [Fact]
        public async Task Concurrency_NeverExceedsLimit()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json(FakeHttpHandler.DetailJson(2)),
                TimeSpan.FromMilliseconds(50));
            var service = CreateService(handler);
            var invoices = Enumerable.Range(1, 10).Select(i => Invoice("id" + i, i)).ToList();
            int lastCompleted = 0;

            var results = await service.PrefetchAsync(invoices, n => InterlockedMax(ref lastCompleted, n));

            Assert.Equal(10, handler.RequestCount);
            Assert.True(handler.MaxConcurrent <= InvoiceDetailService.MaxConcurrency,
                $"Đồng thời tối đa {handler.MaxConcurrent}");
            Assert.Equal(10, Volatile.Read(ref lastCompleted));
            Assert.Equal(10, results.Count);
            Assert.All(results, r => Assert.NotNull(r));
        }

        [Fact]
        public async Task Cancel_DoesNotCachePartialResult()
        {
            using var cts = new CancellationTokenSource();
            // Trang 1 đủ 50 SP, hủy ngay sau khi trang 1 trả về (trước khi xin trang 2)
            var handler = new FakeHttpHandler((req, _) =>
            {
                if (req.RequestUri!.Query.Contains("page=0")) cts.Cancel();
                return FakeHttpHandler.Json(FakeHttpHandler.DetailJson(req.RequestUri!.Query.Contains("page=0") ? 50 : 3));
            });
            var service = CreateService(handler);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetDetailPagesAsync(Invoice("a"), cts.Token));
            int requestsBeforeRetry = handler.RequestCount;

            var after = await service.GetDetailPagesAsync(Invoice("a"));

            Assert.True(handler.RequestCount > requestsBeforeRetry); // không lấy từ cache
            Assert.NotNull(after);
            Assert.Equal(2, after!.Count);
        }

        [Fact]
        public async Task Prefetch_Cancelled_ReturnsNullsAndStopsRequests()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json(FakeHttpHandler.DetailJson(1)));
            var service = CreateService(handler);
            var invoices = Enumerable.Range(1, 5).Select(i => Invoice("id" + i, i)).ToList();

            var results = await service.PrefetchAsync(invoices, null, cts.Token);

            Assert.Equal(5, results.Count);
            Assert.All(results, r => Assert.Null(r));
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public async Task HttpTimeout_IsNotCancel_ReturnsNull()
        {
            // TaskCanceledException do timeout của HttpClient (token của người dùng chưa hủy) vẫn trả null như cũ
            var handler = new FakeHttpHandler((_, _) => throw new TaskCanceledException("timeout"));
            var service = CreateService(handler);

            var pages = await service.GetDetailPagesAsync(Invoice("a"), CancellationToken.None);

            Assert.Null(pages);
        }

        private static void InterlockedMax(ref int target, int value)
        {
            int seen;
            while (value > (seen = Volatile.Read(ref target)) &&
                   Interlocked.CompareExchange(ref target, value, seen) != seen) { }
        }
    }
}
