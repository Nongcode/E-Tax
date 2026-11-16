export type Role = 'customer' | 'admin';
export type UserStatus = 'active' | 'locked';

export interface User {
  id: string;
  email: string;
  fullName: string;
  phone: string;
  companyName: string | null;
  taxCode: string | null;
  role: Role;
  status: UserStatus;
  createdAt: string;
}

export interface Plan {
  id: string;
  name: string;
  description: string;
  durationMonths: number | null;
  maxMachines: number;
  maxMachineChanges: number;
  price: number;
  featured: boolean;
  active: boolean;
  sortOrder: number;
}
export type PlanInput = Omit<Plan, 'id'>;

export type OrderStatus = 'pending' | 'paid' | 'cancelled' | 'expired';

export interface PlanSnapshot {
  name: string;
  durationMonths: number | null;
  maxMachines: number;
  maxMachineChanges: number;
  price: number;
}

export interface Order {
  id: string;
  code: string;
  userId: string;
  planId: string;
  planSnapshot: PlanSnapshot;
  amount: number;
  status: OrderStatus;
  createdAt: string;
  expiresAt: string;
  paidAt: string | null;
  confirmedBy: string | null;
  note: string | null;
}

export interface PaymentInfo {
  qrUrl: string;
  bankName: string;
  accountNo: string;
  accountName: string;
  amount: number;
  transferContent: string;
}

export type OrderDetail = Order & { payment: PaymentInfo; licenseId: string | null };

export type LicenseStatus = 'active' | 'expired' | 'revoked';

export interface License {
  id: string;
  orderId: string;
  userId: string;
  planName: string;
  maxMachines: number;
  maxMachineChanges: number;
  machineChangesUsed: number;
  startsAt: string;
  expiresAt: string | null;
  status: LicenseStatus;
  activeMachines: number;
}

export interface Activation {
  id: string;
  licenseId: string;
  machineId: string;
  machineName: string;
  key: string;
  createdAt: string;
  revokedAt: string | null;
}

/** Khách: chỉ máy chưa gỡ. */
export type LicenseDetail = License & { activations: Activation[] };
/** Admin: cả máy đã gỡ. */
export type AdminLicenseDetail = License & { activations: Activation[]; user: User };
export type AdminOrder = Order & { userEmail: string; userFullName: string; userTaxCode: string | null };
export type AdminLicense = License & { userEmail: string; userFullName: string };
export type UserSummary = User & { orderCount: number; licenseCount: number };
export type AdminUserDetail = User & { orders: Order[]; licenses: License[] };

export interface AdminStats {
  revenueThisMonth: number;
  pendingOrders: number;
  activeLicenses: number;
  newCustomersThisMonth: number;
}

export interface BankSettings {
  bankBin: string;
  bankName: string;
  accountNo: string;
  accountName: string;
}

export interface AppRelease {
  version: string;
  downloadUrl: string;
  releasedAt: string;
  notes: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface RegisterInput {
  email: string;
  password: string;
  fullName: string;
  phone: string;
  companyName?: string;
  taxCode?: string;
}

export interface LoginInput {
  email: string;
  password: string;
}

export interface ProfileInput {
  fullName: string;
  phone: string;
  companyName?: string;
  taxCode?: string;
}

export interface ChangePasswordInput {
  currentPassword: string;
  newPassword: string;
}

export interface ApiErrorBody {
  code: string;
  message: string;
}
