import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { AppProviders } from './App';
import { routes } from './router';
import { saveSession } from './auth/session';
import { setupMockServer } from './test/server';

const mock = setupMockServer();

const originalMatchMedia = window.matchMedia;

/** Giả lập màn rộng để Sider hiện (breakpoint antd dùng min-width). */
function useWideScreen() {
  window.matchMedia = ((query: string) => ({
    ...originalMatchMedia(query),
    matches: query.includes('min-width'),
  })) as typeof window.matchMedia;
}

beforeEach(() => {
  localStorage.clear();
  useWideScreen();
});
afterEach(() => {
  window.matchMedia = originalMatchMedia;
});

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  );
  return router;
}

function loginAs(email: string, password: string) {
  const res = mock.db().login({ email, password });
  saveSession({ token: res.token, user: res.user });
}

describe('router', () => {
  it('/khong-co hiện 404', async () => {
    renderAt('/khong-co');
    expect(await screen.findByText('Không tìm thấy trang')).toBeInTheDocument();
  });

  it('/bang-gia có header E-Tax và nút Đăng nhập', async () => {
    renderAt('/bang-gia');
    expect(await screen.findByText('E-Tax')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Đăng nhập/ })).toBeInTheDocument();
  });

  it('khách vào /tai-khoan/ban-quyen: menu chọn Bản quyền và ComingSoon', async () => {
    loginAs('khach@etax.vn', 'Khach@123');
    renderAt('/tai-khoan/ban-quyen');
    expect(await screen.findByText('Màn hình đang được xây dựng')).toBeInTheDocument();
    const selected = document.querySelector('.ant-menu-item-selected');
    expect(selected).not.toBeNull();
    expect(selected).toHaveTextContent('Bản quyền');
  });

  it('admin vào /quan-tri: Bảng điều khiển và badge 1 trên Đơn hàng', async () => {
    loginAs('admin@etax.vn', 'Admin@123');
    renderAt('/quan-tri');
    expect(await screen.findByText('Màn hình đang được xây dựng')).toBeInTheDocument();
    const item = (await screen.findAllByRole('menuitem', { name: /Đơn hàng/ }))[0];
    expect(await within(item).findByText('1')).toBeInTheDocument();
    expect(screen.getAllByText('Bảng điều khiển').length).toBeGreaterThan(0);
  });

  it('khách vào /quan-tri/goi bị chặn', async () => {
    loginAs('khach@etax.vn', 'Khach@123');
    renderAt('/quan-tri/goi');
    expect(await screen.findByText('Không có quyền truy cập')).toBeInTheDocument();
  });
});
