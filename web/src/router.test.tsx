import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider, type RouteObject } from 'react-router-dom';
import { act } from 'react';
import { RequireAuth, RequireRole } from './auth/guards';
import AccountLayout from './layouts/AccountLayout';
import RootLayout from './layouts/RootLayout';
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

function renderAt(path: string, tree: RouteObject[] = routes, queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })) {
  const router = createMemoryRouter(tree, { initialEntries: [path] });
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

  it('admin vào /tai-khoan bị chặn', async () => {
    loginAs('admin@etax.vn', 'Admin@123');
    renderAt('/tai-khoan');
    expect(await screen.findByText('Không có quyền truy cập')).toBeInTheDocument();
  });

  it('admin vào /tai-khoan thấy nội dung 403 dành cho khách hàng', async () => {
    loginAs('admin@etax.vn', 'Admin@123');
    renderAt('/tai-khoan');
    expect(await screen.findByText('Trang này chỉ dành cho khách hàng.')).toBeInTheDocument();
  });

  it('khách vào /quan-tri thấy nội dung 403 dành cho quản trị viên', async () => {
    loginAs('khach@etax.vn', 'Khach@123');
    renderAt('/quan-tri');
    expect(await screen.findByText('Trang này chỉ dành cho quản trị viên.')).toBeInTheDocument();
  });

  it('lỗi render trong layout giữ menu và tự reset khi đổi route', async () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    function Boom(): never {
      throw new Error('boom');
    }
    const tree: RouteObject[] = [
      {
        element: <RootLayout />,
        children: [
          {
            element: <RequireAuth />,
            children: [
              {
                element: <RequireRole role="customer" />,
                children: [
                  {
                    element: <AccountLayout />,
                    children: [
                      { path: '/tai-khoan/loi', element: <Boom /> },
                      { path: '/tai-khoan/ho-so', element: <div>Nội dung hồ sơ</div> },
                    ],
                  },
                ],
              },
            ],
          },
        ],
      },
    ];
    loginAs('khach@etax.vn', 'Khach@123');
    const router = renderAt('/tai-khoan/loi', tree);
    expect(await screen.findByText('Đã có lỗi xảy ra')).toBeInTheDocument();
    expect(screen.getAllByText('Hồ sơ').length).toBeGreaterThan(0);
    await act(async () => {
      await router.navigate('/tai-khoan/ho-so');
    });
    expect(await screen.findByText('Nội dung hồ sơ')).toBeInTheDocument();
    expect(screen.queryByText('Đã có lỗi xảy ra')).not.toBeInTheDocument();
    spy.mockRestore();
  });

  it('đăng xuất xoá cache truy vấn', async () => {
    loginAs('khach@etax.vn', 'Khach@123');
    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    qc.setQueryData(['x'], 1);
    const router = renderAt('/tai-khoan/ho-so', routes, qc);
    await screen.findByText('Màn hình đang được xây dựng');
    expect(qc.getQueryData(['x'])).toBe(1);
    await act(async () => {
      screen.getByRole('button', { name: 'Đăng xuất' }).click();
    });
    await vi.waitFor(() => expect(router.state.location.pathname).toBe('/'));
    expect(qc.getQueryData(['x'])).toBeUndefined();
  });

  it.each([
    { email: 'admin@etax.vn', password: 'Admin@123', initial: 'Q', item: 'Quản trị' },
    { email: 'khach@etax.vn', password: 'Khach@123', initial: 'N', item: 'Tài khoản' },
  ])('trang công khai khi $email đăng nhập: menu avatar có $item và Đăng xuất', async ({ email, password, initial, item }) => {
    loginAs(email, password);
    renderAt('/bang-gia');
    await userEvent.hover(await screen.findByText(initial, { selector: '.ant-avatar-string' }));
    expect(await screen.findByRole('menuitem', { name: item })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: 'Đăng xuất' })).toBeInTheDocument();
  });
});
