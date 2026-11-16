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
