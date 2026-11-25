# Status — Hiện đại hóa giao diện

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.
> Khi tiếp tục: đọc "Tóm tắt nhanh" + "File chính" là đủ, không cần đọc lại toàn bộ code.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | ✅ DONE |
| Yêu cầu đang làm | — (CR-001, CR-002 xong) |
| Bước Superpowers | Không áp dụng đủ chuỗi: đây là `change` trên màn hình có sẵn, không thêm màn hình/API/model. Vẫn TDD cho logic. |
| Task đang làm | — |
| **Bước tiếp theo** | Chờ người dùng chọn cách tích hợp nhánh (merge / PR / giữ nguyên) |
| Branch | `feature/giao-dien-hien-dai` (tách từ `master` @ 29d5a4c) |
| Cập nhật lần cuối | 2026-11-16 16:50 |
| Ghi chú dở dang | (1) ~~thanh tiến độ bị che~~ — đã xác nhận và sửa ở T-001.5. (2) ~~`lblVersion` ghi sai bản quyền~~ — đã sửa ở T-001.4. (3) Nhánh `chore/release-public-key` (public key thật) chưa merge, độc lập với nhánh này. |

## File chính

| File | Vai trò |
|---|---|
| `E-Tax/Form1.Designer.cs` | Bố cục form: FormBorderStyle, Anchor, StatusStrip |
| `E-Tax/Form1.cs` | Load/Resize/Closing, BindMainGrid, CheckForLicense, demo mode |
| `E-Tax/GridColumnHelper.cs` | Style chung cho bảng cấu hình bằng code |
| `E-Tax/UiTheme.cs` (mới) | Bảng màu và `StyleGrid` |
| `E-Tax/WindowSettings.cs` (mới) | Nhớ vị trí/kích thước cửa sổ |
| `E-Tax/InvoiceSummary.cs` (mới) | Nội dung thanh trạng thái |

## Yêu cầu

### CR-001 — Nhóm 1: cửa sổ co giãn, bảng hiện đại, thanh trạng thái · ✅ DONE · 2026-11-16

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | Lưu/khôi phục vị trí cửa sổ (TDD) | ✅ DONE | xUnit 6 test RED→GREEN, suite 37/37; bỏ qua Playwright: logic thuần, chưa nối UI | Thêm test màn hình phụ |
| T-001.2 | Cửa sổ co giãn, căn giữa đăng nhập | ✅ DONE | TC-01 ✅ TC-03 ✅ TC-04 ✅ (ảnh `T-001.2-*`); TC-02 chờ demo T-001.5 | `MinimumSize = Size` đặt bằng code (không phụ thuộc DPI) |
| T-001.3 | Style bảng hiện đại (UiTheme) | ✅ DONE | xUnit 2 test RED→GREEN, suite 39/39; build 0 lỗi. Ảnh TC-05 dời sang T-001.6 (cần dữ liệu demo của T-001.5) | Ruling: không cố định cột Ký hiệu HĐ/Số HĐ — là cột 5–6, cố định thì kéo theo 4 cột trước (~470px, nửa bề ngang tối thiểu); giữ STT. Ruling: định dạng tiền `#,##0` (thống nhất các bảng khác) thay vì `N0`. Padding ô chỉ ngang 6px (dọc sẽ cắt chữ ở dòng 28px) |
| T-001.4 | Thanh trạng thái (TDD phần tính) | ✅ DONE | xUnit 4 test RED→GREEN, suite 43/43; build 0 lỗi; TC-06 phần đăng nhập ✅ (ảnh `T-001.4-TC06-dang-nhap.png`); phần có dữ liệu chạy ở T-001.6 bằng demo | Thêm `LicenseManager.GetTrialExpiry()` để hiện "còn N ngày". `lblVersion` sửa theo bản quyền (ghi chú dở dang (2) đã xử lý) |
| T-001.5 | Chế độ demo chỉ Debug | ✅ DONE | Demo chạy, ảnh `T-001.5-tien-do.png`; Release dll: 0 lần chuỗi `ETAX_UI_DEMO` (Debug: 1) | Thêm `ETAX_UI_DEMO=progress` để kiểm tra thanh tiến độ. Ruling: thanh tiến độ bị `mainSplitContainer` che (lỗi có sẵn) → `BringToFront()` giữ vị trí cũ thay vì chuyển xuống thanh trạng thái, vì `Visible/Value` được đặt ở >10 chỗ và trong `DetailGridManager` — giá nếu sai: thanh tiến độ đè lên vài dòng bảng khi tải. Ruling: `dgvMain` chọn cả dòng (`FullRowSelect`) — TC-05 cần dòng chọn nền xanh; nút In pdf vốn đọc `SelectedRows` nên giờ in được nhiều HĐ đã chọn (trước đây luôn rỗng) — giá nếu sai: không còn chọn/copy từng ô. Cột "Thông tin HĐ" `Fill` để lấp chiều ngang khi phóng to. Demo đặt focus vào bảng (phím lạc từng bấm nhầm nút Ktra → gọi mạng) |
| T-001.6 | Kiểm thử tổng thể + tài liệu + review | ✅ DONE | TC-01..07 ✅ (xem "Kết quả test gần nhất"); suite 46/46 | Review toàn nhánh (subagent Opus): 2 Important + 1 nâng lên Important đã sửa, 5 Minor hoãn (xem Nhật ký phiên) |

### CR-002 — Bỏ logo màn hình đăng nhập, cân đối khối đăng nhập · ✅ DONE · 2026-11-16

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-002.1 | Bỏ logo, tiêu đề chữ, căn thẳng, căn giữa theo nội dung | ✅ DONE | Build 0 lỗi, suite 46/46; TC-08 ✅ (ảnh `T-002.1-TC08-mac-dinh.png`, `-phong-to.png`), TC-09 ✅ (`T-002.1-TC09-kich-hoat.png`); Release 0 chuỗi `ETAX_UI_DEMO`. Không thêm unit test: chỉ đổi bố cục Designer | Người dùng chọn phương án "Tiêu đề chữ". Sửa luôn minor CR-001 "`CenterLoginPanel` lệch ~11px". `Form1.resx` nhỏ đi ~53 KB (bỏ 2 ảnh) |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| 2026-11-16 16:50 | Playwright MCP: `index.html#progress` | ✅ pass | 0 lỗi console, hiện bảng CR-002 |
| 2026-11-16 16:50 | `dotnet build`; `dotnet test E-Tax.Tests`; app thật TC-08 (1000×725 + phóng to), TC-09 (demo `activation`); Release dll | ✅ 0 lỗi; 46/46; pass; 0 chuỗi demo | Khối đăng nhập giữa vùng trên thanh trạng thái, khối kích hoạt không bị cắt |
| 2026-11-16 16:10 | `dotnet build E-Tax.sln`; `dotnet test E-Tax.Tests` | ✅ 0 lỗi; 46/46 | Sau fix review |
| 2026-11-16 16:10 | App thật (ảnh `PrintWindow`): TC-01, TC-03, TC-04 | ✅ pass | Cửa sổ 1000×725 (vừa laptop 1366×768); kéo 600×400 giữ 1000×725; phóng to; mở lại 150,70 1300×820; mở lại phóng to; lưu 5000,5000 → giữa; lưu Y=-600 (tiêu đề ngoài màn hình) → giữa (1 lần đầu thấy cửa sổ thu nhỏ -32000 — chạy lại 2 lần đều giữa, nghi do thao tác trên máy) |
| 2026-11-16 16:10 | Demo Debug: TC-02, TC-05, TC-06 + thanh tiến độ | ✅ pass | Ảnh chụp lại sau khi bố cục đổi |
| 2026-11-16 16:00 | Playwright MCP: TC-07 `index.html` (qua `http.server`) | ✅ pass | 0 lỗi console |
| 2026-11-16 15:30 | Demo Debug: TC-02, TC-05, TC-06 (có dữ liệu), thanh tiến độ; Release dll | ✅ pass | Ảnh `T-001.6-TC02-phong-to.png`, `T-001.6-TC05-bang.png`, `T-001.5-tien-do.png`; Release 0 chuỗi `ETAX_UI_DEMO` |
| 2026-11-16 15:00 | `dotnet test E-Tax.Tests` + ảnh màn hình đăng nhập | ✅ 43/43; TC-06 (đăng nhập) pass | "Chưa có dữ liệu", "Bản quyền: Đầy đủ" màu xanh; `lblVersion` = "Phiên bản đầy đủ" |
| 2026-11-16 14:40 | `dotnet test E-Tax.Tests` | ✅ 39/39 | T-001.3 (`UiThemeTests` 2 test) |
| 2026-11-16 14:20 | Ảnh chụp app Debug: TC-01, TC-03, TC-04 | ✅ pass | min 1000×725 giữ khi kéo 600×400; phóng to 1936×1048; mở lại 150,70 1300×820; mở lại phóng to; 5000,5000 → giữa 460,153 |
| 2026-11-16 14:00 | `dotnet test E-Tax.Tests` | ✅ 37/37 | T-001.1 |
| 2026-11-16 13:50 | Playwright MCP: TC-07 (`index.html` qua `http.server` cục bộ — `file://` bị chặn) | ✅ pass | 0 lỗi console; ảnh `screenshots/CR-001-prototype.png` |

## Nhật ký phiên

- 2026-11-16 13:45:
  - Init quy trình `feature` cho dự án (`CLAUDE.md`, `docs/features/README.md`).
  - Khảo sát `Form1.Designer.cs` / `GridColumnHelper.cs`.
  - Lập plan/design/prototype CR-001. Dừng chờ duyệt.
- 2026-11-16 14:00 — Người dùng duyệt kế hoạch ("oke"). Xong T-001.1.
- 2026-11-16 14:20 — Xong T-001.2 (build 0 lỗi, TC-01/03/04 đạt bằng script PowerShell ngoài repo).
- 2026-11-16 14:40 — Xong T-001.3 (UiTheme, áp cho 6 bảng).
- 2026-11-16 15:00 — Xong T-001.4 (InvoiceSummary + StatusStrip).
- 2026-11-16 16:10 — Xong T-001.6. Review toàn nhánh (subagent Opus) và sửa trong 1 lượt:
  - Fixed (Important): cửa sổ cao 747px không vừa laptop 1366×768 (vùng làm việc ~728) và `MinimumSize` khoá không thu nhỏ được → trả `ClientSize` về 984×686 (thanh trạng thái lấy 22px của `panelSearch`/`panelLogin`), `MinimumSize = WindowSettings.ClampMinimum(...)` — test `ClampMinimum_SmallScreen_FitsWorkingArea` RED→GREEN, suite 46/46.
  - Fixed (Important): khôi phục cửa sổ có thanh tiêu đề ngoài màn hình / lớn hơn màn hình → `Fit` đòi thanh tiêu đề (30px) nằm trên một màn hình và thu kích thước về vùng làm việc — `Fit_TitleBarAboveScreen_ReturnsNull`, `Fit_LargerThanScreen_ShrinksToWorkingArea` RED→GREEN.
  - Fixed (nâng từ Minor lên Important — số tiền trên thanh trạng thái lệch với bảng khi lọc MST): `UpdateInvoiceSummary(filteredResults)` hiện "Lọc x/y HĐ — …", bỏ lọc thì tính lại. Ruling: không có unit test cho phần nối UI (logic `InvoiceSummary` đã test); chỉ kiểm bằng build — giá nếu sai: chữ thanh trạng thái khi lọc hiển thị sai.
  - Fixed: chú thích `<summary>` của `Activate` bị chèn lệch khi thêm `GetTrialExpiry` (lỗi do chính task T-001.4).
  - Minor (deferred): `lblVersion` khi hết hạn vẫn ghi "dùng thử trong vòng 1 tháng".
  - Minor (deferred): Xuất chi tiết khi chọn nhiều dòng âm thầm lấy `CurrentRow` — nên báo "chọn đúng một hóa đơn".
  - Minor (deferred → đã sửa ở T-002.1): `CenterLoginPanel` căn theo cả vùng có thanh trạng thái (lệch ~11px).
  - Minor (deferred): đóng app khi đang thu nhỏ từ phóng to → lần sau mở không phóng to.
  - Minor (deferred): build có cảnh báo nullable có sẵn (vd Form1.cs CS8600).
- 2026-11-16 15:30 — Xong T-001.5. Người dùng đang dùng máy khi chụp → `CopyFromScreen` chụp nhầm cửa sổ khác và một phím lạc bấm nút Ktra; đổi script chụp sang `PrintWindow` (không `SetForegroundWindow`).
  - Ruling: coi là `change` (không đủ chuỗi Superpowers) vì không thêm màn hình/API/model.
- 2026-11-16 16:50 — CR-002: người dùng yêu cầu bỏ logo đăng nhập, chọn phương án "Tiêu đề chữ". Xong T-002.1.
