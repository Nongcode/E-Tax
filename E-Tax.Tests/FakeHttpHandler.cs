using System.Net;
using System.Text;

namespace E_Tax.Tests
{
    /// <summary>
    /// HttpMessageHandler giả: đếm request, ghi số request đồng thời tối đa, trả response theo hàm responder.
    /// </summary>
    internal sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _responder;
        private readonly TimeSpan _delay;
        private int _requestCount;
        private int _current;
        private int _maxConcurrent;

        /// <param name="responder">(request, số thứ tự request bắt đầu từ 1) → response</param>
        public FakeHttpHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responder, TimeSpan? delay = null)
        {
            _responder = responder;
            _delay = delay ?? TimeSpan.Zero;
        }

        public int RequestCount => Volatile.Read(ref _requestCount);
        public int MaxConcurrent => Volatile.Read(ref _maxConcurrent);
        public List<string> Urls { get; } = new List<string>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int n = Interlocked.Increment(ref _requestCount);
            lock (Urls) Urls.Add(request.RequestUri!.ToString());

            int now = Interlocked.Increment(ref _current);
            int seen;
            while (now > (seen = Volatile.Read(ref _maxConcurrent)) &&
                   Interlocked.CompareExchange(ref _maxConcurrent, now, seen) != seen) { }
            try
            {
                if (_delay > TimeSpan.Zero) await Task.Delay(_delay, cancellationToken);
                return _responder(request, n);
            }
            finally
            {
                Interlocked.Decrement(ref _current);
            }
        }

        public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
            new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        /// <summary>JSON chi tiết HĐ có <paramref name="productCount"/> sản phẩm trong "hdhhdvu".</summary>
        public static string DetailJson(int productCount)
        {
            var products = Enumerable.Range(1, productCount).Select(i => $"{{\"ten\":\"SP{i}\",\"thtien\":1000}}");
            return $"{{\"mhdon\":\"MA1\",\"hdhhdvu\":[{string.Join(",", products)}]}}";
        }
    }
}
