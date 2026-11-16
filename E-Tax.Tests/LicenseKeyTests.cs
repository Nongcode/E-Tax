using System.Security.Cryptography;
using System.Text;

namespace E_Tax.Tests
{
    public class LicenseKeyTests : IDisposable
    {
        private const string Machine = "0123456789ABCDEF";
        private static readonly DateTime Now = new DateTime(2026, 11, 16, 8, 0, 0, DateTimeKind.Utc);

        private readonly ECDsa _privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        private readonly string _publicKeyPem;

        public LicenseKeyTests()
        {
            _publicKeyPem = _privateKey.ExportSubjectPublicKeyInfoPem();
        }

        public void Dispose() => _privateKey.Dispose();

        [Fact]
        public void Verify_ValidKey()
        {
            string key = LicenseKey.Create(Machine, "Công ty ABC", null, _privateKey);

            Assert.Equal(LicenseKeyResult.Valid, LicenseKey.Verify(key, Machine, Now, _publicKeyPem));
            // So sánh mã máy không phân biệt hoa/thường
            Assert.Equal(LicenseKeyResult.Valid, LicenseKey.Verify(key, Machine.ToLowerInvariant(), Now, _publicKeyPem));
        }

        [Fact]
        public void Verify_TamperedPayload()
        {
            string key = LicenseKey.Create(Machine, "Công ty ABC", new DateTime(2026, 12, 31), _privateKey);
            string signature = key.Split('.')[1];

            // Payload bị sửa (gia hạn), giữ nguyên chữ ký cũ
            string forgedJson = "{\"machine\":\"" + Machine + "\",\"customer\":\"Công ty ABC\",\"expires\":null}";
            string forged = LicenseKey.Base64UrlEncode(Encoding.UTF8.GetBytes(forgedJson)) + "." + signature;
            Assert.Equal(LicenseKeyResult.Invalid, LicenseKey.Verify(forged, Machine, Now, _publicKeyPem));

            // Sửa 1 ký tự giữa chữ ký
            int mid = key.Length - signature.Length / 2;
            char flipped = key[mid] == 'A' ? 'B' : 'A';
            string tampered = key.Substring(0, mid) + flipped + key.Substring(mid + 1);
            Assert.Equal(LicenseKeyResult.Invalid, LicenseKey.Verify(tampered, Machine, Now, _publicKeyPem));

            // Key ký bằng khóa khác
            using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            string foreign = LicenseKey.Create(Machine, "Công ty ABC", null, otherKey);
            Assert.Equal(LicenseKeyResult.Invalid, LicenseKey.Verify(foreign, Machine, Now, _publicKeyPem));
        }

        [Fact]
        public void Verify_OtherMachine()
        {
            string key = LicenseKey.Create(Machine, "Công ty ABC", null, _privateKey);

            Assert.Equal(LicenseKeyResult.WrongMachine, LicenseKey.Verify(key, "FEDCBA9876543210", Now, _publicKeyPem));
        }

        [Fact]
        public void Verify_Expired()
        {
            string key = LicenseKey.Create(Machine, "Công ty ABC", new DateTime(2026, 11, 15), _privateKey);

            Assert.Equal(LicenseKeyResult.Expired, LicenseKey.Verify(key, Machine, Now, _publicKeyPem));
            // Còn hiệu lực đến hết ngày hết hạn (UTC)
            Assert.Equal(LicenseKeyResult.Valid, LicenseKey.Verify(key, Machine, new DateTime(2026, 11, 15, 23, 59, 59, DateTimeKind.Utc), _publicKeyPem));
            Assert.Equal(LicenseKeyResult.Expired, LicenseKey.Verify(key, Machine, new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc), _publicKeyPem));
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(".")]
        [InlineData("a.b.c")]
        [InlineData("!!!.@@@")]
        [InlineData("bm90IGpzb24.AAAA")]
        public void Verify_Garbage(string key)
        {
            Assert.Equal(LicenseKeyResult.Invalid, LicenseKey.Verify(key, Machine, Now, _publicKeyPem));
        }

        [Fact]
        public void EmbeddedPublicKey_IsValidP256Pem()
        {
            using ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(LicenseManager.PublicKeyPem);
            Assert.Equal(256, ecdsa.KeySize);
        }

        [Fact]
        public void MachineId_Is16HexChars()
        {
            string id = MachineId.Get();
            Assert.Matches("^[0-9A-F]{16}$", id);
            Assert.Equal(id, MachineId.Get());
        }

        [Fact]
        public void Verify_Garbage_NullAndBadPayload()
        {
            Assert.Equal(LicenseKeyResult.Invalid, LicenseKey.Verify(null!, Machine, Now, _publicKeyPem));
            Assert.Equal(LicenseKeyResult.Invalid, LicenseKey.Verify(LicenseKey.Create(Machine, "x", null, _privateKey), Machine, Now, "not a pem"));

            // Chữ ký đúng nhưng payload không hợp lệ (JSON hỏng / thiếu machine / ngày sai định dạng)
            foreach (string json in new[] { "not json", "{}", "{\"machine\":\"" + Machine + "\",\"customer\":\"x\",\"expires\":\"31/12/2026\"}" })
            {
                byte[] payload = Encoding.UTF8.GetBytes(json);
                byte[] sig = _privateKey.SignData(payload, HashAlgorithmName.SHA256);
                string key = LicenseKey.Base64UrlEncode(payload) + "." + LicenseKey.Base64UrlEncode(sig);
                Assert.Equal(LicenseKeyResult.Invalid, LicenseKey.Verify(key, Machine, Now, _publicKeyPem));
            }
        }
    }
}
