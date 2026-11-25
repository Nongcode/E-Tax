# Hiện trạng chức năng E-Tax V1

Cập nhật: 25/11/2026 · Nhánh `feat/v1-desktop` · Phiên bản 1.0.0

V1 là ứng dụng Windows (WinForms, .NET 8), chạy hoàn toàn trên máy người dùng, không có server. Ứng dụng chỉ **đọc** dữ liệu trên cổng hóa đơn điện tử: không nộp, không sửa dữ liệu trên cơ quan thuế. Người dùng tự đăng nhập và tự nhập captcha.

## Tóm tắt

| Trạng thái | Ý nghĩa | Số đầu việc |
|---|---|---|
| Xong code | Đã có trong app, build và test tự động đạt | 36 |
| Một phần | Có bản dùng được nhưng còn thiếu so với yêu cầu | 10 |
| Chưa làm | Cần dữ liệu, mẫu hoặc hạ tầng chưa có | 17 |

Kiểm chứng: build 0 lỗi, 604/604 test tự động đạt. **Chưa chạy với tài khoản thật** và chưa đối chiếu số liệu với HTKK, nên mọi số trên tờ khai hiện là dự thảo.

## Chức năng đã có

### Tra cứu và tải hóa đơn
- Đăng nhập cổng hóa đơn điện tử (người dùng tự nhập captcha), nhớ tên đăng nhập, không lưu mật khẩu.
- Tra cứu hóa đơn mua vào và bán ra theo khoảng ngày, tự chia 30 ngày mỗi lần gọi, 50 bản ghi mỗi trang.
- Lấy chi tiết hóa đơn (tối đa 3 yêu cầu song song), tải XML/HTML/PDF, gộp PDF.
- Tiếp tục tải sau khi lỗi hoặc tắt app, không tải lại phần đã có.
- Cảnh báo khi số hóa đơn lấy về ít hơn tổng số cổng báo.

### Kho hóa đơn trên máy
- Lưu theo MST và tháng tại `%AppData%\E-Tax\store`, ghi an toàn khi mất điện.
- Màn hình kho: xem theo doanh nghiệp, lọc, phân trang, mở file, ghi chú, xuất Excel theo bộ lọc.
- File tháng hỏng được cách ly thành `.corrupt` (không xóa), báo rõ tên file và MST.
- Sao lưu và khôi phục bằng file ZIP. Khôi phục gộp theo từng hóa đơn và tự tạo bản sao lưu an toàn trước khi ghi.
- Sao lưu tự động mỗi ngày khi mở app, giữ 7 bản gần nhất.

### Phân loại và kiểm tra tự động
- Phân loại hóa đơn gốc, thay thế, điều chỉnh, bị thay thế, bị điều chỉnh, bị hủy; liên kết với hóa đơn gốc.
- Hóa đơn bị hủy và bị thay thế không được tính vào số thuế; file xuất ghi rõ số hóa đơn bị loại.
- Chặn xuất khi có hóa đơn không rõ trạng thái, hóa đơn ngoại tệ hoặc hóa đơn lấy chi tiết lỗi.
- Cảnh báo: tổng dòng hàng lệch tổng hóa đơn, hóa đơn không có dòng hàng, hóa đơn điều chỉnh có hóa đơn gốc ở kỳ khác.
- Danh sách nhà cung cấp rủi ro lưu trên máy.

### Bảng kê và tờ khai
- Bảng kê hóa đơn mua vào, bán ra theo thuế suất, xuất Excel.
- Dự thảo tờ khai 01/GTGT (cần tìm cả mua vào và bán ra của cùng kỳ).
- Dự thảo phụ lục giảm thuế 8% (chỉ cần bán ra).
- Xuất file cho phần mềm kế toán theo mẫu chung, cấu hình được cột.

### Đóng gói và vận hành
- Phiên bản 1.0.0, bắt lỗi toàn cục và ghi log, chỉ cho chạy một bản app.
- Tự tải Chromium khi cần để tạo PDF, báo lỗi rõ khi không tải được.
- Kịch bản đóng gói ZIP và kịch bản installer (Inno Setup).

## Làm một phần

| Chức năng | Đã có | Còn thiếu |
|---|---|---|
| Ghi nhớ đăng nhập | Nhớ tên đăng nhập | Không lưu mật khẩu (có chủ đích) |
| Bộ cài | Gói ZIP | Biên dịch installer, ký số |
| Kiểm tra cập nhật | Cơ chế đọc manifest | Địa chỉ đặt manifest |
| Kỳ của hóa đơn điều chỉnh/thay thế | Tính theo ngày lập, kèm cảnh báo | Quy tắc chính thức, chờ kế toán xác nhận |
| File nhập MISA, 1C, Bravo, Fast, Vacom | Mẫu chung | Khớp mẫu nhập thật của từng phần mềm |
| Một số kiểm tra số liệu | Cảnh báo | Đối chiếu với dữ liệu thật |

## Chưa làm

- Tải tờ khai, thông báo, giấy nộp tiền, bảng tổng hợp nghĩa vụ thuế từ thuedientu: cần khảo sát cổng bằng tài khoản thật.
- Xuất XML tờ khai và file nhập HTKK: cần schema chính thức để đối chiếu.
- Kiểm tra gói dịch vụ, giới hạn thiết bị: cần server (V2).
- Kết nối trực tiếp phần mềm kế toán.
- Kiểm thử trên nhiều phiên bản Windows và với dữ liệu thật.

## Giả định chưa kiểm chứng

- Mã trạng thái `tthai`: 1 gốc, 2 thay thế, 3 điều chỉnh, 4 bị thay thế, 5 bị điều chỉnh, 6 bị hủy.
- Tên trường hóa đơn gốc trong dữ liệu cổng (`khmshdgoc`, `khhdgoc`, `tdlhdgoc`).
- Cách ghép số liệu vào chỉ tiêu tờ khai 01/GTGT và cách tính phụ lục 8%.

## Việc trước khi phát hành

1. Chạy kiểm thử trên tài khoản thật và đối chiếu tờ khai với HTKK.
2. Kế toán xác nhận các quy tắc tính: kỳ của hóa đơn điều chỉnh, dấu số tiền điều chỉnh, chỉ tiêu tờ khai, ngoại tệ.
3. Thay key cấp bản quyền phát hành.
4. Biên dịch installer và ký số.
5. Merge vào `master` và đóng bản 1.0.0.

## Định hướng V2

Nâng lên .NET 10, tách phần lõi dùng chung, thêm server cho tài khoản, gói dịch vụ và giới hạn thiết bị, hoàn thiện các chức năng còn lại theo yêu cầu và phản hồi người dùng V1.
