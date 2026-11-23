using System.Text.Json;

namespace E_Tax
{
    internal sealed record UpdateInfo(Version Version, string DownloadUrl, string? Notes);

    /// <summary>
    /// Kiểm tra bản cập nhật qua manifest JSON: { "version": "1.0.1", "downloadUrl": "https://...", "notes": "..." }.
    /// Không bao giờ ném exception và không log URL/nội dung phản hồi.
    /// </summary>
    internal static class UpdateChecker
    {
        /// <summary>Rỗng = không kiểm tra cập nhật, không gọi mạng. Điền URL manifest (https) khi có nơi phát hành.</summary>
        public const string ManifestUrl = "";

        private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);

        public static UpdateInfo? Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                string? versionText = GetString(root, "version");
                string? url = GetString(root, "downloadUrl");
                if (versionText == null || url == null) return null;
                if (!Version.TryParse(versionText, out Version? version)) return null;

                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
                    return null;

                return new UpdateInfo(version, url, GetString(root, "notes"));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static bool IsNewer(UpdateInfo info, Version current) => info.Version > current;

        /// <summary>Trả thông tin khi có bản mới hơn; null khi không có, hết thời gian, bị hủy hoặc lỗi mạng.</summary>
        public static async Task<UpdateInfo?> CheckAsync(HttpClient http, string manifestUrl, Version current, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(manifestUrl)) return null;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(CheckTimeout);

                using var response = await http.GetAsync(manifestUrl, cts.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                var info = Parse(json);
                return info != null && IsNewer(info, current) ? info : null;
            }
            catch (Exception)
            {
                // Mạng lỗi, hết giờ hoặc bị hủy: bỏ qua, không chặn app
                return null;
            }
        }

        private static string? GetString(JsonElement root, string name) =>
            root.TryGetProperty(name, out JsonElement el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }
}
