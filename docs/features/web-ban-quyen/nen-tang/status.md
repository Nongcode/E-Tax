# Status — Nền tảng web

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.
> Khi tiếp tục: đọc "Tóm tắt nhanh" + "File chính" là đủ, không cần đọc lại toàn bộ code.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-001 — Nền tảng web |
| Bước Superpowers | writing-plans — plan.md đã viết, chờ người dùng duyệt và chọn cách thực hiện |
| Task đang làm | — |
| **Bước tiếp theo** | Người dùng duyệt plan.md, chọn Subagent-driven hoặc Native, rồi làm T-001.1 |
| Branch | feature/web-ban-quyen |
| Cập nhật lần cuối | 2026-11-16 23:55 |
| Ghi chú dở dang | Chưa có code |

## File chính

| File | Vai trò |
|---|---|
| (chưa có) | Điền khi làm. Dự kiến: `web/src/api/`, `web/src/mocks/mockDb.ts`, `web/src/auth/`, `web/src/layouts/`, `web/src/router.tsx` |

## Yêu cầu

### CR-001 — Nền tảng web · 🟡 IN_PROGRESS · 2026-11-16

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | Khung dự án `web/` (Vite, React, antd, Vitest, ESLint) | ⬜ TODO | | |
| T-001.2 | Hàm định dạng và kiểm tra dữ liệu | ⬜ TODO | | |
| T-001.3 | Hợp đồng kiểu dữ liệu và `apiFetch` | ⬜ TODO | | |
| T-001.4 | mockDb: lưu trữ, seed, xác thực, gói, cài đặt | ⬜ TODO | | R1–R3, R16, R18, R20 |
| T-001.5 | mockDb: đơn hàng và xác nhận thanh toán | ⬜ TODO | | R4–R6, R15, R17 |
| T-001.6 | mockDb: bản quyền và máy | ⬜ TODO | | R7–R15 |
| T-001.7 | mockDb: khách hàng và số liệu (admin) | ⬜ TODO | | R19, stats |
| T-001.8 | MSW handlers và các hàm `api/*` | ⬜ TODO | | R16 |
| T-001.9 | Phiên đăng nhập, bảo vệ route, trang đăng nhập/đăng ký/quên mật khẩu | ⬜ TODO | | |
| T-001.10 | Thành phần dùng chung, 404/403, ErrorBoundary | ⬜ TODO | | |
| T-001.11 | Layout, router, công cụ dev, e2e TC-05 | ⬜ TODO | | Playwright + ảnh chụp |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| — | Chưa chạy | — | — |

## Nhật ký phiên

- 2026-11-16 23:15 — Tạo folder và design.md (màn hình chi tiết) trong bước brainstorming.
- 2026-11-16 23:55 — writing-plans: viết plan.md, 11 task T-001.1 → T-001.11. Bổ sung R18–R20 vào spec cha.
