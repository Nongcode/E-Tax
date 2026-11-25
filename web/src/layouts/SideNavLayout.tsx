import { useState, type ReactNode } from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { Avatar, Badge, Button, Drawer, Grid, Layout, Menu, Space, theme as antdTheme, Typography } from 'antd';
import { MenuOutlined } from '@ant-design/icons';
import { useAuth } from '../auth/context';
import { ErrorBoundary } from '../shared/ErrorBoundary';

export interface SideNavItem {
  key: string;
  label: string;
  icon: ReactNode;
  badge?: number;
}

interface Props {
  items: SideNavItem[];
  theme: 'light' | 'dark';
  title: string;
}

/** Mục có key dài nhất là tiền tố của pathname. */
function selectedKey(items: SideNavItem[], pathname: string): string[] {
  const hit = items
    .filter((i) => pathname === i.key || pathname.startsWith(i.key + '/'))
    .sort((a, b) => b.key.length - a.key.length)[0];
  return hit ? [hit.key] : [];
}

export default function SideNavLayout({ items, theme, title }: Props) {
  const screens = Grid.useBreakpoint();
  const { pathname } = useLocation();
  const { user, logout } = useAuth();
  const { token } = antdTheme.useToken();
  const wide = !!screens.lg;
  const dark = theme === 'dark';
  // Ghi route lúc mở: đổi route thì tự đóng; lên màn rộng thì đóng hẳn để khi thu nhỏ lại không tự mở.
  const [openAt, setOpenAt] = useState<string | null>(null);
  if (wide && openAt !== null) setOpenAt(null);
  const open = openAt === pathname;
  const setOpen = (v: boolean) => setOpenAt(v ? pathname : null);

  const menu = (
    <Menu
      mode="inline"
      theme={theme}
      selectedKeys={selectedKey(items, pathname)}
      onClick={() => setOpen(false)}
      style={{ borderInlineEnd: 0 }}
      items={items.map((i) => ({
        key: i.key,
        icon: i.icon,
        label: (
          <Link to={i.key}>
            <Space>
              {i.label}
              {i.badge ? <Badge count={i.badge} size="small" /> : null}
            </Space>
          </Link>
        ),
      }))}
    />
  );

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {wide && (
        <Layout.Sider
          width={220}
          theme={theme}
          style={{ position: 'fixed', insetBlock: 0, insetInlineStart: 0, overflow: 'auto' }}
        >
          <div
            style={{
              height: 64,
              display: 'flex',
              alignItems: 'center',
              padding: '0 24px',
              fontWeight: 700,
              fontSize: 18,
              color: dark ? '#fff' : token.colorPrimary,
            }}
          >
            {title}
          </div>
          {menu}
        </Layout.Sider>
      )}
      {!wide && (
        <Drawer
          open={open && !wide}
          onClose={() => setOpen(false)}
          placement="left"
          width={240}
          title={title}
          styles={{ body: { padding: 0, background: dark ? '#001529' : undefined } }}
        >
          {menu}
        </Drawer>
      )}
      <Layout style={{ marginInlineStart: wide ? 220 : 0, minWidth: 0 }}>
        <Layout.Header
          style={{
            background: '#fff',
            padding: '0 16px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 8,
            borderBottom: '1px solid #f0f0f0',
          }}
        >
          <div>
            {!wide && (
              <Button type="text" aria-label="Mở menu" icon={<MenuOutlined />} onClick={() => setOpen(true)} />
            )}
          </div>
          <Space size="middle">
            <Link to="/">← Trang chủ</Link>
            <Avatar style={{ background: token.colorPrimary }}>{user?.fullName.charAt(0).toUpperCase()}</Avatar>
            {screens.sm && <Typography.Text>{user?.fullName}</Typography.Text>}
            <Button onClick={logout}>Đăng xuất</Button>
          </Space>
        </Layout.Header>
        <Layout.Content style={{ padding: 16 }}>
          <ErrorBoundary key={pathname}>
            <Outlet />
          </ErrorBoundary>
        </Layout.Content>
      </Layout>
    </Layout>
  );
}
