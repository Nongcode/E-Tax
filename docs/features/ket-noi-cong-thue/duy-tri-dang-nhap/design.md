# Design — Duy trì đăng nhập

## Quyết định
- Trước đây app cố ý **không** lưu JWT. Đổi quyết định theo yêu cầu người dùng (2026-11-26), có điều kiện:
  - Chỉ khi người dùng tick "Duy trì đăng nhập trên máy này" (mặc định tắt).
  - Không bao giờ lưu mật khẩu/captcha.
  - JWT + tên đăng nhập mã hóa bằng DPAPI (`ProtectedData`, `CurrentUser`, entropy cố định) tại `%LocalAppData%\E-Tax\session.dat`. User Windows khác hoặc máy khác không giải mã được.
- Mở app: đọc `exp` trong payload JWT (không kiểm chữ ký). Còn ≥ 5 phút thì vào thẳng màn hình tìm kiếm, **không gọi cổng thuế** để kiểm tra. Không có `exp` → không lưu.
- Token bị cổng thu hồi sớm: lần tra cứu đầu nhận 401 → xóa phiên lưu, bỏ kết quả dở, báo "Hết phiên đăng nhập" và đăng xuất.
- Đăng xuất (link ở thanh trạng thái, chặn khi đang có tác vụ): xóa phiên lưu, token, cookie, dữ liệu tìm kiếm, về màn hình đăng nhập và tải captcha.
- Đăng nhập lại không tick → xóa phiên lưu cũ.
- Hết hạn dùng thử (khối kích hoạt hiện) → không khôi phục phiên.

## Rủi ro / chưa biết
- Chưa biết JWT của cổng sống bao lâu. App ghi log "Phiên đăng nhập hết hạn lúc …" (chỉ giờ, không ghi token) sau mỗi lần đăng nhập để biết.
- Ai dùng chung tài khoản Windows sẽ vào được phiên đã lưu.
- Lỗi 401 chỉ xử lý ở tra cứu danh sách; tải chi tiết/tải gốc bị 401 vẫn báo lỗi như cũ.

## File
| File | Vai trò |
|---|---|
| E-Tax/SessionStore.cs | `SessionStore`, `JwtExpiry`, `SavedSession` |
| E-Tax/Form1.cs | `TryRestoreSession`, `RememberSession`, `EnterSearchScreen`, `ApplySessionToken`, `LogoutAsync`, xử lý 401 trong `GetInvoiceListAsync` |
| E-Tax/Form1.Designer.cs | `chkKeepLogin`, `lblLogout`; nút Đăng nhập dời xuống 20px |
| E-Tax.Tests/SessionStoreTests.cs | 13 test |
