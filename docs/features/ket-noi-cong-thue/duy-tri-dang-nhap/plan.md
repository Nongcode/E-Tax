# Plan — Duy trì đăng nhập

## CR-001 — Không phải đăng nhập lại mỗi lần mở app · 2026-11-26

Yêu cầu người dùng (2026-11-26): "thêm chức năng duy trì đăng nhập". Thiết kế đã thống nhất trong chat, người dùng bảo "làm duy trì đăng nhập trước".

| ID | Task | Kiểm thử |
|---|---|---|
| T-001.1 | `SessionStore` + `JwtExpiry` (TDD): lưu tên đăng nhập + JWT mã hóa DPAPI, khôi phục khi còn ≥ 5 phút theo `exp` | `SessionStoreTests` |
| T-001.2 | Giao diện: ô "Duy trì đăng nhập trên máy này", link "Đăng xuất" ở thanh trạng thái | Build + ảnh chụp cửa sổ |
| T-001.3 | Nối Form1: khôi phục khi mở app (không gọi cổng), lưu/xóa khi đăng nhập, 401 khi tra cứu → xóa phiên lưu, báo và đăng xuất | Toàn bộ test + kiểm tra tay trên app thật |
