using System.Text.Json;

namespace E_Tax
{
    internal enum RiskStatus { Safe, Risky, Unknown }

    internal sealed record RiskResult(string TaxCode, RiskStatus Status, string? CompanyName);

    /// <summary>
    /// Kiểm tra doanh nghiệp có trong danh sách cảnh báo rủi ro hay không. Chỉ "Safe" khi phản hồi
    /// khẳng định rõ ràng (false/0); mọi trường hợp lỗi hoặc không hiểu phản hồi là "Unknown",
    /// không bao giờ coi là an toàn. Không ghi URL (chứa token), nội dung phản hồi hay thông điệp lỗi HTTP vào log.
    /// </summary>
    internal sealed class RiskCheckService
    {
        public const int MaxConcurrency = 3;
        private const string BlacklistApiUrl = "https://license.metaecom.net/api/blacklist/check";

        private readonly HttpClient _client;
        private readonly string _token;
        private readonly Action<string> _log;

        public RiskCheckService(HttpClient client, string token, Action<string> log)
        {
            _client = client;
            _token = token;
            _log = log;
        }

        /// <summary>Nhãn hiển thị dùng chung cho lưới và Excel.</summary>
        public static string Label(RiskStatus status) => status switch
        {
            RiskStatus.Risky => "Rủi ro",
            RiskStatus.Safe => "An toàn",
            _ => "Không kiểm tra được"
        };

        public async Task<RiskResult> CheckAsync(string taxCode, CancellationToken ct = default)
        {
            taxCode = (taxCode ?? "").Trim();
            try
            {
                string url = $"{BlacklistApiUrl}?tax_code={Uri.EscapeDataString(taxCode)}&token={Uri.EscapeDataString(_token)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd(GdtApi.BrowserUserAgent);
                using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _log($"⚠️ Kiểm tra rủi ro MST {taxCode}: máy chủ trả mã {(int)response.StatusCode}.");
                    return Unknown(taxCode, null);
                }

                string body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var result = Parse(taxCode, body, out string? reason);
                if (reason != null) _log($"⚠️ Kiểm tra rủi ro MST {taxCode}: {reason}.");
                return result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                _log($"⚠️ Kiểm tra rủi ro MST {taxCode}: hết thời gian chờ.");
                return Unknown(taxCode, null);
            }
            catch (Exception ex)
            {
                // Chỉ ghi loại lỗi, không ghi ex.Message (có thể chứa URL/token)
                _log($"⚠️ Kiểm tra rủi ro MST {taxCode}: lỗi {ex.GetType().Name}.");
                return Unknown(taxCode, null);
            }
        }

        public async Task<IReadOnlyDictionary<string, RiskResult>> CheckManyAsync(
            IEnumerable<string> taxCodes, Action<int>? onCompleted = null, CancellationToken ct = default)
        {
            var codes = taxCodes
                .Select(c => (c ?? "").Trim())
                .Where(c => c.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var results = new Dictionary<string, RiskResult>(StringComparer.Ordinal);
            using var gate = new SemaphoreSlim(MaxConcurrency);
            int completed = 0;

            var tasks = codes.Select(async code =>
            {
                await gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var r = await CheckAsync(code, ct).ConfigureAwait(false);
                    lock (results) results[code] = r;
                }
                finally
                {
                    gate.Release();
                }
                onCompleted?.Invoke(Interlocked.Increment(ref completed));
            }).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return results;
        }

        private static RiskResult Unknown(string taxCode, string? name) => new RiskResult(taxCode, RiskStatus.Unknown, name);

        private static RiskResult Parse(string taxCode, string body, out string? reason)
        {
            reason = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    reason = "phản hồi không phải đối tượng JSON";
                    return Unknown(taxCode, null);
                }

                if (root.TryGetProperty("success", out var successEl) && successEl.ValueKind == JsonValueKind.False)
                {
                    reason = "API báo không thành công";
                    return Unknown(taxCode, null);
                }
                if (root.TryGetProperty("error", out var errorEl) && errorEl.ValueKind == JsonValueKind.String)
                {
                    reason = "API báo lỗi";
                    return Unknown(taxCode, null);
                }

                // "blacklisted" có thể nằm trong "data" hoặc ở gốc
                string? name = null;
                JsonElement flag = default;
                bool found = false;
                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
                {
                    if (dataEl.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                        name = nameEl.GetString();
                    found = dataEl.TryGetProperty("blacklisted", out flag);
                }
                if (!found) found = root.TryGetProperty("blacklisted", out flag);

                if (!found)
                {
                    reason = "phản hồi không có trường blacklisted";
                    return Unknown(taxCode, name);
                }

                var status = MapFlag(flag);
                if (status == RiskStatus.Unknown) reason = "giá trị blacklisted không nhận diện được";
                return new RiskResult(taxCode, status, name);
            }
            catch (JsonException)
            {
                reason = "phản hồi không phải JSON hợp lệ";
                return Unknown(taxCode, null);
            }
        }

        private static RiskStatus MapFlag(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.True:
                    return RiskStatus.Risky;
                case JsonValueKind.False:
                    return RiskStatus.Safe;
                case JsonValueKind.Number:
                    if (el.TryGetDecimal(out var n))
                    {
                        if (n == 1m) return RiskStatus.Risky;
                        if (n == 0m) return RiskStatus.Safe;
                    }
                    return RiskStatus.Unknown;
                case JsonValueKind.String:
                    var s = el.GetString()?.Trim();
                    if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || s == "1") return RiskStatus.Risky;
                    if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) || s == "0") return RiskStatus.Safe;
                    return RiskStatus.Unknown;
                default:
                    return RiskStatus.Unknown;
            }
        }
    }
}
