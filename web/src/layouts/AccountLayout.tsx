import {
  AppstoreOutlined,
  KeyOutlined,
  ShoppingCartOutlined,
  UnorderedListOutlined,
  UserOutlined,
} from '@ant-design/icons';
import SideNavLayout, { type SideNavItem } from './SideNavLayout';

const items: SideNavItem[] = [
  { key: '/tai-khoan', label: 'Tổng quan', icon: <AppstoreOutlined /> },
  { key: '/tai-khoan/mua-goi', label: 'Mua gói', icon: <ShoppingCartOutlined /> },
  { key: '/tai-khoan/don-hang', label: 'Đơn hàng', icon: <UnorderedListOutlined /> },
  { key: '/tai-khoan/ban-quyen', label: 'Bản quyền', icon: <KeyOutlined /> },
  { key: '/tai-khoan/ho-so', label: 'Hồ sơ', icon: <UserOutlined /> },
];

export default function AccountLayout() {
  return <SideNavLayout items={items} theme="light" title="E-Tax" />;
}
