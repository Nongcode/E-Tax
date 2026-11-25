# E-Tax — hướng dẫn cho Claude

Tổng quan dự án (cấu trúc, luồng, file chính): đọc `PROJECT_OVERVIEW.md` trước, không quét lại toàn bộ code.

## Quy trình chức năng (BẮT BUỘC)

Mọi yêu cầu **tạo mới, sửa giao diện, sửa chức năng** (kể cả thay đổi nhỏ) hoặc **tiếp tục công việc dở dang** phải đi theo skill `feature`:

- Tài liệu mỗi chức năng nằm ở `docs/features/<module>/<ten-chuc-nang>/` gồm `plan.md`, `design.md`, `status.md`, `index.html`.
- Sổ đăng ký: `docs/features/README.md`.
- Lập kế hoạch và chia task vào `plan.md` **trước** khi sửa code; chờ người dùng duyệt (trừ khi họ nói "làm luôn").
- Cập nhật `status.md`, `design.md`, `index.html` **ngay sau mỗi task**, không dồn đến cuối.
- **Chức năng mới** (màn hình / API / model mới): BẮT BUỘC đi đủ chuỗi Superpowers — brainstorming → writing-plans → subagent-driven-development (TDD) → requesting-code-review → verification-before-completion → finishing-a-development-branch. Kết quả ghi vào `design.md` / `plan.md` / `status.md` của chức năng, KHÔNG ghi vào `docs/superpowers/`.
- Sau mỗi task có sửa code: chạy Playwright nếu thay đổi ảnh hưởng giao diện / luồng người dùng / API màn hình gọi; nếu bỏ qua phải ghi lý do vào `status.md`.
- Kiểm thử bằng Playwright (MCP hoặc `@playwright/test` nếu project có) + bộ test của project; ghi kết quả vào `status.md`.
- Ngoại lệ: câu hỏi thuần tuý, giải thích code, hoặc người dùng nói rõ "không cần quy trình".

### Lệnh kiểm tra

- Build: `dotnet build E-Tax.sln` (cảnh báo MSB3073 từ target taskkill là bình thường)
- Lint / type-check: không có riêng — dùng build (Nullable bật)
- Test: `dotnet test E-Tax.Tests`
- URL chạy dev (cho Playwright): không có — app WinForms desktop. Playwright chỉ dùng cho `index.html` của chức năng; giao diện app thật kiểm tra bằng ảnh chụp cửa sổ (PowerShell `CopyFromScreen`) lưu vào `screenshots/` của chức năng.

### Khi bắt đầu phiên mới

Nếu người dùng nói "tiếp tục", "làm tiếp", "đang làm dở" hoặc không nói rõ chức năng:
1. Đọc `docs/features/README.md`, tìm chức năng 🟡 IN_PROGRESS.
2. Đọc `status.md` của chức năng đó (Tóm tắt nhanh + File chính) — **không đọc lại toàn bộ code**.
3. Báo đang ở bước nào rồi tiếp tục.
