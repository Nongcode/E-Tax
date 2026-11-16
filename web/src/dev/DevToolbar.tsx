import { useNavigate } from 'react-router-dom';
import { FloatButton } from 'antd';
import { CrownOutlined, DatabaseOutlined, ReloadOutlined, UserOutlined } from '@ant-design/icons';
import { useAuth } from '../auth/context';
import { resolveAfterLogin } from '../auth/redirect';
import { clearSession } from '../auth/session';

export default function DevToolbar() {
  const { login } = useAuth();
  const navigate = useNavigate();
  if (import.meta.env.VITE_USE_MOCK !== 'true') return null;

  const loginAs = async (email: string, password: string) => {
    const user = await login({ email, password });
    navigate(resolveAfterLogin(user, null));
  };

  return (
    <FloatButton.Group
      trigger="click"
      icon={<DatabaseOutlined />}
      tooltip="Dữ liệu mẫu"
      style={{ insetInlineEnd: 16, insetBlockEnd: 16 }}
    >
      <FloatButton
        icon={<ReloadOutlined />}
        tooltip="Đặt lại dữ liệu"
        onClick={async () => {
          (await import('../mocks/browser')).mockDb.reset();
          clearSession();
          location.assign('/');
        }}
      />
      <FloatButton
        icon={<UserOutlined />}
        tooltip="Đăng nhập khách"
        onClick={() => void loginAs('khach@etax.vn', 'Khach@123')}
      />
      <FloatButton
        icon={<CrownOutlined />}
        tooltip="Đăng nhập admin"
        onClick={() => void loginAs('admin@etax.vn', 'Admin@123')}
      />
    </FloatButton.Group>
  );
}
