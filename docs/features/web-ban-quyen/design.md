# Design — Web bán bản quyền E-Tax

> Spec đã chốt qua `superpowers:brainstorming` ngày 2026-11-16. Đây là tài liệu gốc (nguồn quyết định) cho các chức năng con.
> Prototype giao diện: [index.html](index.html).

## Mục tiêu và tiêu chí thành công

**Mục tiêu:** dựng giao diện web **thật của sản phẩm** (không phải mockup bỏ đi) để bán bản quyền phần mềm E-Tax hiện có. Đây là bước đầu của phương án C trong `docs/de-xuat/De-xuat-phuong-an-trien-khai.docx`. Web gồm:
- trang giới thiệu / bảng giá / tải ứng dụng;
- đăng ký, đăng nhập;
- khu tài khoản khách: mua gói, thanh toán VietQR, đơn hàng, bản quyền, lấy key theo mã máy;
- trang quản trị: đơn hàng và xác nhận thanh toán, khách hàng, gói và giá, bản quyền, cài đặt.

**Người dùng chốt:**

| Quyết định | Lựa chọn |
|---|---|
| Mục đích | Giao diện thật, sau này nối thẳng ASP.NET Core API + DB |
| Phạm vi lần này | Web bán bản quyền E-Tax (chưa làm web tra cứu hoá đơn của phương án C) |
| Công nghệ | React + TypeScript (Vite) gọi ASP.NET Core API |
| UI kit | Ant Design (locale `vi_VN`) |
| Vị trí code | Thư mục `web/` trong repo này |
| Dữ liệu | API giả lập (MSW) theo hợp đồng API chốt trước; API thật làm sau |
| Thanh toán | Chuyển khoản VietQR, admin xác nhận tay |
| Gói bán | Thời hạn × số máy; khách tự nhập mã máy để lấy key; đổi máy có giới hạn |

**Giả định** (người dùng không phản đối):
- giao diện tiếng Việt, màu chủ đạo `#0075D6` (giống app desktop);
- dùng tốt trên máy tính và điện thoại (từ 390px).

**Xong khi:**
- `npm run dev` (giả lập bật) đi trọn luồng: khách đăng ký → mua gói → thanh toán (QR) → admin xác nhận → khách thêm máy, lấy key.
- `npm run build`, `npm run lint`, `npm run test` (Vitest) và `npm run e2e` (Playwright TC-01…TC-06) đều pass.

**Ngoài phạm vi lần này:**
- ASP.NET Core API, cơ sở dữ liệu thật, ký key ECDSA thật;
- cổng thanh toán VNPay/MoMo và tự xác nhận chuyển khoản;
- gửi email thật, hoá đơn điện tử;
- web tra cứu hoá đơn và ứng dụng kết nối của phương án C;
- sửa app desktop.

## Kiến trúc

```
Trình duyệt ── React (web/) ── src/api/client.ts ──fetch /api/*──┬─ MSW (VITE_USE_MOCK=true): src/mocks/handlers → mockDb (localStorage)
                                                                 └─ ASP.NET Core API (giai đoạn sau, VITE_API_URL) → DB; ký key bằng LicenseKey.Create
```

- **Lớp API là ranh giới duy nhất.** Màn hình chỉ gọi hàm trong `src/api/*` (qua TanStack Query), không gọi `fetch` trực tiếp. Đổi từ giả lập sang thật chỉ cần sửa biến môi trường.
- **MSW chạy cả trong trình duyệt và trong Vitest:**
  - trình duyệt dùng `setupWorker`;
  - Vitest dùng `setupServer`, nên test màn hình đi qua cùng một bộ handler.
- **`mockDb`:**
  - module thuần TypeScript, chứa toàn bộ quy tắc nghiệp vụ, được test riêng bằng Vitest;
  - handler MSW chỉ làm phần HTTP: đọc token, gọi `mockDb`, trả JSON;
  - đây cũng là đặc tả hành vi cho API C# sau này.
- **Key giả lập** có dạng `ETAX-MOCK-<machineId>-<licenseId>`. Khoá bí mật ECDSA không bao giờ đưa lên trình duyệt; key thật do API C# tạo bằng `LicenseKey.Create(machineId, customer, expires, ecdsa)` (`E-Tax/LicenseKey.cs`).

### Cấu trúc thư mục

```
web/
  package.json, vite.config.ts, tsconfig*.json, eslint.config.js, playwright.config.ts, .env.development
  public/mockServiceWorker.js
  src/
    main.tsx               ← khởi động MSW (nếu bật) rồi render App
    App.tsx                ← ConfigProvider (vi_VN, theme) + QueryClientProvider + RouterProvider
    router.tsx             ← toàn bộ route
    theme.ts               ← token Ant Design
    api/                   ← types.ts (hợp đồng), client.ts, auth.ts, plans.ts, orders.ts, licenses.ts, admin.ts
    mocks/                 ← browser.ts, server.ts, handlers.ts, mockDb.ts, seed.ts
    auth/                  ← AuthProvider/useAuth, RequireAuth, RequireRole
    layouts/               ← PublicLayout, AccountLayout, AdminLayout
    shared/                ← format.ts (VND, ngày), validators.ts (mã máy, MST, SĐT), components (CopyField, StatusTag, PageHeader…)
    features/public/       ← HomePage, PricingPage, DownloadPage
    features/auth/         ← LoginPage, RegisterPage, ForgotPasswordPage
    features/account/      ← DashboardPage, BuyPage, OrderPaymentPage, OrdersPage, LicensesPage, LicenseDetailPage, ProfilePage
    features/admin/        ← AdminDashboardPage, AdminOrdersPage, AdminUsersPage, AdminUserDetailPage, AdminPlansPage, AdminLicensesPage, AdminLicenseDetailPage, AdminSettingsPage
    pages/                 ← NotFoundPage, ForbiddenPage
  tests/e2e/               ← Playwright TC-01…TC-06
```

### Thư viện

- **Chạy:** react, react-dom, react-router-dom (v7), antd (v5) + @ant-design/icons, @tanstack/react-query, dayjs.
- **Phát triển:** vite, typescript, @vitejs/plugin-react, msw, vitest, jsdom, @testing-library/react, @testing-library/user-event, @testing-library/jest-dom, @playwright/test, eslint.
- Tất cả cài trong `web/`. Project .NET không bị ảnh hưởng.

### Biến môi trường

| Biến | Mặc định dev | Ý nghĩa |
|---|---|---|
| `VITE_USE_MOCK` | `true` | Bật MSW |
| `VITE_API_URL` | `` (cùng origin) | Gốc API thật, vd `https://api.etax.vn` |

## Dữ liệu

| Thực thể | Trường | Ràng buộc |
|---|---|---|
| `User` | id, email, fullName, phone, companyName?, taxCode?, role `customer`\|`admin`, status `active`\|`locked`, createdAt | email duy nhất (không phân biệt hoa thường) |
| `Plan` | id, name, description, durationMonths (`number`\|`null` = vĩnh viễn), maxMachines, maxMachineChanges, price (VND, số nguyên), featured, active, sortOrder | price ≥ 0, maxMachines ≥ 1 |
| `Order` | id, code `ET`+6 số, userId, planId, planSnapshot {name, durationMonths, maxMachines, maxMachineChanges, price}, amount, status `pending`\|`paid`\|`cancelled`\|`expired`, createdAt, expiresAt (= createdAt + 72h), paidAt?, confirmedBy?, note? | `code` duy nhất, là nội dung chuyển khoản |
| `License` | id, orderId, userId, planName, maxMachines, maxMachineChanges, machineChangesUsed, startsAt, expiresAt (`null` = vĩnh viễn), status `active`\|`expired`\|`revoked` | 1 đơn `paid` ↔ 1 bản quyền |
| `Activation` | id, licenseId, machineId (16 hex HOA), machineName, key, createdAt, revokedAt? | máy đang dùng = `revokedAt` rỗng |
| `BankSettings` | bankBin, bankName, accountNo, accountName | |
| `AppRelease` | version, downloadUrl, releasedAt, notes | |

**Dữ liệu mẫu (seed):**
- Tài khoản:
  - `admin@etax.vn` / `Admin@123` (admin);
  - `khach@etax.vn` / `Khach@123` (khách, có 1 bản quyền 12 tháng còn hạn với 1 máy và 1 đơn đang chờ).
- Bốn gói:

  | Gói | Giá | Số máy | Lần đổi máy |
  |---|---|---|---|
  | 3 tháng | 300.000 ₫ | 1 | 1 |
  | 6 tháng | 500.000 ₫ | 1 | 2 |
  | 12 tháng | 900.000 ₫ | 2 | 3 (nổi bật) |
  | Vĩnh viễn | 2.500.000 ₫ | 3 | 5 |

- Ngân hàng: Vietcombank BIN `970436`.

## API

Mọi đường dẫn có tiền tố `/api`. Xác thực bằng `Authorization: Bearer <token>`. Lỗi luôn trả JSON `{ code, message }`, trong đó `message` là tiếng Việt và được hiện thẳng lên giao diện.

| Method | Path | Quyền | Request | Response |
|---|---|---|---|---|
| POST | /auth/register | công khai | {email, password, fullName, phone, companyName?, taxCode?} | {token, user} |
| POST | /auth/login | công khai | {email, password} | {token, user} |
| GET | /auth/me | đăng nhập | — | User |
| POST | /auth/forgot-password | công khai | {email} | 204 (luôn 204, không lộ email tồn tại) |
| PUT | /auth/profile | đăng nhập | {fullName, phone, companyName?, taxCode?} | User |
| POST | /auth/change-password | đăng nhập | {currentPassword, newPassword} | 204 |
| GET | /plans | công khai | — | Plan[] (active, theo sortOrder) |
| GET | /app/latest | công khai | — | AppRelease |
| POST | /orders | customer | {planId} | OrderDetail (Order + payment {qrUrl, bankName, accountNo, accountName, amount, transferContent}) |
| GET | /orders | customer | — | Order[] của mình, mới nhất trước |
| GET | /orders/{id} | customer (chủ đơn) | — | OrderDetail |
| POST | /orders/{id}/cancel | customer (chủ đơn) | — | Order |
| GET | /licenses | customer | — | License[] (kèm activeMachines) |
| GET | /licenses/{id} | customer (chủ) | — | LicenseDetail (License + activations[]) |
| POST | /licenses/{id}/activations | customer (chủ) | {machineId, machineName} | Activation (có key) |
| DELETE | /licenses/{id}/activations/{actId} | customer (chủ) | — | 204 |
| GET | /admin/stats | admin | — | {revenueThisMonth, pendingOrders, activeLicenses, newCustomersThisMonth} |
| GET | /admin/users?q= | admin | — | UserSummary[] (kèm số đơn, số bản quyền) |
| GET | /admin/users/{id} | admin | — | User + orders[] + licenses[] |
| PATCH | /admin/users/{id} | admin | {status} | User |
| GET | /admin/plans | admin | — | Plan[] (cả gói ngừng bán) |
| POST | /admin/plans | admin | PlanInput | Plan |
| PUT | /admin/plans/{id} | admin | PlanInput | Plan |
| GET | /admin/orders?status=&q= | admin | — | AdminOrder[] (kèm email, tên khách) |
| POST | /admin/orders/{id}/confirm | admin | {note?} | {order, license} |
| POST | /admin/orders/{id}/cancel | admin | {note?} | Order |
| GET | /admin/licenses?status=&q= | admin | — | AdminLicense[] |
| GET | /admin/licenses/{id} | admin | — | LicenseDetail + user |
| POST | /admin/licenses/{id}/revoke | admin | — | License |
| POST | /admin/licenses/{id}/extend | admin | {months} | License |
| GET / PUT | /admin/settings/bank | admin | BankSettings | BankSettings |
| GET / PUT | /admin/settings/app | admin | AppRelease | AppRelease |

### Quy tắc nghiệp vụ

`mockDb` làm đúng như dưới đây, API C# sau này cũng phải làm y hệt. Mã lỗi `code` cố định:

| # | Quy tắc | Lỗi |
|---|---|---|
| R1 | Đăng ký với email đã có | 409 `EMAIL_TAKEN` "Email đã được đăng ký" |
| R2 | Sai email/mật khẩu | 401 `INVALID_CREDENTIALS` "Email hoặc mật khẩu không đúng" |
| R3 | Tài khoản bị khoá đăng nhập | 403 `ACCOUNT_LOCKED` "Tài khoản đã bị khoá, vui lòng liên hệ hỗ trợ" |
| R4 | Tạo đơn: gói phải `active`. `amount = plan.price`, chụp `planSnapshot`, `expiresAt = createdAt + 72h` | 404 `PLAN_NOT_FOUND` |
| R5 | Đơn `pending` đã quá `expiresAt` thì được coi là `expired` khi đọc. Không xác nhận / huỷ được đơn không còn `pending` | 409 `ORDER_NOT_PENDING` "Đơn không còn ở trạng thái chờ thanh toán" |
| R6 | Admin xác nhận đơn: order → `paid`, `paidAt = now`, `confirmedBy = admin.id`. Tạo License: `startsAt = paidAt`, `expiresAt = startsAt + durationMonths` tháng (`null` nếu vĩnh viễn), lấy maxMachines/maxMachineChanges từ planSnapshot | — |
| R7 | License `active` có `expiresAt < now` thì được coi là `expired` khi đọc | — |
| R8 | Thêm máy: mã máy được chuẩn hoá (bỏ khoảng trắng, viết HOA) và phải khớp `^[0-9A-F]{16}$` | 400 `INVALID_MACHINE_ID` "Mã máy phải gồm 16 ký tự 0-9, A-F" |
| R9 | Thêm máy: license phải `active` | 409 `LICENSE_INACTIVE` "Bản quyền đã hết hạn hoặc bị thu hồi" |
| R10 | Thêm máy mà mã máy đã có activation đang dùng trên license này thì trả lại activation cũ (cùng key), không tính thêm | — |
| R11 | Thêm máy khi số máy đang dùng đã bằng maxMachines | 409 `MACHINE_LIMIT` "Đã dùng hết số máy của gói, hãy gỡ một máy trước" |
| R12 | Gỡ máy: đặt `revokedAt`, `machineChangesUsed += 1`. Từ chối khi `machineChangesUsed >= maxMachineChanges` | 409 `CHANGE_LIMIT` "Đã hết số lần đổi máy của gói" |
| R13 | Gia hạn: `expiresAt = max(expiresAt, now) + months`. Nếu license đang `expired` thì chuyển lại `active`. Gói vĩnh viễn không gia hạn | 409 `LICENSE_PERMANENT` |
| R14 | Thu hồi: status `revoked`, mọi activation đang dùng bị `revokedAt = now` | — |
| R15 | Khách truy cập đơn/bản quyền của người khác | 404 `NOT_FOUND` (không lộ là tồn tại) |
| R16 | Thiếu/sai token → 401 `UNAUTHORIZED`; sai vai trò → 403 `FORBIDDEN` | |
| R17 | Mã đơn `ET` + 6 số ngẫu nhiên, lặp lại đến khi không trùng | — |
| R18 | Đổi mật khẩu: mật khẩu hiện tại sai | 400 `WRONG_PASSWORD` "Mật khẩu hiện tại không đúng" |
| R19 | Admin tự khoá chính mình | 409 `CANNOT_LOCK_SELF` "Không thể khoá tài khoản đang đăng nhập" |
| R20 | Token của tài khoản đã bị khoá được coi như token không hợp lệ, trả 401 `UNAUTHORIZED` để phiên bị đăng xuất | — |

`OrderDetail` có thêm `licenseId` (`null` khi đơn chưa `paid`) để nút "Đến bản quyền" biết đi đâu. `License` có thêm `activeMachines` (số activation chưa gỡ).

**Số liệu `/admin/stats`** (theo giờ máy chủ, tháng dương lịch hiện tại):
- `revenueThisMonth`: tổng `amount` của đơn `paid` có `paidAt` trong tháng;
- `pendingOrders`: số đơn `pending` chưa quá hạn;
- `activeLicenses`: số license `active` (sau khi áp R7);
- `newCustomersThisMonth`: số user `customer` có `createdAt` trong tháng.

**Cộng tháng:** dùng `dayjs().add(n, 'month')`. Ví dụ 31/01 + 1 tháng = 28/02 (hoặc 29/02 năm nhuận).

**QR:** `qrUrl = https://img.vietqr.io/image/{bankBin}-{accountNo}-compact2.png?amount={amount}&addInfo={code}&accountName={encodeURIComponent(accountName)}`

**Token giả lập:** `mock.<userId>`. API thật dùng JWT. Giao diện chỉ coi token là chuỗi mờ, không tự giải mã.

## Giao diện

Theme Ant Design:
- `colorPrimary #0075D6`, `borderRadius 8`, phông `"Segoe UI", Inter, system-ui`;
- locale `vi_VN`;
- tiền `1.200.000 ₫` (`Intl.NumberFormat('vi-VN')`), ngày `DD/MM/YYYY`, ngày giờ `DD/MM/YYYY HH:mm`.

### Route

| Route | Màn hình | Quyền | Chức năng con |
|---|---|---|---|
| `/` | Trang chủ | công khai | trang-cong-khai |
| `/bang-gia` | Bảng giá | công khai | trang-cong-khai |
| `/tai-ung-dung` | Tải ứng dụng | công khai | trang-cong-khai |
| `/dang-nhap`, `/dang-ky`, `/quen-mat-khau` | Xác thực | công khai | nen-tang |
| `/tai-khoan` | Tổng quan | customer | tai-khoan-khach |
| `/tai-khoan/mua-goi?goi=<id>` | Mua gói | customer | tai-khoan-khach |
| `/tai-khoan/don-hang`, `/tai-khoan/don-hang/:id` | Đơn hàng, Thanh toán | customer | tai-khoan-khach |
| `/tai-khoan/ban-quyen`, `/tai-khoan/ban-quyen/:id` | Bản quyền, chi tiết | customer | tai-khoan-khach |
| `/tai-khoan/ho-so` | Hồ sơ | customer | tai-khoan-khach |
| `/quan-tri` | Bảng điều khiển | admin | quan-tri |
| `/quan-tri/don-hang` | Đơn hàng | admin | quan-tri |
| `/quan-tri/khach-hang`, `/quan-tri/khach-hang/:id` | Khách hàng | admin | quan-tri |
| `/quan-tri/goi` | Gói & giá | admin | quan-tri |
| `/quan-tri/ban-quyen`, `/quan-tri/ban-quyen/:id` | Bản quyền | admin | quan-tri |
| `/quan-tri/cai-dat` | Cài đặt | admin | quan-tri |
| `*` | 404 | — | nen-tang |

**Điều hướng sau đăng nhập:**
- Nếu có `next` thì quay về `next`.
- Nếu không: admin vào `/quan-tri`, khách vào `/tai-khoan`.
- Khi đã đăng nhập mà vào `/dang-nhap` thì chuyển thẳng như trên.

### Xử lý lỗi và trạng thái

- **`client.ts`:**
  - Lỗi 401 khi đang có phiên: xoá phiên, chuyển sang `/dang-nhap?next=<đường dẫn hiện tại>`, thông báo "Phiên đăng nhập đã hết hạn".
  - Lỗi 403: ném `ApiError`. Route guard hiện `ForbiddenPage`.
  - Lỗi mạng: ném `ApiError{code:'NETWORK'}`, message "Không kết nối được máy chủ".
- **Trang dữ liệu:**
  - Đang tải: `Skeleton`.
  - Lỗi: `Result status="error"` kèm nút "Thử lại" (`refetch`).
  - Trống: `Empty` kèm lời gợi ý và nút hành động.
- **Mutation:** nút có `loading` trong lúc gửi để chặn bấm hai lần. Thành công gọi `message.success`; lỗi gọi `message.error(err.message)`.
- **`ErrorBoundary`:** bọc từng layout.
- **Trang thanh toán:** khi đơn `pending`, `refetchInterval = 10s`; đổi sang `paid` thì hiện `Result success` và nút "Đến bản quyền".

### Responsive

- Từ 992px trở lên: menu trái cố định.
- Dưới 992px: menu thành `Drawer`, mở bằng nút ☰.
- Bảng có `scroll={{x}}`.
- Thẻ gói xếp 1 cột khi màn ≤ 576px.

Chi tiết màn hình của từng phần nằm trong `design.md` của chức năng con.

## Kiểm thử

- **Vitest (`npm run test`):**
  - `shared/format`, `shared/validators`;
  - `mocks/mockDb`: mỗi quy tắc R1–R20 có ít nhất một test;
  - `api/client` (401, lỗi mạng);
  - các màn hình chính chạy cùng MSW `setupServer`.
- **Playwright (`npm run e2e`):** khởi chạy `vite` với `VITE_USE_MOCK=true`, xoá localStorage trước mỗi test.

| ID | Kịch bản | Mong đợi |
|---|---|---|
| TC-01 | Mở `/`, bấm "Xem bảng giá" | Thấy 4 gói, giá đúng định dạng, gói 12 tháng có nhãn "Phổ biến" |
| TC-02 | Đăng ký tài khoản mới → bảng giá → "Mua gói" 6 tháng → xác nhận | Vào trang thanh toán, thấy QR, số tiền 500.000 ₫, nội dung `ETxxxxxx`, trạng thái "Chờ thanh toán" |
| TC-03 | Đăng nhập admin → Đơn hàng → lọc "Chờ thanh toán" → xác nhận đơn của khách mẫu | Đơn chuyển "Đã thanh toán"; bảng điều khiển giảm 1 đơn chờ |
| TC-04 | Khách mẫu → Bản quyền → chi tiết → Thêm máy (mã sai, rồi mã đúng) → thêm vượt số máy | Mã sai bị báo lỗi; mã đúng hiện key và sao chép được; vượt số máy hiện lỗi `MACHINE_LIMIT` |
| TC-05 | Khách đăng nhập rồi mở `/quan-tri`; chưa đăng nhập mở `/tai-khoan` | Trang "Không có quyền"; chuyển `/dang-nhap?next=/tai-khoan` |
| TC-06 | Viewport 390×844: trang chủ, bảng giá, tài khoản (mở menu ☰) | Không tràn ngang, menu Drawer mở được |

- Mỗi phiên bàn giao chạy thêm `npm run build` và `npm run lint`.
- Ảnh Playwright lưu vào `screenshots/` của chức năng con tương ứng.

## Vị trí code

Chưa có code. Bảng này được điền khi thực hiện, theo từng chức năng con.

| File | Symbol | Dòng | Vai trò |
|---|---|---|---|
| E-Tax/LicenseKey.cs | LicenseKey.Create | — | (tham chiếu) API C# sau này dùng để ký key thật |
| E-Tax/MachineId.cs | MachineId.Get | — | (tham chiếu) định dạng mã máy 16 hex HOA |

## Nhật ký sửa đổi

### CR-001 — Dựng web bán bản quyền (giao diện + API giả lập)

| Task | File:dòng | Thay đổi | Lý do |
|---|---|---|---|
| — | — | Chưa bắt đầu, chờ duyệt spec rồi lập kế hoạch | — |
