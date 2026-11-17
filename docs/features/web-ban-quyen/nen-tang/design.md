# Design — Nền tảng web (khung dự án, API, giả lập, đăng nhập, layout)

> Thuộc [Web bán bản quyền E-Tax](../design.md). Hợp đồng API, mô hình dữ liệu, quy tắc R1–R20 nằm ở design.md cha.

## Kiến trúc

Khởi động: `main.tsx` → `worker.start()` (chỉ khi bật `VITE_USE_MOCK`) → `App`. `App` gồm ConfigProvider (vi_VN + theme), QueryClientProvider, AuthProvider, RouterProvider.

Luồng dữ liệu: màn hình → hook TanStack Query → `src/api/*.ts` → `client.ts` (`fetch` + Bearer) → MSW handlers → `mockDb` (localStorage key `etax-mockdb-v1`).

## Phạm vi

- **Khung dự án:** Vite + React + TypeScript, ESLint, Vitest (jsdom + MSW `setupServer`), Playwright (webServer `npm run dev`).
- **`theme.ts`:** `colorPrimary #0075D6`, `borderRadius 8`, phông Segoe UI / Inter.
- **`api/types.ts`:** toàn bộ kiểu dữ liệu theo hợp đồng.
- **`api/client.ts`:** gồm `apiFetch<T>(path, init)` và `ApiError {status, code, message}`. Xử lý 401 và lỗi mạng như design.md cha.
- **`api/*.ts`:** mỗi endpoint một hàm, mỗi hàm một dòng `apiFetch`.
- **`mocks/mockDb.ts`:**
  - `createMockDb(storage, now)` trả về các thao tác nghiệp vụ;
  - lỗi ném `MockError(status, code, message)`;
  - `reset()` nạp lại seed.
- **`mocks/seed.ts`:** dữ liệu mẫu như design.md cha.
- **`mocks/handlers.ts`:** map toàn bộ endpoint sang `mockDb`, kiểm tra token và vai trò (R16). Trong trình duyệt có độ trễ 300ms.
- **`auth/`:**
  - `AuthProvider` lưu `{token, user}` ở localStorage `etax-session`, có các hàm `login`, `register`, `logout`, `refreshMe`;
  - `RequireAuth`, `RequireRole`.
- **`layouts/`:**
  - `PublicLayout`:
    - header có logo chữ "E-Tax", menu Trang chủ / Bảng giá / Tải ứng dụng, nút Đăng nhập;
    - khi đã đăng nhập thì thay nút bằng avatar có menu Tài khoản / Quản trị / Đăng xuất;
    - có footer.
  - `AccountLayout`: Sider trắng. Màn từ 992px trở lên thì cố định, nhỏ hơn thì thành Drawer.
  - `AdminLayout`: Sider tối, cùng cơ chế với `AccountLayout`.
- **`features/auth/`:**
  - `LoginPage`, `RegisterPage`, `ForgotPasswordPage`: Card rộng 400px, căn giữa.
  - Kiểm tra dữ liệu nhập:
    - email đúng định dạng;
    - mật khẩu ≥ 8 ký tự;
    - SĐT `^0\d{9}$`;
    - MST `^\d{10}(-\d{3})?$`, không bắt buộc;
    - nhập lại mật khẩu phải khớp.
- **`shared/`:**
  - Hàm: `formatVnd`, `formatDate`, `formatDateTime`, `normalizeMachineId`, `isValidMachineId`, `isValidTaxCode`, `isValidPhone`.
  - `CopyField`: hiện giá trị kèm nút sao chép, sao chép xong báo `message.success('Đã sao chép')`.
  - `StatusTag`: chuyển trạng thái đơn/bản quyền thành nhãn tiếng Việt có màu.
  - `PageHeader`, `QueryState` (đang tải / lỗi / trống).
- **`pages/`:** `NotFoundPage`, `ForbiddenPage`.
- **Thanh công cụ dev** (chỉ khi bật giả lập): nút nổi "Dữ liệu mẫu", gồm "Đặt lại" và "Đăng nhập nhanh: khách / admin".

## Nhãn trạng thái

| Giá trị | Nhãn | Màu |
|---|---|---|
| order.pending | Chờ thanh toán | gold |
| order.paid | Đã thanh toán | green |
| order.cancelled | Đã huỷ | default |
| order.expired | Quá hạn | red |
| license.active | Còn hạn | green |
| license.expired | Hết hạn | red |
| license.revoked | Đã thu hồi | default |

## Vị trí code

| File | Symbol | Vai trò |
|---|---|---|
| web/src/theme.ts | `theme` | colorPrimary #0075D6, borderRadius 8, phông Segoe UI/Inter |
| web/src/App.tsx | `AppProviders`, `App` | ConfigProvider(vi_VN) → AntApp → QueryClientProvider |
| web/src/main.tsx | — | dayjs locale vi, render App |
| web/src/test/setup.ts | — | jest-dom, polyfill matchMedia/ResizeObserver, dọn localStorage |
| web/src/shared/format.ts | `formatVnd`, `formatDate`, `formatDateTime` | Tiền `1.200.000 ₫`, ngày DD/MM/YYYY, null → "Trọn đời" |
| web/src/shared/validators.ts | `normalizeMachineId`, `isValidMachineId`, `isValidTaxCode`, `isValidPhone`, `MACHINE_ID_MESSAGE` | Kiểm tra mã máy 16 hex, MST, SĐT |
| web/src/api/types.ts | `User`, `Plan`, `Order`, `OrderDetail`, `License`, `Activation`, `Admin*`, input types | Hợp đồng dữ liệu dùng chung |
| web/src/api/client.ts | `apiFetch`, `ApiError`, `configureClient` | fetch + Bearer; 401 có token → onUnauthorized; lỗi mạng → NETWORK; lỗi không JSON → UNKNOWN |
| web/src/mocks/memoryStorage.ts | `createMemoryStorage` | Storage trong bộ nhớ cho test |
| web/src/mocks/seed.ts | `createSeed(now)` | Dữ liệu mẫu: admin, khách, 4 gói, 2 đơn, bản quyền l-1, máy a-1, ngân hàng, phiên bản app |
| web/src/mocks/mockDb.ts | `createMockDb`, `MockError`, `DbState`, `MockDb` | Quy tắc nghiệp vụ giả lập, lưu localStorage `etax-mockdb-v1`; hỏng → nạp seed |
| web/src/mocks/mockDb.ts | `createOrder`, `getOrder`, `adminConfirmOrder`, `buildQrUrl`, `effectiveOrderStatus`, `toPublicLicense` | Đơn hàng, QR VietQR, xác nhận → sinh bản quyền (R4–R6, R15, R17) |
| web/src/mocks/errors.ts | `MockError` | Lỗi nghiệp vụ giả lập (status, code, message) |
| web/src/mocks/licenses.ts | `listLicenses`, `addActivation`, `removeActivation`, `extendLicense`, `revokeLicense`, `toPublicLicense`, `effectiveLicenseStatus` | Bản quyền và máy (R7–R15) |
| web/src/mocks/admin.ts | `adminListUsers`, `adminGetUser`, `adminSetUserStatus`, `stats` | Khách hàng và số liệu quản trị (R19) |
| web/src/mocks/views.ts | `toPublicUser`, `effectiveOrderStatus` | Hàm chuyển dữ liệu lưu → dữ liệu trả ra |
| web/src/mocks/handlers.ts | `createHandlers(db, {delayMs})` | 34 route MSW theo hợp đồng API, kiểm tra token/vai trò (R16), MockError → JSON |
| web/src/mocks/browser.ts | `mockDb`, `worker` | Giả lập trong trình duyệt (localStorage, trễ 300ms) |
| web/src/api/{auth,plans,orders,licenses,admin}.ts | mỗi endpoint một hàm | Lớp gọi API cho màn hình |
| web/src/test/server.ts | `setupMockServer()` | MSW setupServer + mockDb mới cho mỗi test |
| web/src/auth/session.ts | `loadSession`, `saveSession`, `clearSession` | Phiên `{token,user}` ở localStorage `etax-session` |
| web/src/auth/AuthProvider.tsx, context.ts | `AuthProvider`, `useAuth` | login/register/logout/refreshMe; 401 có token → xoá phiên, cảnh báo, về /dang-nhap?next=… |
| web/src/auth/guards.tsx, redirect.ts | `RequireAuth`, `RequireRole`, `safeNext` | Bảo vệ route theo đăng nhập/vai trò; chỉ chuyển tới đường dẫn nội bộ |
| web/src/features/auth/*.tsx | `LoginPage`, `RegisterPage`, `ForgotPasswordPage`, `AuthCard` | Trang đăng nhập/đăng ký/quên mật khẩu (Card 400px) |
| web/src/pages/ForbiddenPage.tsx | `ForbiddenPage` | Trang 403 (bản tối thiểu, hoàn thiện ở T-001.10) |
| web/src/shared/components/CopyField.tsx | `CopyField` | Hiện giá trị + nút "Sao chép"; báo "Đã sao chép" / lỗi clipboard |
| web/src/shared/components/StatusTag.tsx | `StatusTag` | Nhãn trạng thái đơn/bản quyền theo bảng màu |
| web/src/shared/components/PageHeader.tsx | `PageHeader` | Tiêu đề trang, nút quay lại, `extra` |
| web/src/shared/components/QueryState.tsx | `QueryState` | Đang tải (cả query chưa chạy) / lỗi + "Thử lại" / trống / dữ liệu |
| web/src/shared/ErrorBoundary.tsx | `ErrorBoundary` | Bắt lỗi render, nút "Tải lại trang" |
| web/src/pages/NotFoundPage.tsx, ForbiddenPage.tsx | `NotFoundPage`, `ForbiddenPage` | Trang 404 / 403, nút "Về trang chủ" |
| web/src/router.tsx | `routes`, `router` | Cây route theo bảng Route: RootLayout → Public / auth / RequireAuth+RequireRole customer → AccountLayout / RequireAuth+RequireRole admin → AdminLayout / 404 |
| web/src/layouts/RootLayout.tsx | `RootLayout` | AuthProvider + ErrorBoundary + DevToolbar + Outlet |
| web/src/layouts/PublicLayout.tsx | `PublicLayout` | Header 64px (logo, menu, Đăng nhập/Mua ngay hoặc avatar), Drawer < 768px, footer |
| web/src/layouts/SideNavLayout.tsx | `SideNavLayout` | Sider 220px ≥ 992px, nhỏ hơn thì Drawer (nút "Mở menu"); mục chọn theo pathname |
| web/src/layouts/AccountLayout.tsx, AdminLayout.tsx | `AccountLayout`, `AdminLayout` | Menu khách (sáng) / quản trị (tối, badge đơn chờ) |
| web/src/features/placeholder/ComingSoon.tsx | `ComingSoon` | Giữ chỗ "Màn hình đang được xây dựng" |
| web/src/dev/DevToolbar.tsx | `DevToolbar` | Chỉ khi VITE_USE_MOCK: đặt lại dữ liệu, đăng nhập nhanh khách/admin |
| web/src/main.tsx | — | Bật MSW worker khi VITE_USE_MOCK rồi mới render |
| web/vite.config.ts | `manualChunks` | Tách react / antd / antd-rc / antd-icons / vendor (< 500 kB mỗi chunk) |
| web/playwright.config.ts, web/tests/e2e/nen-tang.spec.ts | — | e2e TC-05a/b, login-admin, mobile-drawer (cổng 5174) |

## Nhật ký sửa đổi

### CR-001 — Nền tảng web

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| T-001.1 | web/ (mới) | Khung Vite + React 18 + antd 5 + Vitest + ESLint | Nền cho mọi màn hình |
| T-001.2 | web/src/shared/format.ts, validators.ts (mới) | Hàm định dạng và kiểm tra | Dùng chung cho mọi màn hình và mockDb |
| T-001.3 | web/src/api/types.ts, client.ts (mới) | Kiểu dữ liệu và hàm gọi API | Mọi màn hình gọi qua apiFetch |
| T-001.4 | web/src/mocks/ (mới) | mockDb: seed, đăng ký/đăng nhập, hồ sơ, đổi mật khẩu, gói, cài đặt (R1–R3, R16, R18, R20) | Nền giả lập backend |
| T-001.5 | web/src/mocks/mockDb.ts | Đơn hàng và xác nhận thanh toán; load() kiểm tra đủ cấu trúc | Luồng mua gói |
| T-001.6 | web/src/mocks/licenses.ts, errors.ts (mới), mockDb.ts | Bản quyền, thêm/gỡ máy, gia hạn, thu hồi | Tách file cho gọn mockDb.ts |
| T-001.7 | web/src/mocks/admin.ts, views.ts (mới), mockDb.ts | Khách hàng, khoá/mở, số liệu tháng | Trang quản trị |
| T-001.8 | web/src/mocks/handlers.ts, browser.ts, web/src/api/*.ts, web/src/test/server.ts, web/public/mockServiceWorker.js (mới) | Nối mockDb với hợp đồng API | Màn hình gọi API thật sự qua MSW |
| T-001.9 | web/src/auth/, web/src/features/auth/, web/src/pages/ForbiddenPage.tsx (mới); web/src/api/client.ts, App.tsx | Phiên đăng nhập, bảo vệ route, 3 trang xác thực; 401 muộn sau đăng xuất bị bỏ qua; đăng xuất về '/' | Nền cho khu tài khoản và quản trị |
| T-001.10 | web/src/shared/components/, ErrorBoundary.tsx, pages/NotFoundPage.tsx (mới); pages/ForbiddenPage.tsx | Thành phần dùng chung và trang lỗi; QueryState coi query chưa chạy là đang tải | Dùng lại ở mọi màn hình |
| T-001.11 | web/src/layouts/, router.tsx, dev/, features/placeholder/, playwright.config.ts, tests/e2e/ (mới); App.tsx, main.tsx, vite.config.ts | Layout, router, công cụ dev, e2e; /tai-khoan chỉ cho khách (theo bảng Route); tách chunk | Khung điều hướng cho các chức năng con |

### CR-002 — Vá lỗi nhỏ còn tồn đọng

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| T-002.1 ✅ | web/src/mocks/errors.ts:13 `validationError`; licenses.ts:116 (tìm mã máy chỉ máy đang dùng), :147-150 `extendLicense` (1–120 tháng, LICENSE_REVOKED); admin.ts:42 (trạng thái), :51 CANNOT_LOCK_ADMIN; mockDb.ts:95 `validatePlan` (gọi đầu createPlan/updatePlan); handlers.ts:56-62 `body()` bắt JSON hỏng; test: mocks/mockDb.validation.test.ts, api/api.test.ts | Kiểm tra đầu vào, mã lỗi mới, body JSON hỏng → 400 | Mock là bản mẫu cho API C# |
| T-002.2 | web/src/api/*.ts, client.ts, App.tsx (retry), main.tsx, dev/DevToolbar.tsx, vite.config.ts | Encode tham số, không retry 4xx, 2xx không JSON, worker lỗi vẫn render, bỏ mockServiceWorker.js khỏi dist | Ổn định lớp API |
| T-002.3 | web/src/auth/session.ts, layouts/SideNavLayout.tsx, PublicLayout.tsx, pages/NotFoundPage.tsx, ForbiddenPage.tsx, shared/components/StatusTag.tsx, shared/format.ts | Phiên hỏng, ngăn kéo tự mở, Link lồng Button, trạng thái lạ, màu theo token, ngày rỗng | Lỗi người dùng gặp |
| T-002.4 | docs/features/web-ban-quyen/design.md (Quy tắc nghiệp vụ) | R21–R24, mục kiểm tra phía máy chủ | Đồng bộ spec với mock |
