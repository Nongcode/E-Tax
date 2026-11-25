# Status — Địa chỉ API cổng hoá đơn điện tử

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | ✅ DONE |
| Yêu cầu đang làm | CR-001 — Chuyển địa chỉ API từ `:30000/` sang `/api/` |
| Bước Superpowers | Không áp dụng (change nhỏ, sửa lỗi) |
| Task đang làm | — |
| **Bước tiếp theo** | Không — đã đẩy lên master |
| Branch | master |
| Cập nhật lần cuối | 2026-11-25 17:10 |
| Ghi chú dở dang | TC-01 đạt (captcha hiện). TC-02 đạt. Chờ người dùng đăng nhập lại để chạy TC-03 (tra cứu) sau T-001.5. Rủi ro: sắp theo 1 trường, phân trang có thể sót/trùng khi nhiều HĐ cùng tdlap — theo dõi cảnh báo thiếu đủ (SearchTotals) |

## File chính

| File | Vai trò |
|---|---|
| E-Tax/GdtApi.cs | Hằng số địa chỉ API, tạo request |
| E-Tax/Form1.cs | Captcha, đăng nhập, tra cứu |

## Yêu cầu

### CR-001 — Chuyển địa chỉ API từ `:30000/` sang `/api/` · ✅ DONE · 2026-11-25

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | Đổi BaseUrl (TDD) | ✅ DONE | Build 0 lỗi, 622/622 test. Bỏ qua Playwright: không đổi giao diện; kiểm tra thật ở T-001.2 | |
| T-001.3 | Thêm header request-id / Action / End-Point (TDD) + log nội dung lỗi đăng nhập | ✅ DONE | 624/624 test | Đăng nhập bị 403 dù captcha OK; trang chính thức gắn 3 header này qua axios interceptor |
| T-001.4 | End-Point `/tra-cuu/tra-cuu-hoa-don` cho lệnh query/sco-query (TDD) + log lỗi tra cứu | ✅ DONE | 627/627 test | Tra cứu bị 500 sau khi đăng nhập OK |
| T-001.5 | Sắp xếp tra cứu chỉ theo `tdlap:desc`; chuyển BuildSearchQuery sang GdtApi (TDD) | ✅ DONE | 630/630 test | Cổng báo 500 "Không hỗ trợ sắp xếp theo nhiều trường" |
| T-001.2 | Kiểm tra trên app thật (captcha, đăng nhập, tra cứu) | ✅ DONE | | Cần người dùng gõ captcha |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| 2026-11-25 15:15 | curl `:30000/captcha` | 🔴 Connection refused | Lý do mở CR-001 |
| 2026-11-25 15:20 | curl `/api/captcha` | ✅ 200, JSON key + content | Địa chỉ mới |
| 2026-11-25 15:40 | `dotnet test E-Tax.Tests` | ✅ 622/622 | Thêm GdtApiUrlTests (3) |
| 2026-11-25 15:45 | TC-01 mở app (ảnh `screenshots/T-001.2-captcha.png`) | ✅ captcha hiện | |
| 2026-11-25 15:21 | TC-02 đăng nhập (trước T-001.3) | 🔴 403 Forbidden ×3 | |
| 2026-11-25 16:05 | `dotnet test E-Tax.Tests` | ✅ 624/624 | Thêm GdtApiRequestHeaderTests (2) |
| 2026-11-25 15:24 | TC-02 đăng nhập (sau T-001.3) | ✅ 200 OK | |
| 2026-11-25 15:26 | TC-03 tra cứu bán ra | 🔴 500 mọi lát | Chưa có nội dung lỗi trong log |
| 2026-11-25 16:30 | `dotnet test E-Tax.Tests` | ✅ 627/627 | |
| 2026-11-25 15:30 | TC-03 tra cứu (sau T-001.4) | 🔴 500 "Không hỗ trợ sắp xếp theo nhiều trường" | Log đã có nội dung lỗi |
| 2026-11-25 16:45 | `dotnet test E-Tax.Tests` | ✅ 630/630 | Thêm GdtApiSearchQueryTests (3) |
| 2026-11-25 17:00 | TC-02, TC-03: đăng nhập 0110443702, tra cứu 01/08–30/11/2025 (ảnh `screenshots/T-001.2-tra-cuu-ok.png`) | ✅ Bán ra 16 HĐ, mua vào 1 HĐ | |

## Nhật ký phiên

- 2026-11-25 15:30 — Phát hiện cổng :30000 bị từ chối, trang chính thức dùng `/api/`. Tạo CR-001.
- 2026-11-25 15:40 — T-001.1 xong: BaseUrl → `/api/`, test GdtApiUrlTests.
- 2026-11-25 16:05 — Đăng nhập 403: so với trang chính thức thiếu header request-id/Action/End-Point → thêm vào GdtApi.CreateRequest (T-001.3). Chờ người dùng thử lại TC-02.
- 2026-11-25 16:30 — Đăng nhập OK. Tra cứu 500 → End-Point theo trang tra cứu (T-001.4). Ghi chú: test của project ghi log vào file log thật của app (%LOCALAPPDATA%\E-Tax\logs) — nên tách riêng.
- 2026-11-25 16:45 — Nguyên nhân 500: sort nhiều trường. Đổi sang `tdlap:desc` (T-001.5).
- 2026-11-25 17:00 — Người dùng xác nhận đăng nhập + tra cứu chạy. CR-001 xong phần code, chờ commit.
- 2026-11-25 17:10 — CR-001 DONE, commit + đẩy master. index.html không mở được bằng Playwright (giao thức file: bị chặn). Ảnh T-001.2-dien-san / tra-cuu-ok chứa MST và dữ liệu khách: giữ ở máy, không đưa lên git.
