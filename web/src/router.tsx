import { createBrowserRouter, type RouteObject } from 'react-router-dom';
import { RequireAuth, RequireRole } from './auth/guards';
import ForgotPasswordPage from './features/auth/ForgotPasswordPage';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import ComingSoon from './features/placeholder/ComingSoon';
import AccountLayout from './layouts/AccountLayout';
import AdminLayout from './layouts/AdminLayout';
import PublicLayout from './layouts/PublicLayout';
import RootLayout from './layouts/RootLayout';
import NotFoundPage from './pages/NotFoundPage';

const soon = (title: string) => <ComingSoon title={title} />;

export const routes: RouteObject[] = [
  {
    element: <RootLayout />,
    children: [
      {
        element: <PublicLayout />,
        children: [
          { path: '/', element: soon('Trang chủ') },
          { path: '/bang-gia', element: soon('Bảng giá') },
          { path: '/tai-ung-dung', element: soon('Tải ứng dụng') },
        ],
      },
      { path: '/dang-nhap', element: <LoginPage /> },
      { path: '/dang-ky', element: <RegisterPage /> },
      { path: '/quen-mat-khau', element: <ForgotPasswordPage /> },
      {
        element: <RequireAuth />,
        children: [
          {
            element: <RequireRole role="customer" />,
            children: [
              {
                element: <AccountLayout />,
                children: [
                  { path: '/tai-khoan', element: soon('Tổng quan') },
                  { path: '/tai-khoan/mua-goi', element: soon('Mua gói') },
                  { path: '/tai-khoan/don-hang', element: soon('Đơn hàng') },
                  { path: '/tai-khoan/don-hang/:id', element: soon('Thanh toán') },
                  { path: '/tai-khoan/ban-quyen', element: soon('Bản quyền') },
                  { path: '/tai-khoan/ban-quyen/:id', element: soon('Chi tiết bản quyền') },
                  { path: '/tai-khoan/ho-so', element: soon('Hồ sơ') },
                ],
              },
            ],
          },
          {
            element: <RequireRole role="admin" />,
            children: [
              {
                element: <AdminLayout />,
                children: [
                  { path: '/quan-tri', element: soon('Bảng điều khiển') },
                  { path: '/quan-tri/don-hang', element: soon('Đơn hàng') },
                  { path: '/quan-tri/khach-hang', element: soon('Khách hàng') },
                  { path: '/quan-tri/khach-hang/:id', element: soon('Chi tiết khách hàng') },
                  { path: '/quan-tri/goi', element: soon('Gói & giá') },
                  { path: '/quan-tri/ban-quyen', element: soon('Bản quyền') },
                  { path: '/quan-tri/ban-quyen/:id', element: soon('Chi tiết bản quyền') },
                  { path: '/quan-tri/cai-dat', element: soon('Cài đặt') },
                ],
              },
            ],
          },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
];

export const router = createBrowserRouter(routes);
