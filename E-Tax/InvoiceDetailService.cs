using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace E_Tax
{
    /// <summary>
    /// Lấy JSON chi tiết hóa đơn (query/invoices/detail) theo từng trang sản phẩm, cache theo Id,
    /// gọi song song tối đa MaxConcurrency request và tự thử lại khi 429/5xx/lỗi kết nối.
    /// </summary>
    internal sealed class InvoiceDetailService
    {
        public const int ProductPageSize = 50;
        public const int MaxConcurrency = 3;

        private static readonly TimeSpan[] DefaultRetryDelays = { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };

        private readonly HttpClient _client;
        private readonly Func<string> _getJwtToken;
        private readonly Action<string> _log;
        private readonly TimeSpan[] _retryDelays;
        // Giới hạn số request đồng thời tới cổng thuế (dùng chung cho mọi lời gọi)
        private readonly SemaphoreSlim _throttle = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        // Chỉ lưu kết quả thành công đầy đủ
        private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _cache = new ConcurrentDictionary<string, IReadOnlyList<string>>();

        public InvoiceDetailService(HttpClient client, Func<string> getJwtToken, Action<string> log, TimeSpan[]? retryDelays = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _getJwtToken = getJwtToken ?? throw new ArgumentNullException(nameof(getJwtToken));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _retryDelays = retryDelays ?? DefaultRetryDelays;
        }

        public void ClearCache() => _cache.Clear();

        /// <summary>
        /// JSON của từng trang sản phẩm (trang đầu luôn có, kể cả khi HĐ không có hàng hóa).
        /// Trả về null nếu thiếu khóa, chưa đăng nhập hoặc có trang lỗi (không trả dữ liệu thiếu trang). Kết quả lỗi không được cache.
        /// </summary>
        public async Task<IReadOnlyList<string>?> GetDetailPagesAsync(SearchResult invoice)
        {
            if (invoice == null || !invoice.HasLookupKeys()) return null;

            bool cacheable = !string.IsNullOrEmpty(invoice.Id);
            if (cacheable && _cache.TryGetValue(invoice.Id, out var cached)) return cached;

            if (string.IsNullOrEmpty(_getJwtToken()))
            {
                _log("❌ (Chi tiết HĐ) Chưa đăng nhập hoặc token không hợp lệ.");
                return null;
            }

            var pages = new List<string>();
            bool complete = false;
            for (int page = 0; ; page++)
            {
                string? json = await FetchPageAsync(invoice, page).ConfigureAwait(false);
                if (json == null) break;

                int productCount;
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var products = InvoiceJson.FindProductArray(InvoiceJson.GetDataElement(doc.RootElement));
                    productCount = products?.GetArrayLength() ?? 0;
                }
                catch (JsonException ex)
                {
                    _log($"❌ (Chi tiết HĐ) Lỗi parse JSON HĐ {invoice.So_hoa_don} (trang {page + 1}): {ex.Message}");
                    break;
                }

                // Trang đầu luôn giữ (Tra cứu cần thông tin chung); trang sau rỗng thì bỏ
                if (page == 0 || productCount > 0) pages.Add(json);
                if (productCount < ProductPageSize)
                {
                    complete = true;
                    break;
                }
            }

            if (!complete) return null;
            if (cacheable) _cache[invoice.Id] = pages;
            return pages;
        }

        /// <summary>
        /// Tải chi tiết cho cả danh sách (song song có giới hạn). Kết quả trả theo đúng thứ tự danh sách.
        /// onCompleted(n) báo số HĐ đã xong, chạy trên SynchronizationContext của nơi gọi (UI thread).
        /// </summary>
        public async Task<IReadOnlyList<IReadOnlyList<string>?>> PrefetchAsync(IReadOnlyList<SearchResult> invoices, Action<int>? onCompleted = null)
        {
            var context = SynchronizationContext.Current;
            int completed = 0;

            void Report()
            {
                int n = Interlocked.Increment(ref completed);
                if (onCompleted == null) return;
                if (context != null) context.Post(_ => onCompleted(n), null);
                else onCompleted(n);
            }

            async Task<IReadOnlyList<string>?> RunOne(SearchResult invoice)
            {
                try
                {
                    return await GetDetailPagesAsync(invoice).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log($"❌ (Chi tiết HĐ) Lỗi khác khi xử lý HĐ {invoice?.So_hoa_don}: {ex.Message}");
                    return null;
                }
                finally
                {
                    Report();
                }
            }

            var tasks = new Task<IReadOnlyList<string>?>[invoices.Count];
            for (int i = 0; i < invoices.Count; i++) tasks[i] = RunOne(invoices[i]);
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task<string?> FetchPageAsync(SearchResult invoice, int page)
        {
            string url = GdtApi.BuildDetailUrl(invoice.Ma_so_thue, invoice.Ky_hieu_hoa_don,
                invoice.So_hoa_don!.Value, invoice.Ky_hieu_ma_so!.Value, page, ProductPageSize);
            string label = $"HĐ {invoice.Ky_hieu_hoa_don}-{invoice.So_hoa_don} (trang {page + 1})";

            for (int attempt = 0; ; attempt++)
            {
                bool canRetry = attempt < _retryDelays.Length;
                await _throttle.WaitAsync().ConfigureAwait(false);
                try
                {
                    using var req = GdtApi.CreateRequest(HttpMethod.Get, url, "application/json", _getJwtToken());
                    using var response = await _client.SendAsync(req).ConfigureAwait(false);
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (response.IsSuccessStatusCode) return body;

                    int status = (int)response.StatusCode;
                    bool transient = status == (int)HttpStatusCode.TooManyRequests || status >= 500;
                    if (!transient || !canRetry)
                    {
                        _log($"❌ (Chi tiết HĐ) Lỗi API {label}: {status} {response.StatusCode} - {Truncate(body)}");
                        return null;
                    }
                    _log($"⚠️ (Chi tiết HĐ) {label} trả {status}, thử lại lần {attempt + 1}...");
                }
                catch (HttpRequestException ex) when (canRetry)
                {
                    _log($"⚠️ (Chi tiết HĐ) Lỗi kết nối {label}: {ex.Message}, thử lại lần {attempt + 1}...");
                }
                catch (HttpRequestException ex)
                {
                    _log($"❌ (Chi tiết HĐ) Lỗi kết nối {label}: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    _log($"❌ (Chi tiết HĐ) Lỗi khác {label}: {ex.Message}");
                    return null;
                }
                finally
                {
                    _throttle.Release();
                }

                await Task.Delay(_retryDelays[attempt]).ConfigureAwait(false);
            }
        }

        private static string Truncate(string text) =>
            string.IsNullOrEmpty(text) || text.Length <= 300 ? text : text.Substring(0, 300) + "...";
    }
}
