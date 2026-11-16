import { useState } from 'react';
import { Alert, Button, Form, Input } from 'antd';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { ApiError } from '../../api/client';
import { useAuth } from '../../auth/context';
import { resolveAfterLogin } from '../../auth/redirect';
import { isValidPhone, isValidTaxCode } from '../../shared/validators';
import AuthCard from './AuthCard';

interface FormValues {
  fullName: string;
  email: string;
  phone: string;
  companyName?: string;
  taxCode?: string;
  password: string;
  confirm: string;
}

export default function RegisterPage() {
  const { user, register } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const next = params.get('next');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (user) return <Navigate to={resolveAfterLogin(user, next)} replace />;

  const onFinish = async (v: FormValues) => {
    setError(null);
    setLoading(true);
    try {
      const u = await register({
        fullName: v.fullName.trim(),
        email: v.email,
        phone: v.phone,
        password: v.password,
        companyName: v.companyName?.trim() || undefined,
        taxCode: v.taxCode?.trim() || undefined,
      });
      navigate(resolveAfterLogin(u, next), { replace: true });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Có lỗi xảy ra');
      setLoading(false);
    }
  };

  return (
    <AuthCard title="Đăng ký tài khoản">
      {error && <Alert type="error" message={error} showIcon style={{ marginBottom: 16 }} />}
      <Form layout="vertical" onFinish={onFinish} requiredMark={false}>
        <Form.Item
          name="fullName"
          label="Họ tên"
          rules={[{ required: true, whitespace: true, message: 'Vui lòng nhập họ tên' }]}
        >
          <Input autoComplete="name" />
        </Form.Item>
        <Form.Item
          name="email"
          label="Email"
          rules={[
            { required: true, message: 'Email không hợp lệ' },
            { type: 'email', message: 'Email không hợp lệ' },
          ]}
        >
          <Input autoComplete="email" />
        </Form.Item>
        <Form.Item
          name="phone"
          label="Số điện thoại"
          rules={[
            {
              validator: (_, value?: string) =>
                value && isValidPhone(value)
                  ? Promise.resolve()
                  : Promise.reject(new Error('Số điện thoại gồm 10 số, bắt đầu bằng 0')),
            },
          ]}
        >
          <Input autoComplete="tel" />
        </Form.Item>
        <Form.Item name="companyName" label="Công ty">
          <Input autoComplete="organization" />
        </Form.Item>
        <Form.Item
          name="taxCode"
          label="Mã số thuế"
          rules={[
            {
              validator: (_, value?: string) =>
                !value || isValidTaxCode(value)
                  ? Promise.resolve()
                  : Promise.reject(new Error('Mã số thuế gồm 10 số hoặc 10 số-3 số')),
            },
          ]}
        >
          <Input />
        </Form.Item>
        <Form.Item
          name="password"
          label="Mật khẩu"
          rules={[{ required: true, min: 8, message: 'Mật khẩu tối thiểu 8 ký tự' }]}
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Form.Item
          name="confirm"
          label="Nhập lại mật khẩu"
          dependencies={['password']}
          rules={[
            ({ getFieldValue }) => ({
              validator: (_, value?: string) =>
                value && value === getFieldValue('password')
                  ? Promise.resolve()
                  : Promise.reject(new Error('Mật khẩu nhập lại không khớp')),
            }),
          ]}
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Button type="primary" htmlType="submit" block loading={loading}>
          Đăng ký
        </Button>
      </Form>
      <div style={{ textAlign: 'center', marginTop: 16 }}>
        Đã có tài khoản? <Link to="/dang-nhap">Đăng nhập</Link>
      </div>
    </AuthCard>
  );
}
