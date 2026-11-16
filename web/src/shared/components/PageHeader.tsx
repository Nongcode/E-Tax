import { ArrowLeftOutlined } from '@ant-design/icons';
import { Button, Flex, Typography } from 'antd';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';

interface PageHeaderProps {
  title: ReactNode;
  extra?: ReactNode;
  backTo?: string;
  backLabel?: string;
}

export function PageHeader({ title, extra, backTo, backLabel = 'Quay lại' }: PageHeaderProps) {
  const navigate = useNavigate();
  return (
    <div style={{ marginBottom: 16 }}>
      {backTo && (
        <Button type="link" icon={<ArrowLeftOutlined />} style={{ padding: 0 }} onClick={() => navigate(backTo)}>
          {backLabel}
        </Button>
      )}
      <Flex justify="space-between" align="center" wrap gap={8}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          {title}
        </Typography.Title>
        {extra && <div>{extra}</div>}
      </Flex>
    </div>
  );
}
