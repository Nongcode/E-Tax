# Plan — Web bán bản quyền E-Tax

## Tổng quan

- **Mục tiêu:** web bán bản quyền phần mềm E-Tax hiện có. Khách tự mua gói, thanh toán VietQR, nhập mã máy để lấy key; admin xác nhận thanh toán và quản lý khách, gói, bản quyền. Đây là giao diện thật (React + Ant Design) chạy với API giả lập; giai đoạn sau nối ASP.NET Core API.
- **Module:** web-ban-quyen (code ở `web/`)
- **Spec:** [design.md](design.md)
- **Chức năng con:** tách 4 phần vì có ≥ 2 vùng độc lập, dự kiến > 8 task, mỗi phần test và bàn giao riêng:

| Thứ tự | Chức năng con | Nội dung |
|---|---|---|
| 1 | [nen-tang](nen-tang/plan.md) | Khung dự án, lớp API, MSW + mockDb, đăng nhập/phân quyền, layout. Ba phần sau đều phụ thuộc phần này |
| 2 | [trang-cong-khai](trang-cong-khai/plan.md) | Trang chủ, bảng giá, tải ứng dụng |
| 3 | [tai-khoan-khach](tai-khoan-khach/plan.md) | Mua gói, thanh toán VietQR, đơn hàng, bản quyền + lấy key, hồ sơ |
| 4 | [quan-tri](quan-tri/plan.md) | Bảng điều khiển, đơn hàng + xác nhận, khách hàng, bản quyền, gói & giá, cài đặt |

## Yêu cầu

### CR-001 — Dựng web bán bản quyền (giao diện + API giả lập) · 2026-11-16

**Mô tả yêu cầu (nguyên văn từ người dùng):**
> bạn hãy thực hiện dựng giao diện web cho tôi trước nhé

Các lựa chọn trong brainstorming: giao diện thật của sản phẩm; React + ASP.NET Core API; web bán bản quyền E-Tax; code trong repo này; API giả lập theo hợp đồng; VietQR; gói thời hạn × số máy; Ant Design.

**Phạm vi**
- Trong phạm vi: toàn bộ giao diện 4 chức năng con, lớp API, giả lập MSW + mockDb có đủ quy tắc nghiệp vụ, Vitest và Playwright.
- Ngoài phạm vi: ASP.NET Core API, DB thật, ký key ECDSA thật, cổng thanh toán, email, web tra cứu hoá đơn của phương án C, sửa app desktop.

**Cách làm:** xem [design.md](design.md). Task chi tiết từng phần được lập ở bước `superpowers:writing-plans`, theo thứ tự 1 → 4.

**Tiêu chí nghiệm thu**
- [ ] `npm run dev` đi trọn luồng khách đăng ký → mua gói → QR → admin xác nhận → khách thêm máy, lấy key
- [ ] Đủ màn hình theo design.md của 4 chức năng con, tiếng Việt, màu #0075D6
- [ ] Mỗi quy tắc R1–R20 có test Vitest
- [ ] `npm run build`, `npm run lint`, `npm run test`, `npm run e2e` pass
- [ ] Không tràn ngang ở 390px
- [ ] Đổi `VITE_USE_MOCK=false` thì không còn import MSW vào bundle; màn hình gọi `VITE_API_URL`

**Kịch bản test (Playwright / e2e)**

| ID | Kịch bản | Kết quả mong đợi |
|---|---|---|
| TC-01 | Mở `/`, bấm "Xem bảng giá" | Thấy 4 gói, giá đúng định dạng, gói 12 tháng có nhãn "Phổ biến" |
| TC-02 | Đăng ký tài khoản mới → bảng giá → "Mua gói" 6 tháng → xác nhận | Trang thanh toán có QR, 500.000 ₫, nội dung `ETxxxxxx`, trạng thái "Chờ thanh toán" |
| TC-03 | Admin → Đơn hàng → lọc "Chờ thanh toán" → xác nhận đơn của khách mẫu | Đơn "Đã thanh toán"; bảng điều khiển giảm 1 đơn chờ |
| TC-04 | Khách mẫu → Bản quyền → chi tiết → Thêm máy (mã sai, rồi mã đúng) → thêm vượt số máy | Mã sai báo lỗi; mã đúng hiện key, sao chép được; vượt số máy báo lỗi MACHINE_LIMIT |
| TC-05 | Khách mở `/quan-tri`; chưa đăng nhập mở `/tai-khoan` | Trang "Không có quyền"; chuyển `/dang-nhap?next=/tai-khoan` |
| TC-06 | Viewport 390×844: trang chủ, bảng giá, tài khoản (mở menu ☰) | Không tràn ngang, Drawer mở được |

**Rủi ro / câu hỏi mở**
- Ảnh QR lấy từ img.vietqr.io (dịch vụ bên ngoài). Khi không truy cập được thì vẫn hiện thông tin chuyển khoản dạng chữ.
- Nhánh được tạo từ `feature/giao-dien-hien-dai` (chưa tích hợp vào master) để dùng chung `docs/features` và `CLAUDE.md`. Cần tích hợp nhánh đó trước hoặc cùng lúc.
- Nội dung quảng cáo trên trang chủ là bản nháp, Bên A cần duyệt lại.
