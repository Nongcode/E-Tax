# Status — Duy trì đăng nhập

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-001 — Không phải đăng nhập lại mỗi lần mở app |
| Bước Superpowers | Thiết kế duyệt trong chat; TDD tự làm (3 task nhỏ, không dùng subagent) |
| Task đang làm | Kiểm tra tay trên app thật |
| **Bước tiếp theo** | Người dùng đăng nhập có tick → tắt/mở lại app → kiểm tra vào thẳng màn hình tìm kiếm |
| Branch | feat/v1-desktop |
| Cập nhật lần cuối | 2026-11-26 |
| Ghi chú dở dang | Build/test chạy trên bản sao ở scratchpad (bỏ target taskkill) để không tắt app đang đăng nhập |

## File chính

| File | Vai trò |
|---|---|
| E-Tax/SessionStore.cs | Lưu/khôi phục phiên |
| E-Tax/Form1.cs | Khôi phục, đăng xuất, 401 |

## Yêu cầu

### CR-001 · 🟡 IN_PROGRESS · 2026-11-26

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | SessionStore + JwtExpiry | ✅ DONE | RED (thiếu kiểu) → GREEN 13/13 | |
| T-001.2 | Ô tick + link Đăng xuất | ✅ DONE | Build 0 lỗi; ảnh `screenshots/T-001.2-dang-nhap.png` | |
| T-001.3 | Nối Form1 | ✅ DONE (code) | 672/672 | Chưa kiểm tra tay. Bỏ qua Playwright: app WinForms, không có trang web |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| 2026-11-26 | `dotnet test` SessionStoreTests (bản sao) | 🔴 CS0246 thiếu `SessionStore` | RED đúng lý do |
| 2026-11-26 | `dotnet test` SessionStoreTests | ✅ 13/13 | |
| 2026-11-26 | `dotnet test E-Tax.Tests` toàn bộ | ✅ 672/672 | Gồm cả test quy tắc 5 triệu chưa commit |
| 2026-11-26 | Build `--no-incremental` | ✅ 0 lỗi, 38 warning (mốc 40) | Không warning mới |
| 2026-11-26 | verify.ps1 -KillApp (repo thật) | ✅ 0 lỗi, 38 warning, 672/672 | |
| 2026-11-26 | TC-01 mở app: màn hình đăng nhập có ô tick | ✅ | ảnh T-001.2-dang-nhap.png |

## Nhật ký phiên

- 2026-11-26 — Thiết kế trong chat, làm T-001.1 → T-001.3.
