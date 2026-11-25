import { useState } from 'react';
import { Alert, Button, Form, Input } from 'antd';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { ApiError } from '../../api/client';
import type { LoginInput } from '../../api/types';
import { useAuth } from '../../auth/context';
import { resolveAfterLogin } from '../../auth/redirect';
import AuthCard from './AuthCard';

export default function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const next = params.get('next');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (user) return <Navigate to={resolveAfterLogin(user, next)} replace />;

  const onFinish = async (v: LoginInput) => {
    setError(null);
    setLoading(true);
    try {
      const u = await login(v);
      navigate(resolveAfterLogin(u, next), { replace: true });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Có lỗi xảy ra');
      setLoading(false);
    }
  };

  return (
    <AuthCard title="Đăng nhập">
      {error && <Alert type="error" message={error} showIcon style={{ marginBottom: 16 }} />}
      <Form layout="vertical" onFinish={onFinish} requiredMark={false}>
        <Form.Item
          name="email"
          label="Email"
          rules={[
            { required: true, message: 'Vui lòng nhập email' },
            { type: 'email', message: 'Email không hợp lệ' },
          ]}
        >
          <Input autoComplete="email" />
        </Form.Item>
        <Form.Item name="password" label="Mật khẩu" rules={[{ required: true, message: 'Vui lòng nhập mật khẩu' }]}>
          <Input.Password autoComplete="current-password" />
        </Form.Item>
        <Button type="primary" htmlType="submit" block loading={loading}>
          Đăng nhập
        </Button>
      </Form>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 16 }}>
        <Link to="/quen-mat-khau">Quên mật khẩu?</Link>
        <Link to="/dang-ky">Đăng ký</Link>
      </div>
    </AuthCard>
  );
}
