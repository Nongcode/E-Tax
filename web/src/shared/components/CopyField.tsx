import { CopyOutlined } from '@ant-design/icons';
import { App, Button, Space, Typography } from 'antd';
import type { ReactNode } from 'react';

interface CopyFieldProps {
  label?: string;
  value: string;
  display?: ReactNode;
  multiline?: boolean;
}

export function CopyField({ label, value, display, multiline }: CopyFieldProps) {
  const { message } = App.useApp();

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      message.success('Đã sao chép');
    } catch {
      message.error('Không sao chép được, hãy chọn và sao chép thủ công');
    }
  };

  return (
    <div>
      {label && <Typography.Text type="secondary">{label}</Typography.Text>}
      <Space align={multiline ? 'start' : 'center'} wrap>
        {multiline ? (
          <Typography.Paragraph
            style={{ marginBottom: 0, whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}
          >
            {display ?? value}
          </Typography.Paragraph>
        ) : (
          <Typography.Text>{display ?? value}</Typography.Text>
        )}
        <Button size="small" icon={<CopyOutlined />} onClick={copy}>
          Sao chép
        </Button>
      </Space>
    </div>
  );
}
