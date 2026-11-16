# Design — Tài khoản khách hàng

> Thuộc [Web bán bản quyền E-Tax](../design.md). Dùng `AccountLayout`, quyền `customer`. Quy tắc R4–R12, R15 ở design.md cha.

## Menu

Tổng quan · Mua gói · Đơn hàng · Bản quyền · Hồ sơ · Đăng xuất.

## Màn hình

### Tổng quan `/tai-khoan` — `DashboardPage`

- Lời chào "Xin chào, {fullName}".
- Ba `Statistic`:
  - bản quyền còn hạn;
  - hạn gần nhất (ngày, hoặc "Trọn đời");
  - đơn chờ thanh toán.
- Khi có đơn `pending`: `Alert` warning "Bạn có đơn ETxxxxxx chờ thanh toán", kèm link tới trang thanh toán.
- Nút "Thêm máy / lấy key":
  - có đúng 1 bản quyền còn hạn: tới chi tiết bản quyền đó;
  - có nhiều bản quyền: tới danh sách bản quyền;
  - không có bản quyền: thay bằng nút "Mua gói".
- Bảng 5 đơn gần nhất.

### Mua gói `/tai-khoan/mua-goi?goi=<id>` — `BuyPage`

- Chọn gói bằng `Radio.Group` dạng thẻ. Gói trong query `goi` được chọn sẵn.
- Khung tóm tắt bên phải: gói, thời hạn, số máy, số lần đổi máy, tổng tiền.
- Nút "Tạo đơn & thanh toán": gọi `POST /orders`, thành công thì chuyển sang `/tai-khoan/don-hang/:id`.

### Thanh toán / chi tiết đơn `/tai-khoan/don-hang/:id` — `OrderPaymentPage`

- **Đầu trang:** mã đơn, `StatusTag`, ngày tạo.
- **Đơn `pending`:**
  - Bên trái là ảnh QR (`qrUrl`, rộng 280px). Nếu ảnh lỗi thì ẩn ảnh và ghi "Không tải được mã QR, vui lòng chuyển khoản theo thông tin bên cạnh".
  - Bên phải là các `CopyField`: Ngân hàng, Số tài khoản, Chủ tài khoản, Số tiền, Nội dung chuyển khoản (`code`, chữ đậm).
  - `Alert` "Ghi đúng nội dung chuyển khoản để được xác nhận nhanh".
  - Đồng hồ "Đơn hết hạn sau" (`Statistic.Timer` đếm tới `expiresAt`).
  - Nút "Huỷ đơn" có `Popconfirm`.
  - Trang tự làm mới mỗi 10 giây.
- **Đơn `paid`:** `Result success` "Đã nhận thanh toán", nút "Đến bản quyền" (tới bản quyền của đơn).
- **Đơn `cancelled` / `expired`:** `Result` tương ứng, nút "Mua lại".

### Đơn hàng `/tai-khoan/don-hang` — `OrdersPage`

- Bảng gồm: mã đơn (link), gói, số tiền, trạng thái, ngày tạo, ngày thanh toán, thao tác. Đơn đang chờ có nút "Thanh toán".
- Khi trống: "Bạn chưa có đơn hàng nào" kèm nút "Xem bảng giá".

### Bản quyền `/tai-khoan/ban-quyen` — `LicensesPage`

Lưới thẻ, mỗi thẻ có:
- tên gói, `StatusTag`;
- "Hiệu lực: dd/MM/yyyy – dd/MM/yyyy", hoặc "Trọn đời";
- `Progress` số máy, ví dụ "1/2 máy";
- "Còn N lần đổi máy";
- nút "Quản lý máy".

### Chi tiết bản quyền `/tai-khoan/ban-quyen/:id` — `LicenseDetailPage`

- `Descriptions`: gói, trạng thái, hiệu lực, số máy, số lần đổi máy còn lại.
- **Bảng máy đang dùng:**
  - Cột: tên máy, mã máy, key (rút gọn, có nút sao chép), ngày kích hoạt, nút "Gỡ máy".
  - "Gỡ máy" hỏi lại bằng `Popconfirm` "Gỡ máy sẽ dùng 1 lần đổi máy (còn N). Tiếp tục?".
  - Hết lượt đổi máy thì nút bị khoá, có tooltip giải thích.
- **Nút "Thêm máy":**
  - Bị khoá khi bản quyền không còn hạn hoặc đã đủ máy, có tooltip lý do.
  - Mở `Modal` với hai ô:
    - "Mã máy": tự viết HOA, tối đa 16 ký tự, kiểm tra bằng `isValidMachineId`, ghi chú "Lấy ở màn kích hoạt của ứng dụng E-Tax";
    - "Tên máy": bắt buộc, ví dụ "Máy kế toán 1".
  - Thành công thì Modal chuyển sang bước kết quả: key nằm trong `CopyField` nhiều dòng, kèm hướng dẫn dán vào app.

### Hồ sơ `/tai-khoan/ho-so` — `ProfilePage`

- Form thông tin: họ tên, SĐT, công ty, MST. Email chỉ đọc.
- Form đổi mật khẩu: mật khẩu hiện tại, mật khẩu mới, nhập lại.

## Vị trí code

Chưa có. Điền khi làm.

## Nhật ký sửa đổi

### CR-001 — Tài khoản khách hàng

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| — | — | Chưa bắt đầu | — |
