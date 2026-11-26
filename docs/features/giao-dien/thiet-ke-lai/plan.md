# Plan — Thiết kế lại giao diện (Phương án 3 — Ribbon)

Thiết kế: [design.md](design.md). Branch `feat/v1-desktop`. Kiểm tra: `verify.ps1` (cần tắt app), ảnh chụp chế độ demo vào `screenshots/`. Không gọi cổng thuế.

Điều kiện trước: commit 2 việc đang treo (quy tắc mua vào ≥ 5 triệu, duy trì đăng nhập) để diff giao diện tách riêng.

## CR-001 — Ribbon kiểu Office

| ID | Task | Kiểm thử |
|---|---|---|
| T-001.1 | **Lưới an toàn (TDD)**: test khởi tạo `Form1` (STA) xác nhận mọi control nghiệp vụ còn tồn tại, còn gắn đúng handler `Click`/`CheckedChanged`, và đều nằm trong `panelSearch`/`panelLogin`. Chạy xanh trên layout cũ trước khi sửa | `Form1LayoutTests` |
| T-001.2 | **UiTheme mới (TDD)**: token màu/font/kích thước theo design; `StyleButton(primary/secondary)`, `StyleInput`, `StyleGrid` (tiêu đề xám), `StatusChip(relation)` → màu nền/chữ | `UiThemeTests` (độ tương phản chữ/nền ≥ 4.5:1, chip đủ 4 loại) |
| T-001.3 | **Icon (TDD)**: `RibbonIcons.Get(name, size, color)` render SVG nhúng bằng `Svg`, cache, lỗi → `null` | `RibbonIconsTests` (đủ 16 tên, đúng kích thước, tên lạ → null) |
| T-001.4 | **Ribbon + thanh lọc**: viết lại phần bố cục `panelSearch` trong Designer (Dock Top/Fill, `FlowLayoutPanel`/`TableLayoutPanel` cho nhóm), chuyển `btnKhoHoaDon`, `lblVersion`, `lblStatusMessage` | T-001.1 xanh; ảnh `T-001.4-demo.png` ở 1000×725 và phóng to |
| T-001.5 | **Tab phẳng + bảng**: dải tab tự vẽ có số dòng, chip trạng thái trong cột Trạng thái, tiêu đề bảng mới | Ảnh demo từng tab |
| T-001.6 | **Màn đăng nhập + khối kích hoạt** | Ảnh `ETAX_UI_DEMO=activation` và màn đăng nhập thật (chỉ chụp, không đăng nhập) |
| T-001.7 | **Form phụ**: áp UiTheme cho KeKhai, Kho HĐ, Tra cứu, Tiến trình | Ảnh từng form ở chế độ demo |
| T-001.8 | Kiểm tra cuối: toàn bộ test, warning ≤ mốc, người dùng thử trên dữ liệu thật; cập nhật `PROJECT_OVERVIEW.md` | `verify.ps1` + checklist tay |

Mỗi task: cập nhật `status.md`/`index.html` ngay sau khi xong. Bỏ qua Playwright cho app (WinForms) — dùng ảnh chụp cửa sổ.

## CR-002 — 3 kiểu giao diện + chọn cột (yêu cầu 2026-11-26)

Người dùng: giữ Ribbon (A), thêm kiểu B (PA2 menu trái, **bộ lọc mở popup** vì cột lọc chiếm chỗ) và kiểu C (PA5 **nền sáng**); bảng có **bánh răng** để bật/tắt và sắp thứ tự cột; người dùng **chọn kiểu giao diện**. Mẫu: `prototypes/5-phuong-an.html?o=6` (B), `?o=7` (C), `&s=filter`, `&s=columns`; ảnh `screenshots/B-*.png`, `C-*.png`.

Quyết định:
- Cả 3 kiểu dùng chung bảng màu UiTheme (Office blue), chung dải tab, nhãn trạng thái, thanh trạng thái. Chỉ khác cách sắp lệnh và bộ lọc.
- B: menu trái — Tra cứu hóa đơn (màn chính), Bảng kê (chuyển tab Bảng kê mua), Lập tờ khai, Kho hóa đơn, Kiểm tra NCC (gọi đúng nút cũ). Hàng trên: Bộ lọc (popup, huy hiệu = số điều kiện đang bật), Tìm kiếm, Xem, PDF ▾ (In/Gộp), Xuất Excel ▾ (Danh sách/Chi tiết/Bảng kê mua/bán), Tải HĐ gốc. 4 thẻ số liệu (bán ra, mua vào, thuế tạm tính, cần chú ý). Ô ghi chú + DN mua lẻ nằm trong popup.
- C: một hàng điều kiện tìm + Tìm kiếm + "Lọc kết quả" (popup), một thanh nút nhỏ, bảng 26px/dòng, số liệu tổng ở thanh trạng thái.
- Đổi kiểu ngay khi chọn (không cần mở lại app); lưu vào `preferences.json` (thêm `Layout`, `Columns`).
- Chọn cột: áp cho bảng của tab đang mở; lưu theo tên bảng; cột STT luôn hiện; "Mặc định" trả về như Designer. Dữ liệu xuất Excel **không** đổi theo cột đang ẩn.

| ID | Task | Kiểm thử |
|---|---|---|
| T-002.1 | Tách bố cục: `LayoutKind {Ribbon, SideMenu, Compact}`; gỡ khung cũ an toàn khi đổi (không Dispose control nghiệp vụ); lưu `Layout` trong UserPreferences | Form1LayoutTests chạy cho **cả 3 kiểu** (handler, vị trí control, đổi qua lại 3 lần không mất control) |
| T-002.2 | Popup bộ lọc (dùng chung B/C): chứa control kỳ/ngày/loại + lọc trong kết quả; đóng popup trả control về chỗ | Test control vào/ra popup; huy hiệu đếm điều kiện |
| T-002.3 | Kiểu B: menu trái, hàng lệnh, nút thả xuống PDF/Xuất Excel, thẻ số liệu (tính từ InvoiceSummary/kết quả hiện có) | Test + ảnh demo |
| T-002.4 | Kiểu C | Test + ảnh demo |
| T-002.5 | Chọn cột: `GridColumnLayout` (thuần, TDD: áp/lấy thứ tự + ẩn hiện, bỏ qua cột lạ/thiếu, STT luôn hiện) + hộp thoại (danh sách tick, kéo hoặc nút ↑↓, Mặc định) + bánh răng ở dải tab | Test logic + test lưu/đọc preferences |
| T-002.6 | Chọn giao diện ở thanh trạng thái (A/B/C), đổi ngay | Test đổi kiểu từ menu |
| T-002.7 | Kiểm tra cuối + người dùng thử | verify.ps1 + checklist |

## CR-003 — Sát mẫu HTML hơn (yêu cầu 2026-11-26: "chưa giống với mẫu lắm")

So ảnh app với `screenshots/pa1-main.png`, `pa2-main.png`, `pa5-main.png`, sửa các điểm lệch (người dùng nói "test lại đi", làm luôn):

| ID | Việc | Kiểm chứng |
|---|---|---|
| T-003.1 | Màu nhấn theo kiểu: Chuẩn navy, Menu trái xanh ngọc, Ribbon xanh Office, Gọn xanh dương (`UiTheme.UseAccent`, gắn theo luồng UI) | `Accent_HasReadableContrast` ×4, `EachLayout_UsesItsAccent` |
| T-003.2 | Ô nhập viền bo cao 32 (`Boxed`), "Loại hóa đơn" thành nút ghép (RadioButton Appearance.Button) | `InvoiceKinds_AreSegmentedButtons_ExceptRibbon`, ảnh |
| T-003.3 | Bảng trong khung trắng viền bo trên nền xám (Chuẩn, Menu trái); tiêu đề cột một dòng; dòng cao 32 | `Tables_SitInCard_WhereMockupHasOne`, ảnh |
| T-003.4 | Chuẩn: thanh nút có viền nằm trong khung bảng, "Bảng kê ▾" gộp mua/bán | ảnh |
| T-003.5 | Menu trái: menu phân hệ, "Xuất Excel ▾"/"PDF ▾", thanh thao tác đáy bảng (số HĐ đang chọn, Xem, Tải HĐ gốc, Kiểm tra NCC); ghi chú + DN mua lẻ vào popup | `SideMenu_ExcelAndPdfMenus_*`, `SideMenu_BusyCommandInMenu_ShowsOnOpener`, ảnh bấm thật |
| T-003.6 | Gọn: số liệu nhỏ ở mép phải hàng tìm kiếm, nút "Lọc kết quả" ở cuối thanh nút | ảnh |

## CR-004 — Thêm màu ("cửa sổ cho tí màu đi, nhìn trắng quá" 2026-11-26)

| ID | Việc | Kiểm chứng |
|---|---|---|
| T-004.1 | Thanh tiêu đề màu nhấn (logo, "Tra cứu hóa đơn điện tử", tài khoản) cho Chuẩn, Ribbon, Gọn; Menu trái tô màu menu bên trái | `EachLayout_HasAccentBand` ×4 |
| T-004.2 | Nền trang xám ngả xanh #ECF0F5, vạch màu trái thẻ số liệu (bán ra xanh lá, mua vào xanh dương, thuế cam, cần chú ý đỏ), tiêu đề bảng tô nhạt theo màu nhấn | tương phản: `Accent_HasReadableContrast` (thêm 3 cặp), ảnh `T-004-*.png` |
