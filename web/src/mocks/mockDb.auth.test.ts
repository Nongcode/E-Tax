import { describe, expect, it } from 'vitest';
import { createMemoryStorage } from './memoryStorage';
import { createMockDb, MockError } from './mockDb';

const t = new Date('2026-11-16T03:00:00.000Z');
const KEY = 'etax-mockdb-v1';

function setup() {
  const storage = createMemoryStorage();
  const db = createMockDb(storage, () => t);
  return { storage, db };
}

function expectError(fn: () => unknown, status: number, code: string, message?: string) {
  try {
    fn();
  } catch (e) {
    expect(e).toBeInstanceOf(MockError);
    const err = e as MockError;
    expect(err.status).toBe(status);
    expect(err.code).toBe(code);
    if (message) expect(err.message).toBe(message);
    return;
  }
  throw new Error('expected MockError');
}

const newUser = { email: 'moi@etax.vn', password: 'Moi@12345', fullName: 'Người Mới', phone: '0987654321' };

describe('mockDb auth', () => {
  it('R1: email trùng (khác hoa thường) bị từ chối', () => {
    const { db } = setup();
    expectError(
      () => db.register({ ...newUser, email: 'KHACH@etax.vn' }),
      409,
      'EMAIL_TAKEN',
      'Email đã được đăng ký',
    );
  });

  it('đăng ký email mới trả token mock và không lộ password', () => {
    const { db } = setup();
    const r = db.register(newUser);
    expect(r.token).toBe('mock.' + r.user.id);
    expect(r.user.role).toBe('customer');
    expect(r.user.status).toBe('active');
    expect(r.user).not.toHaveProperty('password');
  });

  it('R2: sai mật khẩu; email khác hoa thường đăng nhập được', () => {
    const { db } = setup();
    expectError(
      () => db.login({ email: 'khach@etax.vn', password: 'sai' }),
      401,
      'INVALID_CREDENTIALS',
      'Email hoặc mật khẩu không đúng',
    );
    const r = db.login({ email: 'Khach@ETAX.vn', password: 'Khach@123' });
    expect(r.user.id).toBe('u-khach');
    expect(r.user).not.toHaveProperty('password');
  });

  it('R3: tài khoản khoá không đăng nhập được', () => {
    const { db, storage } = setup();
    const s = JSON.parse(storage.getItem(KEY)!);
    s.users.find((u: { id: string }) => u.id === 'u-khach').status = 'locked';
    storage.setItem(KEY, JSON.stringify(s));
    const db2 = createMockDb(storage, () => t);
    expectError(
      () => db2.login({ email: 'khach@etax.vn', password: 'Khach@123' }),
      403,
      'ACCOUNT_LOCKED',
      'Tài khoản đã bị khoá, vui lòng liên hệ hỗ trợ',
    );
    expect(db).toBeDefined();
  });

  it('R16/R20: token thiếu, sai, hoặc của user bị khoá', () => {
    const { storage, db } = setup();
    expect(db.userFromToken('mock.u-khach').id).toBe('u-khach');
    expectError(() => db.userFromToken(null), 401, 'UNAUTHORIZED', 'Vui lòng đăng nhập để tiếp tục');
    expectError(() => db.userFromToken('mock.khong-co'), 401, 'UNAUTHORIZED');
    expectError(() => db.userFromToken('rac'), 401, 'UNAUTHORIZED');
    const s = JSON.parse(storage.getItem(KEY)!);
    s.users.find((u: { id: string }) => u.id === 'u-khach').status = 'locked';
    storage.setItem(KEY, JSON.stringify(s));
    const db2 = createMockDb(storage, () => t);
    expectError(() => db2.userFromToken('mock.u-khach'), 401, 'UNAUTHORIZED');
  });

  it('R18: đổi mật khẩu', () => {
    const { db } = setup();
    expectError(
      () => db.changePassword('u-khach', { currentPassword: 'sai', newPassword: 'Moi@12345' }),
      400,
      'WRONG_PASSWORD',
      'Mật khẩu hiện tại không đúng',
    );
    db.changePassword('u-khach', { currentPassword: 'Khach@123', newPassword: 'Moi@12345' });
    expect(db.login({ email: 'khach@etax.vn', password: 'Moi@12345' }).user.id).toBe('u-khach');
  });

  it('updateProfile cập nhật thông tin', () => {
    const { db } = setup();
    const u = db.updateProfile('u-khach', { fullName: 'Tên Mới', phone: '0911111111' });
    expect(u.fullName).toBe('Tên Mới');
    expect(u.companyName).toBeNull();
    expect(u).not.toHaveProperty('password');
  });

  it('lưu bền giữa hai db trên cùng storage', () => {
    const { db, storage } = setup();
    const r = db.register(newUser);
    const db2 = createMockDb(storage, () => t);
    expect(db2.userFromToken(r.token).email).toBe('moi@etax.vn');
  });

  it.each(['{hỏng', '{"version":0}'])('storage hỏng (%s) nạp lại seed', (raw) => {
    const storage = createMemoryStorage();
    storage.setItem(KEY, raw);
    const db = createMockDb(storage, () => t);
    expect(db.listPlans()).toHaveLength(4);
  });

  it('reset nạp lại seed', () => {
    const { db } = setup();
    db.register(newUser);
    db.reset();
    expectError(() => db.login({ email: newUser.email, password: newUser.password }), 401, 'INVALID_CREDENTIALS');
  });

  it('seed theo brief', () => {
    const { db } = setup();
    expect(db.listPlans().map((p) => p.id)).toEqual(['p3', 'p6', 'p12', 'plife']);
    expect(db.getBank().bankBin).toBe('970436');
    expect(db.getApp().version).toBe('1.4.0');
  });

  it('plans: ngừng bán, tạo, sửa, không tìm thấy', () => {
    const { db } = setup();
    const { id, ...p3 } = db.listPlans()[0];
    expect(id).toBe('p3');
    db.updatePlan('p3', { ...p3, active: false });
    expect(db.listPlans()).toHaveLength(3);
    expect(db.listPlans(true)).toHaveLength(4);
    const created = db.createPlan({ ...p3, name: 'Gói mới', active: true, sortOrder: 5 });
    expect(created.id).toBeTruthy();
    expect(db.listPlans(true)).toHaveLength(5);
    expectError(() => db.updatePlan('x', p3), 404, 'PLAN_NOT_FOUND');
  });

  it('bank/app settings lưu bền', () => {
    const { db, storage } = setup();
    db.setBank({ bankBin: '970415', bankName: 'VietinBank', accountNo: '1', accountName: 'A' });
    db.setApp({ ...db.getApp(), version: '2.0.0' });
    const db2 = createMockDb(storage, () => t);
    expect(db2.getBank().bankName).toBe('VietinBank');
    expect(db2.getApp().version).toBe('2.0.0');
  });
});
