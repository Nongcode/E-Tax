using Microsoft.Win32;
using System;
using System.Globalization;

public static class LicenseManager
{
    // Đường dẫn trong Registry để lưu thông tin. Thay "YourAppName" bằng tên ứng dụng của bạn để tránh trùng lặp.
    private const string RegistryPath = @"Software\ETaxDemoApp";
    private const string ExpiryValueName = "TrialExpiryDate";
    private const string ActivatedValueName = "IsActivated";

    private const string MasterActivationKey = "PDL@10_09_2004-cqnyc";

    /// <summary>
    /// Kiểm tra trạng thái bản quyền của ứng dụng.
    /// </summary>
    /// <returns>Trả về một trong ba trạng thái: Activated, ValidTrial, Expired</returns>
    public static LicenseStatus CheckLicense()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
            {
                // Nếu key tồn tại
                if (key != null)
                {
                    // 1. Ưu tiên kiểm tra xem đã kích hoạt chưa
                    string activatedValue = key.GetValue(ActivatedValueName) as string;
                    if (activatedValue == "true")
                    {
                        return LicenseStatus.Activated;
                    }

                    // 2. Nếu chưa kích hoạt, kiểm tra ngày hết hạn dùng thử
                    string expiryDateStr = key.GetValue(ExpiryValueName) as string;
                    if (DateTime.TryParseExact(expiryDateStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime expiryDate))
                    {
                        if (DateTime.UtcNow > expiryDate)
                        {
                            return LicenseStatus.Expired; // Hết hạn
                        }
                        else
                        {
                            return LicenseStatus.ValidTrial; // Còn hạn
                        }
                    }
                }
            }

            // Nếu key không tồn tại -> đây là lần chạy đầu tiên. Bắt đầu thời gian dùng thử.
            InitializeTrial();
            return LicenseStatus.ValidTrial;
        }
        catch (Exception)
        {
            // Nếu có lỗi đọc registry, coi như hết hạn để đảm bảo an toàn
            return LicenseStatus.Expired;
        }
    }

    /// <summary>
    /// Kích hoạt sản phẩm bằng mã bản quyền.
    /// </summary>
    /// <param name="userProvidedKey">Mã do người dùng nhập vào</param>
    /// <returns>True nếu mã hợp lệ</returns>
    public static bool Activate(string userProvidedKey)
    {
        if (userProvidedKey == MasterActivationKey)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    key.SetValue(ActivatedValueName, "true");
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Lấy số ngày dùng thử còn lại.
    /// </summary>
    public static int GetDaysRemaining()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
            {
                if (key != null)
                {
                    string expiryDateStr = key.GetValue(ExpiryValueName) as string;
                    if (DateTime.TryParseExact(expiryDateStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime expiryDate))
                    {
                        TimeSpan remaining = expiryDate - DateTime.UtcNow;
                        return Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));
                    }
                }
            }
        }
        catch { }
        return 0;
    }


    /// <summary>
    /// Thiết lập thời gian dùng thử cho lần chạy đầu tiên.
    /// </summary>
    private static void InitializeTrial()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                DateTime expiryDate = DateTime.UtcNow.AddDays(30);
                // Lưu ngày ở định dạng "o" (round-trip) để đảm bảo không bị lỗi văn hóa (culture)
                key.SetValue(ExpiryValueName, expiryDate.ToString("o", CultureInfo.InvariantCulture));
                key.SetValue(ActivatedValueName, "false");
            }
        }
        catch (Exception ex)
        {
            // Ghi lại lỗi nếu cần thiết
        }
    }
}

/// <summary>
/// Các trạng thái bản quyền có thể có.
/// </summary>
public enum LicenseStatus
{
    Activated,  // Đã kích hoạt bản quyền
    ValidTrial, // Đang trong thời gian dùng thử hợp lệ
    Expired     // Đã hết hạn dùng thử
}