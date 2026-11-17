# Design — Trang công khai (trang chủ, bảng giá, tải ứng dụng)

> Thuộc [Web bán bản quyền E-Tax](../design.md). Dùng `PublicLayout` của [nen-tang](../nen-tang/design.md).

## Màn hình

### Trang chủ `/` — `HomePage`

1. **Phần mở đầu (hero):**
   - Tiêu đề "Tải hoá đơn điện tử hàng loạt chỉ trong vài phút".
   - Mô tả "E-Tax tự động tra cứu và tải hoá đơn mua vào, bán ra từ cổng hoadondientu.gdt.gov.vn, xuất Excel chi tiết từng dòng hàng."
   - Nút "Xem bảng giá" (primary, tới `/bang-gia`) và nút "Tải ứng dụng" (tới `/tai-ung-dung`).
   - Bên phải là hình minh hoạ cửa sổ app, tạm dựng bằng khối CSS.
2. **Sáu thẻ tính năng:**
   - tải hàng loạt theo kỳ;
   - xuất Excel tổng hợp và chi tiết;
   - nhiều mã số thuế;
   - lọc theo trạng thái, loại HĐ;
   - chạy trên máy của bạn (dữ liệu không qua máy chủ trung gian);
   - kích hoạt theo máy, bảo mật.
3. **Ba bước:** Mua gói → Cài ứng dụng và lấy mã máy → Nhập key kích hoạt.
4. **Bảng giá rút gọn:** dùng chung component `PlanCards`.
5. **Hỏi đáp:** `Collapse` 5 câu: dùng trên mấy máy; đổi máy thế nào; thanh toán ra sao và bao lâu có key; dữ liệu có an toàn không; hết hạn thì sao.
6. **Dải kêu gọi hành động** ở cuối trang.

### Bảng giá `/bang-gia` — `PricingPage`

- `PlanCards` lấy dữ liệu từ `GET /plans`. Mỗi thẻ có:
  - tên gói, giá (`formatVnd`), kèm "/ N tháng" hoặc "trọn đời";
  - quyền lợi: "Dùng trên N máy", "Đổi máy M lần", "Cập nhật miễn phí trong thời hạn", "Hỗ trợ qua Zalo/điện thoại";
  - nút "Mua gói".
- Gói `featured` có `Badge.Ribbon` "Phổ biến" và viền màu chủ đạo.
- Bấm "Mua gói":
  - nếu đã đăng nhập: tới `/tai-khoan/mua-goi?goi=<id>`;
  - nếu chưa: tới `/dang-nhap?next=` kèm đường dẫn mua gói ở trên.
- Ghi chú dưới bảng: thanh toán bằng chuyển khoản VietQR, key được cấp sau khi xác nhận.
- Đang tải hiện 4 thẻ Skeleton. Lỗi hiện `Result` kèm nút "Thử lại".

### Tải ứng dụng `/tai-ung-dung` — `DownloadPage`

- Gọi `GET /app/latest`, hiện phiên bản, ngày phát hành, nút "Tải về" (`downloadUrl`) và ghi chú phát hành.
- Yêu cầu hệ thống: Windows 10/11 64-bit, .NET 8 Desktop Runtime (có link tải), kết nối Internet.
- Hướng dẫn kích hoạt (`Steps`):
  1. Cài và mở E-Tax.
  2. Sao chép "Mã máy" ở màn kích hoạt.
  3. Vào Tài khoản → Bản quyền → Thêm máy, dán mã máy.
  4. Sao chép key và dán vào app.

## Vị trí code

Dự kiến (điền dòng thực tế khi làm):

| File | Symbol | Vai trò |
|---|---|---|
| web/src/features/public/PlanCards.tsx | `PlanCards`, `formatPlanPeriod`, `buyPath` | Thẻ gói dùng chung, query `['plans']` |
| web/src/features/public/PricingPage.tsx | `PricingPage` | `/bang-gia` |
| web/src/features/public/HomePage.tsx | `HomePage` | `/` |
| web/src/features/public/DownloadPage.tsx | `DownloadPage` | `/tai-ung-dung`, query `['app-latest']` |
| web/src/router.tsx | `routes` | Thay `ComingSoon` ở 3 route công khai |

**Quyết định thêm (chờ duyệt):** admin đang đăng nhập thì nút "Mua gói" bị vô hiệu, kèm chữ "Tài khoản quản trị không mua gói" (route mua gói chỉ cho khách).

## Nhật ký sửa đổi

### CR-001 — Trang công khai

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| — | — | Plan 4 task đã lập, chờ duyệt | — |
