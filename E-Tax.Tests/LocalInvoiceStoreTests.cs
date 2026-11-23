using E_Tax;

namespace E_Tax.Tests
{
    public class LocalInvoiceStoreTests : IDisposable
    {
        private const string Own = "0101234567";
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-store-" + Guid.NewGuid());
        private readonly LocalInvoiceStore _store;

        public LocalInvoiceStoreTests()
        {
            Directory.CreateDirectory(_dir);
            _store = new LocalInvoiceStore(Path.Combine(_dir, "store"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static SearchResult Inv(string seller = "0300000001", int number = 1, string date = "2026-03-15T00:00:00",
            int status = 1, string type = "Mua vào", string name = "Công ty Đồng Nai") => new()
        {
            Id = $"id-{seller}-{number}",
            Ma_so_thue = seller,
            Ky_hieu_ma_so = 1,
            Ky_hieu_hoa_don = "C26TAA",
            So_hoa_don = number,
            Ngay_lap = date,
            Thong_tin_hoa_don = name,
            Trang_thai_hoa_don = status,
            Loai_hoa_don = type
        };

        private string MonthFile(string month) => Path.Combine(_dir, "store", Own, month + ".json");

        [Fact]
        public void Upsert_New_ThenQuery_RoundTripsIncludingType()
        {
            var inv = new SearchResult
            {
                Id = "abc",
                Ma_so_thue = "0300000001",
                Ky_hieu_ma_so = 1,
                Ky_hieu_hoa_don = "C26TAA",
                So_hoa_don = 7,
                Ngay_lap = "2026-03-15T00:00:00",
                Thong_tin_hoa_don = "Công ty Đồng Nai",
                Tong_tien_chua_thue = 1000.5m,
                Tong_tien_thue = 80m,
                Tong_tien_chiet_khau = 5m,
                Tong_tien_phi = new List<decimal?> { 1m, null, 2.5m },
                Tong_tien_thanh_toan = 1085.5m,
                Don_vi_tien_te = "VND",
                Trang_thai_hoa_don = 1,
                Ket_qua_kiem_tra_hoa_don = "ok",
                Hoa_don_lien_quan = "12",
                Thong_tin_lien_quan = "địa chỉ",
                Ma_so_thue_nguoi_mua = "0101234567",
                Ten_nguoi_mua = "Người mua",
                Ky_hieu_ma_so_goc = "1",
                Ky_hieu_hoa_don_goc = "C25TAA",
                Ngay_lap_goc = "2025-12-01",
                Loai_hoa_don = "Mua vào"
            };

            var r = _store.Upsert(Own, new[] { inv });
            Assert.Equal(new StoreUpsertResult(1, 0, 0), r);

            var loaded = Assert.Single(new LocalInvoiceStore(Path.Combine(_dir, "store")).Query(Own, new StoreQuery(null, null, null, null, null, null)));
            Assert.Equal(inv.Id, loaded.Id);
            Assert.Equal(inv.Ma_so_thue, loaded.Ma_so_thue);
            Assert.Equal(inv.Ky_hieu_ma_so, loaded.Ky_hieu_ma_so);
            Assert.Equal(inv.Ky_hieu_hoa_don, loaded.Ky_hieu_hoa_don);
            Assert.Equal(inv.So_hoa_don, loaded.So_hoa_don);
            Assert.Equal(inv.Ngay_lap, loaded.Ngay_lap);
            Assert.Equal(inv.Thong_tin_hoa_don, loaded.Thong_tin_hoa_don);
            Assert.Equal(inv.Tong_tien_chua_thue, loaded.Tong_tien_chua_thue);
            Assert.Equal(inv.Tong_tien_thue, loaded.Tong_tien_thue);
            Assert.Equal(inv.Tong_tien_chiet_khau, loaded.Tong_tien_chiet_khau);
            Assert.Equal(inv.Tong_tien_phi, loaded.Tong_tien_phi);
            Assert.Equal(inv.Tong_tien_thanh_toan, loaded.Tong_tien_thanh_toan);
            Assert.Equal(inv.Don_vi_tien_te, loaded.Don_vi_tien_te);
            Assert.Equal(inv.Trang_thai_hoa_don, loaded.Trang_thai_hoa_don);
            Assert.Equal(inv.Ket_qua_kiem_tra_hoa_don, loaded.Ket_qua_kiem_tra_hoa_don);
            Assert.Equal(inv.Hoa_don_lien_quan, loaded.Hoa_don_lien_quan);
            Assert.Equal(inv.Thong_tin_lien_quan, loaded.Thong_tin_lien_quan);
            Assert.Equal(inv.Ma_so_thue_nguoi_mua, loaded.Ma_so_thue_nguoi_mua);
            Assert.Equal(inv.Ten_nguoi_mua, loaded.Ten_nguoi_mua);
            Assert.Equal(inv.Ky_hieu_ma_so_goc, loaded.Ky_hieu_ma_so_goc);
            Assert.Equal(inv.Ky_hieu_hoa_don_goc, loaded.Ky_hieu_hoa_don_goc);
            Assert.Equal(inv.Ngay_lap_goc, loaded.Ngay_lap_goc);
            Assert.Equal("Mua vào", loaded.Loai_hoa_don);
            Assert.True(File.Exists(MonthFile("2026-03")));
        }

        [Fact]
        public void Upsert_SameInvoiceTwice_IsUnchanged()
        {
            _store.Upsert(Own, new[] { Inv() });
            var r = _store.Upsert(Own, new[] { Inv() });
            Assert.Equal(new StoreUpsertResult(0, 0, 1), r);
        }

        [Fact]
        public void Upsert_StatusChanged_UpdatesRecord()
        {
            _store.Upsert(Own, new[] { Inv(status: 1) });
            var r = _store.Upsert(Own, new[] { Inv(status: 5) });
            Assert.Equal(new StoreUpsertResult(0, 1, 0), r);
            var only = Assert.Single(_store.Query(Own, new StoreQuery(null, null, null, null, null, null)));
            Assert.Equal(5, only.Trang_thai_hoa_don);
        }

        [Fact]
        public void Upsert_DateAppears_MovesRecordWithoutDuplicate()
        {
            _store.Upsert(Own, new[] { Inv(date: "") });
            Assert.True(File.Exists(MonthFile("unknown-date")));

            var r = _store.Upsert(Own, new[] { Inv(date: "2026-03-15T00:00:00") });

            Assert.Equal(new StoreUpsertResult(0, 1, 0), r);
            var only = Assert.Single(_store.Query(Own, new StoreQuery(null, null, null, null, null, null)));
            Assert.Equal("2026-03-15T00:00:00", only.Ngay_lap);
            Assert.Equal("[]", File.ReadAllText(MonthFile("unknown-date")));
            Assert.True(File.Exists(MonthFile("2026-03")));
        }

        [Fact]
        public void Upsert_SameNumberDifferentSeller_KeepsBoth()
        {
            _store.Upsert(Own, new[] { Inv(seller: "0300000001"), Inv(seller: "0300000002") });
            Assert.Equal(2, _store.Query(Own, new StoreQuery(null, null, null, null, null, null)).Count);
        }

        [Fact]
        public void Upsert_SameInvoiceBoughtAndSold_KeepsBoth()
        {
            _store.Upsert(Own, new[] { Inv(type: "Mua vào"), Inv(type: "Bán ra") });
            var all = _store.Query(Own, new StoreQuery(null, null, null, null, null, null));
            Assert.Equal(2, all.Count);
            Assert.Contains(all, x => x.Loai_hoa_don == "Mua vào");
            Assert.Contains(all, x => x.Loai_hoa_don == "Bán ra");
        }

        [Fact]
        public void Upsert_WithoutKeysFallsBackToId_AndSkipsWhenNoId()
        {
            var byId = new SearchResult { Id = "only-id", Ngay_lap = "2026-03-01", Loai_hoa_don = "Mua vào" };
            var none = new SearchResult { Ngay_lap = "2026-03-01", Loai_hoa_don = "Mua vào" };
            Assert.Equal(new StoreUpsertResult(1, 0, 0), _store.Upsert(Own, new[] { byId, none }));
            Assert.Equal(new StoreUpsertResult(0, 0, 1), _store.Upsert(Own, new[] { byId }));
        }

        [Fact]
        public void Upsert_CorruptMonthFile_ThrowsAndLeavesFileUntouched()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MonthFile("2026-02"))!);
            File.WriteAllText(MonthFile("2026-02"), "{ not json");

            // hóa đơn mới thuộc tháng khác: vẫn phải dừng trước khi ghi bất cứ gì
            Assert.Throws<InvalidDataException>(() => _store.Upsert(Own, new[] { Inv() }));

            Assert.Equal("{ not json", File.ReadAllText(MonthFile("2026-02")));
            Assert.False(File.Exists(MonthFile("2026-03")));
        }

        [Fact]
        public void Save_LeftoverTempFile_DoesNotAffectData()
        {
            _store.Upsert(Own, new[] { Inv() });
            File.WriteAllText(MonthFile("2026-03") + ".tmp", "rác {{{");

            Assert.Single(_store.Query(Own, new StoreQuery(null, null, null, null, null, null)));
            var r = _store.Upsert(Own, new[] { Inv(number: 2) });
            Assert.Equal(1, r.Added);
            Assert.Equal(2, _store.Query(Own, new StoreQuery(null, null, null, null, null, null)).Count);
        }

        [Fact]
        public void Query_FiltersByPeriodSupplierTypeStatusAndNumber()
        {
            _store.Upsert(Own, new[]
            {
                Inv(seller: "0300000001", number: 1, date: "2026-03-15T00:00:00", status: 1, type: "Mua vào", name: "Công ty Đồng Nai"),
                Inv(seller: "0300000002", number: 2, date: "2026-04-10T00:00:00", status: 5, type: "Bán ra", name: "Hà Nội Co"),
                Inv(seller: "0300000003", number: 3, date: "2026-04-20T00:00:00", status: 1, type: "Mua vào", name: "Hà Nội Co"),
            });

            List<int?> Nums(StoreQuery q) => _store.Query(Own, q).Select(x => x.So_hoa_don).ToList();

            Assert.Equal(new List<int?> { 2, 3 }, Nums(new StoreQuery(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), null, null, null, null)));
            Assert.Equal(new List<int?> { 1 }, Nums(new StoreQuery(null, null, "dong nai", null, null, null)));
            Assert.Equal(new List<int?> { 1, 3 }, Nums(new StoreQuery(null, null, null, "Mua vào", null, null)));
            Assert.Equal(new List<int?> { 2 }, Nums(new StoreQuery(null, null, null, null, 5, null)));
            Assert.Equal(new List<int?> { 3 }, Nums(new StoreQuery(null, null, null, null, null, "3")));
            Assert.Equal(new List<int?> { 3 }, Nums(new StoreQuery(new DateTime(2026, 4, 1), null, "0300000003", "Mua vào", 1, "3")));
        }

        [Fact]
        public void Query_UnknownDate_ExcludedWhenPeriodSet()
        {
            _store.Upsert(Own, new[] { Inv(number: 1, date: ""), Inv(number: 2) });

            Assert.Equal(2, _store.Query(Own, new StoreQuery(null, null, null, null, null, null)).Count);
            var inPeriod = Assert.Single(_store.Query(Own, new StoreQuery(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), null, null, null, null)));
            Assert.Equal(2, inPeriod.So_hoa_don);
        }

        [Fact]
        public void AttachFile_UsesCompanyPeriodSupplierPath_AndNeverEscapesRoot()
        {
            string source = Path.Combine(_dir, "src.pdf");
            File.WriteAllText(source, "pdf");
            var inv = Inv();

            string path = _store.AttachFile(Own, inv, source);

            string expected = Path.Combine(_dir, "store", Own, "files", "2026-03", "0300000001", inv.FileIdentifier + ".pdf");
            Assert.Equal(expected, path);
            Assert.True(File.Exists(path));
            Assert.Equal(new[] { path }, _store.FilesFor(Own, inv));

            // MST doanh nghiệp độc hại
            Assert.Throws<ArgumentException>(() => _store.AttachFile(@"..\evil", inv, source));
            Assert.Throws<ArgumentException>(() => _store.Upsert(@"..\evil", new[] { inv }));
            Assert.Throws<ArgumentException>(() => _store.Query("a/b", new StoreQuery(null, null, null, null, null, null)));

            // MST người bán độc hại rơi vào khong-ro
            var bad = Inv(seller: @"..\..\x");
            string badPath = _store.AttachFile(Own, bad, source);
            Assert.Equal(Path.Combine(_dir, "store", Own, "files", "2026-03", "khong-ro"), Path.GetDirectoryName(badPath));
            Assert.StartsWith(Path.Combine(_dir, "store") + Path.DirectorySeparatorChar, badPath);
        }

        [Fact]
        public void AttachFile_CopyFailure_KeepsExistingTarget_AndLeavesNoTmp()
        {
            string source = Path.Combine(_dir, "src.pdf");
            File.WriteAllText(source, "bản cũ");
            var inv = Inv();
            string target = _store.AttachFile(Own, inv, source);

            Assert.ThrowsAny<IOException>(() => _store.AttachFile(Own, inv, Path.Combine(_dir, "missing.pdf")));

            Assert.Equal("bản cũ", File.ReadAllText(target));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(target)!, "*.tmp"));
        }

        [Fact]
        public void AttachFile_Success_LeavesNoTmp_AndReplacesExisting()
        {
            string source = Path.Combine(_dir, "src.pdf");
            var inv = Inv();
            File.WriteAllText(source, "v1");
            string target = _store.AttachFile(Own, inv, source);
            File.WriteAllText(source, "v2");
            _store.AttachFile(Own, inv, source);

            Assert.Equal("v2", File.ReadAllText(target));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(target)!, "*.tmp"));
        }

        [Fact]
        public void FilesFor_IgnoresTmpFiles()
        {
            string source = Path.Combine(_dir, "src.pdf");
            File.WriteAllText(source, "pdf");
            var inv = Inv();
            string target = _store.AttachFile(Own, inv, source);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(target)!, inv.FileIdentifier + ".tmp"), "dở");

            Assert.Equal(new[] { target }, _store.FilesFor(Own, inv));
        }

        [Fact]
        public void Load_SameKeyInTwoMonthFiles_KeepsOneRecord()
        {
            _store.Upsert(Own, new[] { Inv(date: "") });
            string undated = File.ReadAllText(MonthFile("unknown-date"));
            _store.Upsert(Own, new[] { Inv(date: "2026-03-15T00:00:00") });
            // mô phỏng sự cố: file tháng đã ghi nhưng file unknown-date cũ còn nguyên
            File.WriteAllText(MonthFile("unknown-date"), undated);

            var all = _store.Query(Own, new StoreQuery(null, null, null, null, null, null));
            var only = Assert.Single(all);
            Assert.Equal("2026-03-15T00:00:00", only.Ngay_lap);

            var r = _store.Upsert(Own, new[] { Inv(date: "2026-03-15T00:00:00") });
            Assert.Equal(new StoreUpsertResult(0, 0, 1), r);
            Assert.Single(_store.Query(Own, new StoreQuery(null, null, null, null, null, null)));
        }

        [Fact]
        public void Upsert_MoveWritesDestinationBeforeSource()
        {
            _store.Upsert(Own, new[] { Inv(date: "") });
            string undated = File.ReadAllText(MonthFile("unknown-date"));
            // ghi file đích thất bại (đường dẫn là thư mục): file nguồn phải còn nguyên bản ghi
            Directory.CreateDirectory(MonthFile("2026-03"));

            Assert.ThrowsAny<Exception>(() => _store.Upsert(Own, new[] { Inv(date: "2026-03-15T00:00:00") }));

            Assert.Equal(undated, File.ReadAllText(MonthFile("unknown-date")));
        }

        [Fact]
        public void Upsert_DateMissingLater_KeepsStoredDateAndMonth()
        {
            _store.Upsert(Own, new[] { Inv(status: 1, date: "2026-03-15T00:00:00") });

            var incoming = Inv(status: 5, date: "");
            var r = _store.Upsert(Own, new[] { incoming });
            Assert.Equal(new StoreUpsertResult(0, 1, 0), r);
            Assert.Equal("", incoming.Ngay_lap);

            var r2 = _store.Upsert(Own, new[] { Inv(status: 5, date: "khong-phai-ngay") });
            Assert.Equal(new StoreUpsertResult(0, 0, 1), r2);

            var only = Assert.Single(_store.Query(Own, new StoreQuery(null, null, null, null, null, null)));
            Assert.Equal("2026-03-15T00:00:00", only.Ngay_lap);
            Assert.Equal(5, only.Trang_thai_hoa_don);
            Assert.False(File.Exists(MonthFile("unknown-date")));
        }

        [Fact]
        public void TaxCode_TrailingNewline_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => _store.Upsert("0101\n", new[] { Inv() }));
        }

        [Fact]
        public void Companies_ListsOnlyValidStoreFolders()
        {
            string root = Path.Combine(_dir, "store");
            Directory.CreateDirectory(Path.Combine(root, "0101234567"));
            Directory.CreateDirectory(Path.Combine(root, "0101234567-001"));
            Directory.CreateDirectory(Path.Combine(root, "backup"));
            Directory.CreateDirectory(Path.Combine(root, "12-34-56"));
            File.WriteAllText(Path.Combine(root, "999"), "file");

            Assert.Equal(new[] { "0101234567", "0101234567-001" }, _store.Companies());
        }
    }
}
