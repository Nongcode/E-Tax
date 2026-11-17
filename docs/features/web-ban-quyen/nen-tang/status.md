# Status — Nền tảng web

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.
> Khi tiếp tục: đọc "Tóm tắt nhanh" + "File chính" là đủ, không cần đọc lại toàn bộ code.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-002 — Vá lỗi nhỏ còn tồn đọng |
| Bước Superpowers | change: executing-plans (inline), đang làm T-002.3 |
| Task đang làm | — |
| **Bước tiếp theo** | T-002.3 — giao diện và phiên |
| Branch | feature/web-ban-quyen |
| Cập nhật lần cuối | 2026-11-17 15:51 |
| Ghi chú dở dang | T-002.1 102e2de, T-002.2 25edc20; ledger ở .superpowers/sdd/plan/progress.md |

## File chính

| File | Vai trò |
|---|---|
| web/package.json, web/vite.config.ts | Cấu hình, scripts, Vitest |
| web/src/App.tsx | `AppProviders` (antd vi_VN + theme, AntApp, React Query) |
| web/src/theme.ts | Theme #0075D6 |
| web/src/test/setup.ts | Thiết lập test chung |

## Yêu cầu

### CR-001 — Nền tảng web · ✅ DONE · 2026-11-17

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | Khung dự án `web/` (Vite, React, antd, Vitest, ESLint) | ✅ DONE | ✅ vitest 1/1, lint, build. Bỏ qua Playwright: chỉ có khung, chưa có màn hình | 5df2af0, review sạch |
| T-001.2 | Hàm định dạng và kiểm tra dữ liệu | ✅ DONE | ✅ vitest 17/17, lint, build. Bỏ qua Playwright: hàm thuần, chưa có màn hình | 6b1673d, review sạch |
| T-001.3 | Hợp đồng kiểu dữ liệu và `apiFetch` | ✅ DONE | ✅ vitest 24/24, lint, build. Bỏ qua Playwright: chưa có màn hình | 62b9de2, review sạch |
| T-001.4 | mockDb: lưu trữ, seed, xác thực, gói, cài đặt | ✅ DONE | ✅ vitest 38/38, lint, build. Bỏ qua Playwright: chưa có màn hình | c38887c, review sạch |
| T-001.5 | mockDb: đơn hàng và xác nhận thanh toán | ✅ DONE | ✅ vitest 52/52, lint, build. Bỏ qua Playwright: chưa có màn hình | c2a03a3, review sạch |
| T-001.6 | mockDb: bản quyền và máy | ✅ DONE | ✅ vitest 62/62, lint, build. Bỏ qua Playwright: chưa có màn hình | 9c409d7, review sạch |
| T-001.7 | mockDb: khách hàng và số liệu (admin) | ✅ DONE | ✅ vitest 69/69, lint, build. Bỏ qua Playwright: chưa có màn hình | f2bbfad, review sạch |
| T-001.8 | MSW handlers và các hàm `api/*` | ✅ DONE | ✅ vitest 75/75, lint, build. Bỏ qua Playwright: chưa có màn hình | 9d510ff, 968a85d (đổi tên setupMockServer), review sạch |
| T-001.9 | Phiên đăng nhập, bảo vệ route, trang đăng nhập/đăng ký/quên mật khẩu | ✅ DONE | ✅ vitest 92/92, lint, build. Playwright để T-001.11 (chưa có router/dev server) | 5f8019a, 433b234 (sửa 401 muộn, đăng xuất), review sạch |
| T-001.10 | Thành phần dùng chung, 404/403, ErrorBoundary | ✅ DONE | ✅ vitest 102/102, lint, build. Playwright để T-001.11 (chưa có router/dev server) | 31f7d4f, 8ddfcb3 (QueryState isPending), review đạt |
| T-001.11 | Layout, router, công cụ dev, e2e TC-05 | ✅ DONE | ✅ vitest 108/108, lint, build (không MSW), e2e 4/4 (TC-05a/b, login-admin, mobile-drawer). Ảnh screenshots/T-001.11-*.png | dcbaec5, f8529be (tách chunk, quyền /tai-khoan), review đạt |

### CR-002 — Vá lỗi nhỏ còn tồn đọng · 🟡 IN_PROGRESS · 2026-11-17

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-002.1 | Kiểm tra đầu vào của API giả lập | ✅ DONE | 102e2de | ✅ vitest 121/121, lint, build. Bỏ qua Playwright: chỉ đổi logic giả lập, chưa màn hình nào gọi các thao tác này |
| T-002.2 | Lớp API và khởi động (encode, retry, non-JSON, worker lỗi, dist) | ✅ DONE | 25edc20 | ✅ vitest 125/125, lint, build (dist không có mockServiceWorker.js), e2e 4/4 |
| T-002.3 | Giao diện và phiên (session, ngăn kéo, 404/403, StatusTag, màu, ngày) | ⬜ TODO | | Playwright + ảnh |
| T-002.4 | Cập nhật spec (R21–R24, kiểm tra phía máy chủ) | ⬜ TODO | | |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| 2026-11-16 23:25 | `npm run test && npm run lint && npm run build` (T-001.1) | ✅ 1/1, 0 lỗi lint, build OK | |
| 2026-11-16 23:32 | `npm run test && npm run lint && npm run build` (T-001.2) | ✅ 17/17, lint, build OK | |
| 2026-11-16 23:34 | `npm run test && npm run lint && npm run build` (T-001.3) | ✅ 24/24, lint, build OK | |
| 2026-11-16 23:37 | `npm run test && npm run lint && npm run build` (T-001.4) | ✅ 38/38, lint, build OK | |
| 2026-11-16 23:41 | `npm run test && npm run lint && npm run build` (T-001.5) | ✅ 52/52, lint, build OK | |
| 2026-11-16 23:44 | `npm run test && npm run lint && npm run build` (T-001.6) | ✅ 62/62, lint, build OK | |
| 2026-11-16 23:46 | `npm run test && npm run lint && npm run build` (T-001.7) | ✅ 69/69, lint, build OK | |
| 2026-11-16 23:51 | `npm run test && npm run lint && npm run build` (T-001.8) | ✅ 75/75, lint, build OK | |
| 2026-11-17 00:01 | `npm run test && npm run lint && npm run build` (T-001.9) | ✅ 92/92, lint, build OK | |
| 2026-11-17 00:07 | `npm run test && npm run lint && npm run build` (T-001.10) | ✅ 102/102, lint, build OK, không stderr | |
| 2026-11-17 00:18 | `npm run test && npm run lint && npm run build && npm run e2e` (T-001.11) | ✅ 108/108, lint, build, e2e 4/4; grep MSW trong dist = 0 | Ảnh: screenshots/T-001.11-*.png |
| 2026-11-17 15:46 | `npm run test && npm run lint && npm run build` (T-002.1) | ✅ 121/121, lint 0, build OK | 8 test mới (mockDb.validation, body JSON hỏng) |
| 2026-11-17 15:51 | test + lint + build + `vite build --mode development` + e2e (T-002.2) | ✅ 125/125, lint 0, build OK; dist prod không có worker, dist mock có; e2e 4/4 | |
| 2026-11-17 00:31 | Kiểm tra cuối: `npm run test`, `lint`, `build`, `e2e`, grep giả lập trong dist | ✅ 113/113 (không stderr), lint 0, build không cảnh báo, e2e 4/4, grep = 0 | Sau sửa review tổng (6d5bc75) |

## Nhật ký phiên

- 2026-11-16 23:15 — Tạo folder và design.md (màn hình chi tiết) trong bước brainstorming.
- 2026-11-16 23:55 — writing-plans: viết plan.md, 11 task T-001.1 → T-001.11. Bổ sung R18–R20 vào spec cha.
- 2026-11-16 23:25 — T-001.1 xong (5df2af0): react 18.3.1, antd 5.29.3, vite 6.4.6, vitest 3.2.7, msw 2.15.0, react-router-dom 7.18.5, TS 5.7.3. Review sạch.
- 2026-11-16 23:32 — T-001.2 xong (6b1673d): 16 test mới. Review sạch.
- 2026-11-16 23:34 — T-001.3 xong (62b9de2): 7 test client (401 không token không kích hoạt hết phiên). Review sạch.
- 2026-11-16 23:37 — T-001.4 xong (c38887c): 14 test mockDb (R1–R3, R16, R18, R20, storage hỏng, email hoa thường). Thông báo: UNAUTHORIZED 'Vui lòng đăng nhập để tiếp tục', PLAN_NOT_FOUND 'Không tìm thấy gói'.
- 2026-11-16 23:41 — T-001.5 xong (c2a03a3): 14 test đơn hàng (R4–R6, R15, R17, QR, cộng tháng cuối tháng). NOT_FOUND 'Không tìm thấy dữ liệu'.
- 2026-11-16 23:44 — T-001.6 xong (9c409d7): 10 test bản quyền (R7–R15, mã máy có khoảng trắng/chữ thường). Tách licenses.ts, errors.ts.
- 2026-11-16 23:46 — T-001.7 xong (f2bbfad): 7 test admin (R19, khoá → R3/R20, số liệu tháng). mockDb hoàn tất R1–R20.
- 2026-11-16 23:51 — T-001.8 xong (9d510ff, 968a85d): 34 handler khớp bảng API, 6 test qua MSW. Đổi tên useMockServer → setupMockServer để không vướng luật react-hooks.
- 2026-11-17 00:01 — T-001.9 xong (5f8019a, 433b234): phiên đăng nhập, RequireAuth/RequireRole, 3 trang xác thực. Review: 3 lỗi Important (401 muộn sau đăng xuất, đăng xuất về /dang-nhap, thiếu test hết phiên) đã sửa.
- 2026-11-17 00:07 — T-001.10 xong (31f7d4f, 8ddfcb3): CopyField, StatusTag, PageHeader, QueryState, ErrorBoundary, 404/403. Review đạt; sửa QueryState với query chưa chạy, dọn test (clipboard, route '/').
- 2026-11-17 00:18 — T-001.11 xong (dcbaec5, f8529be): layout, router, công cụ dev, e2e 4/4, 4 ảnh chụp. Review: bỏ nâng ngưỡng chunk → tách chunk; /tai-khoan thêm RequireRole customer theo bảng Route.
- 2026-11-17 00:31 — Review tổng (opus): sẵn sàng sau khi sửa. Đã sửa 4 lỗi Important (6d5bc75): `next` chứa ký tự điều khiển → màn lỗi vĩnh viễn; xoá cache React Query khi đăng xuất/hết phiên/đăng nhập; ErrorBoundary theo từng layout và reset khi đổi route; nội dung 403 theo vai trò. Lỗi nhỏ để sau: menu ngăn kéo tự mở lại sau khi đổi cỡ cửa sổ; phiên hỏng dạng `{user:{}}` làm vỡ layout; mockDb thiếu kiểm tra đầu vào (gia hạn tháng âm, khoá admin khác, giá/số máy, JSON hỏng → 500) — cần ghi "kiểm tra phía server" vào spec trước khi làm API C#; mã lỗi INVALID_MACHINE_NAME/LICENSE_PERMANENT chưa có trong spec; tham số đường dẫn chưa encode; retry cả lỗi 4xx; worker.start() lỗi trên HTTP LAN → trắng trang.
- 2026-11-17 00:31 — verification-before-completion: 113/113, lint, build, e2e 4/4, dist không chứa giả lập. CR-001 nen-tang ✅ DONE.
- 2026-11-17 00:32 — Bỏ qua Playwright mở index.html lần cuối: chỉ đổi chữ nhãn tiến độ; trình duyệt MCP chặn file:// và không được phép chạy máy chủ tĩnh tạm.
- 2026-11-17 15:39 — Người dùng yêu cầu vá các lỗi nhỏ còn tồn đọng. Lập plan CR-002 (4 task), chờ duyệt.
- 2026-11-17 15:46 — Người dùng duyệt CR-002 ("thực hiện đi"). T-002.1 xong (102e2de): VALIDATION_ERROR, LICENSE_REVOKED, CANNOT_LOCK_ADMIN; INVALID_MACHINE_NAME khi thiếu tên máy; body JSON hỏng → 400; tìm mã máy chỉ khớp máy đang dùng; test cuối tháng theo giờ máy (không thấy RED được ở UTC+7 — ghi ruling).
- 2026-11-17 15:51 — T-002.2 xong (25edc20): encodeURIComponent 13 chỗ trong api/*.ts; ApiError UNKNOWN khi 2xx không JSON; shouldRetry (không thử lại 4xx); main.tsx bắt lỗi worker.start; DevToolbar message.error; plugin dropMockWorker. Test mới: client.test (2), api.test (1), DevToolbar.test.tsx.
