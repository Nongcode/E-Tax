import { Result } from 'antd';

export default function ForbiddenPage() {
  return <Result status="403" title="Không có quyền truy cập" />;
}
