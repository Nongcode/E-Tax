import { apiFetch } from './client';
import type {
  AdminLicense,
  AdminLicenseDetail,
  AdminOrder,
  AdminStats,
  AdminUserDetail,
  AppRelease,
  BankSettings,
  License,
  LicenseStatus,
  Order,
  OrderStatus,
  Plan,
  PlanInput,
  User,
  UserStatus,
  UserSummary,
} from './types';

/** Tạo query string, bỏ tham số rỗng. */
function qs(params: Record<string, string | undefined>): string {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) if (v) sp.set(k, v);
  const s = sp.toString();
  return s ? `?${s}` : '';
}

export const getStats = () => apiFetch<AdminStats>('/admin/stats');

export const getUsers = (q?: string) => apiFetch<UserSummary[]>(`/admin/users${qs({ q })}`);
export const getUser = (id: string) => apiFetch<AdminUserDetail>(`/admin/users/${encodeURIComponent(id)}`);
export const setUserStatus = (id: string, status: UserStatus) =>
  apiFetch<User>(`/admin/users/${encodeURIComponent(id)}`, { method: 'PATCH', body: { status } });

export const getAdminPlans = () => apiFetch<Plan[]>('/admin/plans');
export const createPlan = (i: PlanInput) => apiFetch<Plan>('/admin/plans', { method: 'POST', body: i });
export const updatePlan = (id: string, i: PlanInput) =>
  apiFetch<Plan>(`/admin/plans/${encodeURIComponent(id)}`, { method: 'PUT', body: i });

export const getAdminOrders = (f: { status?: OrderStatus; q?: string } = {}) =>
  apiFetch<AdminOrder[]>(`/admin/orders${qs(f)}`);
export const confirmOrder = (id: string, note?: string) =>
  apiFetch<{ order: Order; license: License }>(`/admin/orders/${encodeURIComponent(id)}/confirm`, { method: 'POST', body: { note } });
export const cancelAdminOrder = (id: string, note?: string) =>
  apiFetch<Order>(`/admin/orders/${encodeURIComponent(id)}/cancel`, { method: 'POST', body: { note } });

export const getAdminLicenses = (f: { status?: LicenseStatus; q?: string } = {}) =>
  apiFetch<AdminLicense[]>(`/admin/licenses${qs(f)}`);
export const getAdminLicense = (id: string) => apiFetch<AdminLicenseDetail>(`/admin/licenses/${encodeURIComponent(id)}`);
export const revokeLicense = (id: string) => apiFetch<License>(`/admin/licenses/${encodeURIComponent(id)}/revoke`, { method: 'POST' });
export const extendLicense = (id: string, months: number) =>
  apiFetch<License>(`/admin/licenses/${encodeURIComponent(id)}/extend`, { method: 'POST', body: { months } });

export const getBankSettings = () => apiFetch<BankSettings>('/admin/settings/bank');
export const saveBankSettings = (b: BankSettings) =>
  apiFetch<BankSettings>('/admin/settings/bank', { method: 'PUT', body: b });
export const getAppSettings = () => apiFetch<AppRelease>('/admin/settings/app');
export const saveAppSettings = (a: AppRelease) =>
  apiFetch<AppRelease>('/admin/settings/app', { method: 'PUT', body: a });
