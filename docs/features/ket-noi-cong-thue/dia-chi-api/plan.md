# Plan — Địa chỉ API cổng hoá đơn điện tử

## Tổng quan

- **Mục tiêu:** App gọi được API của hoadondientu.gdt.gov.vn sau khi Cục Thuế đổi địa chỉ (bỏ cổng `:30000`, chuyển sang `/api/`).
- **Module:** ket-noi-cong-thue
- **Chức năng con:** Không tách

## Yêu cầu

### CR-001 — Chuyển địa chỉ API từ `:30000/` sang `/api/` · 2026-11-25

**Mô tả yêu cầu (nguyên văn từ người dùng):**
> bạn hãy thực hiện cập nhật api của cục thuế đi nhé

**Bối cảnh:** Ngày 2026-11-25 app không tải được captcha: `https://hoadondientu.gdt.gov.vn:30000/captcha` bị từ chối kết nối. Trang chính thức đang gọi `https://hoadondientu.gdt.gov.vn/api/captcha` (trả JSON `key` + `content` SVG như cũ).

**Phạm vi**
- Trong phạm vi: hằng số `GdtApi.BaseUrl`, chú thích liên quan, test.
- Ngoài phạm vi: thay đổi định dạng request/response (chưa thấy khác biệt).

**Cách làm:** Mọi request dùng đường dẫn tương đối không có `/` đầu (`captcha`, `security-taxpayer/authenticate`, `query/...`) ghép với `BaseUrl`, nên chỉ cần đổi `BaseUrl` thành `https://hoadondientu.gdt.gov.vn/api/`.

**Task**

#### T-001.1 — Đổi BaseUrl (TDD)
- File: `E-Tax/GdtApi.cs`, `E-Tax/Program.cs` (chú thích), `E-Tax.Tests/GdtApiCookieTests.cs`
- Các bước:
  1. Viết test: `new Uri(new Uri(GdtApi.BaseUrl), "captcha")` = `https://hoadondientu.gdt.gov.vn/api/captcha`; tương tự `query/invoices/purchase` → chạy, thấy fail.
  2. Đổi `BaseUrl` → test pass.
  3. `dotnet build E-Tax.sln`, `dotnet test E-Tax.Tests`.
- Xong khi: build 0 lỗi, toàn bộ test pass.

#### T-001.2 — Kiểm tra trên app thật
- Các bước:
  1. Chạy app, chụp màn hình đăng nhập → captcha hiển thị.
  2. Người dùng gõ captcha, đăng nhập bằng tài khoản thử → tra cứu hoá đơn; chụp màn hình.
- Xong khi: captcha hiện, đăng nhập và tra cứu thành công.

**Tiêu chí nghiệm thu**
- [x] Captcha hiển thị trên màn hình đăng nhập
- [x] Đăng nhập thành công
- [x] Tra cứu hoá đơn mua vào/bán ra trả kết quả

**Kịch bản test (app desktop — ảnh chụp cửa sổ)**

| ID | Kịch bản | Kết quả mong đợi |
|---|---|---|
| TC-01 | Mở app | Captcha hiện trong vài giây |
| TC-02 | Nhập MST, mật khẩu, captcha → Đăng nhập | Vào màn hình chính, không báo lỗi |
| TC-03 | Tra cứu hoá đơn mua vào 1 tháng | Lưới hiện kết quả |

**Rủi ro / câu hỏi mở**
- Đăng nhập / tra cứu có thể đã đổi thêm header hoặc định dạng; nếu TC-02/03 lỗi sẽ mở task tiếp theo.
