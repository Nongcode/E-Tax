using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace E_Tax
{
    public sealed record StoreUpsertResult(int Added, int Updated, int Unchanged);

    public sealed record StoreQuery(DateTime? From, DateTime? To, string? Supplier, string? InvoiceTypeLabel, int? Status, string? InvoiceNumber);

    /// <summary>
    /// Kho HĐ cục bộ: {root}\{mst}\{yyyy-MM}.json (hoặc unknown-date.json) và file đính kèm ở {root}\{mst}\files\...
    /// </summary>
    public sealed class LocalInvoiceStore
    {
        private const string UnknownDate = "unknown-date";
        private const string UnknownSupplier = "khong-ro";

        private static readonly Regex TaxCodePattern = new(@"^[0-9]+(-[0-9]+)?\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex DataFilePattern = new(@"^(\d{4}-(0[1-9]|1[0-2])|unknown-date)\.json\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        // SearchResult.Loai_hoa_don là [JsonIgnore] nên phải lưu kèm trong bao bọc này
        private sealed class StoredInvoice
        {
            [JsonPropertyName("loai")]
            public string Loai { get; set; } = "";

            [JsonPropertyName("hd")]
            public SearchResult Invoice { get; set; } = new();
        }

        private readonly string _root;
        private readonly object _lock = new();

        public LocalInvoiceStore(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("Thư mục gốc không hợp lệ.", nameof(rootPath));
            _root = Path.GetFullPath(rootPath);
        }

        /// <summary>Thư mục gốc của kho (dùng cho sao lưu/khôi phục).</summary>
        public string Root => _root;

        public static string DefaultRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "E-Tax", "store");

        public StoreUpsertResult Upsert(string ownTaxCode, IEnumerable<SearchResult> invoices)
        {
            string companyDir = CompanyDir(ownTaxCode);
            lock (_lock)
            {
                var files = LoadCompany(companyDir, out var cleaned);
                var index = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var (name, list) in files)
                    foreach (var item in list)
                    {
                        string? k = KeyOf(item.Invoice, item.Loai);
                        if (k != null) index[k] = name;
                    }

                // gained: file nhận thêm/ghi đè bản ghi; lost: file chỉ bị bớt bản ghi (ghi sau để lỗi giữa chừng chỉ gây trùng, không mất)
                var gained = new List<string>();
                var lost = new List<string>(cleaned);
                void MarkGained(string m) { if (!gained.Contains(m)) gained.Add(m); }
                void MarkLost(string m) { if (!lost.Contains(m)) lost.Add(m); }
                int added = 0, updated = 0, unchanged = 0;

                foreach (var source in invoices)
                {
                    string? key = KeyOf(source, source.Loai_hoa_don);
                    if (key == null) continue;

                    var invoice = source;
                    bool exists = index.TryGetValue(key, out string? oldMonth);
                    int pos = exists ? files[oldMonth!].FindIndex(x => KeyOf(x.Invoice, x.Loai) == key) : -1;

                    // ngày mới trống/không đọc được thì giữ ngày đã lưu, không chuyển sang unknown-date
                    if (exists && InvoiceDate.ToVietnamDate(invoice.Ngay_lap) == null &&
                        InvoiceDate.ToVietnamDate(files[oldMonth!][pos].Invoice.Ngay_lap) != null)
                    {
                        invoice = JsonSerializer.Deserialize<SearchResult>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)!;
                        invoice.Ngay_lap = files[oldMonth!][pos].Invoice.Ngay_lap;
                        invoice.Loai_hoa_don = source.Loai_hoa_don;
                    }

                    string month = MonthOf(invoice);
                    var record = new StoredInvoice { Loai = invoice.Loai_hoa_don ?? "", Invoice = invoice };

                    if (exists)
                    {
                        var oldList = files[oldMonth!];
                        if (oldMonth == month && Serialize(oldList[pos]) == Serialize(record))
                        {
                            unchanged++;
                            continue;
                        }
                        if (oldMonth == month)
                        {
                            oldList[pos] = record;
                        }
                        else
                        {
                            oldList.RemoveAt(pos);
                            MarkLost(oldMonth!);
                            GetList(files, month).Add(record);
                        }
                        MarkGained(month);
                        index[key] = month;
                        updated++;
                    }
                    else
                    {
                        GetList(files, month).Add(record);
                        MarkGained(month);
                        index[key] = month;
                        added++;
                    }
                }

                if (gained.Count > 0 || lost.Count > 0)
                {
                    Directory.CreateDirectory(companyDir);
                    foreach (string name in gained.Concat(lost.Where(l => !gained.Contains(l))))
                        SaveFile(Path.Combine(companyDir, name + ".json"), files[name]);
                }
                return new StoreUpsertResult(added, updated, unchanged);
            }
        }

        public List<SearchResult> Query(string ownTaxCode, StoreQuery query)
        {
            string companyDir = CompanyDir(ownTaxCode);
            bool periodSet = query.From.HasValue || query.To.HasValue;
            lock (_lock)
            {
                var found = new List<(DateTime? Date, SearchResult Invoice)>();
                foreach (var (name, list) in LoadCompany(companyDir, out _))
                {
                    foreach (var item in list)
                    {
                        item.Invoice.Loai_hoa_don = item.Loai;
                        var date = InvoiceDate.ToVietnamDate(item.Invoice.Ngay_lap);
                        if (periodSet)
                        {
                            if (date == null) continue;
                            if (query.From.HasValue && date.Value < query.From.Value.Date) continue;
                            if (query.To.HasValue && date.Value > query.To.Value.Date) continue;
                        }
                        if (query.Status.HasValue && item.Invoice.Trang_thai_hoa_don != query.Status.Value) continue;
                        found.Add((date, item.Invoice));
                    }
                }

                var filtered = InvoiceFilter.Apply(
                    found.Select(f => f.Invoice),
                    new InvoiceFilterCriteria(query.InvoiceNumber, query.Supplier, query.InvoiceTypeLabel, false),
                    _ => "");
                var allowed = new HashSet<SearchResult>(filtered, ReferenceEqualityComparer.Instance);
                return found.Where(f => allowed.Contains(f.Invoice))
                    .OrderBy(f => f.Date ?? DateTime.MaxValue)
                    .ThenBy(f => f.Invoice.So_hoa_don ?? int.MaxValue)
                    .Select(f => f.Invoice)
                    .ToList();
            }
        }

        public IReadOnlyList<string> Companies()
        {
            if (!Directory.Exists(_root)) return Array.Empty<string>();
            return Directory.GetDirectories(_root)
                .Select(d => Path.GetFileName(d))
                .Where(n => IsValidTaxCode(n))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();
        }

        public string AttachFile(string ownTaxCode, SearchResult invoice, string sourcePath)
        {
            string dir = InvoiceFilesDir(ownTaxCode, invoice);
            string extension = Path.GetExtension(sourcePath);
            string target = Path.GetFullPath(Path.Combine(dir, invoice.FileIdentifier + extension));
            EnsureInsideRoot(target);
            lock (_lock)
            {
                Directory.CreateDirectory(dir);
                File.Copy(sourcePath, target, true);
            }
            return target;
        }

        public IReadOnlyList<string> FilesFor(string ownTaxCode, SearchResult invoice)
        {
            string dir = InvoiceFilesDir(ownTaxCode, invoice);
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            string id = invoice.FileIdentifier;
            return Directory.GetFiles(dir)
                .Where(f => string.Equals(Path.GetFileNameWithoutExtension(f), id, StringComparison.Ordinal))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();
        }

        private static bool IsValidTaxCode(string? value) => value != null && TaxCodePattern.IsMatch(value);

        private string CompanyDir(string ownTaxCode)
        {
            if (!IsValidTaxCode(ownTaxCode))
                throw new ArgumentException("Mã số thuế doanh nghiệp không hợp lệ.", nameof(ownTaxCode));
            return Path.Combine(_root, ownTaxCode);
        }

        private string InvoiceFilesDir(string ownTaxCode, SearchResult invoice)
        {
            string supplier = IsValidTaxCode(invoice.Ma_so_thue?.Trim()) ? invoice.Ma_so_thue!.Trim() : UnknownSupplier;
            string dir = Path.GetFullPath(Path.Combine(CompanyDir(ownTaxCode), "files", MonthOf(invoice), supplier));
            EnsureInsideRoot(dir);
            return dir;
        }

        private void EnsureInsideRoot(string fullPath)
        {
            string prefix = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Đường dẫn nằm ngoài kho dữ liệu.");
        }

        private static string MonthOf(SearchResult invoice) =>
            InvoiceDate.ToVietnamDate(invoice.Ngay_lap) is DateTime d
                ? d.ToString("yyyy-MM", CultureInfo.InvariantCulture)
                : UnknownDate;

        private static string? KeyOf(SearchResult invoice, string? type)
        {
            string t = type ?? "";
            if (invoice.HasLookupKeys())
                return string.Join("|", "k", t, invoice.Ma_so_thue, invoice.Ky_hieu_ma_so, invoice.Ky_hieu_hoa_don, invoice.So_hoa_don);
            if (!string.IsNullOrEmpty(invoice.Id)) return string.Join("|", "i", t, invoice.Id);
            return null;
        }

        private static List<StoredInvoice> GetList(Dictionary<string, List<StoredInvoice>> files, string month)
        {
            if (!files.TryGetValue(month, out var list)) files[month] = list = new List<StoredInvoice>();
            return list;
        }

        private static string Serialize(StoredInvoice record) => JsonSerializer.Serialize(record, JsonOptions);

        // Đọc toàn bộ file tháng của doanh nghiệp; bất kỳ file hỏng nào đều ném InvalidDataException.
        // Cùng một khóa ở hai file (sự cố giữa lúc chuyển tháng): giữ bản ở tháng có ngày, bỏ bản còn lại; cleaned = file bị bớt bản trùng.
        private static Dictionary<string, List<StoredInvoice>> LoadCompany(string companyDir, out List<string> cleaned)
        {
            cleaned = new List<string>();
            var result = new Dictionary<string, List<StoredInvoice>>(StringComparer.Ordinal);
            if (!Directory.Exists(companyDir)) return result;
            foreach (string file in Directory.GetFiles(companyDir, "*.json"))
            {
                string fileName = Path.GetFileName(file);
                if (!DataFilePattern.IsMatch(fileName)) continue;
                result[Path.GetFileNameWithoutExtension(fileName)] = ReadFile(file);
            }

            var owner = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string name in result.Keys.OrderBy(n => n == UnknownDate ? 0 : 1).ThenBy(n => n, StringComparer.Ordinal))
            {
                foreach (var item in result[name].ToList())
                {
                    string? k = KeyOf(item.Invoice, item.Loai);
                    if (k == null) continue;
                    if (owner.TryGetValue(k, out string? prev) && prev != name)
                    {
                        // name đứng sau prev trong thứ tự (unknown-date trước, rồi tháng tăng dần): giữ bản của name
                        result[prev].RemoveAll(x => KeyOf(x.Invoice, x.Loai) == k);
                        if (!cleaned.Contains(prev)) cleaned.Add(prev);
                    }
                    owner[k] = name;
                }
            }
            return result;
        }

        private static List<StoredInvoice> ReadFile(string path)
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<StoredInvoice>>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
                if (list == null || list.Any(x => x == null || x.Invoice == null))
                    throw new InvalidDataException($"File kho hóa đơn bị hỏng: {Path.GetFileName(path)}");
                return list;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"File kho hóa đơn bị hỏng: {Path.GetFileName(path)}", ex);
            }
        }

        private static void SaveFile(string path, List<StoredInvoice> list)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(list, JsonOptions), new UTF8Encoding(false));
            File.Move(temp, path, true);
        }
    }
}
