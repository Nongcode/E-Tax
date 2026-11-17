# Status — Trang công khai

> Nguồn sự thật để tiếp tục công việc. Cập nhật NGAY sau mỗi task.
> Khi tiếp tục: đọc "Tóm tắt nhanh" + "File chính" là đủ, không cần đọc lại toàn bộ code.

## Tóm tắt nhanh

| Mục | Giá trị |
|---|---|
| Trạng thái | 🟡 IN_PROGRESS |
| Yêu cầu đang làm | CR-001 — Trang công khai |
| Bước Superpowers | writing-plans xong — plan chờ người dùng duyệt (thực thi: executing-plans inline) |
| Task đang làm | — |
| **Bước tiếp theo** | Người dùng duyệt plan CR-001, rồi làm T-001.1 |
| Branch | feature/web-ban-quyen |
| Cập nhật lần cuối | 2026-11-17 16:13 |
| Ghi chú dở dang | Không |

## File chính

| File | Vai trò |
|---|---|
| web/src/features/public/ | PlanCards, PricingPage, HomePage, DownloadPage (dự kiến) |
| web/src/router.tsx | Route `/`, `/bang-gia`, `/tai-ung-dung` |
| web/tests/e2e/trang-cong-khai.spec.ts | E2E TC-01, buy-next, TC-06-public |

## Yêu cầu

### CR-001 — Trang công khai · 🟡 IN_PROGRESS · 2026-11-16

| ID | Task | Trạng thái | Test | Ghi chú |
|---|---|---|---|---|
| T-001.1 | PlanCards và trang Bảng giá | ⬜ TODO | vitest | |
| T-001.2 | Trang chủ | ⬜ TODO | vitest | |
| T-001.3 | Trang Tải ứng dụng | ⬜ TODO | vitest | |
| T-001.4 | E2E (TC-01, buy-next, TC-06-public), ảnh chụp | ⬜ TODO | Playwright + ảnh | |

## Kết quả test gần nhất

| Thời gian | Lệnh / kịch bản | Kết quả | Ghi chú |
|---|---|---|---|
| — | Chưa chạy | — | — |

## Nhật ký phiên

- 2026-11-16 23:15 — Tạo folder và design.md (màn hình chi tiết) trong bước brainstorming.
- 2026-11-17 16:13 — writing-plans: plan CR-001 (4 task T-001.1 → T-001.4). Quyết định cần duyệt: admin bấm "Mua gói" thì nút bị vô hiệu kèm "Tài khoản quản trị không mua gói" (spec chưa nói). Chờ duyệt.
