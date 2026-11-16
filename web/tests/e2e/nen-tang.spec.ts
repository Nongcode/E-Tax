import { expect, test, type Page } from '@playwright/test';

async function login(page: Page, email: string, password: string, next?: string) {
  await page.goto(next ? `/dang-nhap?next=${encodeURIComponent(next)}` : '/dang-nhap');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Mật khẩu').fill(password);
  await page.getByRole('button', { name: 'Đăng nhập' }).click();
}

test.beforeEach(async ({ page }) => {
  await page.goto('/');
  await page.evaluate(() => localStorage.clear());
});

test('TC-05a: chưa đăng nhập vào /tai-khoan thì chuyển sang đăng nhập kèm next', async ({ page }) => {
  await page.goto('/tai-khoan');
  await expect(page).toHaveURL(/\/dang-nhap\?next=%2Ftai-khoan$/);
});

test('TC-05b: khách đăng nhập vào /tai-khoan, vào /quan-tri bị chặn', async ({ page }) => {
  await login(page, 'khach@etax.vn', 'Khach@123');
  await expect(page).toHaveURL(/\/tai-khoan$/);
  await page.goto('/quan-tri');
  await expect(page.getByText('Không có quyền truy cập')).toBeVisible();
});

test('login-admin: admin đăng nhập vào /quan-tri', async ({ page }) => {
  await login(page, 'admin@etax.vn', 'Admin@123');
  await expect(page).toHaveURL(/\/quan-tri$/);
  await expect(page.getByRole('menuitem', { name: /Bảng điều khiển/ })).toBeVisible();
});

test('mobile-drawer: 390x844 mở menu thấy Hồ sơ, không tràn ngang', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await login(page, 'khach@etax.vn', 'Khach@123');
  await expect(page).toHaveURL(/\/tai-khoan$/);
  await page.getByLabel('Mở menu').click();
  await expect(page.getByRole('dialog').getByText('Hồ sơ')).toBeVisible();
  const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
  expect(scrollWidth).toBeLessThanOrEqual(390);
});
