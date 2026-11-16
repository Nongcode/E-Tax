import dayjs from 'dayjs';
import type {
  Activation,
  AdminOrder,
  AppRelease,
  AuthResponse,
  BankSettings,
  ChangePasswordInput,
  License,
  LicenseStatus,
  LoginInput,
  Order,
  OrderDetail,
  OrderStatus,
  Plan,
  PlanInput,
  ProfileInput,
  RegisterInput,
  User,
} from '../api/types';
import { createSeed } from './seed';

export const STORAGE_KEY = 'etax-mockdb-v1';

export class MockError extends Error {
  status: number;
  code: string;
  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = 'MockError';
    this.status = status;
    this.code = code;
  }
}

/** Mật khẩu lưu thô, chỉ dùng cho giả lập. */
export interface DbState {
  version: 1;
  users: (User & { password: string })[];
  plans: Plan[];
  orders: Order[];
  licenses: (Omit<License, 'status' | 'activeMachines'> & { status: LicenseStatus })[];
  activations: Activation[];
  bank: BankSettings;
  app: AppRelease;
}

export interface MockDb {
  reset(): void;
  register(i: RegisterInput): AuthResponse;
  login(i: LoginInput): AuthResponse;
  userFromToken(token: string | null): User;
  updateProfile(userId: string, i: ProfileInput): User;
  changePassword(userId: string, i: ChangePasswordInput): void;
  listPlans(includeInactive?: boolean): Plan[];
  createPlan(i: PlanInput): Plan;
  updatePlan(id: string, i: PlanInput): Plan;
  getBank(): BankSettings;
  setBank(b: BankSettings): BankSettings;
  getApp(): AppRelease;
  setApp(a: AppRelease): AppRelease;
  createOrder(userId: string, planId: string): OrderDetail;
  listOrders(userId: string): Order[];
  getOrder(userId: string, id: string): OrderDetail;
  cancelOrder(userId: string, id: string): Order;
  adminListOrders(f: { status?: OrderStatus; q?: string }): AdminOrder[];
  adminConfirmOrder(adminId: string, id: string, note?: string): { order: Order; license: License };
  adminCancelOrder(id: string, note?: string): Order;
}

type StoredLicense = DbState['licenses'][number];

export function buildQrUrl(bank: BankSettings, amount: number, content: string): string {
  return `https://img.vietqr.io/image/${bank.bankBin}-${bank.accountNo}-compact2.png?amount=${amount}&addInfo=${content}&accountName=${encodeURIComponent(bank.accountName)}`;
}

/** R5: đơn pending quá hạn được coi là expired khi đọc, không ghi lại. */
export function effectiveOrderStatus(o: Order, now: Date): OrderStatus {
  return o.status === 'pending' && new Date(o.expiresAt).getTime() < now.getTime() ? 'expired' : o.status;
}

/** Dựng License công khai: R7 (active quá hạn thì expired) và activeMachines tính khi đọc. */
export function toPublicLicense(s: DbState, l: StoredLicense, now: Date): License {
  const expired = l.status === 'active' && l.expiresAt !== null && new Date(l.expiresAt).getTime() < now.getTime();
  return {
    ...l,
    status: expired ? 'expired' : l.status,
    activeMachines: s.activations.filter((a) => a.licenseId === l.id && a.revokedAt === null).length,
  };
}

const normEmail = (e: string) => e.trim().toLowerCase();
const unauthorized = () => new MockError(401, 'UNAUTHORIZED', 'Vui lòng đăng nhập để tiếp tục');
const notFound = () => new MockError(404, 'NOT_FOUND', 'Không tìm thấy dữ liệu');
const notPending = () => new MockError(409, 'ORDER_NOT_PENDING', 'Đơn không còn ở trạng thái chờ thanh toán');

function toPublicUser(u: User & { password: string }): User {
  const pub: User & { password?: string } = { ...u };
  delete pub.password;
  return pub;
}

export function createMockDb(storage: Storage, now: () => Date = () => new Date()): MockDb {
  function save(s: DbState): void {
    storage.setItem(STORAGE_KEY, JSON.stringify(s));
  }

  /** Đọc trạng thái; JSON hỏng hoặc khác phiên bản thì nạp lại seed. */
  function load(): DbState {
    const raw = storage.getItem(STORAGE_KEY);
    if (raw) {
      try {
        const parsed = JSON.parse(raw) as Partial<DbState> | null;
        if (parsed && parsed.version === 1 && Array.isArray(parsed.users) && Array.isArray(parsed.plans) &&
          Array.isArray(parsed.orders) &&
          Array.isArray(parsed.licenses) &&
          Array.isArray(parsed.activations) &&
          !!parsed.bank &&
          typeof parsed.bank === 'object' &&
          !!parsed.app &&
          typeof parsed.app === 'object'
        ) {
          return parsed as DbState;
        }
      } catch {
        // JSON hỏng: rơi xuống nạp seed.
      }
    }
    const seed = createSeed(now());
    save(seed);
    return seed;
  }

  /** Tìm user theo id, ném 401 nếu không có hoặc bị khoá (R16, R20). */
  function requireUser(s: DbState, id: string) {
    const u = s.users.find((x) => x.id === id);
    if (!u || u.status === 'locked') throw unauthorized();
    return u;
  }

  /** Order với trạng thái suy ra theo R5. */
  function viewOrder(o: Order): Order {
    return { ...o, status: effectiveOrderStatus(o, now()) };
  }

  function detail(s: DbState, o: Order): OrderDetail {
    return {
      ...viewOrder(o),
      payment: {
        qrUrl: buildQrUrl(s.bank, o.amount, o.code),
        bankName: s.bank.bankName,
        accountNo: s.bank.accountNo,
        accountName: s.bank.accountName,
        amount: o.amount,
        transferContent: o.code,
      },
      licenseId: s.licenses.find((l) => l.orderId === o.id)?.id ?? null,
    };
  }

  /** Đơn của chính user; đơn người khác hoặc không có đều NOT_FOUND (R15). */
  function findOwnOrder(s: DbState, userId: string, id: string): Order {
    const o = s.orders.find((x) => x.id === id && x.userId === userId);
    if (!o) throw notFound();
    return o;
  }

  function findOrder(s: DbState, id: string): Order {
    const o = s.orders.find((x) => x.id === id);
    if (!o) throw notFound();
    return o;
  }

  /** Mới nhất trước; cùng thời điểm thì đơn thêm sau đứng trước. */
  function newestFirst(orders: Order[]): Order[] {
    return orders
      .map((o, i) => ({ o, i }))
      .sort((a, b) => b.o.createdAt.localeCompare(a.o.createdAt) || b.i - a.i)
      .map(({ o }) => o);
  }

  /** R17: ET + 6 số, lặp đến khi không trùng. */
  function newOrderCode(s: DbState): string {
    let code: string;
    do {
      code = 'ET' + String(Math.floor(100000 + Math.random() * 900000));
    } while (s.orders.some((o) => o.code === code));
    return code;
  }

  function cancel(s: DbState, o: Order, note?: string): Order {
    if (effectiveOrderStatus(o, now()) !== 'pending') throw notPending();
    o.status = 'cancelled';
    if (note !== undefined) o.note = note;
    save(s);
    return viewOrder(o);
  }

  load();

  return {
    reset() {
      save(createSeed(now()));
    },

    register(i) {
      const s = load();
      const email = normEmail(i.email);
      if (s.users.some((u) => normEmail(u.email) === email)) {
        throw new MockError(409, 'EMAIL_TAKEN', 'Email đã được đăng ký');
      }
      const user = {
        id: crypto.randomUUID(),
        email,
        password: i.password,
        fullName: i.fullName,
        phone: i.phone,
        companyName: i.companyName || null,
        taxCode: i.taxCode || null,
        role: 'customer' as const,
        status: 'active' as const,
        createdAt: now().toISOString(),
      };
      s.users.push(user);
      save(s);
      return { token: 'mock.' + user.id, user: toPublicUser(user) };
    },

    login(i) {
      const s = load();
      const email = normEmail(i.email);
      const u = s.users.find((x) => normEmail(x.email) === email);
      if (!u || u.password !== i.password) {
        throw new MockError(401, 'INVALID_CREDENTIALS', 'Email hoặc mật khẩu không đúng');
      }
      if (u.status === 'locked') {
        throw new MockError(403, 'ACCOUNT_LOCKED', 'Tài khoản đã bị khoá, vui lòng liên hệ hỗ trợ');
      }
      return { token: 'mock.' + u.id, user: toPublicUser(u) };
    },

    userFromToken(token) {
      if (!token || !token.startsWith('mock.')) throw unauthorized();
      return toPublicUser(requireUser(load(), token.slice('mock.'.length)));
    },

    updateProfile(userId, i) {
      const s = load();
      const u = requireUser(s, userId);
      u.fullName = i.fullName;
      u.phone = i.phone;
      u.companyName = i.companyName || null;
      u.taxCode = i.taxCode || null;
      save(s);
      return toPublicUser(u);
    },

    changePassword(userId, i) {
      const s = load();
      const u = requireUser(s, userId);
      if (u.password !== i.currentPassword) {
        throw new MockError(400, 'WRONG_PASSWORD', 'Mật khẩu hiện tại không đúng');
      }
      u.password = i.newPassword;
      save(s);
    },

    listPlans(includeInactive = false) {
      return load()
        .plans.filter((p) => includeInactive || p.active)
        .sort((a, b) => a.sortOrder - b.sortOrder);
    },

    createPlan(i) {
      const s = load();
      const plan: Plan = { ...i, id: crypto.randomUUID() };
      s.plans.push(plan);
      save(s);
      return plan;
    },

    updatePlan(id, i) {
      const s = load();
      const idx = s.plans.findIndex((p) => p.id === id);
      if (idx < 0) throw new MockError(404, 'PLAN_NOT_FOUND', 'Không tìm thấy gói');
      s.plans[idx] = { ...i, id };
      save(s);
      return s.plans[idx];
    },

    getBank: () => load().bank,
    setBank(b) {
      const s = load();
      s.bank = { ...b };
      save(s);
      return s.bank;
    },
    getApp: () => load().app,
    setApp(a) {
      const s = load();
      s.app = { ...a };
      save(s);
      return s.app;
    },

    createOrder(userId, planId) {
      const s = load();
      requireUser(s, userId);
      const plan = s.plans.find((p) => p.id === planId && p.active);
      if (!plan) throw new MockError(404, 'PLAN_NOT_FOUND', 'Không tìm thấy gói');
      const createdAt = now();
      const order: Order = {
        id: crypto.randomUUID(),
        code: newOrderCode(s),
        userId,
        planId,
        planSnapshot: {
          name: plan.name,
          durationMonths: plan.durationMonths,
          maxMachines: plan.maxMachines,
          maxMachineChanges: plan.maxMachineChanges,
          price: plan.price,
        },
        amount: plan.price,
        status: 'pending',
        createdAt: createdAt.toISOString(),
        expiresAt: new Date(createdAt.getTime() + 72 * 3600 * 1000).toISOString(),
        paidAt: null,
        confirmedBy: null,
        note: null,
      };
      s.orders.push(order);
      save(s);
      return detail(s, order);
    },

    listOrders(userId) {
      return newestFirst(load().orders.filter((o) => o.userId === userId)).map(viewOrder);
    },

    getOrder(userId, id) {
      const s = load();
      return detail(s, findOwnOrder(s, userId, id));
    },

    cancelOrder(userId, id) {
      const s = load();
      return cancel(s, findOwnOrder(s, userId, id));
    },

    adminListOrders(f) {
      const s = load();
      const q = f.q?.trim().toLowerCase();
      return newestFirst(s.orders).flatMap((o) => {
        const u = s.users.find((x) => x.id === o.userId);
        const view = viewOrder(o);
        if (f.status && view.status !== f.status) return [];
        if (q) {
          const hay = [o.code, u?.email, u?.taxCode].map((x) => (x ?? '').toLowerCase());
          if (!hay.some((h) => h.includes(q))) return [];
        }
        return [{ ...view, userEmail: u?.email ?? '', userFullName: u?.fullName ?? '', userTaxCode: u?.taxCode ?? null }];
      });
    },

    adminConfirmOrder(adminId, id, note) {
      const s = load();
      const o = findOrder(s, id);
      if (effectiveOrderStatus(o, now()) !== 'pending') throw notPending();
      const paidAt = now().toISOString();
      o.status = 'paid';
      o.paidAt = paidAt;
      o.confirmedBy = adminId;
      if (note !== undefined) o.note = note;
      const snap = o.planSnapshot;
      const license: StoredLicense = {
        id: crypto.randomUUID(),
        orderId: o.id,
        userId: o.userId,
        planName: snap.name,
        maxMachines: snap.maxMachines,
        maxMachineChanges: snap.maxMachineChanges,
        machineChangesUsed: 0,
        startsAt: paidAt,
        expiresAt: snap.durationMonths === null ? null : dayjs(paidAt).add(snap.durationMonths, 'month').toISOString(),
        status: 'active',
      };
      s.licenses.push(license);
      save(s);
      return { order: viewOrder(o), license: toPublicLicense(s, license, now()) };
    },

    adminCancelOrder(id, note) {
      const s = load();
      return cancel(s, findOrder(s, id), note);
    },
  };
}
