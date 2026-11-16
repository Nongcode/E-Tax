import type {
  Activation,
  AppRelease,
  AuthResponse,
  BankSettings,
  ChangePasswordInput,
  License,
  LicenseStatus,
  LoginInput,
  Order,
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
}

const normEmail = (e: string) => e.trim().toLowerCase();
const unauthorized = () => new MockError(401, 'UNAUTHORIZED', 'Vui lòng đăng nhập để tiếp tục');

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
        if (parsed && parsed.version === 1 && Array.isArray(parsed.users) && Array.isArray(parsed.plans)) {
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
  };
}
