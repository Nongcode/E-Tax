# Status — Hiện đại hóa giao diện

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.
> Khi tiếp tục: đọc "Tóm tắt nhanh" + "File chính" là đủ, không cần đọc lại toàn bộ code.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-001 — Nhóm 1: cửa sổ co giãn, bảng hiện đại, thanh trạng thái |
| Bước Superpowers | Không áp dụng đủ chuỗi: đây là `change` trên màn hình có sẵn, không thêm màn hình/API/model. Vẫn TDD cho logic. |
| Task đang làm | Chưa bắt đầu — chờ người dùng duyệt kế hoạch |
| **Bước tiếp theo** | Sau khi được duyệt: T-001.1, viết `E-Tax.Tests/WindowSettingsTests.cs` (5 test) và chạy để thấy fail |
| Branch | `feature/giao-dien-hien-dai` (tách từ `master` @ 29d5a4c) |
| Cập nhật lần cuối | 2026-11-16 13:45 |
| Ghi chú dở dang | Nhánh `chore/release-public-key` (public key thật) chưa merge, độc lập với nhánh này. |

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
| T-001.1 | Lưu/khôi phục vị trí cửa sổ (TDD) | ⬜ TODO | xUnit; bỏ qua Playwright: logic thuần | |
| T-001.2 | Cửa sổ co giãn, căn giữa đăng nhập | ⬜ TODO | Ảnh chụp TC-01..04 | |
| T-001.3 | Style bảng hiện đại (UiTheme) | ⬜ TODO | xUnit + ảnh chụp TC-05 | |
| T-001.4 | Thanh trạng thái (TDD phần tính) | ⬜ TODO | xUnit + ảnh chụp TC-06 | |
| T-001.5 | Chế độ demo chỉ Debug | ⬜ TODO | Chạy demo; kiểm tra Release không chứa | |
| T-001.6 | Kiểm thử tổng thể + tài liệu | ⬜ TODO | TC-01..07 | |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| 2026-11-16 13:50 | Playwright MCP: TC-07 (`index.html` qua `http.server` cục bộ — `file://` bị chặn) | ✅ pass | 0 lỗi console; ảnh `screenshots/CR-001-prototype.png` |

## Nhật ký phiên

- 2026-11-16 13:45:
  - Init quy trình `feature` cho dự án (`CLAUDE.md`, `docs/features/README.md`).
  - Khảo sát `Form1.Designer.cs` / `GridColumnHelper.cs`.
  - Lập plan/design/prototype CR-001. Dừng chờ duyệt.
  - Ruling: coi là `change` (không đủ chuỗi Superpowers) vì không thêm màn hình/API/model.
