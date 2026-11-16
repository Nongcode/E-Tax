using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace E_Tax
{
    public enum LicenseKeyResult
    {
        Valid,
        Invalid,
        WrongMachine,
        Expired
    }

    /// <summary>
    /// Key kích hoạt ký số: <c>Base64Url(payloadJson).Base64Url(chữ ký)</c>.
    /// Payload <c>{"machine":"…","customer":"…","expires":"yyyy-MM-dd"|null}</c>, ký ECDSA P-256/SHA-256.
    /// File này được dùng chung (link) với tool <c>tools/E-Tax.KeyGen</c> để ký và xác thực luôn khớp nhau.
    /// </summary>
    public static class LicenseKey
    {
        private const string DateFormat = "yyyy-MM-dd";

        private sealed class Payload
        {
            [JsonPropertyName("machine")]
            public string? Machine { get; set; }

            [JsonPropertyName("customer")]
            public string? Customer { get; set; }

            [JsonPropertyName("expires")]
            public string? Expires { get; set; }
        }

        /// <summary>Tạo key (dùng trong KeyGen). <paramref name="expires"/> = null là vĩnh viễn.</summary>
        public static string Create(string machineId, string customer, DateTime? expires, ECDsa privateKey)
        {
            var payload = new Payload
            {
                Machine = machineId.Trim().ToUpperInvariant(),
                Customer = customer,
                Expires = expires?.ToString(DateFormat, CultureInfo.InvariantCulture)
            };
            byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            byte[] signature = privateKey.SignData(payloadBytes, HashAlgorithmName.SHA256);
            return Base64UrlEncode(payloadBytes) + "." + Base64UrlEncode(signature);
        }

        /// <summary>
        /// Xác thực key. Không bao giờ ném exception: dữ liệu rác trả về <see cref="LicenseKeyResult.Invalid"/>.
        /// Key có hạn còn hiệu lực đến hết ngày <c>expires</c> (UTC).
        /// </summary>
        public static LicenseKeyResult Verify(string key, string machineId, DateTime nowUtc, string publicKeyPem)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return LicenseKeyResult.Invalid;

                string[] parts = key.Trim().Split('.');
                if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                    return LicenseKeyResult.Invalid;

                byte[]? payloadBytes = Base64UrlDecode(parts[0]);
                byte[]? signature = Base64UrlDecode(parts[1]);
                if (payloadBytes == null || signature == null)
                    return LicenseKeyResult.Invalid;

                using (ECDsa ecdsa = ECDsa.Create())
                {
                    ecdsa.ImportFromPem(publicKeyPem);
                    if (!ecdsa.VerifyData(payloadBytes, signature, HashAlgorithmName.SHA256))
                        return LicenseKeyResult.Invalid;
                }

                Payload? payload = JsonSerializer.Deserialize<Payload>(payloadBytes);
                if (payload == null || string.IsNullOrWhiteSpace(payload.Machine))
                    return LicenseKeyResult.Invalid;

                DateTime? expires = null;
                if (payload.Expires != null)
                {
                    if (!DateTime.TryParseExact(payload.Expires, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                        return LicenseKeyResult.Invalid;
                    expires = d.Date;
                }

                if (!string.Equals(payload.Machine.Trim(), (machineId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                    return LicenseKeyResult.WrongMachine;

                if (expires.HasValue && nowUtc.Date > expires.Value)
                    return LicenseKeyResult.Expired;

                return LicenseKeyResult.Valid;
            }
            catch (Exception)
            {
                return LicenseKeyResult.Invalid;
            }
        }

        internal static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        internal static byte[]? Base64UrlDecode(string text)
        {
            string s = text.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
                case 1: return null;
            }
            byte[] buffer = new byte[s.Length * 3 / 4];
            return Convert.TryFromBase64String(s, buffer, out int written) ? buffer[..written] : null;
        }
    }
}
