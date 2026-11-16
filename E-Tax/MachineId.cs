using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace E_Tax
{
    /// <summary>
    /// Mã máy dùng để gắn key kích hoạt: 16 ký tự hex đầu của SHA-256(MachineGuid của Windows).
    /// </summary>
    public static class MachineId
    {
        private static string? _cached;

        public static string Get()
        {
            return _cached ??= Compute();
        }

        private static string Compute()
        {
            string? guid = null;
            try
            {
                // Luôn đọc view 64-bit để process 32/64-bit ra cùng một mã
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                guid = key?.GetValue("MachineGuid") as string;
            }
            catch (Exception)
            {
            }

            // Không đọc được MachineGuid (hiếm gặp): dùng tên máy để vẫn có mã ổn định
            string source = string.IsNullOrWhiteSpace(guid) ? Environment.MachineName : guid;
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source.Trim().ToLowerInvariant()));
            return Convert.ToHexString(hash).Substring(0, 16);
        }
    }
}
