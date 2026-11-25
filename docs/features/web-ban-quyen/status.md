# Status — Web bán bản quyền E-Tax

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.
> Khi tiếp tục: đọc "Tóm tắt nhanh" + "File chính" là đủ, không cần đọc lại toàn bộ code.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-001 — Dựng web bán bản quyền (giao diện + API giả lập) |
| Bước Superpowers | trang-cong-khai: writing-plans |
| Task đang làm | — |
| **Bước tiếp theo** | Người dùng duyệt plan trang-cong-khai |
| Branch | feature/web-ban-quyen (tạo từ feature/giao-dien-hien-dai) |
| Cập nhật lần cuối | 2026-11-17 16:11 |
| Ghi chú dở dang | Code ở `web/`; nen-tang xong (CR-001 + CR-002). Người dùng chọn giữ nhánh (2026-11-17 16:11), làm tiếp các chức năng con trên nhánh này |

## File chính

| File | Vai trò |
|---|---|
| docs/features/web-ban-quyen/design.md | Spec gốc: kiến trúc, dữ liệu, hợp đồng API, quy tắc R1–R20, route, test |
| docs/features/web-ban-quyen/index.html | Prototype các màn hình chính |
| web/ | Mã nguồn React (xem nen-tang/design.md) |

## Chức năng con

| Chức năng con | Trạng thái | Bước tiếp theo |
|---|---|---|
| [nen-tang](nen-tang/status.md) | ✅ DONE | CR-001 11 task + CR-002 vá lỗi nhỏ (4 task), review tổng đã sửa |
| [trang-cong-khai](trang-cong-khai/status.md) | 🟡 IN_PROGRESS | Plan 4 task chờ duyệt |
| [tai-khoan-khach](tai-khoan-khach/status.md) | ⬜ TODO | Sau nen-tang |
| [quan-tri](quan-tri/status.md) | ⬜ TODO | Sau tai-khoan-khach (dùng đơn do khách tạo) |

## Yêu cầu

### CR-001 — Dựng web bán bản quyền · 🟡 IN_PROGRESS · 2026-11-16

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.0 | Brainstorming: chốt thiết kế, viết spec + prototype | ✅ DONE | ✅ Playwright index.html (ảnh `screenshots/T-001.0-*.png`) | Người dùng duyệt spec 2026-11-16 |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| 2026-11-16 23:20 | Playwright MCP: index.html cha — luồng Bảng giá → Mua gói 6 tháng → Thanh toán → Admin xác nhận → Thêm máy (mã sai/đúng) | ✅ pass | Console chỉ có 404 favicon (máy chủ tĩnh tạm) |
| 2026-11-16 23:20 | Playwright MCP: 19 màn hình ở khổ 390px, đo tràn ngang | ✅ pass (sau sửa) | Lần đầu tràn ở khu tài khoản: thêm `.page > * {min-width:0}` |
| 2026-11-16 23:20 | Playwright MCP: tai-khoan-khach/index.html → liên kết sâu `#ui:c-pay` | ✅ pass | |

## Nhật ký phiên

- 2026-11-16 23:15 — Brainstorming xong 4 phần thiết kế, người dùng duyệt từng phần. Tạo nhánh, viết spec design.md (cha + 4 con) và prototype index.html.
- 2026-11-16 23:55 — Người dùng duyệt spec. Bổ sung R18–R20, `OrderDetail.licenseId`, `License.activeMachines`. Viết plan nen-tang (11 task).
- 2026-11-17 00:31 — nen-tang ✅ DONE: 11 task theo subagent-driven-development, review tổng (opus) đã sửa 4 lỗi. 113 test, e2e 4/4.
