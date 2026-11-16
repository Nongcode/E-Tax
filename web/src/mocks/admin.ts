import dayjs from 'dayjs';
import type { AdminStats, AdminUserDetail, User, UserStatus, UserSummary } from '../api/types';
import { MockError } from './errors';
import { toPublicLicense } from './licenses';
import type { DbState } from './mockDb';
import { effectiveOrderStatus, toPublicUser } from './views';

const notFound = () => new MockError(404, 'NOT_FOUND', 'Không tìm thấy dữ liệu');

export function adminListUsers(s: DbState, q?: string): UserSummary[] {
  const needle = q?.trim().toLowerCase();
  return s.users
    .map((u, i) => ({ u, i }))
    .filter(({ u }) => u.role === 'customer')
    .filter(({ u }) => {
      if (!needle) return true;
      return [u.fullName, u.email, u.phone, u.taxCode, u.companyName].some((v) => (v ?? '').toLowerCase().includes(needle));
    })
    .sort((a, b) => b.u.createdAt.localeCompare(a.u.createdAt) || b.i - a.i)
    .map(({ u }) => ({
      ...toPublicUser(u),
      orderCount: s.orders.filter((o) => o.userId === u.id).length,
      licenseCount: s.licenses.filter((l) => l.userId === u.id).length,
    }));
}

export function adminGetUser(s: DbState, now: Date, id: string): AdminUserDetail {
  const u = s.users.find((x) => x.id === id && x.role === 'customer');
  if (!u) throw notFound();
  return {
    ...toPublicUser(u),
    orders: s.orders
      .map((o, i) => ({ o, i }))
      .filter(({ o }) => o.userId === u.id)
      .sort((a, b) => b.o.createdAt.localeCompare(a.o.createdAt) || b.i - a.i)
      .map(({ o }) => ({ ...o, status: effectiveOrderStatus(o, now) })),
    licenses: s.licenses.filter((l) => l.userId === u.id).map((l) => toPublicLicense(s, l, now)),
  };
}

export function adminSetUserStatus(s: DbState, adminId: string, id: string, status: UserStatus): User {
  // R19 kiểm tra trước: admin tự khoá mình phải ra CANNOT_LOCK_SELF.
  if (id === adminId && status === 'locked') {
    throw new MockError(409, 'CANNOT_LOCK_SELF', 'Không thể khoá tài khoản đang đăng nhập');
  }
  const u = s.users.find((x) => x.id === id);
  if (!u) throw notFound();
  u.status = status;
  return toPublicUser(u);
}

export function stats(s: DbState, now: Date): AdminStats {
  const start = dayjs(now).startOf('month');
  const end = start.add(1, 'month');
  const inMonth = (iso: string | null) => {
    if (!iso) return false;
    const t = new Date(iso).getTime();
    return t >= start.valueOf() && t < end.valueOf();
  };
  return {
    revenueThisMonth: s.orders.filter((o) => o.status === 'paid' && inMonth(o.paidAt)).reduce((n, o) => n + o.amount, 0),
    pendingOrders: s.orders.filter((o) => effectiveOrderStatus(o, now) === 'pending').length,
    activeLicenses: s.licenses.filter((l) => toPublicLicense(s, l, now).status === 'active').length,
    newCustomersThisMonth: s.users.filter((u) => u.role === 'customer' && inMonth(u.createdAt)).length,
  };
}
