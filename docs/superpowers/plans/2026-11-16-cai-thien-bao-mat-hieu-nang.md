# Cải thiện bảo mật, lỗi chức năng và hiệu năng — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sửa các vấn đề còn lại trong `PROJECT_OVERVIEW.md` §6: lỗi nhỏ (mật khẩu bị Trim, notes.json), hiệu năng (lấy chi tiết HĐ, Puppeteer), bảo mật/pháp lý (token blacklist, license EPPlus, mã kích hoạt).

**Architecture:** Tách các phần có thể kiểm thử ra khỏi `Form1.cs` thành lớp nhỏ (`NotesStore`, `InvoiceDetailService`, `PdfConverter`, `AppSettings`, `ExcelExporter`, `LicenseKey`), Form1 chỉ gọi vào. Thêm project test xUnit cho các lớp không phụ thuộc UI.

**Tech Stack:** .NET 8 WinForms (`net8.0-windows`), System.Text.Json, PuppeteerSharp 20.2.4, xUnit; ClosedXML (nếu duyệt D1), `System.Security.Cryptography.ECDsa` (nếu duyệt D3).

**Spec:** `PROJECT_OVERVIEW.md` §6 (Đánh giá) + các quyết định D1–D4 dưới đây.

## Quyết định cần duyệt trước khi làm

| # | Vấn đề | Phương án | Đề xuất |
|---|---|---|---|
| D1 | EPPlus 8 đang dùng license *NonCommercialPersonal* trong app bán bản quyền | (a) Mua license EPPlus thương mại — chỉ đổi 1 dòng code; (b) Chuyển sang **ClosedXML** (MIT, miễn phí) — viết lại 3 hàm xuất Excel | **(b)** — nhu cầu Excel đơn giản, bỏ hẳn chi phí license. Chọn (a) thì bỏ Task 7, chỉ đọc key từ cấu hình. |
| D2 | Token API blacklist nằm trong code (và trong lịch sử git) | (a) Đưa ra file `appsettings.json` không commit; (b) Gọi qua server trung gian metaecom, app gửi kèm license | **(a) ngay + đổi (rotate) token phía server metaecom** vì token cũ đã lộ trong git. App desktop luôn có thể bị đọc token — (b) mới triệt để nhưng cần sửa server, ngoài repo này. |
| D3 | Mã kích hoạt master dùng chung, nằm trong code | (a) Kích hoạt online (cần API server); (b) **Key ký số offline**: key = dữ liệu + chữ ký ECDSA, gắn với mã máy; app chỉ chứa public key; tool KeyGen riêng giữ private key | **(b)** — không phụ thuộc server. Máy đã kích hoạt bằng cách cũ (`IsActivated=true`) **vẫn được giữ** để khách hiện tại không bị ảnh hưởng. |
| D4 | Lấy chi tiết HĐ tuần tự + chờ 500ms/HĐ | Gọi song song tối đa **3** request, bỏ delay cố định, tự thử lại khi 429/5xx | Đồng ý mức 3 (an toàn với giới hạn cổng thuế; có thể chỉnh 1 hằng số). |

## Global Constraints

- Giữ `net8.0-windows`, namespace `E_Tax`; chuỗi hiển thị tiếng Việt.
- Không ghi token, mật khẩu, private key vào log hoặc git.
- Build `dotnet build E-Tax.sln`: 0 error, warning không vượt 43.
- `dotnet test` xanh sau mỗi task có test.
- Mỗi task 1 commit trên nhánh `refactor/cleanup-core`; cập nhật `PROJECT_OVERVIEW.md` trong cùng commit nếu cấu trúc/hành vi đổi.
- Task 0: commit 4 bản sửa đang chờ (thông báo in, "Hoàn tất", Logger, `Loai_hoa_don`) trước khi bắt đầu.

## Review Focus

- Token hết hạn (401) giữa lúc tải chi tiết song song → không cache lỗi, không treo, báo HĐ lỗi. (Task 3: `GetDetailPages_ErrorNotCached`)
- Cổng thuế trả 429 → thử lại có giãn cách, không vượt 3 request đồng thời. (Task 3: `Retries_On429`, `Concurrency_NeverExceedsLimit`)
- HĐ có > 50 dòng hàng → đủ mọi trang, ở cả lưới và Excel. (Task 3: `GetDetailPages_FollowsPaging`)
- App cài trong `Program Files` (thư mục chỉ đọc) có `notes.json` cũ → ghi chú vẫn còn, ghi được. (Task 2: `Load_MigratesLegacyFile`)
- Key bị sửa 1 ký tự / dùng trên máy khác / hết hạn → từ chối. (Task 8: các test `Verify_*`)

---

### Task 1: Không Trim mật khẩu khi đăng nhập

**Files:** Modify `E-Tax/Form1.cs:187`

- [ ] **Step 1:** `string password = txtPass.Text;` (giữ Trim cho username, captcha).
- [ ] **Step 2:** Build — 0 error.
- [ ] **Step 3:** Commit `fix: không cắt khoảng trắng mật khẩu khi đăng nhập`.

### Task 2: NotesStore — ghi chú lưu ở %AppData%

**Files:**
- Create: `E-Tax/NotesStore.cs`, `E-Tax.Tests/E-Tax.Tests.csproj`, `E-Tax.Tests/NotesStoreTests.cs`
- Modify: `E-Tax.sln` (thêm project test), `E-Tax/E-Tax.csproj` (`InternalsVisibleTo E-Tax.Tests`), `E-Tax/Form1.cs` (#region Ghi chú, field `_notesFilePath`, `_invoiceNotes`)

**Interfaces — Produces:**
- `internal sealed class NotesStore(string filePath, string? legacyFilePath)`
- `void Load()` · `string Get(string invoiceId)` (trả "" nếu không có) · `void Set(string invoiceId, string note)` · `void Save()` (ném exception khi lỗi ghi để Form1 hiện MessageBox như hiện tại)
- `static string DefaultPath` = `%AppData%\E-Tax\notes.json`

- [ ] **Step 1: Tạo project test** xUnit `net8.0-windows`, tham chiếu `E-Tax.csproj`, thêm vào sln.
- [ ] **Step 2: Viết test (fail)** — mỗi test dùng thư mục tạm riêng:
  - `Load_MigratesLegacyFile`: chỉ có legacy `{"a":"x"}` → `Get("a") == "x"` và file mới tồn tại.
  - `Load_PrefersNewFile`: cả hai tồn tại, khác nội dung → lấy file mới.
  - `Load_CorruptJson_ReturnsEmpty`: file mới = `"{oops"` → `Get("a") == ""`, không ném.
  - `Save_CreatesDirectory`: thư mục chưa có → `Set`+`Save` → đọc lại được.
- [ ] **Step 3:** `dotnet test` → FAIL (chưa có `NotesStore`).
- [ ] **Step 4:** Implement; Form1 dùng `new NotesStore(NotesStore.DefaultPath, Path.Combine(Application.StartupPath, "notes.json"))`. Legacy chỉ đọc, không xóa.
- [ ] **Step 5:** `dotnet test` PASS; build OK.
- [ ] **Step 6:** Commit `feat: lưu ghi chú vào AppData, tự chuyển file cũ`.

### Task 3: InvoiceDetailService — cache + gọi song song có giới hạn

**Files:**
- Create: `E-Tax/InvoiceDetailService.cs`, `E-Tax.Tests/InvoiceDetailServiceTests.cs`, `E-Tax.Tests/FakeHttpHandler.cs`
- Modify: `E-Tax/DetailGridManager.cs` (bỏ `GetInvoiceDetailInternalAsync`, vòng phân trang, `Task.Delay(500/100)`), `E-Tax/Form1.cs` (tạo service, `ClearCache()` đầu `btnLeftSearch_Click`)

**Interfaces — Produces:**
- `internal sealed class InvoiceDetailService(HttpClient client, Func<string> getJwtToken, Action<string> log)`
- `const int ProductPageSize = 50; const int MaxConcurrency = 3;`
- `Task<IReadOnlyList<string>?> GetDetailPagesAsync(SearchResult invoice)` — JSON từng trang (`GdtApi.BuildDetailUrl(..., page, ProductPageSize)`), dừng khi trang có < 50 sản phẩm hoặc rỗng; `null` nếu trang đầu lỗi; cache theo `invoice.Id`, **không** cache khi lỗi.
- `Task PrefetchAsync(IReadOnlyList<SearchResult> invoices, Action<int> onCompleted)` — chạy `GetDetailPagesAsync` với `SemaphoreSlim(MaxConcurrency)`; `onCompleted(n)` báo số HĐ đã xong (gọi về UI thread qua `SynchronizationContext` hiện tại).
- Thử lại: 429/5xx/`HttpRequestException` → tối đa 2 lần, chờ 1s rồi 2s.
- `void ClearCache()`
- DetailGridManager nhận service qua constructor (thay `HttpClient`), gọi `PrefetchAsync` rồi dựng `InvoiceDetailItem` **theo thứ tự danh sách** (giữ STT ổn định).

- [ ] **Step 1: Viết test (fail)** với `FakeHttpHandler` (đếm request, trả JSON theo URL, có thể trễ):
  - `GetDetailPages_CachesById`: gọi 2 lần → handler nhận 1 request.
  - `GetDetailPages_FollowsPaging`: trang 0 có 50 SP, trang 1 có 3 → trả 2 trang, 2 request.
  - `GetDetailPages_ErrorNotCached`: lần 1 trả 401 → `null`; lần 2 trả 200 → có dữ liệu.
  - `Retries_On429`: 429, 429, 200 → thành công sau 3 request.
  - `Concurrency_NeverExceedsLimit`: 10 HĐ, handler trễ 50ms ghi số request đồng thời tối đa → `<= 3`.
- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement service; nối vào DetailGridManager và Form1. Thời gian chờ retry nhận qua tham số tùy chọn `TimeSpan[]? retryDelays` để test chạy nhanh.
- [ ] **Step 4:** `dotnet test` PASS; build OK.
- [ ] **Step 5: Chạy thử thật:** tìm 1 tháng có ≥ 50 HĐ — 4 lưới chi tiết đủ dòng, STT liên tục, thời gian giảm rõ so với trước.
- [ ] **Step 6:** Commit `perf: cache chi tiết HĐ và tải song song tối đa 3 request`.

### Task 4: Excel chi tiết + Tra cứu dùng InvoiceDetailService

**Files:** Modify `E-Tax/Form1.cs` (`ExportInvoiceDetailsToExcelAsync`, `btnExportChiTiet_Click`, `dgvMain_CellContentClick`; xóa `GetInvoiceDetailAsync`)

**Interfaces — Consumes:** `GetDetailPagesAsync`, `PrefetchAsync` (Task 3).

- [ ] **Step 1:** Excel tổng hợp: `PrefetchAsync` toàn bộ danh sách rồi `InvoiceJson.BuildDetailRows` cho **mọi trang**; bỏ `Task.Delay(150)`. Excel 1 HĐ cũng gộp mọi trang. Tra cứu dùng trang đầu.
- [ ] **Step 2:** Build OK; `dotnet test` PASS.
- [ ] **Step 3: Chạy thử:** Tìm kiếm → "Tải tất cả HĐ Gốc" cùng khoảng ngày: bước 3 (Excel chi tiết) gần như tức thì (dùng cache); file Excel đủ dòng như lưới "Chi tiết".
- [ ] **Step 4:** Commit `perf: Excel chi tiết dùng lại dữ liệu đã tải`.

### Task 5: PdfConverter — 1 trình duyệt cho cả lô

**Files:** Create `E-Tax/PdfConverter.cs`; Modify `E-Tax/Form1.cs` (`ConvertHtmlToPdfAsync`, `EnsureChromiumAsync`, `SaveOriginalInvoicesAsync`, `btnCnKoPdf_Click`)

**Interfaces — Produces:** `internal sealed class PdfConverter(Func<Task<string>> getChromiumPath) : IAsyncDisposable` · `Task ConvertAsync(string htmlPath, string pdfPath)` — launch browser lần đầu gọi, mỗi HĐ 1 page mới (A4, Networkidle0, timeout 60s như hiện tại); `DisposeAsync` đóng browser.

- [ ] **Step 1:** Implement; trong 2 hàm lô dùng `await using var pdf = new PdfConverter(EnsureChromiumAsync);` (chỉ launch khi thật sự có HTML cần convert). Xóa `ConvertHtmlToPdfAsync`.
- [ ] **Step 2:** Build OK.
- [ ] **Step 3: Chạy thử:** in/lưu PDF 5 HĐ chỉ có HTML → Task Manager chỉ thấy 1 nhóm tiến trình Chromium, đóng hết sau khi xong.
- [ ] **Step 4:** Commit `perf: dùng chung một trình duyệt khi convert PDF`.

### Task 6: AppSettings — token blacklist ra file cấu hình (D2)

**Files:** Create `E-Tax/AppSettings.cs`, `E-Tax/appsettings.example.json`, `.gitignore` (thêm `E-Tax/appsettings.json`), `E-Tax.Tests/AppSettingsTests.cs`; Modify `E-Tax/E-Tax.csproj` (copy `appsettings.json` ra output nếu tồn tại), `E-Tax/Form1.cs` (xóa `BlacklistApiToken`)

**Interfaces — Produces:** `internal sealed record AppSettings(string? BlacklistApiToken, string? EpplusLicenseKey)` · `static AppSettings Load(string path)` — file thiếu/hỏng → mọi giá trị `null`, không ném.

- [ ] **Step 1: Test (fail):** `Load_MissingFile_ReturnsNulls`, `Load_ReadsToken`, `Load_CorruptJson_ReturnsNulls`.
- [ ] **Step 2:** Implement; `btnKtraDNMuaLe_Click` khi token `null` → MessageBox "Chức năng kiểm tra doanh nghiệp chưa được cấu hình." và không gọi API.
- [ ] **Step 3:** `dotnet test` PASS; build OK; `git grep VpVC1x` chỉ còn trong lịch sử, không còn trong cây hiện tại.
- [ ] **Step 4:** Commit `security: chuyển token blacklist ra appsettings.json`. **Ghi chú cho chủ dự án:** đổi token phía server metaecom.

### Task 7: ExcelExporter dùng ClosedXML (D1 = b)

**Files:** Create `E-Tax/ExcelExporter.cs`, `E-Tax.Tests/ExcelExporterTests.cs`; Modify `E-Tax/E-Tax.csproj` (bỏ EPPlus, thêm ClosedXML bản ổn định mới nhất), `E-Tax/Form1.cs` (bỏ 3 hàm Excel, `SetDetailCellValue`, `SetThinBorders`, dòng `ExcelPackage.License...`)

**Interfaces — Produces (static, không phụ thuộc Form):**
- `void ExportSearchResults(IReadOnlyList<SearchResult> results, string filePath)` — giữ tiêu đề, 16 cột, header row 4, màu xen kẽ, `#,##0` cột 7–11.
- `void ExportDetailSummary(IReadOnlyList<Dictionary<string,string>> rows, string filePath)` — "CHI TIẾT HÓA ĐƠN", có STT; rỗng → sheet "Lỗi" như hiện tại.
- `void ExportSingleInvoiceDetail(IReadOnlyList<Dictionary<string,string>> rows, string filePath)` — "Chi tiết hàng hóa/dịch vụ".
- Cột số (`InvoiceJson.IsNumericColumn`) parse InvariantCulture → số, format `#,##0`, căn phải.

- [ ] **Step 1: Test (fail)** — xuất ra file tạm rồi mở lại bằng ClosedXML:
  - `SearchResults_HeaderAndValues`: ô A4 = "MÃ SỐ THUẾ", dòng 5 đúng MST, cột P = `Loai_hoa_don`.
  - `DetailSummary_NumericCells`: `thtien = "1234567.5"` → ô là số, format `#,##0`.
  - `DetailSummary_Empty_WritesErrorSheet`.
- [ ] **Step 2:** `dotnet test` FAIL → implement → PASS.
- [ ] **Step 3: Chạy thử:** mở 3 file Excel bằng Excel thật, so với file cũ (bố cục, định dạng số).
- [ ] **Step 4:** Commit `refactor: chuyển xuất Excel sang ClosedXML (MIT)`.

### Task 8: Key kích hoạt ký số (D3 = b)

**Files:**
- Create: `E-Tax/LicenseKey.cs`, `E-Tax/MachineId.cs`, `tools/E-Tax.KeyGen/E-Tax.KeyGen.csproj` + `Program.cs`, `E-Tax.Tests/LicenseKeyTests.cs`
- Modify: `E-Tax/LicenseManager.cs` (bỏ `MasterActivationKey`), `E-Tax/Form1.Designer.cs` + `Form1.cs` (panelActivation: hiện mã máy + nút "Sao chép"), `E-Tax.sln`, `.gitignore` (`*.private.pem`)

**Interfaces — Produces:**
- `static string MachineId.Get()` — 16 ký tự hex đầu của SHA-256(`HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`).
- Định dạng key: `Base64Url(payloadJson).Base64Url(signature)`; payload `{"machine":"…","customer":"…","expires":"yyyy-MM-dd"|null}`; chữ ký ECDSA P-256/SHA-256.
- `static LicenseKeyResult LicenseKey.Verify(string key, string machineId, DateTime nowUtc, string publicKeyPem)` → `enum { Valid, Invalid, WrongMachine, Expired }`.
- `LicenseManager.Activate(key)` lưu key vào registry (`LicenseKey`) khi `Valid`; `CheckLicense()` xác thực lại key mỗi lần mở. Registry cũ `IsActivated=true` vẫn trả `Activated`.
- KeyGen: `E-Tax.KeyGen init <privateKeyPath>` (tạo cặp khóa, in public key để dán vào `LicenseManager`) · `E-Tax.KeyGen issue <privateKeyPath> <machineId> <customer> [yyyy-MM-dd]`.

- [ ] **Step 1: Test (fail)** — sinh cặp khóa trong test:
  - `Verify_ValidKey` → `Valid`; `Verify_TamperedPayload` → `Invalid`; `Verify_OtherMachine` → `WrongMachine`; `Verify_Expired` → `Expired`; `Verify_Garbage` (`"abc"`, `""`) → `Invalid`.
- [ ] **Step 2:** `dotnet test` FAIL → implement → PASS.
- [ ] **Step 3:** Chủ dự án chạy `init`, giữ private key **ngoài repo**; dán public key vào code.
- [ ] **Step 4: Chạy thử:** xóa registry `IsActivated` trên máy test → hết hạn trial → nhập key sinh từ KeyGen cho mã máy hiển thị → "Kích hoạt thành công"; key của máy khác → báo không hợp lệ.
- [ ] **Step 5:** Commit `security: kích hoạt bằng key ký số gắn mã máy`.

### Task 9: Rà soát cuối

- [ ] Build 0 error, warning ≤ 43; `dotnet test` PASS.
- [ ] `PROJECT_OVERVIEW.md` §3, §5, §6, §7 khớp code (file mới, cách cấu hình, KeyGen, bỏ các mục đã sửa).
- [ ] Review toàn nhánh, rồi hỏi chủ dự án merge vào `master` / push.
