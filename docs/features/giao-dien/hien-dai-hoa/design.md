# Design — Hiện đại hóa giao diện

## Kiến trúc

App WinForms một form (`Form1`). Ở màn hình đăng nhập (`panelLogin`), sau khi đăng nhập thì `panelSearch` hiện ra. `panelSearch` chứa `mainSplitContainer` (Dock Fill) gồm: bộ lọc ngày, loại HĐ, các nút, `gbNguoiBan` ở góc phải và `tabControlMain` với 6 bảng.

CR-001 thêm 3 lớp nhỏ, không đụng luồng nghiệp vụ:
- `UiTheme`: màu và style dùng chung → `StyleGrid(dgv)`.
- `WindowSettings`: đọc/ghi `%AppData%\E-Tax\window.json`, đặt lại vị trí cửa sổ vào màn hình.
- `InvoiceSummary`: tính số HĐ / tổng thanh toán theo Bán ra / Mua vào cho thanh trạng thái.

## Vị trí code (dự kiến — cập nhật số dòng thật sau mỗi task)

| File | Symbol | Dòng | Vai trò |
|---|---|---|---|
| `E-Tax/Form1.Designer.cs` | `Form1` (FormBorderStyle, ClientSize) | L996–L1001 | Đổi sang `Sizable`, thêm `MinimumSize`, `MaximizeBox` |
| `E-Tax/Form1.Designer.cs` | `panelSearch` | L384–L387 | Thêm `Anchor` 4 cạnh |
| `E-Tax/Form1.Designer.cs` | `gbNguoiBan`, `tabControlMain` | L829, L578 | Đã neo sẵn — chỉ kiểm tra |
| `E-Tax/Form1.Designer.cs` | `statusBar` (mới) | — | `StatusStrip` Dock Bottom, 3 nhãn |
| `E-Tax/Form1.cs` | `Form1_Load` | L60–L70 | Áp dụng `WindowSettings`, demo mode (`#if DEBUG`) |
| `E-Tax/Form1.cs` | `Form1_Resize`, `Form1_FormClosing` (mới) | — | Căn giữa `panelLogin`; lưu cửa sổ |
| `E-Tax/Form1.cs` | `BindMainGrid` | ~L760 | Cập nhật `lblStatusInvoices` |
| `E-Tax/Form1.cs` | `CheckForLicense` | ~L295–L315 | Cập nhật `lblStatusLicense` |
| `E-Tax/Form1.cs` | Đăng nhập thành công | ~L205 | Cập nhật `lblStatusUser` |
| `E-Tax/GridColumnHelper.cs` | `ApplyDefaultStyle` | L11–L18 | Gọi `UiTheme.StyleGrid` |
| `E-Tax/DetailGridManager.cs` | `ConfigureGrid` | L92 | Đã gọi `ApplyDefaultStyle` — không sửa |
| `E-Tax/UiTheme.cs` (mới) | `UiTheme.StyleGrid` | — | Style bảng |
| `E-Tax/WindowSettings.cs` (mới) | `Load`, `Save`, `Fit` | — | Nhớ cửa sổ |
| `E-Tax/InvoiceSummary.cs` (mới) | `Compute`, `Format` | — | Nội dung thanh trạng thái |

## API

Không có API mới, không gọi thêm API cổng thuế.

## Dữ liệu

- `%AppData%\E-Tax\window.json`: `{ "X": 120, "Y": 80, "Width": 1280, "Height": 800, "Maximized": false }`. Thiếu hoặc hỏng → dùng mặc định (giữa màn hình, 1000×725).
- Thanh trạng thái tính từ `_latestResults` (`Loai_hoa_don`, `Tong_tien_thanh_toan`). `null` tính là 0.

## Giao diện

Bảng màu (khớp màu nút hiện có `#0075D6`):

| Token | Giá trị | Dùng cho |
|---|---|---|
| Accent | `#0075D6` | Nền tiêu đề bảng, nút chính |
| HeaderText | `#FFFFFF` | Chữ tiêu đề bảng |
| GridLine | `#E5E7EB` | Đường kẻ ngang |
| RowAlt | `#F8FAFC` | Dòng xen kẽ |
| Selection | `#DBEAFE` / chữ `#1D2330` | Dòng đang chọn |
| StatusBar | nền `#F3F4F6`, chữ `#374151` | Thanh trạng thái |
| License | xanh `#16A34A` đầy đủ · cam `#D97706` dùng thử · đỏ `#DC2626` hết hạn | Ô bản quyền |

Trạng thái:
- **Màn hình đăng nhập:**
  - Panel ở giữa cửa sổ.
  - Thanh trạng thái hiện "Chưa có dữ liệu", ô tài khoản để trống, ô bản quyền vẫn hiện.
- **Chưa tìm kiếm:** bảng trống có tiêu đề, thanh trạng thái hiện "Chưa có dữ liệu".
- **Có dữ liệu:** thanh trạng thái hiện số HĐ và tổng tiền theo loại.

Prototype: `index.html`, tab "Giao diện".

## Nhật ký sửa đổi

### CR-001 — Nhóm 1: cửa sổ co giãn, bảng hiện đại, thanh trạng thái

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| T-001.1 | `E-Tax/WindowSettings.cs` (mới, L1–L50) | `record WindowSettings(X,Y,Width,Height,Maximized)`: `Load` (không ném), `Save` (tạo thư mục), `Fit` (kẹp kích thước tối thiểu; null nếu không giao màn hình nào) | Nhớ cửa sổ, an toàn khi rút màn hình phụ |
| T-001.1 | `E-Tax.Tests/WindowSettingsTests.cs` (mới) | 6 test | TDD |
| T-001.2 | `E-Tax/Form1.Designer.cs:L384`, `:L1001` | `panelSearch.Anchor` 4 cạnh; `FormBorderStyle = Sizable` | Co giãn theo cửa sổ |
| T-001.2 | `E-Tax/Form1.cs:L69-L73` (constructor) | `MinimumSize = Size`; `RestoreWindowBounds()`; `Resize → CenterLoginPanel`; `FormClosing → SaveWindowBounds` | Khôi phục trước khi form hiện |
| T-001.3 | `E-Tax/UiTheme.cs` (mới) | Hằng màu `Accent/HeaderText/GridLine/RowAlt/Selection/SelectionText`; `StyleGrid(dgv)`: tiêu đề 34px khoá, nền Accent; dòng 28px; kẻ ngang; không viền; padding ngang 6px; Segoe UI 9 | Style dùng chung |
| T-001.3 | `E-Tax/GridColumnHelper.cs:L14` `ApplyDefaultStyle` | Gọi `UiTheme.StyleGrid` thay cho font/màu xen kẽ cũ | `dgvDetails`, `dgvMua`, `dgvBan`, `dgvVatNop`, `dgvGiamThue` nhận theme qua đây |
| T-001.3 | `E-Tax/Form1.cs:L66`, `:L101-L113` `StyleMainGrid` | `UiTheme.StyleGrid(dgvMain)`; 3 cột tiền `#,##0` căn phải | `dgvMain` cấu hình trong Designer |
| T-001.3 | `E-Tax.Tests/UiThemeTests.cs` (mới) | `StyleGrid_AppliesTheme`, `ApplyDefaultStyle_UsesTheme` | TDD |
| T-001.2 | `E-Tax/Form1.cs:L99-L137` `#region Cửa sổ` | `RestoreWindowBounds`, `SaveWindowBounds` (lưu `RestoreBounds` khi phóng to), `CenterLoginPanel` (Top ≥ `lblVersion.Bottom`) | |
