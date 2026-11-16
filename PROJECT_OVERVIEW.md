# E-Tax — Tổng quan dự án

> Tài liệu tham chiếu nhanh để cập nhật code mà không cần đọc lại toàn bộ source.
> Khảo sát lần cuối: 2026-11-16 (commit `fb5a65c`), cập nhật sau đợt dọn code cùng ngày. Khi sửa code lớn, nhớ cập nhật lại file này.

## 1. Mục đích

Ứng dụng desktop WinForms giúp doanh nghiệp/kế toán **tra cứu, tổng hợp, xuất Excel, tải bản gốc và in hóa đơn điện tử**
từ cổng **Hóa đơn điện tử của Tổng cục Thuế** (`https://hoadondientu.gdt.gov.vn:30000/`), bằng cách gọi trực tiếp
các API nội bộ mà trang web đó sử dụng (giả lập trình duyệt qua User-Agent/Referer/Origin).

Có cơ chế bản quyền: dùng thử 30 ngày, kích hoạt bằng mã.

## 2. Công nghệ

| Thành phần | Chi tiết |
|---|---|
| Nền tảng | .NET 8 (`net8.0-windows`), WinForms, `Nullable` bật, `ImplicitUsings` bật |
| Solution | `E-Tax.sln` → 1 project `E-Tax/E-Tax.csproj`, namespace gốc `E_Tax` |
| EPPlus 8.2.0 | Tạo file Excel (`ExcelPackage.License.SetNonCommercialPersonal(...)` trong ctor Form1) |
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
├── Form1.cs        (~2130 dòng) # Logic chính: login, search, export, download, print, notes, blacklist (chia #region)
├── Form1.Designer.cs (~1050)  # Layout UI (panelLogin, panelActivation, panelSearch, tabControlMain, ...)
├── DetailGridManager.cs (~390)# Lấy chi tiết hàng hóa của từng HĐ, đổ vào 4 lưới chi tiết; class InvoiceDetailItem
├── SearchResult.cs            # enum InvoiceType, SearchResult (+ HasLookupKeys(), FileIdentifier), SearchResponse
├── GdtApi.cs                  # Hằng URL/User-Agent; CreateRequest(...) set header chung; BuildDetailUrl(...)
├── InvoiceJson.cs             # JSON chi tiết HĐ: Options, DetailExportColumns (20 cột Excel), FindProductArray (hdhhdvu), BuildDetailRows
├── GridColumnHelper.cs        # Style + tạo cột text/số cho DataGridView cấu hình bằng code
├── Logger.cs                  # Ghi log ra file theo ngày (%LocalAppData%/E-Tax/logs)
├── NotesStore.cs              # Ghi chú theo Id HĐ (%AppData%/E-Tax/notes.json)
├── LicenseManager.cs          # static LicenseManager + enum LicenseStatus
├── ProcessingForm.cs          # Form "đang xử lý" (ControlBox=false), có UpdateStatus(string)
├── TraCuuForm.cs              # Popup hiển thị mã tra cứu + link tra cứu NCC hóa đơn + thông tin người bán
└── Properties/Resources.*     # Resource mặc định
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
| `query/invoices/detail` | GET | `GetInvoiceDetailAsync` (Form1), `GetInvoiceDetailInternalAsync` (DetailGridManager) | URL từ `GdtApi.BuildDetailUrl` (+ `page,size` trong DetailGridManager) → mảng hàng hóa `hdhhdvu` (`InvoiceJson.FindProductArray`) |
| `query/invoices/export-xml` | GET | `DownloadSingleInvoiceZipAsync` | Trả file ZIP (XML + HTML/PDF thể hiện) |

Query string tạo bởi `BuildSearchQuery(from, to, type, page, size)`:
`size={size}&page={page}&sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=dd/MM/yyyyTHH:mm:ss;tdlap=le=...[;ttxly==5]`

Giới hạn API (được code xử lý): **tối đa 30 ngày/lần** (hằng `SearchChunkDays = 30`) và **50 bản ghi/trang** (`SearchPageSize = 50`, lặp trang đến khi < 50).

API ngoài: `BlacklistApiUrl?tax_code=...&token=BlacklistApiToken` (hằng trong Form1; kiểm tra DN rủi ro, `btnKtraDNMuaLe_Click`).

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
`Form1()` → tạo `HttpClient` (CookieContainer), `DetailGridManager`, cấu hình lưới giảm thuế.
`Form1_Load` → `CheckForLicense()` → `LoadNotesFromFile()` → `LoadCaptchaAsync()`.

`LicenseManager` (Registry `HKCU\Software\ETaxDemoApp`):
- `IsActivated == "true"` → `Activated`
- `TrialExpiryDate` (ISO "o") còn hạn → `ValidTrial`; hết → `Expired` (hiện `panelActivation`, khóa `panelLogin`)
- Chưa có key → `InitializeTrial()` +30 ngày
- `Activate(key)` so sánh với hằng `MasterActivationKey` (hard-code trong `LicenseManager.cs`)

### 5.2 Đăng nhập
`btnLogin_Click` → `LoginAsync` → lưu `jwtToken`, set header Bearer, gọi `_detailGridManager.SetJwtToken`. Ẩn `panelLogin`, hiện `panelSearch`. Nút `btnRefreshCaptcha` tải lại captcha.

### 5.3 Tìm kiếm (`btnLeftSearch_Click`) — chức năng trung tâm
Input: `dtpFromDate`, `dtpToDate`, radio `rbSold` / `rbBought` / `rbAllInvoices`.
1. `SearchInvoicesAsync(from, to, includeSold, includeBought, reportStatus)` — **dùng chung** với `SaveOriginalInvoicesAsync`:
   chia lát 30 ngày; mỗi lát lặp trang (size 50) cho Bán ra / Mua vào độc lập (`FetchSearchPageAsync` trả false khi hết trang hoặc lỗi).
2. Gắn `Loai_hoa_don = "Bán ra"/"Mua vào"`, dedupe theo `Id`, sort `Ngay_lap` desc, `So_hoa_don` asc → `_latestResults`. Trang lỗi gom vào `FailedChunks`.
3. `BindMainGrid()` bind `dgvMain` (tab **Tổng hợp**); bind `dgvGiamThue` (tab có text **"VAT nộp"**).
4. `DetailGridManager.PopulateDetailGridAsync` → gọi `detail` cho **từng** HĐ (delay 500ms/HĐ) → bind:
   - `dgvDetails` (tab **Chi tiết**) — tất cả dòng hàng
   - `dgvMua` (tab **Bk mua**) — dòng của HĐ "Mua vào"
   - `dgvBan` (tab **BK bán**) — còn lại
   - `dgvVatNop` (tab có text **"Bảng kê Thuế"**) — tất cả, ít cột
5. Hiển thị `ProcessingForm` trong lúc chạy; `this.Enabled = false`.

> Lưu ý tên: `tabVATNop` hiển thị "Bảng kê Thuế" (chứa `dgvVatNop`), còn `tabGiamThue` hiển thị "VAT nộp" (chứa `dgvGiamThue`). Tên biến và text bị lệch nhau.

### 5.4 Các nút thao tác trên kết quả
| Nút | Hàm | Làm gì |
|---|---|---|
| Tải danh sách HĐ (`btnExportDS`) | `ExportSearchResultsToExcelAsync(_latestResults)` | Excel 16 cột từ danh sách đã có |
| Tải chi tiết HĐ (`btnExportChiTiet`) | `InvoiceJson.BuildDetailRows` + `SetDetailCellValue` | Excel hàng hóa của **1** HĐ đang chọn |
| Xem HĐ (`btnXemHD`) | `DownloadSingleInvoiceZipAsync` → giải nén temp → mở HTML/PDF bằng app mặc định | |
| In pdf (`btnCnKoPdf`) | handler + `PrintPdfFilesAsync` / `SavePdfFilesToFolder` | Với các dòng đang chọn: tải ZIP → lấy PDF hoặc `ConvertHtmlToPdfAsync` → có máy in thì `Verb="print"`, không thì chọn thư mục lưu PDF |
| Tải tất cả HĐ Gốc (`btnTaiHDGoc`) | `SaveOriginalInvoicesAsync` | **Tự search lại** theo ngày (`SearchInvoicesAsync`) → Excel DS + Excel chi tiết + tải ZIP từng HĐ + PDF + XML → nén 1 file `E-Tax_Export_{from}_den_{to}.zip` (thư mục con `0_BaoCaoExcel`, `1_HoaDonGoc_ZIP`, `2_HoaDonPDF`, `3_HoaDonXML`) |
| Cột "Tải" trong `dgvMain` (`colDgvTraCuu`) | `dgvMain_CellContentClick` → `BuildTraCuuLink` → `TraCuuForm` | Lấy `mhdon` (mã tra cứu) + `ngcnhat` (NCC: tvan_misa/bkav/vnpt/viettel/thaison) → link tra cứu |
| Tìm kiếm MST ng bán (`btnRightSearch`) | lọc `_latestResults` theo `Ma_so_thue.Contains` | Chỉ lọc `dgvMain` |
| Ghi chú (`btnMoGhiChu`) | `NotesStore` → `%AppData%/E-Tax/notes.json` (lần đầu tự chép `notes.json` cũ cạnh exe) | Tự load khi đổi dòng (`dgvMain_SelectionChanged`) |
| Ktra DN Mua Lẻ (`btnKtraDNMuaLe`) | gọi API blacklist metaecom | MST lấy từ `txtKtraMaDoanhNghiep` (tự điền khi chọn dòng) |

### 5.5 Tiện ích
- `AppendLog(string)` → `Logger.Write` ghi file `%LocalAppData%/E-Tax/logs/etax_yyyyMMdd.log` (giữ 30 ngày, không ném lỗi). DetailGridManager cũng log qua delegate này. **Không log token/mật khẩu.**
- `EnsureChromiumAsync()` tải Chromium lần đầu và **cache đường dẫn** (`_chromiumExecutablePath`); `ConvertHtmlToPdfAsync(html, pdf)` launch Puppeteer mỗi lần gọi (A4, Networkidle0, timeout 60s).
- `DownloadSingleInvoiceZipAsync(invoice, dir)` trả đường dẫn `HD_{FileIdentifier}.zip` hoặc `null` khi lỗi.
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
1. **Mã kích hoạt master hard-code** trong `LicenseManager.cs` — decompile .NET là lấy được ngay. Trial lưu ở HKCU nên xóa key Registry là có thêm 30 ngày. Nên chuyển sang kích hoạt online/ký số (license server metaecom đã có sẵn).
2. **API token blacklist hard-code** trong `btnKtraDNMuaLe_Click` và gửi qua query string. Nên đưa qua server trung gian hoặc ít nhất ra file cấu hình.
3. **EPPlus 8 dùng license NonCommercialPersonal** (`"Your Name"`) trong khi app có bán bản quyền → vi phạm điều khoản EPPlus (Polyform Noncommercial). Cần mua license thương mại hoặc chuyển sang **ClosedXML** (MIT).
4. Mật khẩu người dùng bị `Trim()` trước khi gửi (mật khẩu có khoảng trắng đầu/cuối sẽ sai).

### Bug / hành vi sai

### Hiệu năng
- Chi tiết HĐ gọi tuần tự + `Task.Delay(500)` mỗi HĐ (DetailGridManager) → 1000 HĐ ≈ >8 phút chỉ riêng delay. Tìm kiếm luôn kéo chi tiết toàn bộ dù người dùng chỉ cần danh sách.
- `SaveOriginalInvoicesAsync` gọi lại `detail` cho mọi HĐ (lần 2) để làm Excel chi tiết thay vì dùng dữ liệu đã có.
- Puppeteer **launch 1 Chromium mới cho mỗi HĐ** cần convert; Chromium (~150MB) tải lúc runtime (cần Internet, lần đầu rất chậm).

### Chất lượng code / bảo trì
- `Form1.cs` vẫn là God class (~2.1k dòng, đã chia `#region`); `SaveOriginalInvoicesAsync`, `btnCnKoPdf_Click` vẫn dài.
- ✅ Đã gộp (2026-11-16): vòng search → `SearchInvoicesAsync`; tìm `hdhhdvu` → `InvoiceJson.FindProductArray`; cột Excel chi tiết → `InvoiceJson.DetailExportColumns`; header request → `GdtApi.CreateRequest`; HTML→PDF → `ConvertHtmlToPdfAsync`; kiểm tra 4 khóa HĐ → `SearchResult.HasLookupKeys()`.
- ✅ Đã xóa code chết: `JObject.cs`, `Models/InvoiceDetai.cs`, `Resources/login.html`, `DownloadInvoiceListExcelAsync`, `GetDaysRemaining`, các khối comment cũ, handler rỗng, field Designer không dùng, package ClosedXML/WebView2.
- 43 warning (chủ yếu nullable; trước khi dọn là 97) — `Nullable` bật nhưng model chưa khai báo nullable.
- Quy ước lỗi bằng chuỗi `"❌..."` dễ vỡ; nên dùng `Result`/exception.
- Không có unit test; URL/hằng số hard-code rải rác.

### Đề xuất refactor theo thứ tự ưu tiên
1. Sửa license EPPlus (hoặc chuyển ClosedXML), bỏ secret hard-code, cơ chế kích hoạt an toàn hơn.
2. ✅ (2026-11-16) Ghi log ra file; sửa thông báo in và thông báo hoàn tất tìm kiếm.
3. Tách lớp: `GdtApiClient` (captcha/login/search/detail/download + header chung), `InvoiceSearchService` (chia lát 30 ngày + phân trang, dùng chung cho Search & Tải gốc), `ExcelExporter`, `PdfService` (dùng chung 1 browser Puppeteer), `NotesStore`.
4. Cache chi tiết HĐ đã lấy (theo `Id`) để Export/Tải gốc không gọi lại API; cân nhắc gọi song song có giới hạn (SemaphoreSlim 3–5) thay vì delay cố định.
5. ✅ (2026-11-16) Đã thêm `Loai_hoa_don` thay vì ghi đè `Thong_tin_lien_quan`.
6. Dọn warning nullable còn lại.

## 7. Ghi chú khi sửa code
- Thêm cột cho lưới chi tiết: sửa `InvoiceDetailItem` + `ConfigureFullDetailGrid`/`ConfigureVatNopGrid` + `CreateDetailItem` (DetailGridManager.cs).
- Thêm cột cho danh sách: `SearchResult` (SearchResult.cs) + cột `dgvMain` trong Designer (tên cột dạng `colDgv*`, `AutoGenerateColumns=false`) + `ExportSearchResultsToExcelAsync`.
- Cột Excel chi tiết: chỉ sửa `InvoiceJson.DetailExportColumns` (dùng chung cho Excel 1 HĐ và Excel tổng hợp); cột số → `InvoiceJson.IsNumericColumn`.
- Logic search: chỉ sửa `SearchInvoicesAsync` / `FetchSearchPageAsync`.
- Thêm NCC tra cứu: `BuildTraCuuLink` (switch theo `ngcnhat` lowercase).
- Đổi điều kiện lọc HĐ mua vào: `BuildSearchQuery` (`;ttxly==5`).
- Gọi API mới: dùng `GdtApi.CreateRequest`; hằng URL đặt trong `GdtApi`.
