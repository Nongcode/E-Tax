using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace E_Tax
{
    internal sealed record SavedSession(string Username, string Token, DateTime ExpiresUtc);

    /// <summary>Đọc hạn (claim exp) trong JWT mà không kiểm chữ ký; chỉ dùng để quyết định có khôi phục phiên hay không.</summary>
    internal static class JwtExpiry
    {
        public static DateTime? ReadUtc(string? token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            try
            {
                string b64 = parts[1].Replace('-', '+').Replace('_', '/');
                b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
                using var doc = JsonDocument.Parse(Convert.FromBase64String(b64));
                if (doc.RootElement.ValueKind != JsonValueKind.Object
                    || !doc.RootElement.TryGetProperty("exp", out var exp)
                    || !exp.TryGetInt64(out long seconds)) return null;
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }
            catch (Exception ex) when (ex is FormatException or JsonException or ArgumentOutOfRangeException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// "Duy trì đăng nhập": lưu tên đăng nhập + JWT, mã hóa DPAPI theo user Windows hiện tại, tại %LocalAppData%\E-Tax\session.dat.
    /// Không bao giờ lưu mật khẩu/captcha. Không gọi cổng thuế: hạn phiên đọc từ exp trong token.
    /// </summary>
    internal sealed class SessionStore
    {
        public static string DefaultPath { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "E-Tax", "session.dat");

        /// <summary>Còn ít hơn chừng này thì coi như hết hạn.</summary>
        public static readonly TimeSpan MinRemaining = TimeSpan.FromMinutes(5);

        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("E-Tax.session.v1");

        private sealed class Data
        {
            public string? Username { get; set; }
            public string? Token { get; set; }
        }

        private readonly string _path;
        private readonly Func<DateTime> _utcNow;

        public SessionStore(string path, Func<DateTime>? utcNow = null)
        {
            _path = path;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>
        /// Lưu phiên; false (và xóa phiên cũ) nếu token không có exp hoặc sắp hết hạn. Lỗi ghi file ném exception để nơi gọi quyết định.
        /// </summary>
        public bool Save(string username, string token)
        {
            if (!IsUsable(JwtExpiry.ReadUtc(token)))
            {
                Clear();
                return false;
            }
            byte[] plain = JsonSerializer.SerializeToUtf8Bytes(new Data { Username = username, Token = token });
            byte[] cipher = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string tmp = _path + ".tmp";
            File.WriteAllBytes(tmp, cipher);
            File.Move(tmp, _path, overwrite: true);
            return true;
        }

        /// <summary>Phiên còn hạn; null nếu không có. File hỏng, giải mã lỗi hoặc hết hạn thì xóa file. Không ném exception.</summary>
        public SavedSession? Load()
        {
            try
            {
                if (!File.Exists(_path)) return null;
                byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(_path), Entropy, DataProtectionScope.CurrentUser);
                var data = JsonSerializer.Deserialize<Data>(plain);
                var expires = JwtExpiry.ReadUtc(data?.Token);
                if (data?.Token is string token && !string.IsNullOrEmpty(data.Username) && IsUsable(expires))
                    return new SavedSession(data.Username!, token, expires!.Value);
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException or IOException or UnauthorizedAccessException)
            {
            }
            Clear();
            return null;
        }

        /// <summary>Xóa phiên đã lưu; không ném exception.</summary>
        public void Clear()
        {
            try { File.Delete(_path); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }

        private bool IsUsable(DateTime? expiresUtc) => expiresUtc is DateTime e && e - _utcNow() >= MinRemaining;
    }
}
