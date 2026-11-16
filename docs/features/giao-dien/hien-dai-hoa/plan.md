# Plan — Hiện đại hóa giao diện

## Tổng quan

- **Mục tiêu:** giao diện E-Tax dễ dùng và hiện đại hơn cho kế toán làm việc với bảng nhiều cột. Bắt đầu bằng những thay đổi thấy rõ nhất mà ít động vào logic nghiệp vụ.
- **Module:** `giao-dien` (Form1 và các DataGridView)
- **Chức năng con:** không tách. CR-001 có 6 task, cùng một màn hình, làm và test liên tiếp được. Các nhóm gợi ý sau (thanh công cụ, bộ lọc, màn hình đăng nhập dạng thẻ, toast, màn hình chờ có nút Hủy) sẽ là các CR tiếp theo trong folder này.

## Yêu cầu

### CR-001 — Nhóm 1: cửa sổ co giãn, bảng hiện đại, thanh trạng thái · 2026-11-16

**Mô tả yêu cầu (nguyên văn từ người dùng):**
> bạn có gợi ý nào để cải thiện giao diện cho đẹp mắt và hiện đại hơn không sau bản update vừa rồi ? → bạn hãy thực hiện đi

(Thực hiện Nhóm 1 theo đề xuất đã gửi: mục 1–3.)

**Phạm vi**
- Trong phạm vi:
  - Cửa sổ phóng to / kéo giãn được, có kích thước tối thiểu; bảng và tab co giãn theo; panel đăng nhập luôn ở giữa.
  - Nhớ vị trí, kích thước và trạng thái phóng to giữa các lần mở; tự về giữa màn hình chính nếu vị trí cũ nằm ngoài các màn hình hiện có.
  - Style chung cho cả 6 bảng: tiêu đề xanh `#0075D6` chữ trắng, dòng 28px, chỉ kẻ ngang, màu chọn xanh nhạt, số căn phải.
  - Thanh trạng thái ở đáy: số HĐ và tổng thanh toán theo Bán ra / Mua vào, tài khoản đang đăng nhập, trạng thái bản quyền.
  - Chế độ demo **chỉ trong bản Debug** (biến môi trường `ETAX_UI_DEMO=1`) để chụp màn hình chính không cần đăng nhập cổng thuế.
- Ngoài phạm vi: gom nút thành thanh công cụ, bộ lọc một hàng, đổi tên tab, màn hình đăng nhập dạng thẻ, DPI cao, toast, nút Hủy (các CR sau); dark mode; đổi thư viện UI.

**Cách làm:** chỉ dùng WinForms có sẵn, không thêm thư viện. Style tập trung trong một lớp `UiTheme` để các CR sau dùng lại. Logic thuần (lưu cửa sổ, tính tổng) tách ra lớp riêng và viết test trước (TDD). Phần Designer chỉ đổi thuộc tính (Anchor / FormBorderStyle / MinimumSize) và thêm một `StatusStrip`, không sắp xếp lại vị trí control.

**Task**

#### T-001.1 — Lưu / khôi phục vị trí cửa sổ (TDD)
- File: tạo `E-Tax/WindowSettings.cs`, `E-Tax.Tests/WindowSettingsTests.cs`
- Các bước:
  1. Viết test trước, chạy và thấy fail: `Load_MissingFile_ReturnsNull`, `Load_CorruptJson_ReturnsNull`, `SaveThenLoad_RoundTrips`, `Fit_OffScreen_ReturnsNull`, `Fit_TooSmall_ClampsToMinimum`.
  2. `record WindowSettings(int X, int Y, int Width, int Height, bool Maximized)`:
     - `static WindowSettings? Load(string path)`: không bao giờ ném lỗi.
     - `void Save(string path)`: tự tạo thư mục.
     - `static Rectangle? Fit(WindowSettings s, IReadOnlyList<Rectangle> workingAreas, Size minimum)`: trả null khi cửa sổ không giao với màn hình nào.
     - `DefaultPath = %AppData%\E-Tax\window.json`.
  3. Chạy test, thấy pass.
- Xong khi: 5 test pass, `dotnet test` xanh.

#### T-001.2 — Cửa sổ co giãn
- File: `E-Tax/Form1.Designer.cs`, `E-Tax/Form1.cs`
- Các bước:
  1. Đổi thuộc tính:
     - `FormBorderStyle = Sizable`, `MaximizeBox = true`, `MinimumSize = 1000×725` (bằng kích thước hiện tại).
     - `panelSearch.Anchor` = cả 4 cạnh.
     - Kiểm tra `gbNguoiBan` (đã neo Top|Right), `tabControlMain` (đã neo 4 cạnh), thanh tiến độ và nhãn trạng thái tải.
  2. `Form1_Resize`: căn giữa `panelLogin`.
  3. `Form1_Load` áp dụng `WindowSettings.Fit(...)`; `FormClosing` lưu `RestoreBounds` và cờ Maximized.
- Xong khi: build xanh; TC-01..TC-04 đạt.

#### T-001.3 — Style bảng hiện đại
- File: tạo `E-Tax/UiTheme.cs`; sửa `E-Tax/GridColumnHelper.cs`, `E-Tax/Form1.cs` (constructor); test `E-Tax.Tests/UiThemeTests.cs`
- Các bước:
  1. Viết test `StyleGrid_AppliesTheme` trước (màu tiêu đề, `EnableHeadersVisualStyles = false`, chiều cao dòng 28, `CellBorderStyle = SingleHorizontal`); chạy và thấy fail.
  2. `UiTheme`:
     - Hằng màu: `Accent #0075D6`, `HeaderText` trắng, `GridLine #E5E7EB`, `RowAlt #F8FAFC`, `Selection #DBEAFE`, `SelectionText #1D2330`.
     - `static void StyleGrid(DataGridView dgv)`: tiêu đề cao 34px, không cho kéo cao tiêu đề; nền trắng; `BorderStyle = None`; padding ô 6px; font Segoe UI 9.
  3. `GridColumnHelper.ApplyDefaultStyle` gọi `UiTheme.StyleGrid` (bỏ màu xen kẽ cũ). Constructor Form1 áp dụng cho `dgvMain`, `dgvDetails`, `dgvMua`, `dgvBan`, `dgvVatNop`, `dgvGiamThue`.
  4. Rà các cột tiền trong `dgvMain` (Designer): căn phải, định dạng `N0`; cố định (Frozen) cột Ký hiệu HĐ và Số HĐ nếu thứ tự cột cho phép.
- Xong khi: test pass; TC-05 đạt.

#### T-001.4 — Thanh trạng thái (TDD cho phần tính)
- File: tạo `E-Tax/InvoiceSummary.cs`, `E-Tax.Tests/InvoiceSummaryTests.cs`; sửa `Form1.Designer.cs` (thêm `StatusStrip statusBar` docked Bottom) và `Form1.cs`
- Các bước:
  1. Viết test trước, chạy và thấy fail: `Compute_Empty`, `Compute_SplitsByType` (Bán ra / Mua vào), `Compute_NullTotalsCountAsZero`, `Format_UsesThousandSeparators` (kiểu `1.250.000`).
  2. `record InvoiceSummary(int SoldCount, decimal SoldTotal, int BoughtCount, decimal BoughtTotal)`:
     - `static InvoiceSummary Compute(IEnumerable<SearchResult>)`: tổng theo `Tong_tien_thanh_toan`.
     - `string Format()` → `"Bán ra: 80 HĐ · 1.250.000.000 ₫   |   Mua vào: 43 HĐ · 640.000.000 ₫"`; khi rỗng → `"Chưa có dữ liệu"`.
  3. `StatusStrip` gồm 3 ô:
     - `lblStatusInvoices` (Spring, căn trái).
     - `lblStatusUser` (`👤 <tài khoản>`).
     - `lblStatusLicense` (Đầy đủ / Dùng thử còn N ngày / Hết hạn, có màu).
  4. Cập nhật thanh trạng thái:
     - `BindMainGrid` → `lblStatusInvoices`.
     - Đăng nhập thành công → `lblStatusUser`.
     - `CheckForLicense` → `lblStatusLicense`.
  5. `panelSearch`/`panelLogin` không bị StatusStrip che: ClientSize tăng thêm chiều cao StatusStrip.
- Xong khi: test pass; TC-06 đạt.

#### T-001.5 — Chế độ demo cho chụp màn hình (chỉ Debug)
- File: `E-Tax/Form1.cs` (khối `#if DEBUG`)
- Các bước:
  1. Nếu `ETAX_UI_DEMO=1`:
     - Bỏ qua đăng nhập, hiện `panelSearch`.
     - Nạp 60 HĐ mẫu vào `_latestResults`, gọi `BindMainGrid()` và gán `dgvGiamThue`.
     - Không gọi API.
  2. Kiểm tra bản Release không chứa đoạn này: build Release, tìm chuỗi `ETAX_UI_DEMO` trong dll, phải không có.
- Xong khi: chạy Debug với biến môi trường thấy màn hình chính có dữ liệu; Release không có chuỗi.

#### T-001.6 — Kiểm thử tổng thể và cập nhật tài liệu
- File: `docs/features/giao-dien/hien-dai-hoa/*`, `PROJECT_OVERVIEW.md`
- Các bước:
  1. Chạy TC-01..TC-07, chụp ảnh vào `screenshots/`.
  2. Cập nhật `PROJECT_OVERVIEW.md` (UiTheme, WindowSettings, InvoiceSummary, StatusStrip, chế độ demo).
  3. Cập nhật index.html (ảnh thật) và `status.md`.
- Xong khi: mọi TC đạt, `dotnet build` 0 lỗi, `dotnet test` xanh.

**Tiêu chí nghiệm thu**
- [ ] Cửa sổ phóng to / kéo giãn được; không control nào bị che hay lệch ở kích thước tối thiểu và khi phóng to.
- [ ] Mở lại app thấy đúng vị trí, kích thước, trạng thái phóng to lần trước; vị trí ngoài màn hình thì về giữa.
- [ ] Cả 6 bảng cùng một style mới; số tiền căn phải, có dấu phân cách.
- [ ] Thanh trạng thái hiển thị đúng số HĐ, tổng theo loại, tài khoản, bản quyền.
- [ ] Mọi tính năng cũ (tìm kiếm, xuất Excel, tải HĐ gốc, ghi chú, kích hoạt) không đổi hành vi.
- [ ] `dotnet build E-Tax.sln` 0 lỗi; `dotnet test E-Tax.Tests` xanh.

**Kịch bản test**

App là WinForms nên không chạy được bằng Playwright. Giao diện thật được kiểm tra bằng ảnh chụp cửa sổ (PowerShell) lưu vào `screenshots/`; Playwright MCP chỉ dùng cho `index.html`.

| ID | Kịch bản | Kết quả mong đợi |
|---|---|---|
| TC-01 | Mở app (màn hình đăng nhập), kéo cửa sổ to/nhỏ, phóng to | Panel đăng nhập luôn ở giữa; không kéo nhỏ hơn 1000×725 |
| TC-02 | Debug + `ETAX_UI_DEMO=1`, phóng to | Bảng chiếm hết phần còn lại; nhóm "Người bán" dính góc phải; thanh trạng thái ở đáy |
| TC-03 | Kéo cửa sổ sang vị trí/kích thước khác, đóng, mở lại; lặp lại khi đang phóng to | Mở lại đúng vị trí/kích thước; lần 2 mở ở trạng thái phóng to |
| TC-04 | Sửa `window.json` thành X=5000,Y=5000 rồi mở | Cửa sổ về giữa màn hình chính |
| TC-05 | Chế độ demo: xem bảng Tổng hợp, chọn một dòng | Tiêu đề xanh chữ trắng; dòng 28px; kẻ ngang; dòng chọn nền xanh nhạt; cột tiền căn phải `1.250.000` |
| TC-06 | Chế độ demo: xem thanh trạng thái; màn hình đăng nhập | Demo: "Bán ra: 30 HĐ · … ₫ \| Mua vào: 30 HĐ · … ₫", tài khoản demo, bản quyền; đăng nhập: "Chưa có dữ liệu" |
| TC-07 | Playwright MCP mở `index.html` | Hiển thị đủ 3 tab, không lỗi console |

**Rủi ro / câu hỏi mở**
- Chưa có ảnh màn hình chính thật (cần đăng nhập cổng thuế). Chế độ demo T-001.5 giải quyết việc này mà không đưa vào bản phát hành.
- Thanh tiến độ `downloadProgressBar` / `lblDownloadStatus` / `pictureBox2` nằm trong `panelSearch` ở toạ độ cố định. Khi co giãn cần kiểm tra chúng không bị bảng che. Nếu bị che, chuyển tiến độ xuống thanh trạng thái (ghi Ruling trong `status.md`).
- Tổng tiền gộp Bán ra + Mua vào không có nghĩa kế toán, nên tách theo loại. Chưa trừ HĐ bị hủy/điều chỉnh: thanh trạng thái chỉ để tham khảo, ghi rõ trong tooltip.
