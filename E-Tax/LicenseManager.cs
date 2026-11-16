using Microsoft.Win32;
using System.Globalization;

namespace E_Tax
{
    /// <summary>
    /// Quản lý bản quyền: 30 ngày dùng thử, kích hoạt bằng key ký số gắn mã máy (xem <see cref="LicenseKey"/>).
    /// Lưu trạng thái trong HKCU\Software\ETaxDemoApp.
    /// </summary>
    public static class LicenseManager
    {
        private const string RegistryPath = @"Software\ETaxDemoApp";
        private const string ExpiryValueName = "TrialExpiryDate";
        // Cách kích hoạt cũ (mã master, đã bỏ): máy đã có IsActivated=true vẫn được giữ là Activated
        private const string ActivatedValueName = "IsActivated";
        private const string LicenseKeyValueName = "LicenseKey";

        // KEY DEV — chủ dự án PHẢI thay bằng public key thật: chạy `E-Tax.KeyGen init <privateKeyPath>`
        // (giữ private key ngoài repo) rồi dán public key in ra vào đây. Key cấp bằng khóa dev sẽ mất hiệu lực.
        internal const string PublicKeyPem = """
            -----BEGIN PUBLIC KEY-----
            MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAErgV5CtK7v1bmJNteeONwAe557rH5
            JvS9g72Mm9BOqeapMANT5/cNGBOldgg+uQJXJnjTccqBAUR4R7rM8aIE/w==
            -----END PUBLIC KEY-----
            """;

        public static LicenseStatus CheckLicense()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        // Ưu tiên trạng thái đã kích hoạt, sau đó mới xét hạn dùng thử
                        if (key.GetValue(ActivatedValueName) as string == "true")
                        {
                            return LicenseStatus.Activated;
                        }

                        // Key ký số được xác thực lại mỗi lần mở app
                        if (key.GetValue(LicenseKeyValueName) is string storedKey
                            && Verify(storedKey) == LicenseKeyResult.Valid)
                        {
                            return LicenseStatus.Activated;
                        }

                        string? expiryDateStr = key.GetValue(ExpiryValueName) as string;
                        if (DateTime.TryParseExact(expiryDateStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime expiryDate))
                        {
                            return DateTime.UtcNow > expiryDate ? LicenseStatus.Expired : LicenseStatus.ValidTrial;
                        }
                    }
                }

                // Lần chạy đầu tiên: bắt đầu thời gian dùng thử
                InitializeTrial();
                return LicenseStatus.ValidTrial;
            }
            catch (Exception)
            {
                // Lỗi đọc registry: coi như hết hạn để đảm bảo an toàn
                return LicenseStatus.Expired;
            }
        }

        /// <summary>Xác thực key; nếu hợp lệ thì lưu vào registry (giá trị <c>LicenseKey</c>).</summary>
        public static LicenseKeyResult Activate(string userProvidedKey)
        {
            // Bỏ khoảng trắng/xuống dòng lẫn vào khi dán key
            string normalized = string.Concat((userProvidedKey ?? "").Where(c => !char.IsWhiteSpace(c)));

            LicenseKeyResult result = Verify(normalized);
            if (result != LicenseKeyResult.Valid)
                return result;

            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                key.SetValue(LicenseKeyValueName, normalized);
                return LicenseKeyResult.Valid;
            }
            catch (Exception)
            {
                return LicenseKeyResult.Invalid;
            }
        }

        private static LicenseKeyResult Verify(string licenseKey)
        {
            return LicenseKey.Verify(licenseKey, MachineId.Get(), DateTime.UtcNow, PublicKeyPem);
        }

        private static void InitializeTrial()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                DateTime expiryDate = DateTime.UtcNow.AddDays(30);
                // Định dạng "o" (round-trip) để không phụ thuộc culture
                key.SetValue(ExpiryValueName, expiryDate.ToString("o", CultureInfo.InvariantCulture));
                key.SetValue(ActivatedValueName, "false");
            }
            catch (Exception)
            {
            }
        }
    }

    public enum LicenseStatus
    {
        Activated,
        ValidTrial,
        Expired
    }
}
