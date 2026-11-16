# Design — Trang quản trị

> Thuộc [Web bán bản quyền E-Tax](../design.md). Dùng `AdminLayout`, quyền `admin`. Quy tắc R5, R6, R13, R14 ở design.md cha.

## Menu

Bảng điều khiển · Đơn hàng (Badge số đơn chờ) · Khách hàng · Bản quyền · Gói & giá · Cài đặt.

## Màn hình

### Bảng điều khiển `/quan-tri` — `AdminDashboardPage`

- Bốn `Statistic`: doanh thu tháng này (VND), đơn chờ xác nhận, bản quyền còn hạn, khách mới tháng này.
- Bảng "Đơn chờ xác nhận", cùng cột và nút "Xác nhận" như trang Đơn hàng. Khi trống hiện "Không có đơn chờ".

### Đơn hàng `/quan-tri/don-hang` — `AdminOrdersPage`

- **Lọc:**
  - `Segmented` theo trạng thái: Tất cả / Chờ thanh toán / Đã thanh toán / Đã huỷ / Quá hạn.
  - Ô tìm "Mã đơn, email, MST".
- **Bảng:** mã đơn, khách (tên + email, link tới chi tiết khách), gói, số tiền, trạng thái, ngày tạo, thao tác.
- **Xác nhận** (chỉ đơn `pending`):
  - Mở `Modal` đối chiếu: mã đơn / nội dung chuyển khoản, số tiền cần nhận, khách, gói.
  - Có ô ghi chú, ví dụ "VCB 16/11 FT123".
  - Nút "Đã nhận tiền – cấp bản quyền".
  - Thành công: `message.success`, rồi làm mới danh sách đơn, thống kê và badge trên menu.
- **Huỷ** (đơn `pending`): hỏi lại bằng `Popconfirm`.

### Khách hàng `/quan-tri/khach-hang` — `AdminUsersPage`

- Tìm theo tên, email, SĐT, MST.
- Bảng: họ tên, email, SĐT, công ty / MST, số đơn, số bản quyền, trạng thái (Hoạt động / Đã khoá), ngày đăng ký, thao tác Khoá/Mở (có `Popconfirm`).
- Không cho admin khoá chính tài khoản mình đang đăng nhập.

### Chi tiết khách `/quan-tri/khach-hang/:id` — `AdminUserDetailPage`

- `Descriptions` thông tin khách, nút Khoá/Mở.
- `Tabs`:
  - "Đơn hàng": bảng đơn của khách, có nút xác nhận;
  - "Bản quyền": bảng bản quyền, có link tới chi tiết.

### Bản quyền `/quan-tri/ban-quyen` — `AdminLicensesPage`

- Lọc trạng thái (Còn hạn / Hết hạn / Đã thu hồi). Tìm theo email, mã đơn hoặc mã máy.
- Bảng: khách, gói, hiệu lực, máy (đang dùng / tối đa), đổi máy (đã dùng / tối đa), trạng thái.

### Chi tiết bản quyền `/quan-tri/ban-quyen/:id` — `AdminLicenseDetailPage`

- `Descriptions` và bảng máy. Bảng hiện cả máy đã gỡ, có cột "Gỡ lúc".
- "Gia hạn": `Modal` chọn 1/3/6/12 tháng. Ẩn với gói trọn đời.
- "Thu hồi": `Popconfirm` cảnh báo màu đỏ.

### Gói & giá `/quan-tri/goi` — `AdminPlansPage`

- Bảng: thứ tự, tên, thời hạn, số máy, đổi máy, giá, nổi bật, đang bán (Switch sửa nhanh), thao tác Sửa.
- "Thêm gói" / "Sửa" mở `Drawer` form:
  - tên (bắt buộc), mô tả;
  - thời hạn: Select 1/3/6/12/24 tháng hoặc Trọn đời;
  - số máy (≥ 1), số lần đổi máy (≥ 0);
  - giá: InputNumber định dạng VND, ≥ 0;
  - nổi bật, đang bán, thứ tự.
- Ghi chú trên form: sửa giá không ảnh hưởng đơn đã tạo, vì đơn đã chụp `planSnapshot`.

### Cài đặt `/quan-tri/cai-dat` — `AdminSettingsPage`

- **Tài khoản nhận tiền:**
  - Ngân hàng chọn từ danh sách BIN: Vietcombank 970436, VietinBank 970415, BIDV 970418, Agribank 970405, Techcombank 970407, MB 970422, ACB 970416, VPBank 970432, TPBank 970423, Sacombank 970403.
  - Số tài khoản.
  - Tên chủ tài khoản: tự viết HOA, không dấu.
  - Xem trước mã QR mẫu 10.000 ₫.
- **Phiên bản ứng dụng:** phiên bản, link tải, ngày phát hành, ghi chú.

## Vị trí code

Chưa có. Điền khi làm.

## Nhật ký sửa đổi

### CR-001 — Trang quản trị

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| — | — | Chưa bắt đầu | — |
