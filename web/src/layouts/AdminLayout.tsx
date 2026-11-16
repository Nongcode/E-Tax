import {
  DashboardOutlined,
  KeyOutlined,
  SettingOutlined,
  TagsOutlined,
  TeamOutlined,
  UnorderedListOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { getStats } from '../api/admin';
import SideNavLayout, { type SideNavItem } from './SideNavLayout';

export default function AdminLayout() {
  const { data } = useQuery({ queryKey: ['admin', 'stats'], queryFn: getStats });
  const items: SideNavItem[] = [
    { key: '/quan-tri', label: 'Bảng điều khiển', icon: <DashboardOutlined /> },
    { key: '/quan-tri/don-hang', label: 'Đơn hàng', icon: <UnorderedListOutlined />, badge: data?.pendingOrders },
    { key: '/quan-tri/khach-hang', label: 'Khách hàng', icon: <TeamOutlined /> },
    { key: '/quan-tri/ban-quyen', label: 'Bản quyền', icon: <KeyOutlined /> },
    { key: '/quan-tri/goi', label: 'Gói & giá', icon: <TagsOutlined /> },
    { key: '/quan-tri/cai-dat', label: 'Cài đặt', icon: <SettingOutlined /> },
  ];
  return <SideNavLayout items={items} theme="dark" title="E-Tax Admin" />;
}
