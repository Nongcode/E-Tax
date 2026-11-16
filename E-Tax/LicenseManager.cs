using Microsoft.Win32;
using System.Globalization;

namespace E_Tax
{
    /// <summary>
    /// Quản lý bản quyền: 30 ngày dùng thử, lưu trạng thái trong HKCU\Software\ETaxDemoApp.
    /// </summary>
    public static class LicenseManager
    {
        private const string RegistryPath = @"Software\ETaxDemoApp";
        private const string ExpiryValueName = "TrialExpiryDate";
        private const string ActivatedValueName = "IsActivated";

        private const string MasterActivationKey = "PDL@10_09_2004-cqnyc";

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

        public static bool Activate(string userProvidedKey)
        {
            if (userProvidedKey != MasterActivationKey)
                return false;

            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                key.SetValue(ActivatedValueName, "true");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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
