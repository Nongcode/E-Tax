# Status — Nền tảng web

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.
> Khi tiếp tục: đọc "Tóm tắt nhanh" + "File chính" là đủ, không cần đọc lại toàn bộ code.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-001 — Nền tảng web |
| Bước Superpowers | subagent-driven-development (người dùng chọn 2026-11-16) |
| Task đang làm | T-001.4 |
| **Bước tiếp theo** | T-001.4 mockDb: lưu trữ, seed, xác thực, gói |
| Branch | feature/web-ban-quyen |
| Cập nhật lần cuối | 2026-11-16 23:34 |
| Ghi chú dở dang | Sổ SDD: `.superpowers/sdd/plan/progress.md` (git-ignored) |

## File chính

| File | Vai trò |
|---|---|
| web/package.json, web/vite.config.ts | Cấu hình, scripts, Vitest |
| web/src/App.tsx | `AppProviders` (antd vi_VN + theme, AntApp, React Query) |
| web/src/theme.ts | Theme #0075D6 |
| web/src/test/setup.ts | Thiết lập test chung |

## Yêu cầu

### CR-001 — Nền tảng web · 🟡 IN_PROGRESS · 2026-11-16

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | Khung dự án `web/` (Vite, React, antd, Vitest, ESLint) | ✅ DONE | ✅ vitest 1/1, lint, build. Bỏ qua Playwright: chỉ có khung, chưa có màn hình | 5df2af0, review sạch |
| T-001.2 | Hàm định dạng và kiểm tra dữ liệu | ✅ DONE | ✅ vitest 17/17, lint, build. Bỏ qua Playwright: hàm thuần, chưa có màn hình | 6b1673d, review sạch |
| T-001.3 | Hợp đồng kiểu dữ liệu và `apiFetch` | ✅ DONE | ✅ vitest 24/24, lint, build. Bỏ qua Playwright: chưa có màn hình | 62b9de2, review sạch |
| T-001.4 | mockDb: lưu trữ, seed, xác thực, gói, cài đặt | 🔄 DOING | | |
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
| 2026-11-16 23:25 | `npm run test && npm run lint && npm run build` (T-001.1) | ✅ 1/1, 0 lỗi lint, build OK | |
| 2026-11-16 23:32 | `npm run test && npm run lint && npm run build` (T-001.2) | ✅ 17/17, lint, build OK | |
| 2026-11-16 23:34 | `npm run test && npm run lint && npm run build` (T-001.3) | ✅ 24/24, lint, build OK | |

## Nhật ký phiên

- 2026-11-16 23:15 — Tạo folder và design.md (màn hình chi tiết) trong bước brainstorming.
- 2026-11-16 23:55 — writing-plans: viết plan.md, 11 task T-001.1 → T-001.11. Bổ sung R18–R20 vào spec cha.
- 2026-11-16 23:25 — T-001.1 xong (5df2af0): react 18.3.1, antd 5.29.3, vite 6.4.6, vitest 3.2.7, msw 2.15.0, react-router-dom 7.18.5, TS 5.7.3. Review sạch.
- 2026-11-16 23:32 — T-001.2 xong (6b1673d): 16 test mới. Review sạch.
- 2026-11-16 23:34 — T-001.3 xong (62b9de2): 7 test client (401 không token không kích hoạt hết phiên). Review sạch.
