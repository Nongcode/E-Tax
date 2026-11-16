# E-Tax — Tổng quan dự án

> Tài liệu tham chiếu nhanh để cập nhật code mà không cần đọc lại toàn bộ source.
> Khảo sát lần cuối: 2026-11-16 (commit `fb5a65c`), cập nhật sau đợt dọn code cùng ngày. Khi sửa code lớn, nhớ cập nhật lại file này.

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
| Svg 3.4.7 | Render ảnh captcha SVG → Bitmap |
| System.Text.Json | Parse toàn bộ JSON (không dùng Newtonsoft) |

Test: `dotnet test` (project `E-Tax.Tests`, xUnit, `InternalsVisibleTo`).

Build: `dotnet build E-Tax.sln` (csproj có target `KillProcessBeforeBuild` chạy `taskkill /IM E-Tax.exe /F` trước khi build).

## 3. Cấu trúc file

```
E-Tax.sln
E-Tax/
├── Program.cs                 # Entry point → Application.Run(new Form1())
├── Form1.cs        (~1890 dòng) # Logic chính: login, search, export, download, print, notes, blacklist (chia #region)
├── Form1.Designer.cs (~1050)  # Layout UI (panelLogin, panelActivation, panelSearch, tabControlMain, ...)
├── DetailGridManager.cs (~320)# Dựng dòng hàng từ InvoiceDetailService, đổ vào 4 lưới chi tiết; class InvoiceDetailItem
├── InvoiceDetailService.cs   # API detail: phân trang SP (50/trang), cache theo Id, song song tối đa 3 request, thử lại 429/5xx
├── SearchResult.cs            # enum InvoiceType, SearchResult (+ HasLookupKeys(), FileIdentifier), SearchResponse
├── GdtApi.cs                  # Hằng URL/User-Agent; CreateRequest(...) set header chung; BuildDetailUrl(...)
├── InvoiceJson.cs             # JSON chi tiết HĐ: Options, DetailExportColumns (20 cột Excel), FindProductArray (hdhhdvu), BuildDetailRows
├── ExcelExporter.cs            # ClosedXML: ExportSearchResults (DS 16 cột), ExportDetailSummary (chi tiết tổng hợp), ExportSingleInvoiceDetail (1 HĐ) — chỉ ghi file
├── GridColumnHelper.cs        # Tạo cột text/số cho DataGridView cấu hình bằng code; ApplyDefaultStyle → UiTheme.StyleGrid
├── UiTheme.cs                 # Bảng màu (Accent #0075D6, ...) + StyleGrid(dgv): tiêu đề xanh 34px, dòng 28px, kẻ ngang
├── WindowSettings.cs          # Nhớ vị trí/kích thước/phóng to cửa sổ (%AppData%/E-Tax/window.json); Fit() kéo về màn hình
├── InvoiceSummary.cs          # Số HĐ + tổng thanh toán theo Bán ra/Mua vào → chữ trên thanh trạng thái
├── Logger.cs                  # Ghi log ra file theo ngày (%LocalAppData%/E-Tax/logs)
├── NotesStore.cs              # Ghi chú theo Id HĐ (%AppData%/E-Tax/notes.json)
├── PdfConverter.cs            # HTML → PDF (PuppeteerSharp), 1 trình duyệt cho cả lô
├── AppSettings.cs             # Đọc appsettings.json (token blacklist); appsettings.example.json là mẫu
├── LicenseManager.cs          # static LicenseManager + enum LicenseStatus; hằng PublicKeyPem (public key xác thực key)
├── LicenseKey.cs              # Định dạng key ký số: Create/Verify + enum LicenseKeyResult (link sang KeyGen)
├── MachineId.cs               # Mã máy = 16 hex đầu SHA-256(HKLM MachineGuid)
├── ProcessingForm.cs          # Form "đang xử lý" (ControlBox=false), có UpdateStatus(string)
├── TraCuuForm.cs              # Popup hiển thị mã tra cứu + link tra cứu NCC hóa đơn + thông tin người bán
└── Properties/Resources.*     # Resource mặc định
tools/E-Tax.KeyGen/            # Console net8.0 cấp key (init/issue); link E-Tax/LicenseKey.cs
E-Tax.Tests/                   # xUnit: NotesStoreTests, InvoiceDetailServiceTests (+ FakeHttpHandler), AppSettingsTests, LicenseKeyTests, ExcelExporterTests, WindowSettingsTests, UiThemeTests, InvoiceSummaryTests
```

Các `#region` trong `Form1.cs`: Đăng nhập & bản quyền · Gọi API hóa đơn · Excel & PDF · Tìm kiếm & lọc ·
Thao tác trên hóa đơn · Tra cứu hóa đơn · Lưới bảng kê giảm thuế · Ghi chú · Kiểm tra doanh nghiệp rủi ro.

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

API ngoài: `BlacklistApiUrl?tax_code=...&token=...` (kiểm tra DN rủi ro, `btnKtraDNMuaLe_Click`). Token đọc từ `appsettings.json` cạnh exe qua `AppSettings` — file **không commit**, mẫu ở `E-Tax/appsettings.example.json`; thiếu token thì nút báo "chưa được cấu hình".

### Mapping trường JSON → `SearchResult`

| JSON | Property | Ý nghĩa |
|---|---|---|
| id | Id | ID hóa đơn (dùng làm key dedupe & key ghi chú) |
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
Khối đăng nhập không có logo: tiêu đề `lblLoginTitle` + `lblLoginSubtitle`; `CenterLoginPanel` đặt chiều cao theo nội dung và căn giữa vùng trên thanh trạng thái (gọi lại khi `Resize`/`Shown`/`panelActivation.VisibleChanged`).
`btnLogin_Click` → `LoginAsync` → lưu `jwtToken`, set header Bearer. Ẩn `panelLogin`, hiện `panelSearch`. Nút `btnRefreshCaptcha` tải lại captcha.

### 5.3 Tìm kiếm (`btnLeftSearch_Click`) — chức năng trung tâm
Input: `dtpFromDate`, `dtpToDate`, radio `rbSold` / `rbBought` / `rbAllInvoices`.
1. `SearchInvoicesAsync(from, to, includeSold, includeBought, reportStatus)` — **dùng chung** với `SaveOriginalInvoicesAsync`:
   chia lát 30 ngày; mỗi lát lặp trang (size 50) cho Bán ra / Mua vào độc lập (`FetchSearchPageAsync` trả false khi hết trang hoặc lỗi).
2. Gắn `Loai_hoa_don = "Bán ra"/"Mua vào"`, dedupe theo `Id`, sort `Ngay_lap` desc, `So_hoa_don` asc → `_latestResults`. Trang lỗi gom vào `FailedChunks`.
3. `BindMainGrid()` bind `dgvMain` (tab **Tổng hợp**); bind `dgvGiamThue` (tab có text **"VAT nộp"**).
4. `DetailGridManager.PopulateDetailGridAsync` → `InvoiceDetailService.PrefetchAsync` (song song tối đa `MaxConcurrency = 3`, không delay cố định; progress qua `onCompleted`) → dựng dòng **theo thứ tự danh sách** (STT ổn định) từ mọi trang → bind:
   - `dgvDetails` (tab **Chi tiết**) — tất cả dòng hàng
   - `dgvMua` (tab **Bk mua**) — dòng của HĐ "Mua vào"
   - `dgvBan` (tab **BK bán**) — còn lại
   - `dgvVatNop` (tab có text **"Bảng kê Thuế"**) — tất cả, ít cột
   Đầu `btnLeftSearch_Click` gọi `_detailService.ClearCache()`; trong phiên tìm kiếm, chi tiết đã tải được dùng lại (cache theo `Id`, chỉ cache kết quả thành công đủ trang).
   HĐ lỗi hoặc thiếu trang → `GetDetailPagesAsync` trả null (không trả dữ liệu thiếu); `PopulateDetailGridAsync` trả số HĐ lỗi → cảnh báo "Thiếu dữ liệu chi tiết", không báo "Hoàn tất". Lưu hàng loạt: HĐ lỗi chi tiết trong Excel được thêm vào danh sách lỗi cuối.
5. Hiển thị `ProcessingForm` trong lúc chạy; `this.Enabled = false`.

> Lưu ý tên: `tabVATNop` hiển thị "Bảng kê Thuế" (chứa `dgvVatNop`), còn `tabGiamThue` hiển thị "VAT nộp" (chứa `dgvGiamThue`). Tên biến và text bị lệch nhau.

### 5.4 Các nút thao tác trên kết quả
| Nút | Hàm | Làm gì |
|---|---|---|
| Tải danh sách HĐ (`btnExportDS`) | `ExportSearchResultsToExcelAsync(_latestResults)` → `ExcelExporter.ExportSearchResults` | Excel 16 cột từ danh sách đã có |
| Tải chi tiết HĐ (`btnExportChiTiet`) | `GetDetailPagesAsync` → `BuildDetailRows(pages)` (gộp mọi trang) → `ExcelExporter.ExportSingleInvoiceDetail` | Excel hàng hóa của **1** HĐ đang chọn |
| Xem HĐ (`btnXemHD`) | `DownloadSingleInvoiceZipAsync` → giải nén temp → mở HTML/PDF bằng app mặc định | |
| In pdf (`btnCnKoPdf`) | handler + `PrintPdfFilesAsync` / `SavePdfFilesToFolder` | Với các dòng đang chọn: tải ZIP → lấy PDF hoặc `PdfConverter.ConvertAsync` → có máy in thì `Verb="print"`, không thì chọn thư mục lưu PDF |
| Tải tất cả HĐ Gốc (`btnTaiHDGoc`) | `SaveOriginalInvoicesAsync` | **Tự search lại** theo ngày (`SearchInvoicesAsync`) → Excel DS + Excel chi tiết (`ExportInvoiceDetailsToExcelAsync`: `PrefetchAsync` dùng lại cache, gộp mọi trang, đánh STT → `ExcelExporter.ExportDetailSummary`) + tải ZIP từng HĐ + PDF + XML → nén 1 file `E-Tax_Export_{from}_den_{to}.zip` (thư mục con `0_BaoCaoExcel`, `1_HoaDonGoc_ZIP`, `2_HoaDonPDF`, `3_HoaDonXML`) |
| Cột "Tải" trong `dgvMain` (`colDgvTraCuu`) | `dgvMain_CellContentClick` → `GetDetailPagesAsync` (trang đầu) → `BuildTraCuuLink` → `TraCuuForm` | Lấy `mhdon` (mã tra cứu) + `ngcnhat` (NCC: tvan_misa/bkav/vnpt/viettel/thaison) → link tra cứu |
| Tìm kiếm MST ng bán (`btnRightSearch`) | lọc `_latestResults` theo `Ma_so_thue.Contains` | Chỉ lọc `dgvMain` |
| Ghi chú (`btnMoGhiChu`) | `NotesStore` → `%AppData%/E-Tax/notes.json` (lần đầu tự chép `notes.json` cũ cạnh exe) | Tự load khi đổi dòng (`dgvMain_SelectionChanged`) |
| Ktra DN Mua Lẻ (`btnKtraDNMuaLe`) | gọi API blacklist metaecom | MST lấy từ `txtKtraMaDoanhNghiep` (tự điền khi chọn dòng) |

### 5.5 Tiện ích
- `AppendLog(string)` → `Logger.Write` ghi file `%LocalAppData%/E-Tax/logs/etax_yyyyMMdd.log` (giữ 30 ngày, không ném lỗi). DetailGridManager cũng log qua delegate này. **Không log token/mật khẩu.**
- `EnsureChromiumAsync()` tải Chromium lần đầu và **cache đường dẫn** (`_chromiumExecutablePath`); `PdfConverter` (PdfConverter.cs) mở **1 trình duyệt cho cả lô**, mỗi HĐ 1 tab (A4, Networkidle0, timeout 60s), đóng khi `DisposeAsync`.
- `DownloadSingleInvoiceZipAsync(invoice, dir)` trả đường dẫn `HD_{FileIdentifier}.zip` hoặc `null` khi lỗi.
- `GetSelectedInvoiceWithKeys(row, msg)` — lấy HĐ từ dòng lưới và kiểm tra đủ khóa, tự hiện thông báo nếu không hợp lệ.
- `ShowStatusMessage` + `statusTimer` (2s) — thông báo tạm trên `lblStatusMessage`.
- `DataGridView_DataError` — nuốt lỗi binding của mọi lưới.
- Cửa sổ co giãn (`Sizable`, tối thiểu = kích thước thiết kế): `RestoreWindowBounds`/`SaveWindowBounds` (`#region Cửa sổ`), `CenterLoginPanel` khi Resize.
- Thanh trạng thái `statusBar`: `lblStatusInvoices` (`UpdateInvoiceSummary`, gọi trong `BindMainGrid` và khi bắt đầu tìm), `lblStatusUser` (đăng nhập), `lblStatusLicense` + `lblVersion` (`ShowLicenseStatus` trong `CheckForLicense`).
- `dgvMain` chọn cả dòng (`FullRowSelect`) → In pdf in tất cả dòng đang chọn. Style/định dạng tiền ở `StyleMainGrid`.
- **Chế độ demo (chỉ Debug):** đặt biến môi trường `ETAX_UI_DEMO=1` (hoặc `progress` để hiện thanh tiến độ; `activation` để hiện khối kích hoạt trên màn hình đăng nhập) → bỏ đăng nhập, nạp 60 HĐ mẫu (`ShowDemoData`), không gọi API. Dùng để chụp màn hình; không có trong Release.
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
- `Form1.cs` vẫn là God class (~2.1k dòng, đã chia `#region`); `SaveOriginalInvoicesAsync`, `btnCnKoPdf_Click` vẫn dài.
- ✅ Đã gộp (2026-11-16): vòng search → `SearchInvoicesAsync`; tìm `hdhhdvu` → `InvoiceJson.FindProductArray`; cột Excel chi tiết → `InvoiceJson.DetailExportColumns`; header request → `GdtApi.CreateRequest`; HTML→PDF → `PdfConverter`; kiểm tra 4 khóa HĐ → `SearchResult.HasLookupKeys()`.
- ✅ Đã xóa code chết: `JObject.cs`, `Models/InvoiceDetai.cs`, `Resources/login.html`, `DownloadInvoiceListExcelAsync`, `GetDaysRemaining`, các khối comment cũ, handler rỗng, field Designer không dùng, package ClosedXML/WebView2 (ClosedXML sau đó được dùng lại thay EPPlus).
- 43 warning (chủ yếu nullable; trước khi dọn là 97) — `Nullable` bật nhưng model chưa khai báo nullable.
- Quy ước lỗi bằng chuỗi `"❌..."` dễ vỡ; nên dùng `Result`/exception.
- Không có unit test; URL/hằng số hard-code rải rác.

### Đề xuất refactor theo thứ tự ưu tiên
1. ✅ Đã chuyển ClosedXML (bỏ EPPlus). Bỏ secret hard-code, cơ chế kích hoạt an toàn hơn.
2. ✅ (2026-11-16) Ghi log ra file; sửa thông báo in và thông báo hoàn tất tìm kiếm.
3. Tách lớp: `GdtApiClient` (captcha/login/search/detail/download + header chung), `InvoiceSearchService` (chia lát 30 ngày + phân trang, dùng chung cho Search & Tải gốc), ✅ `ExcelExporter`, `PdfService` (dùng chung 1 browser Puppeteer), `NotesStore`.
4. ✅ (2026-11-16) Cache chi tiết HĐ theo `Id` + gọi song song có giới hạn (`InvoiceDetailService`).
5. ✅ (2026-11-16) Đã thêm `Loai_hoa_don` thay vì ghi đè `Thong_tin_lien_quan`.
6. Dọn warning nullable còn lại.

## 7. Ghi chú khi sửa code
- Thêm cột cho lưới chi tiết: sửa `InvoiceDetailItem` + `ConfigureFullDetailGrid`/`ConfigureVatNopGrid` + `CreateDetailItem` (DetailGridManager.cs).
- Thêm cột cho danh sách: `SearchResult` (SearchResult.cs) + cột `dgvMain` trong Designer (tên cột dạng `colDgv*`, `AutoGenerateColumns=false`) + `SearchResultHeaders` và khối ghi ô trong `ExcelExporter.ExportSearchResults` (header dòng 4 viết HOA, cột 7–11 `#,##0`; cột 16 = `Loai_hoa_don`).
- Cột Excel chi tiết: chỉ sửa `InvoiceJson.DetailExportColumns` (dùng chung cho Excel 1 HĐ và Excel tổng hợp); cột số → `InvoiceJson.IsNumericColumn` (parse InvariantCulture, không parse được thì giữ chữ).
- Excel: `ExcelExporter` chỉ ghi file (không gọi API/UI), Form1 lấy dữ liệu rồi gọi qua `Task.Run`; test ở `E-Tax.Tests/ExcelExporterTests.cs`.
- Lấy chi tiết HĐ (phân trang, cache, retry, số request đồng thời): chỉ sửa `InvoiceDetailService` (dùng chung cho lưới, Excel chi tiết, Tra cứu; trả `null` khi lỗi, nơi gọi hiện `DetailFetchError`) (hằng `ProductPageSize`, `MaxConcurrency`); test ở `E-Tax.Tests/InvoiceDetailServiceTests.cs`.
- Logic search: chỉ sửa `SearchInvoicesAsync` / `FetchSearchPageAsync`.
- Thêm NCC tra cứu: `BuildTraCuuLink` (switch theo `ngcnhat` lowercase).
- Đổi điều kiện lọc HĐ mua vào: `BuildSearchQuery` (`;ttxly==5`).
- Gọi API mới: dùng `GdtApi.CreateRequest`; hằng URL đặt trong `GdtApi`.
- Màu/style giao diện: lấy từ `UiTheme` (không hard-code màu mới). Lưới mới cấu hình bằng code → `GridColumnHelper.ApplyDefaultStyle`; lưới tạo trong Designer → gọi `UiTheme.StyleGrid`.
- Thêm control vào `panelSearch`: `mainSplitContainer` (Dock Fill) đứng trên cùng z-order — control nổi phải `BringToFront()`.
