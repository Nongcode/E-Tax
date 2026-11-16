import { Button, Result } from 'antd';
import { Link } from 'react-router-dom';

export default function ForbiddenPage({ subTitle = 'Trang này chỉ dành cho quản trị viên.' }: { subTitle?: string }) {
  return (
    <Result
      status="403"
      title="Không có quyền truy cập"
      subTitle={subTitle}
      extra={
        <Link to="/">
          <Button type="primary">Về trang chủ</Button>
        </Link>
      }
    />
  );
}
