import { Result } from 'antd';

export default function ComingSoon({ title }: { title: string }) {
  return <Result status="info" title={title} subTitle="Màn hình đang được xây dựng" />;
}
