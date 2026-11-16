import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router-dom';
import { AppProviders } from '../../App';
import { AuthProvider } from '../../auth/AuthProvider';
import { RequireAuth, RequireRole } from '../../auth/guards';
import { setupMockServer } from '../../test/server';
import LoginPage from './LoginPage';
import RegisterPage from './RegisterPage';
import ForgotPasswordPage from './ForgotPasswordPage';

setupMockServer();

function RootLayout() {
  return (
    <AuthProvider>
      <Outlet />
    </AuthProvider>
  );
}

function renderApp(initial: string) {
  const router = createMemoryRouter(
    [
      {
        element: <RootLayout />,
        children: [
          { path: '/dang-nhap', element: <LoginPage /> },
          { path: '/dang-ky', element: <RegisterPage /> },
          { path: '/quen-mat-khau', element: <ForgotPasswordPage /> },
          { element: <RequireAuth />, children: [{ path: '/tai-khoan', element: <div>Trang tài khoản</div> }] },
          {
            element: <RequireRole role="admin" />,
            children: [{ path: '/quan-tri', element: <div>Trang quản trị</div> }],
          },
        ],
      },
    ],
    { initialEntries: [initial] },
  );
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  );
  return router;
}

type U = ReturnType<typeof userEvent.setup>;

async function login(user: U, email: string, password: string) {
  await user.type(screen.getByLabelText('Email'), email);
  await user.type(screen.getByLabelText('Mật khẩu'), password);
  await user.click(screen.getByRole('button', { name: 'Đăng nhập' }));
}

describe('trang đăng nhập', () => {
  it('đăng nhập đúng thì vào trang tài khoản', async () => {
    const user = userEvent.setup({ delay: null });
    renderApp('/dang-nhap');
    await login(user, 'khach@etax.vn', 'Khach@123');
    expect(await screen.findByText('Trang tài khoản')).toBeInTheDocument();
  });

  it('sai mật khẩu thì báo lỗi và ở lại', async () => {
    const user = userEvent.setup({ delay: null });
    const router = renderApp('/dang-nhap');
    await login(user, 'khach@etax.vn', 'sai-mat-khau');
    expect(await screen.findByText('Email hoặc mật khẩu không đúng')).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/dang-nhap');
  });

  it('chưa đăng nhập vào /tai-khoan thì chuyển sang đăng nhập rồi quay lại', async () => {
    const user = userEvent.setup({ delay: null });
    const router = renderApp('/tai-khoan');
    expect(await screen.findByRole('button', { name: 'Đăng nhập' })).toBeInTheDocument();
    expect(router.state.location.pathname + router.state.location.search).toBe('/dang-nhap?next=%2Ftai-khoan');
    await login(user, 'khach@etax.vn', 'Khach@123');
    expect(await screen.findByText('Trang tài khoản')).toBeInTheDocument();
  });

  it('next độc hại thì về trang mặc định', async () => {
    const user = userEvent.setup({ delay: null });
    renderApp('/dang-nhap?next=%2F%2Fevil.com');
    await login(user, 'khach@etax.vn', 'Khach@123');
    expect(await screen.findByText('Trang tài khoản')).toBeInTheDocument();
  });

  it('khách vào /quan-tri thì thấy không có quyền', async () => {
    const user = userEvent.setup({ delay: null });
    renderApp('/quan-tri');
    await login(user, 'khach@etax.vn', 'Khach@123');
    expect(await screen.findByText('Không có quyền truy cập')).toBeInTheDocument();
  });
});

describe('trang đăng ký', () => {
  async function fill(user: U, o: { phone: string; confirm: string }) {
    await user.type(screen.getByLabelText('Họ tên'), 'Nguyễn Văn A');
    await user.type(screen.getByLabelText('Email'), 'moi@etax.vn');
    await user.type(screen.getByLabelText('Số điện thoại'), o.phone);
    await user.type(screen.getByLabelText('Mật khẩu'), 'Matkhau@123');
    await user.type(screen.getByLabelText('Nhập lại mật khẩu'), o.confirm);
    await user.click(screen.getByRole('button', { name: 'Đăng ký' }));
  }

  it('SĐT sai định dạng', async () => {
    const user = userEvent.setup({ delay: null });
    renderApp('/dang-ky');
    await fill(user, { phone: '0912', confirm: 'Matkhau@123' });
    expect(await screen.findByText('Số điện thoại gồm 10 số, bắt đầu bằng 0')).toBeInTheDocument();
  });

  it('mật khẩu nhập lại không khớp', async () => {
    const user = userEvent.setup({ delay: null });
    renderApp('/dang-ky');
    await fill(user, { phone: '0912345678', confirm: 'Khac@12345' });
    expect(await screen.findByText('Mật khẩu nhập lại không khớp')).toBeInTheDocument();
  });

  it('đăng ký hợp lệ thì vào trang tài khoản', async () => {
    const user = userEvent.setup({ delay: null });
    renderApp('/dang-ky');
    await fill(user, { phone: '0912345678', confirm: 'Matkhau@123' });
    expect(await screen.findByText('Trang tài khoản')).toBeInTheDocument();
  });
});

describe('quên mật khẩu', () => {
  it('gửi xong hiện thông báo thành công', async () => {
    const user = userEvent.setup({ delay: null });
    renderApp('/quen-mat-khau');
    await user.type(screen.getByLabelText('Email'), 'khach@etax.vn');
    await user.click(screen.getByRole('button', { name: 'Gửi hướng dẫn' }));
    expect(
      await screen.findByText('Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu'),
    ).toBeInTheDocument();
  });
});
