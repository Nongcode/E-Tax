# Status — Thiết kế lại giao diện (Phương án 3 — Ribbon)

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-004 — thêm màu ("nhìn trắng quá" 2026-11-26) |
| Bước Superpowers | TDD tự làm (Native), chưa review cuối |
| Task đang làm | T-001.8 + T-002.7 — kiểm tra cuối trên repo thật |
| **Bước tiếp theo** | Hỏi người dùng tắt app → `verify.ps1 -KillApp` → người dùng xem lại → commit |
| Branch | feat/v1-desktop |
| Cập nhật lần cuối | 2026-11-26 |
| Ghi chú dở dang | Build/test/chụp ảnh chạy trên bản sao ở scratchpad (bỏ taskkill, bỏ khóa chạy-một-bản khi `ETAX_UI_DEMO`) để không tắt app đang đăng nhập. Chưa commit. |

## File chính

| File | Vai trò |
|---|---|
| E-Tax/Form1.Layouts.cs | Khung đổi bố cục (ApplyLayout), kiểu Chuẩn/Menu trái/Gọn, popup bộ lọc, thẻ số liệu, menu "Giao diện", bánh răng chọn cột |
| E-Tax/GridColumnLayout.cs, ColumnChooserForm.cs | Lưu/áp thứ tự + ẩn hiện cột; hộp thoại chọn cột |
| E-Tax/InvoiceKpis.cs | Số liệu thẻ tổng hợp, đếm điều kiện lọc |
| E-Tax/UserPreferences.cs | Thêm `Layout`, `Columns` (vẫn không lưu mật khẩu/JWT) |
| E-Tax/Form1.Ribbon.cs | Dựng ribbon, thanh lọc, dải tab phẳng, nhãn trạng thái, thanh tiến độ (chuyển control Designer sang khung mới) |
| E-Tax/Form1.Login.cs | Thẻ đăng nhập + khối kích hoạt |
| E-Tax/UiTheme.cs | Token màu/font, StyleGrid/StyleButton/ApplyTo, chip, tương phản WCAG |
| E-Tax/RibbonIcons.cs | 17 icon SVG nhúng → Bitmap (thư viện Svg) |
| E-Tax/Form1.cs | Gọi ApplyRibbonLayout/ApplyLoginLayout; SetLoginInputsEnabled liệt kê đích danh; cỡ cửa sổ mặc định 1280×800; chữ nút khi khôi phục |
| E-Tax/{KeKhaiForm,KhoHoaDonForm,TraCuuForm,ProcessingForm}.cs | `UiTheme.ApplyTo(this)` cuối constructor |
| E-Tax.Tests/Form1LayoutTests.cs, UiThemeTests.cs, RibbonIconsTests.cs | Test |

## Khác với design.md (quyết định khi làm)

- Lệnh phụ là **nút nhỏ xếp 3 hàng** (In PDF/Gộp PDF/Tải HĐ gốc; Chi tiết hàng hóa/Bảng kê mua/Bảng kê bán) thay vì toàn nút lớn: toàn nút lớn rộng ~1.400px, tràn ở 1280px.
- Bố cục dựng bằng code lúc chạy, **Designer vẫn hiện bố cục cũ**. Lý do: không phải viết lại ~1.350 dòng Designer, và mở đường cho tùy chọn nhiều kiểu bố cục (người dùng hỏi 2026-11-26).
- Cửa sổ < ~1.150px: ribbon cuộn ngang (nới cao cho thanh cuộn), thanh lọc tự xuống dòng.
- Cột "Phân loại" và "Rủi ro NCC" chuyển lên ngay sau "Tổng tiền", vẽ thành nhãn màu.
- Bảng: số tiền luôn định dạng vi-VN (33.420.000) bất kể vùng Windows (trước đây theo máy: máy en-US hiện 33,420,000).
- Đăng nhập: Enter = Đăng nhập (AcceptButton chỉ khi ở màn đăng nhập); ô nhập đổi từ nhiều dòng sang một dòng.

## Yêu cầu

### CR-001 · 🟡 IN_PROGRESS · 2026-11-26

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | Lưới an toàn control/handler | ✅ DONE | 28 test xanh trên bố cục cũ | |
| T-001.2 | UiTheme mới | ✅ DONE | RED (thiếu ChipKind) → GREEN | Ghi đè nhầm UiThemeTests.cs cũ rồi gộp lại từ git |
| T-001.3 | RibbonIcons | ✅ DONE | 21 test; test viết cùng code nên kiểm bằng đột biến (bỏ vẽ → 18 fail) | |
| T-001.4 | Ribbon + thanh lọc | ✅ DONE | RED 9 test → GREEN; ảnh `T-001.4-demo-1280.png`, `-1000.png` | Sửa: tabControl bị Dispose cùng khung cũ; ribbon tràn; thanh lọc bị cắt |
| T-001.5 | Tab phẳng + nhãn trạng thái | ✅ DONE | `StatusColumns_FollowTotalColumn`, chip theo rủi ro; ảnh `T-001.5-chip.png` | Dữ liệu demo không có trạng thái → toàn "Không rõ trạng thái" |
| T-001.6 | Màn đăng nhập + kích hoạt | ✅ DONE | 5 test; ảnh chỉ lưu ở scratchpad (có mã máy/MST thật) | **Sửa lỗi do thay đổi**: SetLoginInputsEnabled khóa luôn khối kích hoạt → hết hạn không kích hoạt được. Có test `ExpiredTrial_LocksLoginInputs_ButActivationStaysUsable` |
| T-001.7 | Form phụ | ✅ DONE | `ApplyTo_StylesNestedButtonsAndGrids`; ảnh Kho HĐ chỉ ở scratchpad (dữ liệu thật) | KeKhai chưa chụp được ở demo (cần dữ liệu tìm kiếm) |
| T-001.8 | Kiểm tra cuối | 🟡 | Bản sao: 764/764, 38 warning (mốc 40) | Chờ tắt app để chạy trên repo thật; người dùng thử tay |

### CR-002 · 🟡 IN_PROGRESS · 2026-11-26

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-002.1 | Khung đổi bố cục + lưu kiểu | ✅ DONE | Form1LayoutKindsTests (19): 4 kiểu giữ đủ control/handler, đổi 6 lần không mất/không gắn trùng handler | Kiểm bằng đột biến: giấu 1 nút → test fail |
| T-002.2 | Popup bộ lọc | ✅ DONE | Popup ẩn sẵn, chứa ô lọc; nút mở có số điều kiện | Popup là Panel trong panelSearch (control không rời form); cao theo nội dung |
| T-002.3 | Kiểu Menu trái | ✅ DONE | ảnh `T-002-SideMenu-*.png` | Lệnh nằm ở menu trái (không dùng menu thả xuống vì nút ẩn không PerformClick được); ghi chú + DN mua lẻ ở cuối menu |
| T-002.4 | Kiểu Gọn + kiểu Chuẩn | ✅ DONE | ảnh `T-002-Compact-1280.png`, `T-002-Classic-*.png` | Chuẩn/Gọn: ghi chú + DN mua lẻ trong popup |
| T-002.5 | Chọn cột | ✅ DONE | GridColumnLayoutTests (7), ColumnChooserFormTests (4), ảnh `T-002-cot.png` | STT/cột cố định luôn hiện, luôn đầu; lưu theo tên bảng |
| T-002.6 | Chọn giao diện ở thanh trạng thái | ✅ DONE | LayoutMenu test | Đổi ngay; chặn khi đang có tác vụ |
| T-002.7 | Kiểm tra cuối | 🟡 | Bản sao: 804/804, 38 warning | Chờ tắt app; bánh răng chưa bấm thử trên app chạy (không điều khiển được chuột trong môi trường này) |

### CR-003 · 🟡 IN_PROGRESS · 2026-11-26

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-003.1 | Màu nhấn theo kiểu | ✅ DONE | 6 test mới | Màu nhấn gắn theo luồng để test song song không lẫn |
| T-003.2 | Ô nhập viền bo + nút ghép loại HĐ | ✅ DONE | 3 test | |
| T-003.3 | Khung bảng, tiêu đề một dòng | ✅ DONE | 3 test | Vẫn cuộn ngang ở 1280px (bảng thật nhiều cột hơn mẫu) |
| T-003.4 | Thanh nút kiểu Chuẩn trong khung bảng | ✅ DONE | ảnh `T-003-Classic.png` | Lần đầu tràn 2 dòng → gộp "Bảng kê ▾"; chữ nút lệch → cố định cao 32 |
| T-003.5 | Menu trái theo PA2 | ✅ DONE | 2 test + ảnh bấm thật `T-003-SideMenu-excel.png`, `-filter.png` | Logo đè chữ lần đầu → sửa |
| T-003.6 | Gọn theo PA5 | ✅ DONE | ảnh `T-003-Compact.png` | |
| T-003.7 | Kiểm tra cuối | 🟡 | Repo thật: 821/821, 38 warning | Test viết cùng lúc với code (không RED trước) — ghi nhận lệch TDD; chờ chạy trên repo thật |

### CR-004 · 🟡 IN_PROGRESS · 2026-11-26

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-004.1 | Thanh tiêu đề / menu trái màu nhấn | ✅ DONE | `EachLayout_HasAccentBand` ×4 | |
| T-004.2 | Nền, vạch thẻ số liệu, tiêu đề bảng | ✅ DONE | contrast test thêm 3 cặp | Test bắt chữ phụ trên xanh kiểu Gọn 4.41:1 → đậm màu lên #1D4ED8 |
| T-004.3 | Kiểm tra cuối | 🟡 | Bản sao: 825/825, 38 warning | Chờ chạy trên repo thật |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| 2026-11-26 | Playwright 10 màn mẫu HTML | ✅ | |
| 2026-11-26 | `dotnet test` toàn bộ (bản sao) | ✅ 764/764 | |
| 2026-11-26 | Build `--no-incremental` (bản sao) | ✅ 0 lỗi, 38 warning | Không warning ở file mới |
| 2026-11-26 | Demo 1000/1280/1440, demo kích hoạt, Kho HĐ | ✅ sau khi sửa | Không gọi cổng thuế. Bỏ qua Playwright cho app (WinForms) |
| 2026-11-26 | `dotnet test` toàn bộ sau CR-002 (bản sao) | ✅ 804/804 | 38 warning, không ở file mới |
| 2026-11-26 14:22 | `verify.ps1 -KillApp` trên repo thật | ✅ 804/804, 0 lỗi, 38 warning (mốc 40) | Warning ở Form1.cs/TraCuuForm.cs là dòng cũ; app đã mở lại |
| 2026-11-26 | CR-003: build sạch + `dotnet test` (bản sao) | ✅ 821/821, 38 warning | Không warning ở file mới |
| 2026-11-26 16:49 | CR-003: `verify.ps1 -KillApp` trên repo thật | ✅ 821/821, 0 lỗi, 38 warning (mốc 40) | App đã mở lại |
| 2026-11-26 | CR-003: demo 4 kiểu 1280px, bấm thật menu Xuất Excel và Bộ lọc | ✅ | Ảnh `T-003-*.png` |
| 2026-11-26 | Demo 4 kiểu (`ETAX_UI_LAYOUT`), popup Chuẩn/Menu trái, 1000px | ✅ | |

## Chưa kiểm chứng

- Màn đăng nhập thật (có captcha) — cần tải captcha từ cổng, chưa làm.
- Form Kê khai, Tra cứu, Tiến trình — chưa chụp.
- High-DPI (125%/150%).


