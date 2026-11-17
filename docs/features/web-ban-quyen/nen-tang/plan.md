# Plan — Nền tảng web

## Tổng quan

- **Mục tiêu:** khung dự án `web/`, theme, lớp API theo hợp đồng, giả lập MSW + `mockDb` (R1–R20), đăng nhập/phân quyền, ba layout, các route (màn chưa làm hiện "Đang xây dựng"), 404/403, công cụ dev.
- **Module:** web-ban-quyen
- **Chức năng con:** Không tách. Là con số 1 của [web-ban-quyen](../plan.md).
- **Spec:** [design.md](design.md) + [design.md cha](../design.md).

## Yêu cầu

### CR-001 — Nền tảng web · 2026-11-16

**Mô tả yêu cầu (nguyên văn từ người dùng):**
> bạn hãy thực hiện dựng giao diện web cho tôi trước nhé

**Phạm vi**
- Trong phạm vi: như mục tiêu ở trên.
- Ngoài phạm vi: các màn nghiệp vụ của trang-cong-khai, tai-khoan-khach, quan-tri. Trong phần này chúng chỉ là route giữ chỗ.

---

# Nền tảng web — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** dựng khung React chạy được, có lớp API theo hợp đồng, giả lập MSW đúng quy tắc R1–R20, đăng nhập/phân quyền và layout, để ba chức năng con sau chỉ cần thêm màn hình.

**Architecture:**
- Mọi màn hình gọi `src/api/*.ts`. Các hàm này đi qua `apiFetch` (`src/api/client.ts`).
- Khi `VITE_USE_MOCK=true`, MSW (`src/mocks/handlers.ts`) chặn `*/api/*` và gọi `mockDb` (`src/mocks/mockDb.ts`). `mockDb` là TypeScript thuần, chứa toàn bộ quy tắc nghiệp vụ, lưu vào một `Storage`.
- Router dùng `createBrowserRouter`. Route gốc `RootLayout` bọc `AuthProvider` để `useNavigate` dùng được trong phiên.

**Tech Stack:**
- Chạy: Node 22, Vite 6, React 18.3, TypeScript 5.7, Ant Design 5, React Router 7, TanStack Query 5, dayjs.
- Giả lập và test: MSW 2, Vitest 3 + jsdom + Testing Library, Playwright, ESLint 9.

**Spec:** `docs/features/web-ban-quyen/design.md`, `docs/features/web-ban-quyen/nen-tang/design.md`

## Global Constraints

- Toàn bộ code nằm trong `D:\E-Tax\web\`. Không sửa project .NET. Lệnh chạy trong `web/` (`cd /d/E-Tax/web`).
- Chữ hiển thị là tiếng Việt có dấu. Thông báo lỗi giữ nguyên văn như bảng R1–R20.
- Theme: `colorPrimary: '#0075D6'`, `borderRadius: 8`, `fontFamily: '"Segoe UI", Inter, system-ui, -apple-system, sans-serif'`, locale `vi_VN` của antd, `dayjs.locale('vi')`.
- Định dạng: tiền `Intl.NumberFormat('vi-VN')` + `' ₫'` (vd `1.200.000 ₫`), ngày `DD/MM/YYYY`, ngày giờ `DD/MM/YYYY HH:mm`.
- Ngày giờ trong API là chuỗi ISO 8601 UTC (`toISOString()`).
- localStorage: `etax-mockdb-v1` (dữ liệu giả lập), `etax-session` (`{token, user}`).
- Biến môi trường: `VITE_USE_MOCK` (`'true'` khi dev), `VITE_API_URL` (rỗng = cùng origin). Đọc qua `import.meta.env`.
- Key giả lập: `ETAX-MOCK-${machineId}-${licenseId}`. Token giả lập: `mock.${userId}`.
- Thư viện (chỉ cài trong `web/`):
  - dependencies: `react@^18.3.1 react-dom@^18.3.1 antd@^5 @ant-design/icons@^5 react-router-dom@^7 @tanstack/react-query@^5 dayjs@^1.11`;
  - devDependencies: `vite@^6 @vitejs/plugin-react@^4 typescript@~5.7 @types/react@^18 @types/react-dom@^18 vitest@^3 jsdom@^25 msw@^2 @testing-library/react@^16 @testing-library/dom@^10 @testing-library/user-event@^14 @testing-library/jest-dom@^6 @playwright/test@^1 eslint@^9 @eslint/js@^9 typescript-eslint@^8 eslint-plugin-react-hooks@^5 eslint-plugin-react-refresh@^0.4 globals@^15`.
- Scripts trong `package.json`:
  - `dev`: `vite`
  - `build`: `tsc -b && vite build`
  - `lint`: `eslint .`
  - `test`: `vitest run`
  - `e2e`: `playwright test`
- Cuối mỗi task phải pass hết: `npm run build`, `npm run lint`, `npm run test`.
- Commit message dạng `feat(web): … — T-001.n`, kết thúc bằng dòng `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **`next` độc hại sau đăng nhập.** `?next=//evil.com` hoặc `?next=https://evil.com` phải về trang mặc định, không được chuyển ra ngoài. Test ở Task 9: `resolveAfterLogin`.
2. **Mã máy dán kèm khoảng trắng / xuống dòng / chữ thường** (vd `" 3fa2c91b7e0d4a55\n"`) phải được chấp nhận sau chuẩn hoá. Mã có gạch ngang thì báo lỗi. Test ở Task 2 và Task 6.
3. **localStorage `etax-mockdb-v1` hỏng hoặc khác phiên bản** phải tự nạp lại seed, không được trắng trang. Áp dụng tương tự cho `etax-session` hỏng. Test ở Task 4 và Task 9.
4. **Email khác hoa thường** (`KHACH@etax.vn`): đăng ký phải bị `EMAIL_TAKEN`, đăng nhập phải thành công. Test ở Task 4.
5. **Lỗi 401 khi đăng nhập sai mật khẩu** (chưa có token) không được kích hoạt luồng "phiên hết hạn", mà phải trả lỗi `INVALID_CREDENTIALS` cho form. Test ở Task 3.

---

### Task 1 (T-001.1): Khung dự án `web/`

**Files:**
- Create:
  - `web/package.json`, `web/.gitignore` (`node_modules`, `dist`, `test-results`, `playwright-report`, `*.local`)
  - `web/index.html` (`lang="vi"`, title "E-Tax — Tải hoá đơn điện tử hàng loạt")
  - `web/vite.config.ts`, `web/tsconfig.json`, `web/tsconfig.app.json`, `web/tsconfig.node.json`, `web/eslint.config.js`
  - `web/.env.development` (`VITE_USE_MOCK=true`), `web/.env.production` (`VITE_USE_MOCK=false`)
  - `web/src/vite-env.d.ts`: khai báo `ImportMetaEnv { VITE_USE_MOCK: string; VITE_API_URL?: string }`
  - `web/src/theme.ts`, `web/src/main.tsx`, `web/src/App.tsx`
  - `web/src/test/setup.ts`, `web/src/App.test.tsx`

**Interfaces:**
- Produces:
  - `theme: ThemeConfig` (`src/theme.ts`);
  - `AppProviders({children})` (`src/App.tsx`): bọc `ConfigProvider` (theme, `locale={viVN}`) → `AntApp` (để dùng `App.useApp()`) → `QueryClientProvider`. `queryClient` mặc định `{ queries: { retry: 1, refetchOnWindowFocus: false } }`;
  - `src/test/setup.ts` dùng cho mọi test.

- [ ] **Step 1: Tạo các file cấu hình.**
  - `package.json`: thư viện và scripts đúng như Global Constraints, `"type": "module"`, `"private": true`. Chạy `npm install`.
  - `vite.config.ts` (import `defineConfig` từ `vitest/config`):
    - `plugins: [react()]`;
    - `test: { environment: 'jsdom', setupFiles: ['src/test/setup.ts'], globals: true, css: false, env: { VITE_API_URL: 'http://localhost', VITE_USE_MOCK: 'false' }, include: ['src/**/*.test.{ts,tsx}'] }`.
  - TS: `strict: true`, `noUnusedLocals`, `noUnusedParameters`, `jsx: react-jsx`, `types: ["vitest/globals", "@testing-library/jest-dom"]`.
  - ESLint flat config theo mẫu Vite react-ts (js recommended, typescript-eslint recommended, react-hooks, react-refresh). Bỏ qua `dist`, `public/mockServiceWorker.js`, `playwright-report`, `test-results`.
- [ ] **Step 2: Viết `src/test/setup.ts`.**
  - import `@testing-library/jest-dom/vitest`.
  - polyfill `window.matchMedia` (trả về `{matches:false, addListener(){}, removeListener(){}, addEventListener(){}, removeEventListener(){}, dispatchEvent(){return false}, media, onchange:null}`) và `ResizeObserver` (class rỗng).
  - `afterEach(() => { cleanup(); localStorage.clear(); })`.
- [ ] **Step 3: Viết test thất bại `src/App.test.tsx`.**
  ```tsx
  it('AppProviders áp dụng theme màu chủ đạo #0075D6 và locale vi', () => {
    render(<AppProviders><Button type="primary">Mua</Button><DatePicker open /></AppProviders>);
    expect(screen.getByRole('button', { name: 'Mua' })).toBeInTheDocument();
    expect(theme.token?.colorPrimary).toBe('#0075D6');
    expect(document.body.textContent).toContain('Hôm nay');
  });
  ```
- [ ] **Step 4: Chạy test để thấy lỗi.** `npm run test`. Mong đợi: FAIL, không tìm thấy `./App`.
- [ ] **Step 5: Viết `theme.ts`, `App.tsx` (`AppProviders` + `App` tạm render `<h1>E-Tax</h1>`) và `main.tsx`.** `main.tsx` gọi `dayjs.locale('vi')`, `createRoot(...).render(<StrictMode><App/></StrictMode>)`.
- [ ] **Step 6: Chạy kiểm tra.** `npm run test && npm run lint && npm run build`. Mong đợi: PASS 1 test, lint 0 lỗi, build tạo ra `dist/`.
- [ ] **Step 7: Commit.** `git add web && git commit -m "feat(web): khung dự án Vite + React + Ant Design — T-001.1"`

### Task 2 (T-001.2): Hàm định dạng và kiểm tra dữ liệu

**Files:**
- Create: `web/src/shared/format.ts`, `web/src/shared/validators.ts`
- Test: `web/src/shared/format.test.ts`, `web/src/shared/validators.test.ts`

**Interfaces:**
- Produces:
  - `formatVnd(n: number): string`
  - `formatDate(iso: string | null | undefined): string`: `null` cho ra `'Trọn đời'`
  - `formatDateTime(iso: string): string`
  - `normalizeMachineId(s: string): string`: bỏ mọi khoảng trắng (`/\s/g`) rồi viết HOA
  - `isValidMachineId(s: string): boolean`: dùng `^[0-9A-F]{16}$` sau khi chuẩn hoá
  - `isValidTaxCode(s: string): boolean`: `^\d{10}(-\d{3})?$`
  - `isValidPhone(s: string): boolean`: `^0\d{9}$`
  - `MACHINE_ID_MESSAGE = 'Mã máy phải gồm 16 ký tự 0-9, A-F'`

- [ ] **Step 1: Viết test.**
  - `formatVnd(1200000) === '1.200.000 ₫'`, `formatVnd(0) === '0 ₫'`.
  - `formatDate('2026-03-02T00:00:00.000Z') === '02/03/2026'` (đặt `process.env.TZ` không cần, vì dùng giờ địa phương dayjs; trong test so sánh với `dayjs('2026-03-02T00:00:00.000Z').format('DD/MM/YYYY')`).
  - `formatDate(null) === 'Trọn đời'`.
  - `normalizeMachineId(' 3fa2 c91b7e0d4a55\n') === '3FA2C91B7E0D4A55'`.
  - `isValidMachineId(' 3fa2c91b7e0d4a55\n') === true`; `isValidMachineId('3FA2-C91B-7E0D-4A55') === false`; `isValidMachineId('3FA2C91B7E0D4A5') === false` (15 ký tự); `isValidMachineId('3FA2C91B7E0D4A5G') === false`.
  - `isValidTaxCode('0101234567')`, `isValidTaxCode('0101234567-001')` là `true`; `isValidTaxCode('010123456')` là `false`.
  - `isValidPhone('0912345678') === true`; `isValidPhone('912345678') === false`.
- [ ] **Step 2: Chạy test.** `npm run test -- src/shared`. Mong đợi: FAIL (thiếu module).
- [ ] **Step 3: Viết code hai file.**
- [ ] **Step 4: Chạy lại.** `npm run test -- src/shared`. Mong đợi: PASS.
- [ ] **Step 5: Commit.** `feat(web): định dạng tiền/ngày và kiểm tra mã máy, MST, SĐT — T-001.2`

### Task 3 (T-001.3): Hợp đồng kiểu dữ liệu và `apiFetch`

**Files:**
- Create: `web/src/api/types.ts`, `web/src/api/client.ts`
- Test: `web/src/api/client.test.ts`

**Interfaces:**
- Produces (`types.ts`). Mọi ngày là `string` ISO; trường có thể rỗng dùng `string | null`.
  ```ts
  type Role = 'customer' | 'admin'; type UserStatus = 'active' | 'locked';
  interface User { id; email; fullName; phone; companyName: string | null; taxCode: string | null; role: Role; status: UserStatus; createdAt }
  interface Plan { id; name; description; durationMonths: number | null; maxMachines: number; maxMachineChanges: number; price: number; featured: boolean; active: boolean; sortOrder: number }
  type PlanInput = Omit<Plan, 'id'>;
  type OrderStatus = 'pending' | 'paid' | 'cancelled' | 'expired';
  interface PlanSnapshot { name; durationMonths: number | null; maxMachines; maxMachineChanges; price }
  interface Order { id; code; userId; planId; planSnapshot: PlanSnapshot; amount: number; status: OrderStatus; createdAt; expiresAt; paidAt: string | null; confirmedBy: string | null; note: string | null }
  interface PaymentInfo { qrUrl; bankName; accountNo; accountName; amount: number; transferContent }
  type OrderDetail = Order & { payment: PaymentInfo; licenseId: string | null };
  type LicenseStatus = 'active' | 'expired' | 'revoked';
  interface License { id; orderId; userId; planName; maxMachines; maxMachineChanges; machineChangesUsed; startsAt; expiresAt: string | null; status: LicenseStatus; activeMachines: number }
  interface Activation { id; licenseId; machineId; machineName; key; createdAt; revokedAt: string | null }
  type LicenseDetail = License & { activations: Activation[] };          // khách: chỉ máy chưa gỡ
  type AdminLicenseDetail = License & { activations: Activation[]; user: User }; // admin: cả máy đã gỡ
  type AdminOrder = Order & { userEmail: string; userFullName: string; userTaxCode: string | null };
  type AdminLicense = License & { userEmail: string; userFullName: string };
  type UserSummary = User & { orderCount: number; licenseCount: number };
  type AdminUserDetail = User & { orders: Order[]; licenses: License[] };
  interface AdminStats { revenueThisMonth; pendingOrders; activeLicenses; newCustomersThisMonth }   // number
  interface BankSettings { bankBin; bankName; accountNo; accountName }
  interface AppRelease { version; downloadUrl; releasedAt; notes }
  interface AuthResponse { token: string; user: User }
  interface RegisterInput { email; password; fullName; phone; companyName?: string; taxCode?: string }
  interface LoginInput { email; password }
  interface ProfileInput { fullName; phone; companyName?: string; taxCode?: string }
  interface ChangePasswordInput { currentPassword; newPassword }
  interface ApiErrorBody { code: string; message: string }
  ```
- Produces (`client.ts`):
  - `class ApiError extends Error { status: number; code: string }`
  - `configureClient(opts: { getToken: () => string | null; onUnauthorized: () => void }): void`
  - `apiFetch<T>(path: string, init?: { method?: 'GET'|'POST'|'PUT'|'PATCH'|'DELETE'; body?: unknown }): Promise<T>`
    - URL là `(import.meta.env.VITE_API_URL ?? '') + '/api' + path`.
    - Gửi `Content-Type: application/json` khi có body, và `Authorization: Bearer <token>` khi có token.
    - HTTP 204 trả `undefined`.

- [ ] **Step 1: Viết test `client.test.ts`.** Dùng `setupServer` của msw với handler riêng trong file test.
  - `it('gửi Bearer token khi có')`: handler đọc header, trả `{ok:true}`; khẳng định header là `'Bearer abc'`.
  - `it('lỗi JSON {code,message} thành ApiError')`: 409 `{code:'EMAIL_TAKEN', message:'Email đã được đăng ký'}` thì `rejects` với `status 409`, `code 'EMAIL_TAKEN'`, `message 'Email đã được đăng ký'`.
  - `it('401 khi có token gọi onUnauthorized')`: spy được gọi 1 lần, promise vẫn reject `ApiError` 401.
  - `it('401 khi chưa có token (đăng nhập sai) KHÔNG gọi onUnauthorized')`: `getToken: () => null`, 401 `INVALID_CREDENTIALS` thì spy không được gọi, reject với `code 'INVALID_CREDENTIALS'`.
  - `it('lỗi mạng thành ApiError NETWORK')`: `http.get(..., () => HttpResponse.error())` thì `status 0`, `code 'NETWORK'`, `message 'Không kết nối được máy chủ'`.
  - `it('lỗi không phải JSON')`: 500 text thì `code 'UNKNOWN'`, `message 'Có lỗi xảy ra (HTTP 500)'`.
  - `it('204 trả undefined')`.
- [ ] **Step 2: Chạy test.** `npm run test -- src/api`. Mong đợi: FAIL.
- [ ] **Step 3: Viết `types.ts` và `client.ts`.**
- [ ] **Step 4: Chạy lại.** `npm run test -- src/api`. Mong đợi: PASS 7.
- [ ] **Step 5: Commit.** `feat(web): hợp đồng kiểu dữ liệu và apiFetch — T-001.3`

### Task 4 (T-001.4): `mockDb` — lưu trữ, seed, xác thực, gói, cài đặt

**Files:**
- Create: `web/src/mocks/memoryStorage.ts`, `web/src/mocks/seed.ts`, `web/src/mocks/mockDb.ts`
- Test: `web/src/mocks/mockDb.auth.test.ts`

**Interfaces:**
- Consumes: kiểu từ `api/types.ts` (Task 3).
- Produces:
  - `createMemoryStorage(): Storage`
  - `class MockError extends Error { status: number; code: string }`
  - `interface DbState { version: 1; users: (User & {password: string})[]; plans: Plan[]; orders: Order[]; licenses: (Omit<License,'status'|'activeMachines'> & {status: LicenseStatus})[]; activations: Activation[]; bank: BankSettings; app: AppRelease }`. Mật khẩu lưu thô, chỉ dùng cho giả lập.
  - `createSeed(now: Date): DbState`. Seed:
    - Users:
      - `u-admin` `admin@etax.vn` / `Admin@123`, Quản trị viên, `0900000000`, role admin;
      - `u-khach` `khach@etax.vn` / `Khach@123`, `Nguyễn Văn An`, `0912345678`, công ty `Công ty TNHH Kế toán An Phát`, MST `0101234567`.
      - `createdAt` của cả hai là `now − 9 tháng`.
    - Plans (đúng thứ tự, `sortOrder` 1–4, cả 4 đều active):
      - `p3` "Gói 3 tháng" 3 tháng / 1 máy / 1 lần đổi / 300000;
      - `p6` "Gói 6 tháng" 6 / 1 / 2 / 500000;
      - `p12` "Gói 12 tháng" 12 / 2 / 3 / 900000, featured;
      - `plife` "Trọn đời" null / 3 / 5 / 2500000.
      - `description` mỗi gói một câu tiếng Việt.
    - Orders:
      - `o-1` `ET903311`, `u-khach`, `p12`, paid. `createdAt = paidAt = now − 8 tháng`, `confirmedBy 'u-admin'`.
      - `o-2` `ET482915`, `u-khach`, `p6`, pending. `createdAt = now − 1 giờ`, `expiresAt = createdAt + 72h`.
    - License `l-1` (theo `o-1`):
      - `startsAt = now − 8 tháng`, `expiresAt = startsAt + 12 tháng`;
      - `maxMachines 2`, `maxMachineChanges 3`, `machineChangesUsed 0`, active.
    - Activation `a-1` cho `l-1`:
      - `3FA2C91B7E0D4A55`, tên "Máy kế toán 1";
      - key `ETAX-MOCK-3FA2C91B7E0D4A55-l-1`.
    - Bank: `970436`, `Vietcombank`, `0071000123456`, `CONG TY TNHH ETAX`.
    - App: `1.4.0`, `/downloads/E-Tax-1.4.0.zip`, `releasedAt = now − 6 ngày`, notes `Giao diện mới, thanh trạng thái tổng tiền.`
  - `createMockDb(storage: Storage, now: () => Date = () => new Date()): MockDb`
    - Đọc `etax-mockdb-v1`. JSON hỏng hoặc `version !== 1` thì nạp seed.
    - Mọi thao tác ghi đều `setItem` ngay.
  - `MockDb` (task này):
    - `reset(): void`
    - `register(i: RegisterInput): AuthResponse`
    - `login(i: LoginInput): AuthResponse`
    - `userFromToken(token: string | null): User`
    - `updateProfile(userId: string, i: ProfileInput): User`
    - `changePassword(userId: string, i: ChangePasswordInput): void`
    - `listPlans(includeInactive?: boolean): Plan[]` (sắp theo `sortOrder`)
    - `createPlan(i: PlanInput): Plan`
    - `updatePlan(id: string, i: PlanInput): Plan`
    - `getBank()`, `setBank(b)`, `getApp()`, `setApp(a)`
  - Quy ước chung:
    - Mọi lỗi ném `MockError` với code và message đúng bảng R.
    - Id mới dùng `crypto.randomUUID()`.
    - User trả ra ngoài không có trường `password`.

- [ ] **Step 1: Viết test `mockDb.auth.test.ts`.** Dùng `const t = new Date('2026-11-16T03:00:00.000Z'); const db = createMockDb(createMemoryStorage(), () => t)`.
  - `R1`: `register({email:'KHACH@etax.vn', ...})` ném `{status:409, code:'EMAIL_TAKEN', message:'Email đã được đăng ký'}`. Đăng ký email mới thì trả `token === 'mock.' + user.id`, `user.role === 'customer'`, và không có `password`.
  - `R2`: sai mật khẩu ném `401 INVALID_CREDENTIALS 'Email hoặc mật khẩu không đúng'`. `login({email:'Khach@ETAX.vn', password:'Khach@123'})` thành công.
  - `R3`: đặt user khoá (sửa thẳng storage, hoặc dùng `adminSetUserStatus` ở Task 7; trong task này sửa qua storage) thì login ném `403 ACCOUNT_LOCKED 'Tài khoản đã bị khoá, vui lòng liên hệ hỗ trợ'`.
  - `R16/R20`: `userFromToken(null)`, `userFromToken('mock.khong-co')`, và token của user bị khoá đều ném `401 UNAUTHORIZED`.
  - `R18`: `changePassword` sai mật khẩu hiện tại thì `400 WRONG_PASSWORD 'Mật khẩu hiện tại không đúng'`. Đúng thì login bằng mật khẩu mới thành công.
  - **Lưu bền:** tạo db thứ hai trên cùng storage thì thấy user vừa đăng ký.
  - **Storage hỏng:** `storage.setItem('etax-mockdb-v1', '{hỏng')` rồi `createMockDb(storage)` thì `listPlans()` có độ dài 4. Làm tương tự với `{"version":0}`.
  - `listPlans()` trả 4 gói theo thứ tự `p3,p6,p12,plife`. Sau `updatePlan('p3', {...p3, active:false})` thì `listPlans()` còn 3 và `listPlans(true)` vẫn 4.
  - `updatePlan('x', …)` ném `404 PLAN_NOT_FOUND`.
- [ ] **Step 2: Chạy test.** `npm run test -- src/mocks`. Mong đợi: FAIL.
- [ ] **Step 3: Viết code ba file.**
- [ ] **Step 4: Chạy lại.** `npm run test -- src/mocks`. Mong đợi: PASS.
- [ ] **Step 5: Commit.** `feat(web): mockDb lưu trữ, seed, xác thực, gói — T-001.4`

### Task 5 (T-001.5): `mockDb` — đơn hàng và xác nhận thanh toán

**Files:**
- Modify: `web/src/mocks/mockDb.ts`
- Test: `web/src/mocks/mockDb.orders.test.ts`

**Interfaces:**
- Produces (thêm vào `MockDb`):
  - `createOrder(userId, planId): OrderDetail`
  - `listOrders(userId): Order[]` (mới nhất trước)
  - `getOrder(userId, id): OrderDetail`
  - `cancelOrder(userId, id): Order`
  - `adminListOrders(f: { status?: OrderStatus; q?: string }): AdminOrder[]`. `q` khớp không phân biệt hoa thường theo `code`, email, hoặc `taxCode`.
  - `adminConfirmOrder(adminId, id, note?: string): { order: Order; license: License }`
  - `adminCancelOrder(id, note?: string): Order`
  - `buildQrUrl(bank: BankSettings, amount: number, content: string): string`, export riêng. Công thức đúng như spec: `https://img.vietqr.io/image/${bin}-${acc}-compact2.png?amount=${amount}&addInfo=${content}&accountName=${encodeURIComponent(accountName)}`.

- [ ] **Step 1: Viết test.**
  - `R4`:
    - `createOrder('u-khach','p6')` cho `status 'pending'`, `amount 500000`, `planSnapshot.name 'Gói 6 tháng'`, `expiresAt === t + 72h`, `code` khớp `/^ET\d{6}$/`.
    - `payment.transferContent === code`, `payment.qrUrl` chứa `970436-0071000123456-compact2.png?amount=500000&addInfo=` + code.
    - `licenseId === null`.
    - Gói ngừng bán hoặc không tồn tại thì `404 PLAN_NOT_FOUND`.
  - `R4` (chụp giá): sau `updatePlan('p6', {...price: 999000})`, đơn cũ vẫn `amount 500000`.
  - `R5`:
    - tăng `t` thêm 73h thì `getOrder` cho `status 'expired'`;
    - `adminConfirmOrder` lúc đó ném `409 ORDER_NOT_PENDING 'Đơn không còn ở trạng thái chờ thanh toán'`;
    - `cancelOrder` cũng ném lỗi này.
  - `R6`:
    - `adminConfirmOrder('u-admin', o.id, 'VCB FT1')` cho `order.status 'paid'`, `paidAt === t`, `confirmedBy 'u-admin'`, `note 'VCB FT1'`;
    - `license.startsAt === t`, `license.expiresAt === dayjs(t).add(6,'month').toISOString()`, `maxMachines 1`, `maxMachineChanges 2`, `activeMachines 0`;
    - sau đó `getOrder(...).licenseId === license.id`.
    - Xác nhận đơn `plife` thì `license.expiresAt === null`.
    - Cộng tháng cuối tháng: `t = 2027-01-31T03:00Z`, gói 1 tháng (tạo bằng `createPlan`) cho `expiresAt` ngày `2027-02-28`.
  - `R15`: user khác gọi `getOrder` hoặc `cancelOrder` đơn của `u-khach` thì `404 NOT_FOUND`.
  - `R17`: mock `Math.random` trả lần lượt cùng giá trị (trùng `ET482915`) rồi giá trị khác thì code mới khác `ET482915`.
  - `adminListOrders({status:'pending'})` chứa `ET482915`. `adminListOrders({q:'0101234567'})` trả đơn của khách mẫu kèm `userEmail 'khach@etax.vn'`.
- [ ] **Step 2: Chạy test.** Mong đợi: FAIL.
- [ ] **Step 3: Viết code.**
  - Trạng thái suy ra (R5) tính khi đọc bằng hàm `effectiveOrderStatus(o, now)`, không ghi lại vào storage.
  - Cộng tháng bằng `dayjs(x).add(n,'month')`.
- [ ] **Step 4: Chạy lại.** Mong đợi: PASS.
- [ ] **Step 5: Commit.** `feat(web): mockDb đơn hàng và xác nhận thanh toán — T-001.5`

### Task 6 (T-001.6): `mockDb` — bản quyền và máy

**Files:**
- Modify: `web/src/mocks/mockDb.ts`
- Test: `web/src/mocks/mockDb.licenses.test.ts`

**Interfaces:**
- Consumes: `normalizeMachineId`, `isValidMachineId`, `MACHINE_ID_MESSAGE` (Task 2).
- Produces (thêm vào `MockDb`):
  - `listLicenses(userId): License[]`
  - `getLicense(userId, id): LicenseDetail` (chỉ máy chưa gỡ)
  - `addActivation(userId, licenseId, i: { machineId: string; machineName: string }): Activation`
  - `removeActivation(userId, licenseId, actId): void`
  - `adminListLicenses(f: { status?: LicenseStatus; q?: string }): AdminLicense[]`. `q` khớp email, `code` của đơn, hoặc mã máy của activation.
  - `adminGetLicense(id): AdminLicenseDetail` (gồm cả máy đã gỡ)
  - `revokeLicense(id): License`
  - `extendLicense(id, months: number): License`

- [ ] **Step 1: Viết test.** Dùng license seed `l-1` (2 máy, đang dùng 1).
  - `R7`: tăng `t` qua `expiresAt` thì `listLicenses` cho `status 'expired'`.
  - `R8`:
    - `machineId ' 1a2b3c4d5e6f7a8b\n'` được lưu thành `'1A2B3C4D5E6F7A8B'`, `key === 'ETAX-MOCK-1A2B3C4D5E6F7A8B-l-1'`;
    - `'1A2B-3C4D'` ném `400 INVALID_MACHINE_ID 'Mã máy phải gồm 16 ký tự 0-9, A-F'`;
    - `machineName` rỗng sau trim ném `400 INVALID_MACHINE_NAME 'Vui lòng nhập tên máy'`.
  - `R9`: license expired hoặc revoked thì `409 LICENSE_INACTIVE 'Bản quyền đã hết hạn hoặc bị thu hồi'`.
  - `R10`: thêm lại `3FA2C91B7E0D4A55` thì trả activation `a-1` (cùng `id` và `key`), `activeMachines` vẫn 1.
  - `R11`: thêm máy thứ 2 thành công. Thêm máy thứ 3 ném `409 MACHINE_LIMIT 'Đã dùng hết số máy của gói, hãy gỡ một máy trước'`.
  - `R12`:
    - `removeActivation` cho `machineChangesUsed` +1 và máy biến mất khỏi `getLicense().activations`, nhưng vẫn còn trong `adminGetLicense().activations` với `revokedAt === t`;
    - khi `machineChangesUsed === maxMachineChanges` thì ném `409 CHANGE_LIMIT 'Đã hết số lần đổi máy của gói'`.
  - `R13`:
    - license còn hạn, `extendLicense(id, 3)` cho `expiresAt` = cũ + 3 tháng;
    - license đã hết hạn (t vượt 10 ngày) thì `expiresAt = t + 3 tháng` và `status 'active'`;
    - gói trọn đời ném `409 LICENSE_PERMANENT 'Bản quyền trọn đời không cần gia hạn'`.
  - `R14`: `revokeLicense` cho `status 'revoked'`, mọi activation có `revokedAt === t`, và `activeMachines 0`.
  - `R15`: user khác thì `getLicense`, `addActivation`, `removeActivation` đều `404 NOT_FOUND`.
  - `adminListLicenses({q:'3FA2C91B'})` có `l-1`.
- [ ] **Step 2: Chạy test.** Mong đợi: FAIL.
- [ ] **Step 3: Viết code.** `effectiveLicenseStatus(l, now)`: `revoked` giữ nguyên; nếu `expiresAt` không null và `< now` thì `expired`; còn lại `active`.
- [ ] **Step 4: Chạy lại.** Mong đợi: PASS.
- [ ] **Step 5: Commit.** `feat(web): mockDb bản quyền, thêm/gỡ máy, gia hạn, thu hồi — T-001.6`

### Task 7 (T-001.7): `mockDb` — khách hàng và số liệu (admin)

**Files:**
- Modify: `web/src/mocks/mockDb.ts`
- Test: `web/src/mocks/mockDb.admin.test.ts`

**Interfaces:**
- Produces:
  - `adminListUsers(q?: string): UserSummary[]`. Chỉ role customer, mới nhất trước. `q` khớp tên, email, SĐT, MST, không phân biệt hoa thường.
  - `adminGetUser(id): AdminUserDetail`
  - `adminSetUserStatus(adminId, id, status: UserStatus): User`
  - `stats(): AdminStats`

- [ ] **Step 1: Viết test.**
  - `adminListUsers()` có `khach@etax.vn` với `orderCount 2`, `licenseCount 1`, và không có admin.
  - `adminListUsers('an phát')` vẫn ra khách mẫu (khớp tên công ty).
  - `R19`: `adminSetUserStatus('u-admin','u-admin','locked')` ném `409 CANNOT_LOCK_SELF 'Không thể khoá tài khoản đang đăng nhập'`.
  - Khoá khách thì `login` của khách ném `ACCOUNT_LOCKED` (R3), và `userFromToken('mock.u-khach')` ném 401 (R20).
  - `stats()` với seed: `revenueThisMonth 0`, `pendingOrders 1`, `activeLicenses 1`, `newCustomersThisMonth 0`. Sau `register` 1 khách và xác nhận `ET482915`: `revenueThisMonth 500000`, `pendingOrders 0`, `newCustomersThisMonth 1`.
  - `adminGetUser('u-khach').orders.length === 2`. `adminGetUser('x')` ném `404 NOT_FOUND`.
- [ ] **Step 2: Chạy test.** Mong đợi: FAIL.
- [ ] **Step 3: Viết code.**
- [ ] **Step 4: Chạy lại.** Mong đợi: PASS.
- [ ] **Step 5: Commit.** `feat(web): mockDb khách hàng và số liệu quản trị — T-001.7`

### Task 8 (T-001.8): MSW handlers và các hàm `api/*`

**Files:**
- Create:
  - `web/src/mocks/handlers.ts`, `web/src/mocks/browser.ts`
  - `web/src/api/auth.ts`, `web/src/api/plans.ts`, `web/src/api/orders.ts`, `web/src/api/licenses.ts`, `web/src/api/admin.ts`
  - `web/src/test/server.ts`, `web/public/mockServiceWorker.js` (sinh bằng `npx msw init public --save`)
- Test: `web/src/api/api.test.ts`

**Interfaces:**
- Consumes: `MockDb` (Task 4–7), `apiFetch` và `configureClient` (Task 3).
- Produces:
  - `createHandlers(db: MockDb, opts?: { delayMs?: number }): HttpHandler[]`
    - Mỗi route dùng mẫu `*/api/...`.
    - Lấy token từ header `Authorization` (`Bearer x`).
    - Route cần vai trò: user khác vai trò thì `403 FORBIDDEN 'Bạn không có quyền thực hiện thao tác này'`.
    - Bắt `MockError` và trả `HttpResponse.json({code, message}, {status})`.
    - Thao tác `void` trả `new HttpResponse(null, {status: 204})`.
    - `POST /auth/forgot-password` luôn 204.
  - `browser.ts`: `export const mockDb = createMockDb(localStorage)`, `export const worker = setupWorker(...createHandlers(mockDb, { delayMs: 300 }))`.
  - `test/server.ts`: `setupMockServer(): { db: () => MockDb }`. Hàm này đăng ký `beforeAll(listen({onUnhandledRequest:'error'}))`, `beforeEach` tạo db mới trên `createMemoryStorage()` rồi `server.resetHandlers(...createHandlers(db))`, và `afterAll(close)`.
  - Hàm API, mỗi hàm một dòng `apiFetch`, đúng bảng API của spec:
    - `auth.ts`: `register(i)`, `login(i)`, `getMe()`, `forgotPassword(email)`, `updateProfile(i)`, `changePassword(i)`.
    - `plans.ts`: `getPlans()`, `getAppLatest()`.
    - `orders.ts`: `createOrder(planId)`, `getOrders()`, `getOrder(id)`, `cancelOrder(id)`.
    - `licenses.ts`: `getLicenses()`, `getLicense(id)`, `addActivation(licenseId, {machineId, machineName})`, `removeActivation(licenseId, actId)`.
    - `admin.ts`:
      - `getStats()`;
      - `getUsers(q?)`, `getUser(id)`, `setUserStatus(id, status)`;
      - `getAdminPlans()`, `createPlan(i)`, `updatePlan(id, i)`;
      - `getAdminOrders({status?, q?})`, `confirmOrder(id, note?)`, `cancelAdminOrder(id, note?)`;
      - `getAdminLicenses({status?, q?})`, `getAdminLicense(id)`, `revokeLicense(id)`, `extendLicense(id, months)`;
      - `getBankSettings()`, `saveBankSettings(b)`, `getAppSettings()`, `saveAppSettings(a)`.
      - Query string tạo bằng `URLSearchParams`, bỏ tham số rỗng.

- [ ] **Step 1: Viết test `api.test.ts`.** Dùng `setupMockServer()` và `configureClient({getToken: () => token, onUnauthorized: vi.fn()})`.
  - Chưa đăng nhập: `getPlans()` có 4 phần tử. `getOrders()` reject `401 UNAUTHORIZED`.
  - `login` khách rồi `createOrder('p6')` cho `amount 500000`. Sau đó `getOrders()` có 3 phần tử.
  - Khách gọi `getStats()` thì reject `403 FORBIDDEN`.
  - Admin: `confirmOrder(id, 'ok')` trả `{order:{status:'paid'}}`. Khách (đổi token) gọi `getLicenses()` có 2 phần tử.
  - `addActivation('l-1', {machineId:'zz', machineName:'A'})` reject `400 INVALID_MACHINE_ID`.
  - `removeActivation('l-1','a-1')` resolve `undefined` (204).
  - `forgotPassword('khong-co@x.vn')` resolve `undefined`.
  - `getAdminOrders({status:'pending', q:''})` gọi URL không có `q=`. Kiểm tra bằng handler `onUnhandledRequest` không báo lỗi và kết quả có `ET482915`.
- [ ] **Step 2: Chạy test.** Mong đợi: FAIL.
- [ ] **Step 3: Viết handlers, browser.ts, các file api và `test/server.ts`. Chạy `npx msw init public --save`.**
- [ ] **Step 4: Chạy lại.** `npm run test`. Mong đợi: PASS toàn bộ.
- [ ] **Step 5: Commit.** `feat(web): MSW handlers và lớp gọi API — T-001.8`

### Task 9 (T-001.9): Phiên đăng nhập, bảo vệ route, trang đăng nhập/đăng ký/quên mật khẩu

**Files:**
- Create:
  - `web/src/auth/session.ts`, `web/src/auth/redirect.ts`, `web/src/auth/AuthProvider.tsx`, `web/src/auth/guards.tsx`
  - `web/src/features/auth/LoginPage.tsx`, `web/src/features/auth/RegisterPage.tsx`, `web/src/features/auth/ForgotPasswordPage.tsx`, `web/src/features/auth/AuthCard.tsx`
- Test: `web/src/auth/auth.test.ts`, `web/src/features/auth/auth-pages.test.tsx`

**Interfaces:**
- Consumes: `api/auth.ts`, `configureClient` (Task 3, 8), validators (Task 2), `setupMockServer` (Task 8).
- Produces:
  - `loadSession(): {token: string; user: User} | null`: JSON hỏng thì xoá key và trả `null`.
  - `saveSession(s)`, `clearSession()`
  - `resolveAfterLogin(user: User, next: string | null): string`:
    - trả `next` chỉ khi `next` bắt đầu bằng `/` và không bắt đầu bằng `//` hoặc `/\`;
    - nếu không: admin vào `/quan-tri`, khách vào `/tai-khoan`.
  - `AuthProvider({children})` và `useAuth(): { user: User | null; login(i: LoginInput): Promise<User>; register(i: RegisterInput): Promise<User>; logout(): void; refreshMe(): Promise<void>; setUser(u: User): void }`.
    - Khi mount gọi `configureClient`. `onUnauthorized` làm ba việc: `clearSession()`, set user null, rồi `navigate('/dang-nhap?next=' + encodeURIComponent(pathname + search))` và `message.warning('Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại')`.
    - `logout` chuyển về `/`.
  - `RequireAuth()`: render `<Outlet/>`. Chưa đăng nhập thì `<Navigate to={'/dang-nhap?next=' + encodeURIComponent(pathname+search)} replace/>`.
  - `RequireRole({role})`: render `<Outlet/>`. Sai vai trò thì `<ForbiddenPage/>` (Task 10, tạm import từ `pages/ForbiddenPage` tạo ở task này dạng `Result 403`, Task 10 hoàn thiện).
  - Trang:
    - `LoginPage` (`/dang-nhap`): ô Email, Mật khẩu; nút "Đăng nhập"; link "Quên mật khẩu?" và "Đăng ký". Đã đăng nhập thì `Navigate` tới `resolveAfterLogin(user, next)`.
    - `RegisterPage` (`/dang-ky`): Họ tên, Email, Số điện thoại, Công ty, Mã số thuế, Mật khẩu, Nhập lại mật khẩu. Thông báo lỗi:
      - "Vui lòng nhập họ tên";
      - "Email không hợp lệ";
      - "Số điện thoại gồm 10 số, bắt đầu bằng 0";
      - "Mã số thuế gồm 10 số hoặc 10 số-3 số";
      - "Mật khẩu tối thiểu 8 ký tự";
      - "Mật khẩu nhập lại không khớp".
    - `ForgotPasswordPage` (`/quen-mat-khau`): gửi xong hiện `Result success` "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu".
    - `AuthCard`: Card rộng tối đa 400px, căn giữa trên nền gradient. Có logo chữ "E-Tax" (link `/`) và tiêu đề.
    - Lỗi API hiện `Alert type="error"` trên form với `err.message`. Nút submit có `loading` trong lúc gửi.

- [ ] **Step 1: Viết test.**
  - `auth.test.ts`:
    - `resolveAfterLogin(customer, null) === '/tai-khoan'`;
    - `(admin, null) === '/quan-tri'`;
    - `(customer, '/tai-khoan/mua-goi?goi=p6') === '/tai-khoan/mua-goi?goi=p6'`;
    - `(customer, '//evil.com')`, `(customer, 'https://evil.com')`, `(customer, '/\\evil.com')` đều `=== '/tai-khoan'`;
    - `localStorage.setItem('etax-session','{hỏng')` thì `loadSession() === null` và key đã bị xoá.
  - `auth-pages.test.tsx`. Render bằng `createMemoryRouter` (các route `/dang-nhap`, `/dang-ky`, `/tai-khoan` → `RequireAuth` → `<div>Trang tài khoản</div>`, `/quan-tri` → `RequireRole admin` → `<div>Trang quản trị</div>`), bọc `AppProviders` và `AuthProvider` qua một `RootLayout` thử nghiệm.
    - Đăng nhập `khach@etax.vn`/`Khach@123` thì thấy "Trang tài khoản".
    - Sai mật khẩu thì thấy "Email hoặc mật khẩu không đúng" và vẫn ở trang đăng nhập.
    - Vào `/tai-khoan` khi chưa đăng nhập thì router ở `/dang-nhap?next=%2Ftai-khoan`. Đăng nhập xong quay về "Trang tài khoản".
    - Khách vào `/quan-tri` thì thấy "Không có quyền truy cập".
    - Đăng ký với SĐT `0912` thì thấy "Số điện thoại gồm 10 số, bắt đầu bằng 0". Mật khẩu nhập lại khác thì thấy "Mật khẩu nhập lại không khớp".
    - Đăng ký hợp lệ thì vào "Trang tài khoản".
- [ ] **Step 2: Chạy test.** Mong đợi: FAIL.
- [ ] **Step 3: Viết code.**
- [ ] **Step 4: Chạy lại.** `npm run test`. Mong đợi: PASS.
- [ ] **Step 5: Commit.** `feat(web): phiên đăng nhập, bảo vệ route, trang đăng nhập/đăng ký — T-001.9`

### Task 10 (T-001.10): Thành phần dùng chung và trang lỗi

**Files:**
- Create:
  - `web/src/shared/components/CopyField.tsx`, `StatusTag.tsx`, `PageHeader.tsx`, `QueryState.tsx`
  - `web/src/pages/NotFoundPage.tsx`
  - `web/src/shared/ErrorBoundary.tsx`
- Modify: `web/src/pages/ForbiddenPage.tsx`
- Test: `web/src/shared/components/components.test.tsx`

**Interfaces:**
- Produces:
  - `CopyField({ label?: string; value: string; display?: ReactNode; multiline?: boolean })`
    - Hiện `display ?? value` kèm nút "Sao chép".
    - Bấm nút: `navigator.clipboard.writeText(value)` rồi `message.success('Đã sao chép')`. Lỗi clipboard thì `message.error('Không sao chép được, hãy chọn và sao chép thủ công')`.
  - `StatusTag({ kind: 'order' | 'license'; status: OrderStatus | LicenseStatus })`: nhãn và màu đúng bảng "Nhãn trạng thái" trong nen-tang/design.md.
  - `PageHeader({ title: ReactNode; extra?: ReactNode; backTo?: string; backLabel?: string })`
  - `QueryState<T>({ query: UseQueryResult<T>; isEmpty?: (d: T) => boolean; emptyText?: string; emptyAction?: ReactNode; children: (d: T) => ReactNode })`
    - Đang tải: `Skeleton active`.
    - Lỗi: `Result status="error" title="Không tải được dữ liệu" subTitle={err.message}` kèm nút "Thử lại" gọi `refetch`.
    - Trống: `Empty description={emptyText}` cùng `emptyAction`.
  - `ErrorBoundary({children})`: lỗi render thì hiện `Result status="error" title="Đã có lỗi xảy ra"` kèm nút "Tải lại trang".
  - `NotFoundPage`: `Result 404` "Không tìm thấy trang", nút "Về trang chủ".
  - `ForbiddenPage`: `Result 403` "Không có quyền truy cập", subtitle "Trang này chỉ dành cho quản trị viên.", nút "Về trang chủ".

- [ ] **Step 1: Viết test.**
  - `StatusTag` order pending hiện "Chờ thanh toán"; license revoked hiện "Đã thu hồi".
  - `CopyField` dùng mock `navigator.clipboard.writeText`. Bấm "Sao chép" thì được gọi với value và thấy "Đã sao chép".
  - `QueryState`: query lỗi thì thấy "Không tải được dữ liệu", bấm "Thử lại" thì `refetch` được gọi. Dữ liệu `[]` với `isEmpty = d => d.length === 0` thì thấy `emptyText`. Có dữ liệu thì render children.
  - `ErrorBoundary` bọc component ném lỗi thì thấy "Đã có lỗi xảy ra". Spy `console.error` để test không ồn.
- [ ] **Step 2: Chạy test.** Mong đợi: FAIL.
- [ ] **Step 3: Viết code.**
- [ ] **Step 4: Chạy lại.** Mong đợi: PASS.
- [ ] **Step 5: Commit.** `feat(web): thành phần dùng chung, trang 404/403, ErrorBoundary — T-001.10`

### Task 11 (T-001.11): Layout, router, công cụ dev, e2e TC-05

**Files:**
- Create:
  - `web/src/layouts/PublicLayout.tsx`, `AccountLayout.tsx`, `AdminLayout.tsx`, `SideNavLayout.tsx` (phần dùng chung của hai layout có menu trái)
  - `web/src/layouts/RootLayout.tsx`, `web/src/router.tsx`
  - `web/src/features/placeholder/ComingSoon.tsx`, `web/src/dev/DevToolbar.tsx`
  - `web/playwright.config.ts`, `web/tests/e2e/nen-tang.spec.ts`
- Modify: `web/src/App.tsx`, `web/src/main.tsx`
- Test: `web/src/router.test.tsx`

**Interfaces:**
- Consumes: mọi thứ ở trên.
- Produces:
  - `routes: RouteObject[]` (export cho test) và `router = createBrowserRouter(routes)`. Cây route đúng bảng Route trong spec:
    - `RootLayout` (AuthProvider + ErrorBoundary + DevToolbar + Outlet) bọc tất cả.
    - `PublicLayout`: `/`, `/bang-gia`, `/tai-ung-dung`.
    - Không layout: `/dang-nhap`, `/dang-ky`, `/quen-mat-khau`.
    - `RequireAuth` → `AccountLayout`: `/tai-khoan` và các con `mua-goi`, `don-hang`, `don-hang/:id`, `ban-quyen`, `ban-quyen/:id`, `ho-so`.
    - `RequireAuth` → `RequireRole admin` → `AdminLayout`: `/quan-tri` và các con `don-hang`, `khach-hang`, `khach-hang/:id`, `goi`, `ban-quyen`, `ban-quyen/:id`, `cai-dat`.
    - `*` → `NotFoundPage`.
    - Màn chưa làm: `<ComingSoon title="…"/>` (`Result status="info"` với title tên màn và subtitle "Màn hình đang được xây dựng"). Tên màn lấy theo cột Màn hình của bảng Route.
  - `PublicLayout`:
    - Header cao 64px, gồm logo chữ "E-Tax" (`colorPrimary`, đậm), menu ngang "Trang chủ / Bảng giá / Tải ứng dụng".
    - Bên phải: chưa đăng nhập thì nút "Đăng nhập" và nút primary "Mua ngay" (`/bang-gia`). Đã đăng nhập thì `Dropdown` avatar có "Tài khoản" (khách) hoặc "Quản trị" (admin) và "Đăng xuất".
    - Màn < 768px: menu thu vào nút ☰ mở `Drawer`.
    - Footer: `© {năm} E-Tax · Hỗ trợ: 0900 000 000`.
  - `SideNavLayout({ items: {key: string; label: string; icon: ReactNode; badge?: number}[]; theme: 'light' | 'dark'; title: string })`
    - Màn ≥ 992px (`Grid.useBreakpoint().lg`): `Layout.Sider` cố định rộng 220px.
    - Nhỏ hơn: Sider ẩn; header có nút ☰ (`aria-label="Mở menu"`) mở `Drawer` chứa cùng `Menu`.
    - Mục menu đang chọn suy từ `pathname`.
    - Header có avatar, tên user, nút "Đăng xuất" và link "← Trang chủ".
  - `AccountLayout`: items Tổng quan `/tai-khoan`, Mua gói, Đơn hàng, Bản quyền, Hồ sơ. Theme light.
  - `AdminLayout`: items Bảng điều khiển `/quan-tri`, Đơn hàng (badge = `stats.pendingOrders` qua `useQuery(['admin','stats'], getStats)`), Khách hàng, Bản quyền, Gói & giá, Cài đặt. Theme dark.
  - `DevToolbar`:
    - Chỉ render khi `import.meta.env.VITE_USE_MOCK === 'true'`.
    - `FloatButton.Group` tooltip "Dữ liệu mẫu" gồm:
      - "Đặt lại dữ liệu": `(await import('../mocks/browser')).mockDb.reset()`, `clearSession`, rồi `location.assign('/')`;
      - "Đăng nhập khách" và "Đăng nhập admin": gọi `useAuth().login` với tài khoản seed, rồi điều hướng `resolveAfterLogin`.
  - `main.tsx`: nếu `import.meta.env.VITE_USE_MOCK === 'true'` thì `const { worker } = await import('./mocks/browser'); await worker.start({ onUnhandledRequest: 'bypass' })`, rồi mới render.
  - `App` render `<AppProviders><RouterProvider router={router}/></AppProviders>`.
  - `playwright.config.ts`:
    - `testDir: 'tests/e2e'`, `use.baseURL 'http://localhost:5174'`, chỉ project `chromium`;
    - `webServer: { command: 'npm run dev -- --port 5174 --strictPort', url: 'http://localhost:5174', reuseExistingServer: true }`.

- [ ] **Step 1: Viết test `router.test.tsx`.** Dùng `createMemoryRouter(routes, {initialEntries})`, `AppProviders`, `setupMockServer`.
  - `/khong-co` thì thấy "Không tìm thấy trang".
  - `/bang-gia` thì thấy header "E-Tax" và nút "Đăng nhập".
  - Sau khi `saveSession` khách (dùng `login` từ mockDb), `/tai-khoan/ban-quyen` hiện menu "Bản quyền" ở trạng thái chọn (`.ant-menu-item-selected` chứa "Bản quyền") và ComingSoon "Bản quyền".
  - Session admin, `/quan-tri` thì thấy "Bảng điều khiển" và badge "1" trên mục "Đơn hàng".
  - Khách vào `/quan-tri/goi` thì thấy "Không có quyền truy cập".
- [ ] **Step 2: Chạy test.** Mong đợi: FAIL.
- [ ] **Step 3: Viết code các file.**
- [ ] **Step 4: Chạy lại.** `npm run test && npm run lint && npm run build`. Mong đợi: PASS.
- [ ] **Step 5: Kiểm tra build tắt giả lập không chứa MSW.** Chạy `npm run build` (mode production đọc `.env.production`, `VITE_USE_MOCK=false`), rồi `grep -l "setupWorker\|mockServiceWorker" dist/assets/*.js | wc -l`. Mong đợi: `0`. Nếu khác 0, đổi điều kiện trong `main.tsx` để Vite thay thế tĩnh được (`if (import.meta.env.VITE_USE_MOCK === 'true')` ở cấp module), rồi chạy lại.
- [ ] **Step 6: Viết e2e `tests/e2e/nen-tang.spec.ts`.**
  - `TC-05a`: `goto('/tai-khoan')` thì URL là `/dang-nhap?next=%2Ftai-khoan`.
  - `TC-05b`: đăng nhập khách thì URL `/tai-khoan`. Tiếp theo `goto('/quan-tri')` thì thấy text "Không có quyền truy cập".
  - `login-admin`: đăng nhập admin thì URL `/quan-tri`, thấy menu "Bảng điều khiển".
  - `mobile-drawer`: viewport 390×844, đăng nhập khách, bấm `getByLabel('Mở menu')` thì thấy Drawer có "Hồ sơ". `document.documentElement.scrollWidth <= 390`.
- [ ] **Step 7: Chạy e2e.** `npx playwright install chromium` (nếu chưa có), rồi `npm run e2e`. Mong đợi: 4 passed.
- [ ] **Step 8: Chụp màn hình.** Bằng Playwright MCP trên `npm run dev`: `/dang-nhap`, `/tai-khoan` (desktop), `/quan-tri` (desktop), `/tai-khoan` 390px mở menu. Lưu vào `docs/features/web-ban-quyen/nen-tang/screenshots/T-001.11-*.png`.
- [ ] **Step 9: Commit.** `feat(web): layout, router, công cụ dev, e2e TC-05 — T-001.11`

---

**Tiêu chí nghiệm thu**
- [ ] `npm run dev` mở được `/`: header, footer, các route giữ chỗ hiện "Màn hình đang được xây dựng".
- [ ] Đăng nhập khách/admin bằng tài khoản mẫu và bằng công cụ dev. Phân quyền đúng TC-05.
- [ ] Mỗi quy tắc R1–R20 có test Vitest pass.
- [ ] `npm run build`, `npm run lint`, `npm run test`, `npm run e2e` pass.
- [ ] Build production không chứa MSW.

**Kịch bản test (Playwright / e2e):** TC-05 (spec cha), cộng thêm `login-admin` và `mobile-drawer` ở Task 11.

**Rủi ro / câu hỏi mở**
- antd 5 chạy chính thức với React 18. Chọn React 18.3 để không cần gói vá tương thích React 19.
- `npx playwright install chromium` tải trình duyệt (~150MB) ở lần đầu.

---

## CR-002 — Vá các lỗi nhỏ còn tồn đọng sau review tổng

**Mục tiêu:** sửa toàn bộ các lỗi nhỏ đã ghi lại sau review tổng CR-001, trước khi làm màn hình thật và API C#.

**Cách làm:**
- Làm trong phiên này theo executing-plans.
- Mỗi task theo TDD: viết test tái hiện lỗi, thấy FAIL, sửa, thấy PASS.
- Sau mỗi task chạy `npm run test && npm run lint && npm run build`. Task có ảnh hưởng giao diện chạy thêm `npm run e2e`.
- Cuối cùng review một lần toàn bộ thay đổi CR-002.

**Mã lỗi mới / bổ sung** (thêm vào bảng quy tắc ở design.md cha):

| HTTP | Mã | Message |
|---|---|---|
| 400 | `VALIDATION_ERROR` | Nói rõ trường sai. Dùng cho số tháng, trạng thái, gói và body JSON hỏng |
| 400 | `INVALID_MACHINE_NAME` | "Vui lòng nhập tên máy" |
| 409 | `LICENSE_PERMANENT` | "Bản quyền trọn đời không cần gia hạn" |
| 409 | `CANNOT_LOCK_ADMIN` | "Không thể khoá tài khoản quản trị" |
| 409 | `LICENSE_REVOKED` | "Bản quyền đã bị thu hồi, không thể gia hạn" |

### Task 1 (T-002.1): Kiểm tra đầu vào của API giả lập

**Files:**
- Modify: `web/src/mocks/licenses.ts`, `admin.ts`, `mockDb.ts`, `handlers.ts`
- Test: các file `*.test.ts` sẵn có trong `web/src/mocks/`

**Thay đổi:**
- `extendLicense`:
  - `months` phải là số nguyên từ 1 đến 120, nếu không thì `VALIDATION_ERROR` "Số tháng gia hạn không hợp lệ";
  - license `revoked` thì `LICENSE_REVOKED`.
- `adminSetUserStatus`:
  - status chỉ nhận `active` hoặc `locked`, nếu không thì `VALIDATION_ERROR`;
  - khoá một admin khác thì `CANNOT_LOCK_ADMIN`; R19 (tự khoá chính mình) giữ nguyên.
- `createPlan` / `updatePlan`:
  - `name` không rỗng;
  - `price` là số nguyên ≥ 0;
  - `maxMachines` ≥ 1;
  - `maxMachineChanges` ≥ 0;
  - `durationMonths` là `null` hoặc số nguyên ≥ 1.
  - Sai thì trả `VALIDATION_ERROR` với message theo trường.
- `addActivation`: thiếu `machineName`, hoặc không phải chuỗi, thì trả `INVALID_MACHINE_NAME` (không còn 500).
- `handlers.ts`: body JSON hỏng thì trả 400 `VALIDATION_ERROR` "Dữ liệu gửi lên không hợp lệ" (không còn 500).
- Tìm kiếm `q` của admin chỉ khớp mã máy của activation đang dùng, tức là chưa gỡ.
- Test cộng tháng cuối tháng không phụ thuộc múi giờ: dựng mốc thời gian và so sánh đều bằng dayjs theo giờ máy.

**Test:** mỗi gạch đầu dòng trên có một test mới, viết trước và FAIL trước khi sửa. Riêng `handlers`: POST với body `{bad` phải nhận 400 `VALIDATION_ERROR`.

### Task 2 (T-002.2): Lớp API và khởi động

**Files:**
- Modify: `web/src/api/client.ts`, `web/src/api/{orders,licenses,admin,plans}.ts`, `web/src/App.tsx`, `web/src/main.tsx`, `web/src/dev/DevToolbar.tsx`, `web/vite.config.ts`
- Test: `web/src/api/client.test.ts` và file test API sẵn có

**Thay đổi:**
- Mọi tham số đường dẫn trong `api/*.ts` đi qua `encodeURIComponent`.
  - Test: `getOrder('a/../b')` gọi URL chứa `a%2F..%2Fb`.
- `apiFetch`: response 2xx không phải JSON (trừ 204) thì ném `ApiError(status, 'UNKNOWN', 'Phản hồi không hợp lệ từ máy chủ')`.
- QueryClient: hàm `shouldRetry(failureCount, error)`.
  - Không thử lại `ApiError` có status từ 400 đến 499.
  - Lỗi khác thử lại 1 lần.
- `main.tsx`: nếu `worker.start()` lỗi (ví dụ HTTP qua IP mạng LAN), app vẫn render và ghi `console.warn`.
- `DevToolbar.loginAs`: lỗi thì hiện `message.error(err.message)`.
- Build production không có `mockServiceWorker.js`: thêm plugin nhỏ trong `vite.config.ts`, xoá `dist/mockServiceWorker.js` khi `VITE_USE_MOCK !== 'true'`.
  - Kiểm tra: `npm run build` rồi `test ! -f dist/mockServiceWorker.js`.

### Task 3 (T-002.3): Giao diện và phiên

**Files:**
- Modify:
  - `web/src/auth/session.ts`
  - `web/src/layouts/SideNavLayout.tsx`, `PublicLayout.tsx`
  - `web/src/pages/NotFoundPage.tsx`, `ForbiddenPage.tsx`
  - `web/src/shared/components/StatusTag.tsx`
  - `web/src/shared/format.ts`
- Test: các file test sẵn có, cùng `web/src/router.test.tsx` và `tests/e2e/nen-tang.spec.ts`

**Thay đổi:**
- `loadSession`: chỉ nhận khi `token` là chuỗi và `user` có đủ `id`, `email`, `fullName` (chuỗi) cùng `role` ∈ {customer, admin}. Nếu không thì xoá `etax-session` và trả `null`.
  - Test: `{"token":"x","user":{}}` trả `null`, và storage bị xoá.
- Ngăn kéo menu (SideNavLayout, PublicLayout): đóng lại khi chuyển sang màn rộng, và khi đổi route.
  - Test e2e `drawer-resize`: viewport 390, mở menu, chuyển 1280, rồi về 390 thì menu không tự mở.
- 404/403: bỏ cách lồng `<Link><Button>`, thay bằng `<Button onClick={() => navigate('/')}>`.
  - Test: bấm "Về trang chủ" thì về `/`, và trang không còn phần tử `a button`.
- `StatusTag`: gặp trạng thái lạ thì hiện `Tag` mặc định với chính giá trị đó, không làm vỡ trang.
- Màu `#0075D6` viết cứng trong layout chuyển sang `theme.useToken().token.colorPrimary`.
- `formatDate` / `formatDateTime` với chuỗi rỗng hoặc ngày không hợp lệ thì hiện "—".
- Bổ sung test còn thiếu:
  - QueryState: khung chờ khi đang tải, và `emptyAction`;
  - PageHeader: nút quay lại;
  - PublicLayout khi đã đăng nhập: menu avatar có "Quản trị" (admin) hoặc "Tài khoản" (khách), và "Đăng xuất".

### Task 4 (T-002.4): Cập nhật spec

**Files:** `docs/features/web-ban-quyen/design.md`

- Thêm R21–R24 vào bảng quy tắc:
  - R21: khoá tài khoản admin;
  - R22: gia hạn license đã thu hồi;
  - R23: `VALIDATION_ERROR` (số tháng, trạng thái, gói, body hỏng);
  - R24: `INVALID_MACHINE_NAME`.
- Ghi message cho `LICENSE_PERMANENT` ở R13.
- Thêm mục "Kiểm tra phía máy chủ": ràng buộc của từng trường, để API ASP.NET Core làm giống hệt.
- Ghi chú cho màn hình: không truyền query `enabled: false` cố định vào `QueryState`; dùng render có điều kiện thay thế.

**Tiêu chí nghiệm thu**
- [ ] Mọi lỗi nhỏ ghi trong "Nhật ký phiên" của CR-001 đều đã sửa và có test (FAIL trước, PASS sau).
- [ ] `npm run test`, `lint`, `build`, `e2e` pass, output không có stderr hay cảnh báo.
- [ ] `dist/` không chứa `mockServiceWorker.js` hay mã giả lập.

**Kịch bản test (Playwright):**
- Chạy các e2e sẵn có: TC-05a/b, `login-admin`, `mobile-drawer`.
- Thêm `drawer-resize`.
- Chụp `screenshots/T-002.3-*.png`: trang 404 và menu điện thoại sau khi sửa.
