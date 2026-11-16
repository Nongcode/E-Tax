# Status — Hiện đại hóa giao diện

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.
> Khi tiếp tục: đọc "Tóm tắt nhanh" + "File chính" là đủ, không cần đọc lại toàn bộ code.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-001 — Nhóm 1: cửa sổ co giãn, bảng hiện đại, thanh trạng thái |
| Bước Superpowers | Không áp dụng đủ chuỗi: đây là `change` trên màn hình có sẵn, không thêm màn hình/API/model. Vẫn TDD cho logic. |
| Task đang làm | T-001.3 |
| **Bước tiếp theo** | T-001.3: viết `E-Tax.Tests/UiThemeTests.cs` (`StyleGrid_AppliesTheme`) và chạy để thấy fail, rồi tạo `E-Tax/UiTheme.cs` |
| Branch | `feature/giao-dien-hien-dai` (tách từ `master` @ 29d5a4c) |
| Cập nhật lần cuối | 2026-11-16 14:20 |
| Ghi chú dở dang | (1) `downloadProgressBar`/`lblDownloadStatus` nằm dưới `mainSplitContainer` (Dock Fill, z-order trên) nên có thể bị che từ trước — kiểm tra bằng demo, xử lý ở T-001.4. (2) `lblVersion` luôn ghi "Phiên bản dùng thử trong vòng 1 tháng" kể cả bản đầy đủ — xử lý cùng thanh trạng thái. (3) Nhánh `chore/release-public-key` (public key thật) chưa merge, độc lập với nhánh này. |

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

### CR-001 — Nhóm 1: cửa sổ co giãn, bảng hiện đại, thanh trạng thái · 🟡 IN_PROGRESS · 2026-11-16

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | Lưu/khôi phục vị trí cửa sổ (TDD) | ✅ DONE | xUnit 6 test RED→GREEN, suite 37/37; bỏ qua Playwright: logic thuần, chưa nối UI | Thêm test màn hình phụ |
| T-001.2 | Cửa sổ co giãn, căn giữa đăng nhập | ✅ DONE | TC-01 ✅ TC-03 ✅ TC-04 ✅ (ảnh `T-001.2-*`); TC-02 chờ demo T-001.5 | `MinimumSize = Size` đặt bằng code (không phụ thuộc DPI) |
| T-001.3 | Style bảng hiện đại (UiTheme) | ⬜ TODO | xUnit + ảnh chụp TC-05 | |
| T-001.4 | Thanh trạng thái (TDD phần tính) | ⬜ TODO | xUnit + ảnh chụp TC-06 | |
| T-001.5 | Chế độ demo chỉ Debug | ⬜ TODO | Chạy demo; kiểm tra Release không chứa | |
| T-001.6 | Kiểm thử tổng thể + tài liệu | ⬜ TODO | TC-01..07 | |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
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
  - Ruling: coi là `change` (không đủ chuỗi Superpowers) vì không thêm màn hình/API/model.
