# Design — Thiết kế lại giao diện theo Phương án 3 (Ribbon kiểu Office)

Người dùng chọn Phương án 3 ngày 2026-11-26. Mẫu: `prototypes/5-phuong-an.html#o=3`, ảnh `screenshots/pa3-main.png`, `screenshots/pa3-login.png`.

## Mục tiêu / không làm

- Mục tiêu: màn hình chính dạng ribbon như Excel, màn đăng nhập dạng thẻ có dải xanh, mọi ô nhập có nhãn, kích thước/khoảng cách đồng nhất.
- **Không đổi nghiệp vụ**: giữ nguyên tên control, event handler và code trong `Form1.cs`. Chỉ đổi bố cục (Designer) và style (UiTheme). Code hiện không tham chiếu `mainSplitContainer`, `gbNguoiBan`, `gbInvoiceType` nên đổi container cha an toàn.
- Không làm dark mode, không đổi font ngoài Segoe UI.

## Hệ thống thiết kế (UiTheme)

| Token | Giá trị |
|---|---|
| Primary (Office blue) | `#185ABD` |
| Chữ | `#1F1F1F`, phụ `#5C5C5C` |
| Viền | `#D1D1D1`, kẻ dòng bảng `#E6E6E6` |
| Nền ribbon / thanh lọc | `#F3F3F3` / `#FAFAFA` |
| Dòng chọn | `#CFE3FF` |
| Trạng thái | ok `#107C10`/`#DFF6DD`, cảnh báo `#8A5A00`/`#FFF4CE`, lỗi `#A80000`/`#FDE7E9`, thông tin `#185ABD`/`#DEECF9` |
| Font | Segoe UI 9pt; nhãn nhóm ribbon 8pt; tiêu đề đăng nhập 15pt bold |
| Kích thước | ô nhập/nút nhỏ cao 26px trong ribbon, 32px ở đăng nhập; nút lớn ribbon 58×74 (icon 24px + chữ 2 dòng); dòng bảng 28px, tiêu đề bảng 34px |

Icon: SVG kiểu Lucide nhúng trong code, vẽ ra Bitmap bằng thư viện `Svg` đã có (đang dùng cho captcha), tô màu primary. Không dùng emoji.

## Bố cục màn hình chính (từ trên xuống, toàn bộ Dock)

1. **Ribbon** (Dock Top, cao ~96px, nền trắng, viền dưới). Một trang lệnh duy nhất (bỏ dải tab "Trang chủ/Bảng kê/…" của mẫu vì 1 trang đã đủ chỗ — tránh lệnh bị giấu). Các nhóm, mỗi nhóm có nhãn ở đáy và vạch ngăn bên phải:
   - **Tra cứu**: Kỳ thuế (`cbPeriodKind`, `cbPeriodNumber`, `nudPeriodYear`), Từ–đến (`dtpFromDate`, `dtpToDate`), 3 radio (`rbSold`, `rbBought`, `rbAllInvoices`), nút lớn **Tìm kiếm** (`btnLeftSearch`).
   - **Hóa đơn**: Xem hóa đơn (`btnXemHD`), In PDF (`btnCnKoPdf`), Gộp PDF (`btnGopPdf`), Tải HĐ gốc (`btnTaiHDGoc`).
   - **Xuất Excel**: Danh sách (`btnExportDS`), Chi tiết (`btnExportChiTiet`), Bảng kê mua (`btnExportBangKeMua`), Bảng kê bán (`btnExportBangKeBan`).
   - **Kê khai**: Lập tờ khai (`btnKeKhai`), Kho hóa đơn (`btnKhoHoaDon`, chuyển từ góc phải form vào đây).
   - **Kiểm tra**: Kiểm tra NCC (`btnKiemTraNCC`); ô MST + nút "Kiểm tra DN mua lẻ" (`txtKtraMaDoanhNghiep`, `btnKtraDNMuaLe`).
   - Cửa sổ hẹp: ribbon cuộn ngang thay vì cắt (AutoScroll), kích thước tối thiểu giữ như hiện tại.
2. **Thanh lọc** (Dock Top, cao 36px, nền `#FAFAFA`): Số HĐ (`txtLocSoHD`), NCC tên/MST (`txtLocNCC`), Loại (`cboLocLoai`), `chkChiCoGhiChu`, nút Lọc (`btnRightSearch`); bên phải: ô ghi chú HĐ đang chọn (`txtGhiChu` 1 dòng) + nút Ghi chú (`btnMoGhiChu`); ngoài cùng phải: "Hiển thị x / y hóa đơn".
3. **Tab bảng** (Fill): dải tab phẳng gạch chân xanh (tự vẽ; `tabControlMain` ẩn tiêu đề gốc), chữ: Tổng hợp · Chi tiết · Bảng kê mua · Bảng kê bán · Bảng kê thuế · VAT nộp, kèm số dòng. Tên control giữ nguyên (`tabVATNop` vẫn là "Bảng kê thuế", `tabGiamThue` vẫn là "VAT nộp").
4. **Thanh tiến độ tải** (`downloadProgressBar`, `lblDownloadStatus`): một dải mảnh trên thanh trạng thái, chỉ hiện khi đang tải.
5. **Thanh trạng thái** nền xanh `#185ABD` chữ trắng: tổng bán ra/mua vào · người dùng · Đăng xuất · bản quyền. `lblVersion` và `lblStatusMessage` (thông báo tạm) chuyển xuống thanh này.

Bảng: tiêu đề nền `#F3F3F3` chữ đậm tối (thay nền xanh đặc hiện tại), cột tiền căn phải, cột "Trạng thái" vẽ nhãn màu (chip) theo `InvoiceClassifier`.

## Màn đăng nhập

Thẻ trắng 420px giữa nền `#F3F3F3`, dải xanh trên cùng "E-Tax · Đăng nhập cổng hóa đơn điện tử". Nhãn trên mỗi ô: "Mã số thuế / Tài khoản", "Mật khẩu", "Mã xác nhận". Ô viền mảnh 32px (thay kiểu gạch chân hiện tại). Captcha 48px + nút tải lại vuông 48px, ô nhập mã dưới ảnh, ô tick "Duy trì đăng nhập", nút Đăng nhập 40px rộng hết thẻ. Dòng chú thích "App không lưu mật khẩu". Khối kích hoạt (`panelActivation`) nằm trong thẻ, dưới nút đăng nhập, cùng style.

## Form phụ

`KeKhaiForm`, `KhoHoaDonForm`, `TraCuuForm`, `ProcessingForm`: chỉ áp màu/font/nút/bảng qua `UiTheme` (không đổi bố cục).

## Rủi ro

- Viết lại phần bố cục trong `Form1.Designer.cs` (~1.350 dòng): dễ sót control → kiểm bằng test đếm control/handler (task T-001.1) và chạy demo.
- Icon SVG render lúc chạy: lỗi render phải fallback về nút chỉ có chữ, không làm hỏng app.
- Kiểm tra giao diện bằng chế độ demo (`ETAX_UI_DEMO=1`, `activation`) — không gọi cổng thuế.

## CR-003 — chi tiết sát mẫu

- Màu nhấn: Chuẩn #1E3A5F, Menu trái #0F766E, Ribbon #185ABD, Gọn #2563EB; đều ≥ 4.5:1 với chữ trắng. `UiTheme.Primary/PrimaryHover/HoverBack/Selection` đổi theo kiểu, lưu `[ThreadStatic]` (app một luồng UI; test chạy song song không giẫm nhau).
- Ô nhập: `InputHost` vẽ viền bo 4px (màu nhấn khi focus); control gốc đặt trong khung cắt để giấu viền Windows (ComboBox 1px, DateTimePicker 2px), TextBox/NumericUpDown bỏ viền. Ribbon trả viền gốc.
- Menu thả ("Xuất Excel ▾", "PDF ▾", "Bảng kê ▾"): Panel ẩn trong panelSearch chứa chính nút Designer (giống popup lọc), nên handler, chữ "Đang xử lý..." và khóa nút vẫn của nút gốc; khi lệnh trong menu đang chạy, nút mở menu hiện chữ đó.
- Bấm vào bảng đóng popup/menu. Lệnh trong menu bấm xong thì menu đóng.
- Demo (Debug) có trạng thái HĐ (phần lớn gốc, vài HĐ thay thế/điều chỉnh/hủy) để nhãn màu giống dữ liệu thật.
- Chưa làm theo mẫu: cột chọn (checkbox) và cột "Chiều" trong mẫu không có trong bảng thật; bảng thật nhiều cột hơn nên ở 1280px vẫn phải cuộn ngang tới "Phân loại" — dùng bánh răng "Cột" để ẩn cột ít dùng.

## CR-004 — mảng màu

- `appBar` cao 40, nền màu nhấn, vẽ tay (không thêm control): icon + "E-Tax" đậm + "· Tra cứu hóa đơn điện tử" (`OnPrimaryMuted` #E6EEF6), mép phải tên tài khoản lấy từ `lblStatusUser`.
- Menu trái: nền màu nhấn, chữ/icon trắng, mục đang chọn nền `PrimaryHover`.
- Kiểu Gọn đổi màu nhấn #2563EB → #1D4ED8 vì chữ phụ trên thanh tiêu đề chỉ đạt 4.41:1 (test bắt được).
