# E-Tax — Tổng quan dự án

> Tài liệu tham chiếu nhanh để cập nhật code mà không cần đọc lại toàn bộ source.
> Khảo sát lần cuối: 2026-11-23 (commit `2c413e1`, nhánh `feat/hoan-thien-viec-dang-do`), cập nhật sau đợt hoàn thiện các việc dở. Khi sửa code lớn, nhớ cập nhật lại file này.

## 1. Mục đích

Ứng dụng desktop WinForms giúp doanh nghiệp/kế toán **tra cứu, tổng hợp, xuất Excel, tải bản gốc và in hóa đơn điện tử**
từ cổng **Hóa đơn điện tử của Tổng cục Thuế** (`https://hoadondientu.gdt.gov.vn:30000/`), bằng cách gọi trực tiếp
các API nội bộ mà trang web đó sử dụng (giả lập trình duyệt qua User-Agent/Referer/Origin).

Có cơ chế bản quyền: dùng thử 30 ngày, kích hoạt bằng key ký số (ECDSA) gắn với mã máy.

## 2. Công nghệ

| Thành phần | Chi tiết |
|---|---|
| Nền tảng | .NET 8 (`net8.0-windows`), WinForms, `Nullable` bật, `ImplicitUsings` bật |
| Solution | `E-Tax.sln` → app `E-Tax/E-Tax.csproj` (namespace gốc `E_Tax`), `E-Tax.Tests`, tool `tools/E-Tax.KeyGen` |
| ClosedXML 0.105.1 | Tạo file Excel (MIT, không cần license key) — toàn bộ trong `ExcelExporter.cs` |
| PuppeteerSharp 20.2.4 | Convert HTML hóa đơn → PDF (tự tải Chromium lần đầu qua `BrowserFetcher`) |
| PDFsharp `[6.2.4]` | Gộp nhiều PDF thành một (`PdfMerger.cs`), MIT. **Ghim đúng `[6.2.4]`, không nâng cấp**: 6.5.0 đổi sang AGPL-3.0 |
| Svg 3.4.7 | Render ảnh captcha SVG → Bitmap |
| System.Text.Json | Parse toàn bộ JSON (không dùng Newtonsoft) |

Test: `dotnet test` (project `E-Tax.Tests`, xUnit, `InternalsVisibleTo`).

Build: `dotnet build E-Tax.sln` (csproj có target `KillProcessBeforeBuild` chạy `taskkill /IM E-Tax.exe /F` trước khi build).

## 3. Cấu trúc file

```
E-Tax.sln
E-Tax/
├── Program.cs                 # Entry point → Application.Run(new Form1())
├── Form1.cs        (~2360 dòng) # Logic chính: login, kỳ thuế, search, lọc, export, bảng kê, download, in/gộp PDF, notes, kiểm tra rủi ro (chia #region)
├── Form1.Designer.cs (~1250)  # Layout UI (panelLogin, panelActivation, panelSearch, tabControlMain, ...)
├── DetailGridManager.cs (~400)# Dựng dòng hàng từ InvoiceDetailService, đổ vào 4 lưới chi tiết; class InvoiceDetailItem; LatestItems/LastFailedCount/LastNoLineCount cho bảng kê
├── InvoiceDetailService.cs   # API detail: phân trang SP (50/trang), cache theo Id, song song tối đa 3 request, thử lại 429/5xx; nhận CancellationToken
├── TaxPeriod.cs               # record TaxPeriod (Month/Quarter/Year/Custom + Label); InvoiceDate.ToVietnamDate chuẩn hóa ngày lập về UTC+7
├── InvoiceFilter.cs           # Lọc danh sách theo số HĐ (khớp đúng số), NCC (MST/tên, bỏ dấu), loại, chỉ HĐ có ghi chú
├── InvoiceSearchResults.cs    # KeyOf (Id, hoặc MST|ký hiệu|số khi thiếu Id), Deduplicate theo (loại, khóa), ParsePage/IsEndOfResults phân loại trang
├── PdfMerger.cs               # Gộp PDF bằng PDFsharp; file lỗi/mã hóa/không tồn tại bị bỏ và báo lại; ghi file tạm rồi đổi tên
├── BatchProgress.cs           # record (Step, Done, Total, Failed) cho ProcessingForm
├── TaxListing.cs              # Dựng bảng kê mua/bán theo thuế suất (NormalizeRate, Build, CountNonVnd, ExportBlockReason)
├── RiskCheckService.cs        # Kiểm tra NCC rủi ro hàng loạt (song song 3); chỉ Safe khi phản hồi khẳng định rõ, còn lại Unknown
├── SearchResult.cs            # enum InvoiceType, SearchResult (+ HasLookupKeys(), FileIdentifier = MST_KHMS_KHHĐ_Số), SearchResponse
├── InvoiceDownloadPlan.cs     # Danh sách tải gốc/in/gộp: mỗi HĐ thật một lần, tên file không trùng, thứ tự gộp PDF
├── GdtApi.cs                  # Hằng URL/User-Agent; CreateRequest(...) set header chung; BuildDetailUrl(...)
├── InvoiceJson.cs             # JSON chi tiết HĐ: Options, DetailExportColumns (20 cột Excel), FindProductArray (hdhhdvu), BuildDetailRows
├── ExcelExporter.cs            # ClosedXML: ExportSearchResults (DS 18 cột), ExportDetailSummary (chi tiết tổng hợp), ExportSingleInvoiceDetail (1 HĐ) — chỉ ghi file
├── GridColumnHelper.cs        # Style + tạo cột text/số cho DataGridView cấu hình bằng code
├── Logger.cs                  # Ghi log ra file theo ngày (%LocalAppData%/E-Tax/logs)
├── NotesStore.cs              # Ghi chú theo Id HĐ (%AppData%/E-Tax/notes.json)
├── PdfConverter.cs            # HTML → PDF (PuppeteerSharp), 1 trình duyệt cho cả lô
├── AppSettings.cs             # Đọc appsettings.json (token blacklist); appsettings.example.json là mẫu
├── LicenseManager.cs          # static LicenseManager + enum LicenseStatus; hằng PublicKeyPem (public key xác thực key)
├── LicenseKey.cs              # Định dạng key ký số: Create/Verify + enum LicenseKeyResult (link sang KeyGen)
├── MachineId.cs               # Mã máy = 16 hex đầu SHA-256(HKLM MachineGuid)
├── ProcessingForm.cs          # Form tiến trình có nút Hủy (CancellationToken), thanh tiến trình, Report(BatchProgress), UpdateStatus(string); chặn Alt+F4 khi đang chạy
├── TraCuuForm.cs              # Popup hiển thị mã tra cứu + link tra cứu NCC hóa đơn + thông tin người bán
└── Properties/Resources.*     # Resource mặc định
tools/E-Tax.KeyGen/            # Console net8.0 cấp key (init/issue); link E-Tax/LicenseKey.cs
E-Tax.Tests/                   # xUnit (12 file test): NotesStore, InvoiceDetailService (+ FakeHttpHandler), AppSettings, LicenseKey, ExcelExporter, TaxPeriod, InvoiceFilter, InvoiceSearchResults, PdfMerger, BatchProgress, TaxListing, RiskCheckService
```

Các `#region` trong `Form1.cs`: Đăng nhập & bản quyền · Gọi API hóa đơn · Excel & PDF · Tìm kiếm & lọc ·
Thao tác trên hóa đơn · Tra cứu hóa đơn · Lưới bảng kê giảm thuế · Ghi chú · Kiểm tra doanh nghiệp rủi ro (đơn lẻ + hàng loạt NCC).

## 4. API cổng thuế được dùng

BaseAddress `HttpClient` = `GdtApi.BaseUrl` (`https://hoadondientu.gdt.gov.vn:30000/`). Auth: `Authorization: Bearer <jwt>` + cookie `jwt`.
Mọi request tạo qua `GdtApi.CreateRequest(method, url, accept, jwtToken?, withPortalHeaders)`.

| Endpoint | Method | Dùng ở | Ghi chú |
|---|---|---|---|
| `captcha` | GET | `LoadCaptchaAsync` | Trả `{key, content(SVG)}` (cũng chấp nhận `ckey/captchaKey`, `image/cvalue`) |
| `security-taxpayer/authenticate` | POST | `LoginAsync` | Body `{username, password, cvalue, ckey}` → token từ cookie `jwt` hoặc field `token` |
| `query/invoices/sold` | GET | `FetchSearchPageAsync` | HĐ bán ra |
| `query/invoices/purchase` | GET | `FetchSearchPageAsync` | HĐ mua vào (thêm `;ttxly==5` vào search) |
| `query/invoices/detail` | GET | `InvoiceDetailService.GetDetailPagesAsync` / `PrefetchAsync` (lưới chi tiết, Excel chi tiết, Tra cứu) | URL từ `GdtApi.BuildDetailUrl(..., page, 50)`, lặp trang đến khi < 50 SP → mảng hàng hóa `hdhhdvu` (`InvoiceJson.FindProductArray`) |
| `query/invoices/export-xml` | GET | `DownloadSingleInvoiceZipAsync` | Trả file ZIP (XML + HTML/PDF thể hiện) |

Query string tạo bởi `BuildSearchQuery(from, to, type, page, size)`:
`size={size}&page={page}&sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=dd/MM/yyyyTHH:mm:ss;tdlap=le=...[;ttxly==5]`

Giới hạn API (được code xử lý): **tối đa 30 ngày/lần** (hằng `SearchChunkDays = 30`) và **50 bản ghi/trang** (`SearchPageSize = 50`, lặp trang đến khi < 50).

Phân loại trang kết quả (`InvoiceSearchResults.ParsePage`): có `datas` khác rỗng → còn dữ liệu; `datas` là mảng rỗng, hoặc `datas` null/thiếu kèm `total` = 0 → trang cuối hợp lệ; thân không đọc được hoặc sai cấu trúc → **trang lỗi** (không coi là hết dữ liệu; gom vào `FailedChunks`, giữ kết quả các trang khác). Dedupe theo **(loại HĐ, khóa)**: cùng `Id` ở Mua vào và Bán ra là hai HĐ khác nhau; HĐ thiếu `Id` dùng khóa `MST|ký hiệu mã số|ký hiệu|số` (`InvoiceSearchResults.KeyOf`).

API ngoài: `BlacklistApiUrl?tax_code=...&token=...` (kiểm tra DN rủi ro, `btnKtraDNMuaLe_Click`). Token đọc từ `appsettings.json` cạnh exe qua `AppSettings` — file **không commit**, mẫu ở `E-Tax/appsettings.example.json`; thiếu token thì nút báo "chưa được cấu hình".

### Mapping trường JSON → `SearchResult`

| JSON | Property | Ý nghĩa |
|---|---|---|
| id | Id | ID hóa đơn (key dedupe cùng loại HĐ & key ghi chú) |
| nbmst | Ma_so_thue | MST người bán |
| khmshdon | Ky_hieu_ma_so | Ký hiệu mẫu số |
| khhdon | Ky_hieu_hoa_don | Ký hiệu HĐ |
| shdon | So_hoa_don | Số HĐ |
| tdlap | Ngay_lap | Ngày lập (string) |
| nbten | Thong_tin_hoa_don | Tên người bán |
| tgtcthue / tgtthue / ttcktmai | Tong_tien_chua_thue / _thue / _chiet_khau | |
| thttlphi | Tong_tien_phi | `List<decimal?>` (cộng tổng khi hiển thị) |
| tgtttbso | Tong_tien_thanh_toan | |
| dvtte | Don_vi_tien_te | |
| tthai | Trang_thai_hoa_don | int |
| kqcht | Ket_qua_kiem_tra_hoa_don | |
| shdgoc | Hoa_don_lien_quan | |
| nmdchi | Thong_tin_lien_quan | Giữ nguyên dữ liệu gốc (không còn bị ghi đè) |
| _(không có)_ | Loai_hoa_don | `[JsonIgnore]`, gán `"Bán ra"` / `"Mua vào"` sau khi search; dùng cho cột `colDgvLoaiHD`, lọc BK mua/bán, cột Excel "Loại hóa đơn" |

Trường hàng hóa (trong `hdhhdvu`): `ten, dvtinh, sluong, dgia, thtien, stckhau, thtcthue, tsuat, tthue, solo, hsd, ghichu`.

## 5. Luồng chức năng (UI → hàm xử lý)

### 5.1 Khởi động & bản quyền
`Form1()` → tạo `HttpClient` (CookieContainer), `InvoiceDetailService` (token lấy qua `() => jwtToken`), `DetailGridManager`, cấu hình lưới giảm thuế.
`Form1_Load` → `CheckForLicense()` → `LoadNotesFromFile()` → `LoadCaptchaAsync()`.

`LicenseManager` (Registry `HKCU\Software\ETaxDemoApp`):
- `IsActivated == "true"` → `Activated` (máy kích hoạt bằng mã master cũ — giữ để không ảnh hưởng khách hiện tại; mã master đã bị xóa khỏi code)
- `LicenseKey` (chuỗi key) → xác thực lại mỗi lần mở bằng `LicenseKey.Verify(key, MachineId.Get(), UtcNow, PublicKeyPem)`; `Valid` → `Activated`
- `TrialExpiryDate` (ISO "o") còn hạn → `ValidTrial`; hết → `Expired` (hiện `panelActivation` kèm mã máy + nút "Sao chép", khóa các ô đăng nhập qua `SetLoginInputsEnabled(false)` — không tắt cả `panelLogin` vì `panelActivation` nằm trong đó)
- Chưa có key → `InitializeTrial()` +30 ngày
- `Activate(key)` (bỏ khoảng trắng) → `LicenseKeyResult`; `Valid` thì lưu registry `LicenseKey`. Form báo riêng `WrongMachine` / `Expired` / không hợp lệ.
- Định dạng key: `Base64Url(payloadJson).Base64Url(chữ ký)`, payload `{"machine","customer","expires":"yyyy-MM-dd"|null}`, ECDSA P-256/SHA-256; còn hiệu lực đến hết ngày `expires` (UTC); so mã máy không phân biệt hoa/thường. `Verify` không ném lỗi, dữ liệu rác → `Invalid`.

#### Cấp key cho khách
1. (Một lần) `dotnet run --project tools/E-Tax.KeyGen -- init <đường-dẫn-ngoài-repo>\etax.private.pem` → dán public key in ra vào `LicenseManager.PublicKeyPem`, build lại app. Hiện code đang chứa **public key DEV** — phải thay trước khi phát hành. Giữ file private key bí mật, sao lưu; không commit (`*.private.pem` đã ignore).
2. Khách mở app hết hạn → bấm "Sao chép" cạnh **Mã máy** và gửi mã (16 ký tự hex).
3. `dotnet run --project tools/E-Tax.KeyGen -- issue <private.pem> <mã máy> "<tên khách>" [yyyy-MM-dd]` (bỏ ngày = vĩnh viễn) → gửi chuỗi key in ra cho khách dán vào ô **Mã** → "Kích hoạt".

### 5.2 Đăng nhập
`btnLogin_Click` → `LoginAsync` → lưu `jwtToken`, set header Bearer. Ẩn `panelLogin`, hiện `panelSearch`. Nút `btnRefreshCaptcha` tải lại captcha.

### 5.3 Tìm kiếm (`btnLeftSearch_Click`) — chức năng trung tâm
Input: bộ chọn kỳ thuế (`cbPeriodKind` Tùy chọn/Tháng/Quý/Năm + `cbPeriodNumber` + `nudPeriodYear`; `ApplyPeriod` gán `dtpFromDate`/`dtpToDate`), `dtpFromDate`, `dtpToDate`, radio `rbSold` / `rbBought` / `rbAllInvoices`. Lúc tìm, `CaptureSelectedPeriod` lưu kỳ vào `_lastSearchPeriod` (giữ kỳ đã chọn chỉ khi khoảng ngày còn khớp, nếu không dùng `TaxPeriod.Custom`) để đặt tên/nhãn file bảng kê.
1. `SearchInvoicesAsync(from, to, includeSold, includeBought, reportStatus)` — **dùng chung** với `SaveOriginalInvoicesAsync`:
   chia lát 30 ngày; mỗi lát lặp trang (size 50) cho Bán ra / Mua vào độc lập (`FetchSearchPageAsync` trả false khi hết trang hoặc lỗi).
2. Gắn `Loai_hoa_don = "Bán ra"/"Mua vào"`, `InvoiceSearchResults.Deduplicate` (theo loại + khóa), sort `Ngay_lap` desc, `So_hoa_don` asc → `_latestResults`. Trang lỗi gom vào `FailedChunks`; số trang lỗi lưu `_lastFailedSearchPages`.
3. `BindMainGrid()` bind `dgvMain` (tab **Tổng hợp**; ngày hiển thị `dd/MM/yyyy` qua `CellFormatting`, `Ngay_lap` trong model giữ giá trị gốc); bind `dgvGiamThue` (tab có text **"VAT nộp"**).
4. `DetailGridManager.PopulateDetailGridAsync` → `InvoiceDetailService.PrefetchAsync` (song song tối đa `MaxConcurrency = 3`, không delay cố định; progress qua `onCompleted`) → dựng dòng **theo thứ tự danh sách** (STT ổn định) từ mọi trang → bind:
   - `dgvDetails` (tab **Chi tiết**) — tất cả dòng hàng
   - `dgvMua` (tab **Bk mua**) — dòng của HĐ "Mua vào"
   - `dgvBan` (tab **BK bán**) — còn lại
   - `dgvVatNop` (tab có text **"Bảng kê Thuế"**) — tất cả, ít cột
   Đầu `btnLeftSearch_Click` gọi `_detailService.ClearCache()`; trong phiên tìm kiếm, chi tiết đã tải được dùng lại (cache theo `Id`, chỉ cache kết quả thành công đủ trang).
   Trang chi tiết chỉ hợp lệ khi có mảng `hdhhdvu` toàn object, hoặc có `shdon` khớp số HĐ (HĐ không có dòng hàng); `shdon` không khớp, phần tử không phải object, hoặc thân lỗi kèm HTTP 200 đều là trang lỗi (`InvoiceJson.IsValidDetailPage`).
   HĐ lỗi hoặc thiếu trang → `GetDetailPagesAsync` trả null, không cache (không trả dữ liệu thiếu); `PopulateDetailGridAsync` trả số HĐ lỗi → cảnh báo "Thiếu dữ liệu chi tiết", không báo "Hoàn tất". Lưu hàng loạt: HĐ lỗi chi tiết trong Excel được thêm vào danh sách lỗi cuối.
5. Hiển thị `ProcessingForm` (nút **Hủy**, tiến trình theo `BatchProgress`) trong lúc chạy; `this.Enabled = false`. **Hủy** chỉ có ở ba luồng: Tìm kiếm và "Kiểm tra NCC" (dùng `ProcessingForm.ShowFor`), "Tải tất cả HĐ Gốc" (dùng `new ProcessingForm()`); "In pdf" và "Gộp PDF" không có Hủy. Không có hóa đơn nhưng có trang lỗi thì báo lỗi tải kèm danh sách trang, không báo "không tìm thấy" (cả Tìm kiếm và Tải gốc). Với tìm kiếm, Hủy truyền `CancellationToken` xuống search/chi tiết (không đổi thành chuỗi `"❌"`): bỏ mọi kết quả dở dang, xóa `DetailGridManager.LatestItems`/`LastFailedCount` để bảng kê không xuất dữ liệu cũ, không báo lỗi hay "Hoàn tất".

> Lưu ý tên: `tabVATNop` hiển thị "Bảng kê Thuế" (chứa `dgvVatNop`), còn `tabGiamThue` hiển thị "VAT nộp" (chứa `dgvGiamThue`). Tên biến và text bị lệch nhau.

### 5.4 Các nút thao tác trên kết quả
**Lọc** (`btnRightSearch`, nhãn "Lọc"): `InvoiceFilter.Apply` trên `_latestResults` theo số HĐ (`txtLocSoHD`, khớp đúng số), NCC (`txtLocNCC`, MST hoặc tên, không phân biệt dấu/hoa thường), loại (`cboLocLoai`: Tất cả/Mua vào/Bán ra), `chkChiCoGhiChu`. Chỉ lọc `dgvMain`.

| Nút | Hàm | Làm gì |
|---|---|---|
| Tải danh sách HĐ (`btnExportDS`) | `ExportSearchResultsToExcelAsync(_latestResults)` → `ExcelExporter.ExportSearchResults` | Excel 18 cột từ danh sách đã có (cột 17 Ghi chú, 18 Rủi ro NCC) |
| Tải chi tiết HĐ (`btnExportChiTiet`) | `GetDetailPagesAsync` → `BuildDetailRows(pages)` (gộp mọi trang) → `ExcelExporter.ExportSingleInvoiceDetail` | Excel hàng hóa của **1** HĐ đang chọn |
| Xem HĐ (`btnXemHD`) | `DownloadSingleInvoiceZipAsync` → giải nén temp → mở HTML/PDF bằng app mặc định | |
| In pdf (`btnCnKoPdf`) | handler + `PrintPdfFilesAsync` / `SavePdfFilesToFolder` | Với các dòng đang chọn: tải ZIP → lấy PDF hoặc `PdfConverter.ConvertAsync` → có máy in thì `Verb="print"`, không thì chọn thư mục lưu PDF |
| Tải tất cả HĐ Gốc (`btnTaiHDGoc`) | `SaveOriginalInvoicesAsync` | **Tự search lại** theo ngày (`SearchInvoicesAsync`) → Excel DS + Excel chi tiết (`ExportInvoiceDetailsToExcelAsync`: `PrefetchAsync` dùng lại cache, gộp mọi trang, đánh STT → `ExcelExporter.ExportDetailSummary`) + tải ZIP từng HĐ (mỗi HĐ thật một lần qua `InvoiceDownloadPlan`; Excel vẫn đủ theo loại) + PDF + XML, `_GopTatCa.pdf` chỉ gồm PDF tạo được trong lượt này → nén 1 file `E-Tax_Export_{from}_den_{to}.zip` (thư mục con `0_BaoCaoExcel`, `1_HoaDonGoc_ZIP`, `2_HoaDonPDF`, `3_HoaDonXML`) |
| Cột "Tải" trong `dgvMain` (`colDgvTraCuu`) | `dgvMain_CellContentClick` → `GetDetailPagesAsync` (trang đầu) → `BuildTraCuuLink` → `TraCuuForm` | Lấy `mhdon` (mã tra cứu) + `ngcnhat` (NCC: tvan_misa/bkav/vnpt/viettel/thaison) → link tra cứu |
| Gộp PDF (`btnGopPdf`) | `btnGopPdf_Click`: hỏi nơi lưu trước → tải ZIP từng HĐ đã chọn → lấy PDF hoặc `PdfConverter.ConvertAsync` (HĐ HTML) → `PdfMerger.Merge` theo thứ tự chọn | **Không có Hủy** (không dùng `ProcessingForm`/`CancellationToken`); chặn chạy đồng thời với In pdf; HĐ lỗi không để lại trang nào và được liệt kê cuối; thư mục tạm dọn ở `finally` |
| Xuất bảng kê mua / bán (`btnExportBangKeMua` / `btnExportBangKeBan`) | `ExportTaxListing(type)` → `TaxListing.Build(LatestItems)` → `ExcelExporter.ExportTaxListing` | Nhóm theo thuế suất, gộp theo (HĐ, thuế suất). File `BangKe_{Mua/Ban}_{kỳ}.xlsx` theo `_lastSearchPeriod`. Xem mục Bảng kê dưới |
| Kiểm tra NCC (`btnKiemTraNCC`) | `RiskCheckService.CheckManyAsync` cho MST người bán (distinct) của HĐ "Mua vào" đang hiển thị | Song song 3, có Hủy qua `ProcessingForm` (hủy = bỏ toàn bộ kết quả lần đó); kết quả gộp vào `_riskResults`, tô dòng hồng (Rủi ro) / xám (Không kiểm tra được), có trong Excel DS. Cần token ở `appsettings.json`. Lỗi mạng/phản hồi lạ = "Không kiểm tra được", **không bao giờ coi là an toàn** |
| Ghi chú (`btnMoGhiChu`) | `NotesStore` → `%AppData%/E-Tax/notes.json` (lần đầu tự chép `notes.json` cũ cạnh exe) | Tự load khi đổi dòng (`dgvMain_SelectionChanged`) |
| Ktra DN Mua Lẻ (`btnKtraDNMuaLe`) | gọi API blacklist metaecom | MST lấy từ `txtKtraMaDoanhNghiep` (tự điền khi chọn dòng) |

#### Bảng kê (mua vào / bán ra)
- Dữ liệu từ `LatestItems` (dòng hàng của lần tìm gần nhất); thuế suất chuẩn hóa bởi `TaxListing.NormalizeRate` (có `%` là phần trăm; số trần 0<x<1 là tỷ lệ; KCT/KKKNT giữ nguyên; rỗng = "Không rõ"). Đối tác: Mua vào = người bán, Bán ra = người mua (thiếu thì để trống).
- **Chặn xuất** (`ExportBlockReason`) khi `LastFailedCount + _lastFailedSearchPages > 0` (HĐ lỗi/thiếu chi tiết hoặc trang tìm kiếm lỗi): không bao giờ xuất bảng kê thiếu số liệu. Chưa tìm kiếm thì báo chưa có dữ liệu.
- **Một** xác nhận Yes/No trước khi xuất (`TaxListing.ExportWarnings`, rỗng thì không hỏi, No thì dừng): dòng có trạng thái khác 1 hoặc null (vẫn tính vào tổng); dòng thiếu số tiền (tính là 0); HĐ không có dòng hàng (`LastNoLineCount`, sẽ vắng trong bảng kê); HĐ ngoại tệ (`CountNonVnd`, cộng nguyên số tiền, không quy đổi). Hai cảnh báo đầu được ghi thêm ở dòng 3 của file (`TaxListing.ExportNotes`).
- Dòng có `DoanhSo` null, hoặc `TienThue` null trên thuế suất phần trăm, bị đánh dấu `ThieuSoTien` (KCT/KKKNT thuế null không tính là thiếu). Dòng có `tthai` null hoặc khác 1 được tô vàng trong Excel.

### 5.5 Tiện ích
- `AppendLog(string)` → `Logger.Write` ghi file `%LocalAppData%/E-Tax/logs/etax_yyyyMMdd.log` (giữ 30 ngày, không ném lỗi). DetailGridManager cũng log qua delegate này. **Không log token/mật khẩu.**
- `EnsureChromiumAsync()` tải Chromium lần đầu và **cache đường dẫn** (`_chromiumExecutablePath`); `PdfConverter` (PdfConverter.cs) mở **1 trình duyệt cho cả lô**, mỗi HĐ 1 tab (A4, Networkidle0, timeout 60s), đóng khi `DisposeAsync`.
- `DownloadSingleInvoiceZipAsync(invoice, dir, ct, fileIdentifier)` trả đường dẫn `HD_{fileIdentifier ?? FileIdentifier}.zip` hoặc `null` khi lỗi. Tên file gồm MST người bán + KH mẫu số + ký hiệu + số, ký tự không hợp lệ thành `_`.
- `GetSelectedInvoiceWithKeys(row, msg)` — lấy HĐ từ dòng lưới và kiểm tra đủ khóa, tự hiện thông báo nếu không hợp lệ.
- `ShowStatusMessage` + `statusTimer` (2s) — thông báo tạm trên `lblStatusMessage`.
- `DataGridView_DataError` — nuốt lỗi binding của mọi lưới.
- Quy ước lỗi: các hàm gọi API trả về **chuỗi bắt đầu bằng `"❌"`** khi lỗi thay vì throw → nơi gọi kiểm `StartsWith("❌")`.

## 6. Đánh giá

### Điểm tốt
- Chức năng phong phú, bám sát nhu cầu kế toán thực tế (bảng kê mua/bán, VAT, tải gốc hàng loạt, in hàng loạt).
- Đã xử lý giới hạn 30 ngày / 50 bản ghi của API, dedupe kết quả.
- Xử lý lỗi theo từng hóa đơn — 1 HĐ lỗi không làm hỏng cả lô; có tổng hợp danh sách lỗi cho người dùng.
- Dọn dẹp thư mục temp trong `finally`. Build sạch (0 error).

### Vấn đề nghiêm trọng (bảo mật / pháp lý)
1. ✅ (2026-11-16) **Mã kích hoạt master** đã bỏ → key ký số ECDSA gắn mã máy, app chỉ chứa public key (xem §5.1). Còn lại: public key hiện là key DEV, chủ dự án phải `init` và thay; máy cũ có `IsActivated=true` vẫn được chấp nhận nên ai sửa Registry giá trị này vẫn qua được (đánh đổi đã duyệt ở D3); trial vẫn ở HKCU (xóa Registry là có thêm 30 ngày); app .NET vẫn có thể bị patch nhị phân — ký số chỉ chống tự sinh key.
2. ✅ Token blacklist đã chuyển ra `appsettings.json` (2026-11-16). **Còn phải làm:** đổi token phía server metaecom vì token cũ nằm trong lịch sử git; về lâu dài nên gọi qua server trung gian (app desktop luôn có thể bị đọc token).
3. ✅ (2026-11-16) Đã bỏ EPPlus (license NonCommercialPersonal không hợp lệ cho app bán bản quyền), chuyển sang **ClosedXML** (MIT) trong `ExcelExporter.cs`.
4. ✅ (2026-11-16) Mật khẩu không còn bị `Trim()`.

### Bug / hành vi sai

### Hiệu năng
- ✅ (2026-11-16) Chi tiết HĐ: bỏ gọi tuần tự + `Task.Delay(500)`/HĐ → `InvoiceDetailService` gọi song song tối đa 3 request, thử lại 429/5xx (1s, 2s). Còn lại: tìm kiếm luôn kéo chi tiết toàn bộ dù người dùng chỉ cần danh sách.
- ✅ (2026-11-16) Excel chi tiết (Tải tất cả HĐ Gốc) dùng lại cache của `InvoiceDetailService`, bỏ `Task.Delay(150)`/HĐ; Excel chi tiết giờ đủ mọi trang (> 50 dòng hàng).
- Chromium (~150MB) tải lúc runtime (cần Internet, lần đầu rất chậm). ✅ Đã dùng chung 1 trình duyệt cho cả lô.

### Chất lượng code / bảo trì
- `Form1.cs` vẫn là God class (~2.4k dòng, đã chia `#region`); `SaveOriginalInvoicesAsync`, `btnCnKoPdf_Click` vẫn dài.
- ✅ Đã gộp (2026-11-16): vòng search → `SearchInvoicesAsync`; tìm `hdhhdvu` → `InvoiceJson.FindProductArray`; cột Excel chi tiết → `InvoiceJson.DetailExportColumns`; header request → `GdtApi.CreateRequest`; HTML→PDF → `PdfConverter`; kiểm tra 4 khóa HĐ → `SearchResult.HasLookupKeys()`.
- ✅ Đã xóa code chết: `JObject.cs`, `Models/InvoiceDetai.cs`, `Resources/login.html`, `DownloadInvoiceListExcelAsync`, `GetDaysRemaining`, các khối comment cũ, handler rỗng, field Designer không dùng, package ClosedXML/WebView2 (ClosedXML sau đó được dùng lại thay EPPlus).
- 40 warning (chủ yếu nullable; trước khi dọn là 97) — `Nullable` bật nhưng model chưa khai báo nullable.
- Quy ước lỗi bằng chuỗi `"❌..."` dễ vỡ; nên dùng `Result`/exception.
- Có unit test xUnit (12 file, `dotnet test`) cho logic thuần; `Form1` không có test. URL/hằng số hard-code rải rác.

### Đề xuất refactor theo thứ tự ưu tiên
1. ✅ Đã chuyển ClosedXML (bỏ EPPlus). Bỏ secret hard-code, cơ chế kích hoạt an toàn hơn.
2. ✅ (2026-11-16) Ghi log ra file; sửa thông báo in và thông báo hoàn tất tìm kiếm.
3. Tách lớp: `GdtApiClient` (captcha/login/search/detail/download + header chung), `InvoiceSearchService` (chia lát 30 ngày + phân trang, dùng chung cho Search & Tải gốc), ✅ `ExcelExporter`, `PdfService` (dùng chung 1 browser Puppeteer), `NotesStore`.
4. ✅ (2026-11-16) Cache chi tiết HĐ theo `Id` + gọi song song có giới hạn (`InvoiceDetailService`).
5. ✅ (2026-11-16) Đã thêm `Loai_hoa_don` thay vì ghi đè `Thong_tin_lien_quan`.
6. Dọn warning nullable còn lại.
7. Mục 7.1 (đăng nhập bằng tài khoản hệ thống và kho hóa đơn dùng chung) **chưa làm**: cần server, không làm được trong app desktop thuần.

### Chưa kiểm chứng trên dữ liệu thật
Các điểm dưới đây dựa trên giả định/test giả lập, chưa chạy với phản hồi thật của cổng thuế:
- Định dạng thật của `tdlap` (hiện chuẩn hóa về giờ Việt Nam bởi `InvoiceDate.ToVietnamDate`).
- `nmten`, `nmmst` (tên/MST người mua trong bảng kê bán).
- Định dạng `tsuat` (`NormalizeRate` đoán các dạng "10%", "0.1", "10", KCT/KKKNT).
- Số tiền có theo nguyên tệ hay không (bảng kê chỉ cảnh báo, không quy đổi).
- Dòng đặc biệt như ghi chú, chiết khấu thương mại trong `hdhhdvu`.
- Bố cục UI và các luồng (kỳ thuế, lọc, Gộp PDF, Hủy, bảng kê, Kiểm tra NCC) chưa được chạy thử.

## 7. Ghi chú khi sửa code
- Thêm cột cho lưới chi tiết: sửa `InvoiceDetailItem` + `ConfigureFullDetailGrid`/`ConfigureVatNopGrid` + `CreateDetailItem` (DetailGridManager.cs).
- Thêm cột cho danh sách: `SearchResult` (SearchResult.cs) + cột `dgvMain` trong Designer (tên cột dạng `colDgv*`, `AutoGenerateColumns=false`) + `SearchResultHeaders` và khối ghi ô trong `ExcelExporter.ExportSearchResults` (header dòng 4 viết HOA, cột 7–11 `#,##0`; cột 16 = `Loai_hoa_don`, 17 = Ghi chú, 18 = Rủi ro NCC, hai cột sau lấy qua delegate `getNote`/`getRisk`).
- Cột Excel chi tiết: chỉ sửa `InvoiceJson.DetailExportColumns` (dùng chung cho Excel 1 HĐ và Excel tổng hợp); cột số → `InvoiceJson.IsNumericColumn` (parse InvariantCulture, không parse được thì giữ chữ).
- Excel: `ExcelExporter` chỉ ghi file (không gọi API/UI), Form1 lấy dữ liệu rồi gọi qua `Task.Run`; test ở `E-Tax.Tests/ExcelExporterTests.cs`.
- Lấy chi tiết HĐ (phân trang, cache, retry, số request đồng thời): chỉ sửa `InvoiceDetailService` (dùng chung cho lưới, Excel chi tiết, Tra cứu; trả `null` khi lỗi, nơi gọi hiện `DetailFetchError`) (hằng `ProductPageSize`, `MaxConcurrency`); test ở `E-Tax.Tests/InvoiceDetailServiceTests.cs`.
- Logic search: chỉ sửa `SearchInvoicesAsync` / `FetchSearchPageAsync`.
- Thêm NCC tra cứu: `BuildTraCuuLink` (switch theo `ngcnhat` lowercase).
- Đổi điều kiện lọc HĐ mua vào: `BuildSearchQuery` (`;ttxly==5`).
- Gọi API mới: dùng `GdtApi.CreateRequest`; hằng URL đặt trong `GdtApi`.
- Tác vụ dài: nhận `CancellationToken` từ `ProcessingForm.CancellationToken`, ném lại `OperationCanceledException` khi hủy (đừng đổi thành `"❌"`) và bỏ kết quả dở dang.
- Bảng kê: HĐ thiếu chi tiết phải trả `null` (không dữ liệu một phần) để `LastFailedCount` chặn xuất; thêm cột/quy tắc thì sửa `TaxListing` + `ExcelExporter.ExportTaxListing` + `TaxListingTests`.
- Khóa HĐ: dùng `InvoiceSearchResults.KeyOf`, không tự ghép; dedupe luôn kèm loại HĐ.
- PDFsharp ghim `[6.2.4]` (MIT): không nâng lên 6.5.0 (AGPL-3.0).
