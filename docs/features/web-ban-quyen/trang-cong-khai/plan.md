# Plan — Trang công khai

## Tổng quan

- **Mục tiêu:** Trang chủ, Bảng giá (PlanCards), Tải ứng dụng.
- **Module:** web-ban-quyen
- **Chức năng con:** Không tách. Đây là con của [web-ban-quyen](../plan.md).
- **Spec:** [design.md](design.md) + [design.md cha](../design.md).

## Yêu cầu

### CR-001 — Trang công khai · 2026-11-16

**Mô tả yêu cầu (nguyên văn từ người dùng):**
> bạn hãy thực hiện dựng giao diện web cho tôi trước nhé

**Phạm vi**
- Trong phạm vi: Trang chủ, Bảng giá (PlanCards), Tải ứng dụng.
- Ngoài phạm vi: Blog/tin tức, SEO nâng cao, đa ngôn ngữ.

**Cách làm:** ba trang trong `web/src/features/public/`, thay các route `ComingSoon` công khai trong `router.tsx`. `PlanCards` dùng chung cho trang chủ và bảng giá. Dữ liệu lấy qua `getPlans` / `getAppLatest` (đã có ở `api/plans.ts`, MSW đã có handler). Không thêm thư viện.

**Task**

> **Dành cho agent:** BẮT BUỘC dùng superpowers:executing-plans (người dùng đã chọn thực thi inline). Mỗi task: viết test → chạy thấy FAIL → code → PASS → commit → cập nhật status.md / design.md / index.html.

**Mục tiêu:** khách vãng lai xem được trang chủ, bảng giá 4 gói từ API, trang tải ứng dụng; bấm "Mua gói" đi đúng luồng đăng nhập / mua gói.

**Kiến trúc:** trang thuần hiển thị, đọc dữ liệu bằng `useQuery` + `QueryState` (nen-tang). Điều hướng bằng `useNavigate`. Bố cục responsive bằng `Row`/`Col` của antd (`xs=24`), không CSS riêng ngoài style inline.

**Công nghệ:** React 18, antd 5, TanStack Query 5, react-router 7, Vitest + Testing Library, Playwright.

**Spec:** [design.md](design.md) + [design.md cha](../design.md) (mục Route, API `/plans`, `/app/latest`, Giao diện, TC-01, TC-06).

#### Ràng buộc chung

- Không thêm dependency. Mọi chữ hiển thị là tiếng Việt, đúng nguyên văn trong design.md.
- Tiền dùng `formatVnd`, ngày dùng `formatDate` (`shared/format.ts`).
- Query key: `['plans']` và `['app-latest']` (khu quản trị sẽ invalidate theo key này).
- Đang tải / lỗi / rỗng đi qua `QueryState`; không truyền query `enabled:false` vào `QueryState`.
- Viewport 390 px không tràn ngang (TC-06).
- Lệnh kiểm tra (trong `web/`): `npm run test`, `npm run lint`, `npm run build`, `npm run e2e`.

#### Review Focus

1. Không có gói nào đang bán (`GET /plans` trả `[]`) → hiện `Empty` "Hiện chưa có gói nào đang bán", không trang trắng. (Test ở T-001.1.)
2. Gói trọn đời (`durationMonths: null`) → hiện "trọn đời", không "/ null tháng". (T-001.1.)
3. Admin đang đăng nhập bấm "Mua gói" → spec chỉ nói "đã đăng nhập thì tới `/tai-khoan/mua-goi`", nhưng route đó chỉ cho khách nên admin sẽ gặp 403. **Quyết định:** với admin, nút bị vô hiệu kèm chữ "Tài khoản quản trị không mua gói". (T-001.1.)
4. `next` chứa query `?goi=` phải được mã hoá: `/dang-nhap?next=%2Ftai-khoan%2Fmua-goi%3Fgoi%3Dp6`, và đăng nhập xong quay về đúng `/tai-khoan/mua-goi?goi=p6`. (T-001.1 unit, T-001.4 e2e.)
5. `/app/latest` lỗi hoặc `notes` rỗng → lỗi hiện `Result` + "Thử lại"; `notes` rỗng thì ẩn khối ghi chú. (T-001.3.)

---

##### Task 1 (T-001.1): PlanCards và trang Bảng giá

**Files:**
- Create: `web/src/features/public/PlanCards.tsx`, `web/src/features/public/PricingPage.tsx`, `web/src/features/public/public.test.tsx`
- Modify: `web/src/router.tsx` (route `/bang-gia`)

**Interfaces:**
- Consumes: `getPlans(): Promise<Plan[]>`, `QueryState`, `formatVnd`, `useAuth()`.
- Produces:
  - `formatPlanPeriod(p: Pick<Plan,'durationMonths'>): string` → `"/ 3 tháng"` hoặc `"trọn đời"`.
  - `buyPath(planId: string): string` → `/tai-khoan/mua-goi?goi=<encodeURIComponent(id)>`.
  - `export function PlanCards({ compact }: { compact?: boolean })` — tự gọi `useQuery({ queryKey: ['plans'], queryFn: getPlans })`; `compact` bỏ dòng mô tả (dùng ở trang chủ).
  - `export default function PricingPage()`.

- [ ] **Bước 1: Viết test** trong `public.test.tsx` (render qua `AppProviders` + `createMemoryRouter(routes)`, `setupMockServer()`, `QueryClient` retry false — giống `router.test.tsx`):
  - `formatPlanPeriod({durationMonths: 6})` = `'/ 6 tháng'`; `formatPlanPeriod({durationMonths: null})` = `'trọn đời'`.
  - `/bang-gia`: thấy 4 tên gói "Gói 3 tháng", "Gói 6 tháng", "Gói 12 tháng", "Trọn đời"; thấy "500.000 ₫" (dùng `formatVnd(500000)` để so); thấy đúng một "Phổ biến"; thẻ p12 có "Dùng trên 2 máy" và "Đổi máy 3 lần"; có "Cập nhật miễn phí trong thời hạn", "Hỗ trợ qua Zalo/điện thoại"; có ghi chú chứa "VietQR".
  - Chưa đăng nhập, bấm "Mua gói" của Gói 6 tháng → `router.state.location` là `/dang-nhap` với `search` = `?next=%2Ftai-khoan%2Fmua-goi%3Fgoi%3Dp6`.
  - Khách đăng nhập, bấm "Mua gói" Gói 6 tháng → pathname `/tai-khoan/mua-goi`, search `?goi=p6`.
  - Admin đăng nhập → mọi nút "Mua gói" `disabled`, thấy "Tài khoản quản trị không mua gói".
  - `mock.db()` đặt mọi gói `active:false` qua `updatePlan` → thấy "Hiện chưa có gói nào đang bán".
  - `server.use(http.get('*/api/plans', () => HttpResponse.json({code:'X',message:'Lỗi thử'},{status:500})))` → thấy "Không tải được dữ liệu" và nút "Thử lại".
- [ ] **Bước 2: Chạy** `npx vitest run src/features/public` → FAIL (chưa có module / vẫn ComingSoon).
- [ ] **Bước 3: Code** `PlanCards.tsx`, `PricingPage.tsx`, gắn route `/bang-gia`.
  - Lưới `Row gutter={[16,16]}`, mỗi thẻ `Col xs={24} sm={12} lg={6}`, `Card` cao bằng nhau.
  - Gói `featured`: bọc `Badge.Ribbon text="Phổ biến"`, viền `token.colorPrimary`.
  - Đang tải: `PlanCards` tự kiểm `query.isPending` và hiện 4 thẻ `Card loading` (spec yêu cầu 4 thẻ Skeleton, `QueryState` chỉ có 1); lỗi / rỗng / dữ liệu đi qua `QueryState`.
  - Trang: tiêu đề "Bảng giá", `PlanCards`, ghi chú "Thanh toán bằng chuyển khoản VietQR. Key bản quyền được cấp ngay sau khi chúng tôi xác nhận thanh toán."
- [ ] **Bước 4: Chạy** `npx vitest run src/features/public` → PASS; `npm run test && npm run lint && npm run build` → xanh.
- [ ] **Bước 5: Commit** `feat(web): bảng giá và PlanCards — T-001.1`.

##### Task 2 (T-001.2): Trang chủ

**Files:** Create `web/src/features/public/HomePage.tsx`; Modify `router.tsx` (route `/`), `public.test.tsx`.

**Interfaces:** Consumes `PlanCards` (compact). Produces `export default function HomePage()`.

- [ ] **Bước 1: Test** `/`:
  - thấy tiêu đề "Tải hoá đơn điện tử hàng loạt chỉ trong vài phút";
  - đủ 6 tiêu đề thẻ tính năng (nguyên văn design.md mục 2), 3 bước "Mua gói", "Cài ứng dụng và lấy mã máy", "Nhập key kích hoạt";
  - thấy "Gói 12 tháng" (PlanCards rút gọn);
  - 5 câu hỏi hỏi đáp; bấm câu "Dùng trên mấy máy?" thì thấy câu trả lời chứa "số máy của gói";
  - bấm "Xem bảng giá" → pathname `/bang-gia`; bấm "Tải ứng dụng" (nút ở hero, không phải menu) → `/tai-ung-dung`.
- [ ] **Bước 2: Chạy** → FAIL.
- [ ] **Bước 3: Code** `HomePage.tsx` theo design.md mục 1–6. Hình minh hoạ hero là khối `div` CSS giả cửa sổ app (thanh tiêu đề + vài dòng bảng), ẩn khi `xs`. Nội dung 5 câu trả lời hỏi đáp:
  - Dùng trên mấy máy? — "Mỗi gói ghi rõ số máy của gói; có thể gỡ máy cũ để chuyển sang máy mới trong giới hạn số lần đổi máy."
  - Đổi máy thế nào? — "Vào Tài khoản → Bản quyền → gỡ máy cũ, rồi thêm mã máy mới để nhận key mới."
  - Thanh toán ra sao, bao lâu có key? — "Chuyển khoản theo mã VietQR trên đơn hàng. Key có ngay sau khi chúng tôi xác nhận thanh toán, thường trong giờ làm việc."
  - Dữ liệu có an toàn không? — "Ứng dụng chạy trên máy của bạn và tải hoá đơn trực tiếp từ cổng thuế; dữ liệu không đi qua máy chủ trung gian."
  - Hết hạn thì sao? — "Ứng dụng ngừng tải hoá đơn mới; dữ liệu đã tải vẫn giữ nguyên. Gia hạn bằng cách mua gói mới."
  - Dải cuối trang: "Sẵn sàng tiết kiệm hàng giờ nhập liệu?" + nút "Xem bảng giá".
- [ ] **Bước 4: Chạy** → PASS; test + lint + build xanh.
- [ ] **Bước 5: Commit** `feat(web): trang chủ — T-001.2`.

##### Task 3 (T-001.3): Trang Tải ứng dụng

**Files:** Create `web/src/features/public/DownloadPage.tsx`; Modify `router.tsx` (route `/tai-ung-dung`), `public.test.tsx`.

**Interfaces:** Consumes `getAppLatest(): Promise<AppRelease>`, `formatDate`. Produces `export default function DownloadPage()`; query key `['app-latest']`.

- [ ] **Bước 1: Test** `/tai-ung-dung`:
  - thấy "Phiên bản 1.4.0", ngày phát hành = `formatDate(seed releasedAt)`, link "Tải về" có `href` `/downloads/E-Tax-1.4.0.zip`, ghi chú "Giao diện mới, thanh trạng thái tổng tiền.";
  - yêu cầu hệ thống: "Windows 10/11 64-bit", link ".NET 8 Desktop Runtime" `href` `https://dotnet.microsoft.com/download/dotnet/8.0`, "Kết nối Internet";
  - 4 bước kích hoạt (nguyên văn design.md);
  - `mock.db().setApp({...app, notes: ''})` → không có khối "Ghi chú phát hành";
  - `/app/latest` trả 500 → "Không tải được dữ liệu" + "Thử lại".
- [ ] **Bước 2: Chạy** → FAIL.
- [ ] **Bước 3: Code** `DownloadPage.tsx`: `Card` phiên bản (nút `Button type="primary" href={downloadUrl}` "Tải về"), `Card` yêu cầu hệ thống, `Steps direction="vertical"` 4 bước.
- [ ] **Bước 4: Chạy** → PASS; test + lint + build xanh. Xoá import `ComingSoon` khỏi route công khai (vẫn giữ cho khu tài khoản/quản trị).
- [ ] **Bước 5: Commit** `feat(web): trang tải ứng dụng — T-001.3`.

##### Task 4 (T-001.4): E2E, ảnh chụp

**Files:** Create `web/tests/e2e/trang-cong-khai.spec.ts`; ảnh `docs/features/web-ban-quyen/trang-cong-khai/screenshots/`.

- [ ] **Bước 1: Viết e2e** (beforeEach xoá localStorage như `nen-tang.spec.ts`):
  - `TC-01`: mở `/`, bấm "Xem bảng giá" (nút hero) → URL `/bang-gia`, 4 thẻ có nút "Mua gói", thấy "900.000 ₫", "Phổ biến" nằm trong thẻ có "Gói 12 tháng".
  - `buy-next`: chưa đăng nhập, `/bang-gia` bấm "Mua gói" thẻ Gói 6 tháng → URL `/dang-nhap?next=…`; đăng nhập khách → URL `/tai-khoan/mua-goi?goi=p6`.
  - `TC-06-public`: viewport 390×844, lần lượt `/`, `/bang-gia`, `/tai-ung-dung`: `document.documentElement.scrollWidth <= 390`; mở menu ☰ thấy "Bảng giá".
- [ ] **Bước 2: Chạy** `npm run e2e` → mọi test PASS (gồm 6 test nen-tang cũ).
- [ ] **Bước 3: Chụp ảnh** bằng spec tạm (xoá sau khi chụp; đường dẫn tuyệt đối dạng `D:/E-Tax/...`): `T-001.4-trang-chu.png` (1280), `T-001.4-bang-gia.png` (1280), `T-001.4-tai-ung-dung.png` (1280), `T-001.4-bang-gia-mobile.png` (390). Xem lại từng ảnh.
- [ ] **Bước 4: Commit** `test(web): e2e trang công khai — T-001.4`.

**Tiêu chí nghiệm thu**
- [ ] 3 trang thay `ComingSoon`, nội dung đúng design.md.
- [ ] Luồng "Mua gói" đúng cho khách vãng lai / khách / admin.
- [ ] `npm run test`, `lint`, `build`, `e2e` xanh, không stderr.

**Kịch bản test (Playwright / e2e)**

| ID | Kịch bản | Mong đợi |
|---|---|---|
| TC-01 | Mở `/`, bấm "Xem bảng giá" | 4 gói, giá đúng định dạng, Gói 12 tháng có "Phổ biến" |
| buy-next | Chưa đăng nhập bấm "Mua gói" Gói 6 tháng, rồi đăng nhập khách | Về `/tai-khoan/mua-goi?goi=p6` |
| TC-06-public | 390×844: `/`, `/bang-gia`, `/tai-ung-dung`, mở ☰ | Không tràn ngang, Drawer mở được |
