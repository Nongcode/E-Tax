# Design — Nền tảng web (khung dự án, API, giả lập, đăng nhập, layout)

> Thuộc [Web bán bản quyền E-Tax](../design.md). Hợp đồng API, mô hình dữ liệu, quy tắc R1–R17 nằm ở design.md cha.

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

Chưa có. Điền khi làm.

## Nhật ký sửa đổi

### CR-001 — Nền tảng web

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| — | — | Chưa bắt đầu | — |
