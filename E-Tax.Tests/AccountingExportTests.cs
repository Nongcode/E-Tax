using ClosedXML.Excel;
using E_Tax;

namespace E_Tax.Tests
{
    public class AccountingExportTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-acc-" + Guid.NewGuid());

        public AccountingExportTests()
        {
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private string NewFile(string name) => Path.Combine(_dir, name);

        private void WriteProfile(string file, string json) => File.WriteAllText(NewFile(file), json);

        private static InvoiceDetailItem Item(string name = "Bút bi", decimal? qty = 2m) => new()
        {
            STT = 1, KyHieuHD = "C24TAA", SoHD = 12, NgayHD = "01/10/2024", TenSanPham = name,
            SoLuong = qty, DonGia = 1500.5m, ThanhTien = 3001m, InvoiceKey = "SECRET-KEY"
        };

        [Fact]
        public void BuiltIn_HasFiveUnverifiedProfiles()
        {
            var all = AccountingProfiles.BuiltIn();
            Assert.Equal(new[] { "MISA", "1C", "Bravo", "Fast", "Vacom" }, all.Select(p => p.Name).ToArray());
            Assert.All(all, p => Assert.False(p.Verified));
            Assert.All(all, p => Assert.NotEmpty(p.Columns));
        }

        [Fact]
        public void Export_ColumnsFollowProfileOrder()
        {
            var p = new AccountingProfile("T", true, new[]
            {
                new AccountingColumn("Tên hàng", "TenSanPham", null),
                new AccountingColumn("SL", "SoLuong", null),
                new AccountingColumn("Số HĐ", "SoHD", null),
            });
            var f = NewFile("a.xlsx");
            AccountingExporter.Export(p, new[] { Item() }, f);

            using var wb = new XLWorkbook(f);
            var ws = wb.Worksheet(1);
            Assert.Equal("Tên hàng", ws.Cell(1, 1).GetString());
            Assert.Equal("SL", ws.Cell(1, 2).GetString());
            Assert.Equal("Số HĐ", ws.Cell(1, 3).GetString());
            Assert.Equal("Bút bi", ws.Cell(2, 1).GetString());
            Assert.Equal(XLDataType.Number, ws.Cell(2, 2).DataType);
            Assert.Equal(2d, ws.Cell(2, 2).GetDouble());
            Assert.Equal(12d, ws.Cell(2, 3).GetDouble());
        }

        [Fact]
        public void Load_RejectsUnknownFieldAndInternalKey()
        {
            WriteProfile("bad1.json", """{"name":"X1","columns":[{"header":"A","field":"NoSuchField"}]}""");
            WriteProfile("bad2.json", """{"name":"X2","columns":[{"header":"A","field":"InvoiceKey"}]}""");
            var all = AccountingProfiles.LoadAll(_dir);
            Assert.DoesNotContain(all, p => p.Name == "X1" || p.Name == "X2");
            Assert.Equal(5, all.Count);
        }

        [Fact]
        public void Load_RejectsNoColumnsEmptyHeaderDuplicateHeaders_KeepsBuiltIn()
        {
            WriteProfile("a.json", """{"name":"misa","columns":[]}""");
            WriteProfile("b.json", """{"name":"1C","columns":[{"header":" ","field":"SoHD"}]}""");
            WriteProfile("c.json", """{"name":"Fast","columns":[{"header":"A","field":"SoHD"},{"header":"A","field":"STT"}]}""");
            var all = AccountingProfiles.LoadAll(_dir);
            Assert.Equal(5, all.Count);
            Assert.All(all, p => Assert.False(p.Verified));
            Assert.True(all.Single(p => p.Name == "MISA").Columns.Count > 1);
        }

        [Fact]
        public void Load_UserProfileOverridesBuiltInByName_CaseInsensitive_HonoursVerified()
        {
            WriteProfile("misa.json", """
                {"name":"misa","verified":true,"columns":[
                  {"header":"Mã","field":"SoHD"},
                  {"header":"Kho","field":null,"default":"KHO1"}]}
                """);
            var all = AccountingProfiles.LoadAll(_dir);
            Assert.Equal(5, all.Count);
            var misa = all.Single(p => string.Equals(p.Name, "MISA", StringComparison.OrdinalIgnoreCase));
            Assert.True(misa.Verified);
            Assert.Equal(2, misa.Columns.Count);
            Assert.Equal("KHO1", misa.Columns[1].Default);
        }

        [Fact]
        public void Load_CorruptJsonSkipped_BuiltInsRemain()
        {
            WriteProfile("broken.json", "{ not json");
            var all = AccountingProfiles.LoadAll(_dir);
            Assert.Equal(5, all.Count);
        }

        [Fact]
        public void Load_MissingDirectory_ReturnsBuiltIns()
        {
            Assert.Equal(5, AccountingProfiles.LoadAll(Path.Combine(_dir, "none")).Count);
        }

        [Fact]
        public void Export_Unverified_HasWarningSheet_DataStartsRow2()
        {
            var p = AccountingProfiles.BuiltIn()[0];
            var f = NewFile("u.xlsx");
            AccountingExporter.Export(p, new[] { Item() }, f);

            using var wb = new XLWorkbook(f);
            Assert.True(wb.TryGetWorksheet("Ghi chú", out var note));
            Assert.Contains(note.CellsUsed(), c => c.GetString().Contains("CHƯA ĐỐI CHIẾU VỚI MẪU GỐC"));
            var main = wb.Worksheet(1);
            Assert.NotEqual("Ghi chú", main.Name);
            Assert.Equal(p.Columns[0].Header, main.Cell(1, 1).GetString());
            Assert.False(main.Cell(2, 1).IsEmpty());
            Assert.True(main.Cell(3, 1).IsEmpty());
        }

        [Fact]
        public void Export_Verified_HasNoWarningSheet()
        {
            var p = AccountingProfiles.BuiltIn()[0] with { Verified = true };
            var f = NewFile("v.xlsx");
            AccountingExporter.Export(p, new[] { Item() }, f);
            using var wb = new XLWorkbook(f);
            Assert.False(wb.TryGetWorksheet("Ghi chú", out _));
        }

        [Fact]
        public void Export_FormulaLikeText_WrittenAsLiteralText()
        {
            var p = new AccountingProfile("T", true, new[]
            {
                new AccountingColumn("Tên", "TenSanPham", null),
                new AccountingColumn("Cố định", null, "=1+1"),
            });
            var f = NewFile("f.xlsx");
            foreach (var s in new[] { "=1+1", "+cmd", "-x", "@SUM(A1)" })
            {
                AccountingExporter.Export(p, new[] { Item(s) }, f);
                using var wb = new XLWorkbook(f);
                var c = wb.Worksheet(1).Cell(2, 1);
                Assert.False(c.HasFormula);
                Assert.Equal(XLDataType.Text, c.DataType);
                Assert.Equal(s, c.GetString());
                var d = wb.Worksheet(1).Cell(2, 2);
                Assert.False(d.HasFormula);
                Assert.Equal("=1+1", d.GetString());
            }
        }

        [Fact]
        public void Export_NullValueUsesDefaultOrEmpty_EmptyFieldWritesDefault()
        {
            var p = new AccountingProfile("T", true, new[]
            {
                new AccountingColumn("ĐVT", "DonViTinh", "Cái"),
                new AccountingColumn("Lô", "SoLo", null),
                new AccountingColumn("Kho", "", "K1"),
            });
            var f = NewFile("d.xlsx");
            AccountingExporter.Export(p, new[] { Item() }, f);
            using var wb = new XLWorkbook(f);
            var ws = wb.Worksheet(1);
            Assert.Equal("Cái", ws.Cell(2, 1).GetString());
            Assert.True(ws.Cell(2, 2).IsEmpty());
            Assert.Equal("K1", ws.Cell(2, 3).GetString());
        }
    }
}
