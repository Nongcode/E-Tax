using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace E_Tax.KeyGen
{
    /// <summary>
    /// Tool cấp key kích hoạt E-Tax. Giữ file private key NGOÀI repo, không commit.
    ///   E-Tax.KeyGen init  &lt;privateKeyPath&gt;
    ///   E-Tax.KeyGen issue &lt;privateKeyPath&gt; &lt;machineId&gt; &lt;customer&gt; [yyyy-MM-dd]
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                if (args.Length == 2 && args[0] == "init")
                    return Init(args[1]);
                if ((args.Length == 4 || args.Length == 5) && args[0] == "issue")
                    return Issue(args[1], args[2], args[3], args.Length == 5 ? args[4] : null);

                PrintUsage();
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Lỗi: " + ex.Message);
                return 1;
            }
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine("Cách dùng:");
            Console.Error.WriteLine("  E-Tax.KeyGen init  <privateKeyPath>");
            Console.Error.WriteLine("  E-Tax.KeyGen issue <privateKeyPath> <machineId> <customer> [yyyy-MM-dd]");
        }

        private static int Init(string privateKeyPath)
        {
            if (File.Exists(privateKeyPath))
            {
                Console.Error.WriteLine($"File đã tồn tại, không ghi đè: {privateKeyPath}");
                return 1;
            }

            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            File.WriteAllText(privateKeyPath, ecdsa.ExportPkcs8PrivateKeyPem());

            Console.WriteLine($"Đã lưu private key: {Path.GetFullPath(privateKeyPath)}");
            Console.WriteLine("GIỮ FILE NÀY BÍ MẬT, NGOÀI REPO. Mất file = không cấp được key mới cho public key này.");
            Console.WriteLine();
            Console.WriteLine("Dán public key sau vào hằng PublicKeyPem trong E-Tax/LicenseManager.cs:");
            Console.WriteLine(ecdsa.ExportSubjectPublicKeyInfoPem());
            return 0;
        }

        private static int Issue(string privateKeyPath, string machineId, string customer, string? expiresText)
        {
            DateTime? expires = null;
            if (expiresText != null)
            {
                if (!DateTime.TryParseExact(expiresText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                {
                    Console.Error.WriteLine($"Ngày hết hạn không đúng định dạng yyyy-MM-dd: {expiresText}");
                    return 1;
                }
                expires = d;
            }

            machineId = machineId.Trim();
            if (machineId.Length == 0)
            {
                Console.Error.WriteLine("Thiếu mã máy.");
                return 1;
            }

            using ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(File.ReadAllText(privateKeyPath));

            string key = LicenseKey.Create(machineId, customer, expires, ecdsa);

            // Tự kiểm tra lại bằng đúng hàm Verify của app
            LicenseKeyResult check = LicenseKey.Verify(key, machineId, DateTime.UtcNow, ecdsa.ExportSubjectPublicKeyInfoPem());
            if (check != LicenseKeyResult.Valid && !(check == LicenseKeyResult.Expired && expires.HasValue))
            {
                Console.Error.WriteLine($"Key vừa tạo không xác thực được ({check}).");
                return 1;
            }
            if (check == LicenseKeyResult.Expired)
                Console.Error.WriteLine("Cảnh báo: ngày hết hạn đã qua, key sẽ bị từ chối.");

            Console.WriteLine(key);
            return 0;
        }
    }
}
