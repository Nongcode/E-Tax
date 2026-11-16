import { Button, Result } from 'antd';
import { Link } from 'react-router-dom';

export default function ForbiddenPage() {
  return (
    <Result
      status="403"
      title="Không có quyền truy cập"
      subTitle="Trang này chỉ dành cho quản trị viên."
      extra={
        <Link to="/">
          <Button type="primary">Về trang chủ</Button>
        </Link>
      }
    />
  );
}
