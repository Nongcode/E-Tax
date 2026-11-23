using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClosedXML.Excel;

namespace E_Tax
{
    /// <summary>
    /// Danh sách NCC rủi ro nhập từ CSV/XLSX, lưu cục bộ để kiểm tra khi không có token API.
    /// Chỉ trả Risky (có trong danh sách) hoặc Unknown; không có trong danh sách KHÔNG có nghĩa là an toàn.
    /// </summary>
    internal sealed class LocalRiskList
    {
        private sealed class Entry
        {
            [JsonPropertyName("mst")] public string Mst { get; set; } = "";
            [JsonPropertyName("ten")] public string? Name { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private readonly string _path;
        private readonly Action<string> _log;
        private readonly object _lock = new();
        private Dictionary<string, string?> _items = new(StringComparer.Ordinal);

        public LocalRiskList(string filePath, Action<string>? log = null)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Đường dẫn không hợp lệ.", nameof(filePath));
            _path = filePath;
            _log = log ?? (_ => { });
            Load();
        }

        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "risk-list.json");

        public int Count { get { lock (_lock) return _items.Count; } }

        public RiskResult Check(string taxCode)
        {
            string code = (taxCode ?? "").Trim();
            lock (_lock)
            {
                return _items.TryGetValue(code, out var name)
                    ? new RiskResult(code, RiskStatus.Risky, name)
                    : new RiskResult(code, RiskStatus.Unknown, null);
            }
        }

        /// <summary>Thay toàn bộ danh sách bằng file CSV/XLSX. Không có MST hợp lệ nào thì ném InvalidDataException và giữ danh sách cũ.</summary>
        public int Import(string csvOrXlsxPath)
        {
            string ext = Path.GetExtension(csvOrXlsxPath).ToLowerInvariant();
            List<string?[]> rows = ext switch
            {
                ".csv" => ReadCsv(csvOrXlsxPath),
                ".xlsx" => ReadXlsx(csvOrXlsxPath),
                _ => throw new InvalidDataException("Chỉ hỗ trợ file .csv hoặc .xlsx.")
            };

            int taxCol = 0, nameCol = -1, start = 0;
            if (rows.Count > 0)
            {
                var header = rows[0];
                int t = Array.FindIndex(header, h => h != null && IsTaxHeader(Normalize(h)));
                if (t >= 0)
                {
                    taxCol = t;
                    start = 1;
                    nameCol = Array.FindIndex(header, h => h != null && Normalize(h).Contains("ten"));
                    if (nameCol == taxCol) nameCol = -1;
                }
            }

            var items = new Dictionary<string, string?>(StringComparer.Ordinal);
            int skipped = 0;
            for (int i = start; i < rows.Count; i++)
            {
                var row = rows[i];
                string code = taxCol < row.Length ? (row[taxCol] ?? "").Trim() : "";
                if (code.Length == 0) continue; // dòng trống
                if (!InvoiceClassifier.IsValidTaxCode(code)) { skipped++; continue; }
                string? name = nameCol >= 0 && nameCol < row.Length ? row[nameCol]?.Trim() : null;
                if (string.IsNullOrEmpty(name)) name = null;
                if (!items.TryGetValue(code, out var old) || (old == null && name != null)) items[code] = name;
            }
            if (skipped > 0) _log($"⚠️ Nhập danh sách NCC rủi ro: bỏ qua {skipped} dòng có MST không hợp lệ.");
            if (items.Count == 0)
                throw new InvalidDataException("File không có mã số thuế hợp lệ nào. Danh sách hiện tại được giữ nguyên.");

            lock (_lock)
            {
                Save(items); // ghi thành công mới đổi bộ nhớ
                _items = items;
            }
            return items.Count;
        }

        private void Load()
        {
            if (!File.Exists(_path)) return;
            try
            {
                var list = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(_path, Encoding.UTF8), JsonOptions);
                if (list == null || list.Any(e => e == null || e.Mst == null))
                    throw new InvalidDataException("Nội dung không hợp lệ.");
                var items = new Dictionary<string, string?>(StringComparer.Ordinal);
                foreach (var e in list) items[e.Mst] = e.Name;
                _items = items;
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException or UnauthorizedAccessException)
            {
                _log($"⚠️ File danh sách NCC rủi ro bị hỏng hoặc không đọc được ({ex.GetType().Name}); coi như danh sách rỗng, cần nhập lại.");
                _items = new Dictionary<string, string?>(StringComparer.Ordinal);
            }
        }

        private void Save(Dictionary<string, string?> items)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(_path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var list = items.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new Entry { Mst = kv.Key, Name = kv.Value }).ToList();
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(list, JsonOptions), new UTF8Encoding(false));
            File.Move(temp, _path, true);
        }

        private static bool IsTaxHeader(string normalized) =>
            normalized.Contains("mst") || normalized.Contains("ma so thue");

        /// <summary>Chữ thường, bỏ dấu (kể cả đ), gộp khoảng trắng.</summary>
        private static string Normalize(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c is 'đ' or 'Đ' ? 'd' : char.ToLowerInvariant(c));
            }
            return string.Join(' ', sb.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        private static List<string?[]> ReadXlsx(string path)
        {
            var rows = new List<string?[]>();
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.FirstOrDefault();
            var used = ws?.RangeUsed();
            if (ws == null || used == null) return rows;
            int firstCol = used.FirstColumn().ColumnNumber();
            int lastCol = used.LastColumn().ColumnNumber();
            foreach (var row in used.Rows())
            {
                var cells = new string?[lastCol - firstCol + 1];
                for (int c = firstCol; c <= lastCol; c++)
                {
                    var cell = ws.Cell(row.RowNumber(), c);
                    if (cell.IsEmpty()) continue;
                    if (cell.DataType == XLDataType.Number)
                    {
                        double d = cell.GetDouble();
                        // Excel làm mất số 0 đầu của MST 10 số: số nguyên 9 chữ số thì đệm lại 10
                        string digits = d == Math.Floor(d) && d >= 0 && d < 1e15
                            ? ((long)d).ToString(CultureInfo.InvariantCulture)
                            : d.ToString(CultureInfo.InvariantCulture);
                        cells[c - firstCol] = digits.Length == 9 && digits.All(char.IsAsciiDigit) ? "0" + digits : digits;
                    }
                    else cells[c - firstCol] = cell.GetString();
                }
                rows.Add(cells);
            }
            return rows;
        }

        private static List<string?[]> ReadCsv(string path)
        {
            string text = File.ReadAllText(path, Encoding.UTF8); // tự bỏ BOM
            int firstEol = text.IndexOfAny(new[] { '\r', '\n' });
            string firstLine = firstEol < 0 ? text : text[..firstEol];
            char delim = CountOutsideQuotes(firstLine, ';') > CountOutsideQuotes(firstLine, ',') ? ';' : ',';

            var rows = new List<string?[]>();
            var row = new List<string?>();
            var field = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(ch);
                }
                else if (ch == '"') inQuotes = true;
                else if (ch == delim) { row.Add(field.ToString()); field.Clear(); }
                else if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(field.ToString()); field.Clear();
                    rows.Add(row.ToArray()); row.Clear();
                }
                else field.Append(ch);
            }
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row.ToArray()); }
            return rows;
        }

        private static int CountOutsideQuotes(string line, char target)
        {
            int n = 0; bool q = false;
            foreach (char c in line)
            {
                if (c == '"') q = !q;
                else if (c == target && !q) n++;
            }
            return n;
        }
    }
}
