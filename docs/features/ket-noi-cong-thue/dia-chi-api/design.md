# Design — Địa chỉ API cổng hoá đơn điện tử

## Kiến trúc

Form1 / InvoiceDetailService → `HttpClient` (`BaseAddress = GdtApi.BaseUrl`) → API hoadondientu.gdt.gov.vn. Mọi đường dẫn là tương đối, ghép sau `BaseUrl`.

## Vị trí code

| File | Symbol | Dòng | Vai trò |
|---|---|---|---|
| E-Tax/GdtApi.cs | `GdtApi.BaseUrl` | L12 | Địa chỉ gốc API |
| E-Tax/Form1.cs | constructor (`BaseAddress`) | L85 | HttpClient dùng chung |
| E-Tax/Form1.cs | `LoadCaptchaAsync` | L321 | GET `captcha` |
| E-Tax/Form1.cs | đăng nhập | L496 | POST `security-taxpayer/authenticate` |
| E-Tax/Form1.cs | tra cứu | L816, L858 | `query/invoices/...` |

## API

| Method | Path (sau BaseUrl) | Quyền | Request | Response |
|---|---|---|---|---|
| GET | captcha | — | — | `{key, content(svg)}` |
| POST | security-taxpayer/authenticate | — | username, password, cvalue, ckey | token |
| GET | query/invoices/sold, purchase, detail, export-xml | Bearer | tham số tìm kiếm | JSON / file |

## Dữ liệu

Không đổi.

## Giao diện

Không đổi.

## Nhật ký sửa đổi

### CR-001 — Chuyển địa chỉ API từ `:30000/` sang `/api/`

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| T-001.1 | E-Tax/GdtApi.cs:L12 | `BaseUrl` = `https://hoadondientu.gdt.gov.vn/api/` | Cổng :30000 bị từ chối |
| T-001.1 | E-Tax/Program.cs:L36 | Cập nhật chú thích địa chỉ | Đồng bộ |
| T-001.3 | E-Tax/GdtApi.cs `CreateRequest` | Thêm `request-id` (GUID mới), `Action` (rỗng), `End-Point` (`/`) | Cổng trả 403 nếu thiếu |
| T-001.3 | E-Tax/Form1.cs `LoginAsync` | Log 300 ký tự đầu của phản hồi lỗi | Chẩn đoán |
| T-001.4 | E-Tax/GdtApi.cs `EndPointFor` | `query/`, `sco-query/` → `/tra-cuu/tra-cuu-hoa-don`, còn lại `/` | Trang chính thức gửi End-Point = đường dẫn trang đang đứng |
| T-001.4 | E-Tax/Form1.cs `GetInvoiceListAsync` | Log 300 ký tự lỗi tra cứu | Chẩn đoán |
| T-001.5 | E-Tax/GdtApi.cs `BuildSearchQuery` (chuyển từ Form1) | `sort=tdlap:desc` | Cổng không cho sort nhiều trường |
| T-001.1 | E-Tax.Tests/GdtApiUrlTests.cs | 3 test ghép đường dẫn | Chống lùi lỗi |
