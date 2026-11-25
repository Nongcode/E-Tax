using System.Text;
using ClosedXML.Excel;
using E_Tax;

namespace E_Tax.Tests
{
    public class LocalRiskListTests : IDisposable
    {
        private const string A = "0100109106";
        private const string B = "0101234565";
        private const string C = "0300000018";
        private const string Valid9 = "100109106"; // thiếu số 0 đầu của A
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-risk-" + Guid.NewGuid());
        private readonly List<string> _logs = new();

        public LocalRiskListTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private string P(string name) => Path.Combine(_dir, name);
        private LocalRiskList NewList() => new(P("risk-list.json"), _logs.Add);

        private string WriteCsv(string name, string content, bool bom = false)
        {
            string path = P(name);
            File.WriteAllText(path, content, new UTF8Encoding(bom));
            return path;
        }

        private string WriteXlsx(string name, Action<IXLWorksheet> fill)
        {
            string path = P(name);
            using var wb = new XLWorkbook();
            fill(wb.AddWorksheet("Sheet1"));
            wb.SaveAs(path);
            return path;
        }

        [Fact]
        public void Import_Csv_ComaWithMstHeader_MarksRisky()
        {
            var list = NewList();
            int n = list.Import(WriteCsv("a.csv", $"STT,MST,Tên\n1,{A},Công ty A\n2,{B},Công ty B\n"));
            Assert.Equal(2, n);
            Assert.Equal(2, list.Count);
            var r = list.Check(A);
            Assert.Equal(RiskStatus.Risky, r.Status);
            Assert.Equal("Công ty A", r.CompanyName);
            Assert.Equal(RiskStatus.Risky, list.Check(B).Status);
        }

        [Fact]
        public void Import_Csv_SemicolonQuotedBom()
        {
            var list = NewList();
            int n = list.Import(WriteCsv("b.csv", $"\"Mã số thuế\";\"Tên đơn vị\"\n\"{A}\";\"Công ty; A\"\n", bom: true));
            Assert.Equal(1, n);
            Assert.Equal("Công ty; A", list.Check(A).CompanyName);
        }

        [Fact]
        public void Import_Csv_NoMstHeader_UsesFirstColumn()
        {
            var list = NewList();
            int n = list.Import(WriteCsv("c.csv", $"{A},x\n{B},y\n"));
            Assert.Equal(2, n);
            Assert.Equal(RiskStatus.Risky, list.Check(B).Status);
        }

        [Fact]
        public void Import_Csv_SkipsInvalidAndDuplicates_LogsCountOnly()
        {
            var list = NewList();
            int n = list.Import(WriteCsv("d.csv", $"MST\n{A}\n{A}\n0100109107\nabc-secret\n{B}\n"));
            Assert.Equal(2, n);
            Assert.Contains(_logs, l => l.Contains('2'));
            Assert.DoesNotContain(_logs, l => l.Contains("abc-secret") || l.Contains("0100109107"));
        }

        [Fact]
        public void Import_Xlsx_AccentInsensitiveHeader_NumericCellLeadingZero()
        {
            var list = NewList();
            string path = WriteXlsx("a.xlsx", ws =>
            {
                ws.Cell(1, 1).Value = "STT";
                ws.Cell(1, 2).Value = "MA SO THUE";
                ws.Cell(1, 3).Value = "Ten NCC";
                ws.Cell(2, 1).Value = 1;
                ws.Cell(2, 2).Value = long.Parse(Valid9); // numeric 9 chữ số
                ws.Cell(2, 3).Value = "Công ty A";
                ws.Cell(3, 1).Value = 2;
                ws.Cell(3, 2).Value = B; // text
            });
            int n = list.Import(path);
            Assert.Equal(2, n);
            Assert.Equal(RiskStatus.Risky, list.Check(A).Status);
            Assert.Equal("Công ty A", list.Check(A).CompanyName);
            Assert.Equal(RiskStatus.Risky, list.Check(B).Status);
        }

        [Fact]
        public void Check_NotListedOrEmpty_IsUnknownNeverSafe()
        {
            var list = NewList();
            Assert.Equal(RiskStatus.Unknown, list.Check(A).Status);
            list.Import(WriteCsv("e.csv", $"MST\n{A}\n"));
            var r = list.Check(C);
            Assert.Equal(RiskStatus.Unknown, r.Status);
            Assert.NotEqual(RiskStatus.Safe, list.Check("").Status);
            Assert.Equal(RiskStatus.Risky, list.Check("  " + A + " ").Status);
        }

        [Fact]
        public void Import_NoValidCode_ThrowsAndKeepsOldList_AlsoOnDisk()
        {
            var list = NewList();
            list.Import(WriteCsv("ok.csv", $"MST\n{A}\n"));
            string bad = WriteCsv("bad.csv", "MST\n123\nxyz\n");
            Assert.Throws<InvalidDataException>(() => list.Import(bad));
            Assert.Equal(1, list.Count);
            Assert.Equal(RiskStatus.Risky, list.Check(A).Status);
            Assert.Equal(RiskStatus.Risky, NewList().Check(A).Status);
        }

        [Fact]
        public void Import_ReplacesWholeList_AndPersists()
        {
            var list = NewList();
            list.Import(WriteCsv("1.csv", $"MST\n{A}\n"));
            list.Import(WriteCsv("2.csv", $"MST\n{B}\n"));
            var reloaded = NewList();
            Assert.Equal(1, reloaded.Count);
            Assert.Equal(RiskStatus.Unknown, reloaded.Check(A).Status);
            Assert.Equal(RiskStatus.Risky, reloaded.Check(B).Status);
            Assert.False(File.Exists(P("risk-list.json.tmp")));
        }

        [Fact]
        public void CorruptFile_LoadsEmpty_LogsAndIsNotOverwrittenUntilImport()
        {
            string path = P("risk-list.json");
            File.WriteAllText(path, "{ not json");
            var list = NewList();
            Assert.Equal(0, list.Count);
            Assert.Equal(RiskStatus.Unknown, list.Check(A).Status);
            Assert.NotEmpty(_logs);
            Assert.Equal("{ not json", File.ReadAllText(path));
            list.Import(WriteCsv("ok.csv", $"MST\n{A}\n"));
            Assert.Equal(RiskStatus.Risky, NewList().Check(A).Status);
        }

        [Fact]
        public void DefaultPath_IsUnderAppData() =>
            Assert.EndsWith(Path.Combine("E-Tax", "risk-list.json"), LocalRiskList.DefaultPath);
    }
}
