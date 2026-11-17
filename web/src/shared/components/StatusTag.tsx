import { Tag } from 'antd';
import type { LicenseStatus, OrderStatus } from '../../api/types';

const ORDER: Record<OrderStatus, { label: string; color: string }> = {
  pending: { label: 'Chờ thanh toán', color: 'gold' },
  paid: { label: 'Đã thanh toán', color: 'green' },
  cancelled: { label: 'Đã huỷ', color: 'default' },
  expired: { label: 'Quá hạn', color: 'red' },
};

const LICENSE: Record<LicenseStatus, { label: string; color: string }> = {
  active: { label: 'Còn hạn', color: 'green' },
  expired: { label: 'Hết hạn', color: 'red' },
  revoked: { label: 'Đã thu hồi', color: 'default' },
};

type StatusTagProps =
  | { kind: 'order'; status: OrderStatus }
  | { kind: 'license'; status: LicenseStatus };

export function StatusTag(props: StatusTagProps) {
  const info = props.kind === 'order' ? ORDER[props.status] : LICENSE[props.status];
  // Máy chủ có thể thêm trạng thái mới trước khi giao diện kịp cập nhật.
  if (!info) return <Tag>{props.status}</Tag>;
  return <Tag color={info.color}>{info.label}</Tag>;
}
