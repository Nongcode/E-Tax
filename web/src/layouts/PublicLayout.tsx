import { useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Avatar, Button, Drawer, Dropdown, Grid, Layout, Menu, Space } from 'antd';
import { MenuOutlined } from '@ant-design/icons';
import { useAuth } from '../auth/context';
import { ErrorBoundary } from '../shared/ErrorBoundary';

const navItems = [
  { key: '/', label: <Link to="/">Trang chủ</Link> },
  { key: '/bang-gia', label: <Link to="/bang-gia">Bảng giá</Link> },
  { key: '/tai-ung-dung', label: <Link to="/tai-ung-dung">Tải ứng dụng</Link> },
];

export default function PublicLayout() {
  const screens = Grid.useBreakpoint();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [open, setOpen] = useState(false);
  const mobile = !screens.md;

  const right = user ? (
    <Dropdown
      menu={{
        items: [
          user.role === 'admin'
            ? { key: 'admin', label: 'Quản trị', onClick: () => navigate('/quan-tri') }
            : { key: 'account', label: 'Tài khoản', onClick: () => navigate('/tai-khoan') },
          { key: 'logout', label: 'Đăng xuất', onClick: logout },
        ],
      }}
    >
      <Avatar style={{ background: '#0075D6', cursor: 'pointer' }}>{user.fullName.charAt(0).toUpperCase()}</Avatar>
    </Dropdown>
  ) : (
    <Space>
      <Button onClick={() => navigate('/dang-nhap')}>Đăng nhập</Button>
      <Button type="primary" onClick={() => navigate('/bang-gia')}>
        Mua ngay
      </Button>
    </Space>
  );

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Layout.Header
        style={{
          height: 64,
          background: '#fff',
          padding: '0 16px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 16,
          borderBottom: '1px solid #f0f0f0',
        }}
      >
        <Space size="large">
          {mobile && (
            <Button type="text" aria-label="Mở menu" icon={<MenuOutlined />} onClick={() => setOpen(true)} />
          )}
          <Link to="/" style={{ fontWeight: 700, fontSize: 20, color: '#0075D6' }}>
            E-Tax
          </Link>
          {!mobile && (
            <Menu
              mode="horizontal"
              selectedKeys={[pathname]}
              items={navItems}
              style={{ borderBottom: 0, minWidth: 360 }}
            />
          )}
        </Space>
        {right}
      </Layout.Header>
      <Drawer open={open && mobile} onClose={() => setOpen(false)} placement="left" width={240} title="E-Tax">
        <Menu
          mode="inline"
          selectedKeys={[pathname]}
          items={navItems}
          onClick={() => setOpen(false)}
          style={{ borderInlineEnd: 0 }}
        />
      </Drawer>
      <Layout.Content>
        <ErrorBoundary key={pathname}>
          <Outlet />
        </ErrorBoundary>
      </Layout.Content>
      <Layout.Footer style={{ textAlign: 'center' }}>
        © {new Date().getFullYear()} E-Tax · Hỗ trợ: 0900 000 000
      </Layout.Footer>
    </Layout>
  );
}
