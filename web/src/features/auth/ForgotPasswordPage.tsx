import { useState } from 'react';
import { Alert, Button, Form, Input, Result } from 'antd';
import { Link } from 'react-router-dom';
import { forgotPassword } from '../../api/auth';
import { ApiError } from '../../api/client';
import AuthCard from './AuthCard';

export default function ForgotPasswordPage() {
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const onFinish = async (v: { email: string }) => {
    setError(null);
    setLoading(true);
    try {
      await forgotPassword(v.email);
      setSent(true);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Có lỗi xảy ra');
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthCard title="Quên mật khẩu">
      {sent ? (
        <Result
          status="success"
          title="Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu"
          extra={<Link to="/dang-nhap">Quay lại đăng nhập</Link>}
        />
      ) : (
        <>
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
            <Button type="primary" htmlType="submit" block loading={loading}>
              Gửi hướng dẫn
            </Button>
          </Form>
          <div style={{ textAlign: 'center', marginTop: 16 }}>
            <Link to="/dang-nhap">Quay lại đăng nhập</Link>
          </div>
        </>
      )}
    </AuthCard>
  );
}
