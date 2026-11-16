import { describe, expect, it } from 'vitest';
import { createMemoryStorage } from './memoryStorage';
import { createMockDb, MockError } from './mockDb';

const NOW = new Date('2026-11-16T03:00:00.000Z');

function setup() {
  return { db: createMockDb(createMemoryStorage(), () => NOW) };
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

describe('mockDb admin', () => {
  it('adminListUsers: chỉ khách, có số đơn/bản quyền, không lộ mật khẩu', () => {
    const { db } = setup();
    const list = db.adminListUsers();
    expect(list.map((u) => u.email)).toEqual(['khach@etax.vn']);
    expect(list[0].orderCount).toBe(2);
    expect(list[0].licenseCount).toBe(1);
    expect('password' in list[0]).toBe(false);
  });

  it('adminListUsers: q khớp tên công ty không phân biệt hoa thường; mới nhất trước', () => {
    const { db } = setup();
    expect(db.adminListUsers('an phát').map((u) => u.email)).toEqual(['khach@etax.vn']);
    expect(db.adminListUsers('AN PHÁT')).toHaveLength(1);
    expect(db.adminListUsers('khong-co-ai')).toHaveLength(0);
    db.register({ email: 'moi@etax.vn', password: 'x', fullName: 'Mới', phone: '0911222333', taxCode: '0312345678' });
    expect(db.adminListUsers().map((u) => u.email)).toEqual(['moi@etax.vn', 'khach@etax.vn']);
    expect(db.adminListUsers('0312345678').map((u) => u.email)).toEqual(['moi@etax.vn']);
    expect(db.adminListUsers('0911').map((u) => u.email)).toEqual(['moi@etax.vn']);
  });

  it('R19: admin không tự khoá mình', () => {
    const { db } = setup();
    expectError(
      () => db.adminSetUserStatus('u-admin', 'u-admin', 'locked'),
      409,
      'CANNOT_LOCK_SELF',
      'Không thể khoá tài khoản đang đăng nhập',
    );
  });

  it('R3/R20: khoá khách thì login bị từ chối và token mất hiệu lực; mở khoá khôi phục', () => {
    const { db } = setup();
    const u = db.adminSetUserStatus('u-admin', 'u-khach', 'locked');
    expect(u.status).toBe('locked');
    expect('password' in u).toBe(false);
    expectError(() => db.login({ email: 'khach@etax.vn', password: 'Khach@123' }), 403, 'ACCOUNT_LOCKED');
    expectError(() => db.userFromToken('mock.u-khach'), 401, 'UNAUTHORIZED');
    db.adminSetUserStatus('u-admin', 'u-khach', 'active');
    expect(db.userFromToken('mock.u-khach').id).toBe('u-khach');
  });

  it('adminSetUserStatus: id lạ thì NOT_FOUND', () => {
    const { db } = setup();
    expectError(() => db.adminSetUserStatus('u-admin', 'x', 'locked'), 404, 'NOT_FOUND', 'Không tìm thấy dữ liệu');
  });

  it('stats: seed rồi sau khi có khách mới và xác nhận đơn', () => {
    const { db } = setup();
    expect(db.stats()).toEqual({ revenueThisMonth: 0, pendingOrders: 1, activeLicenses: 1, newCustomersThisMonth: 0 });
    const { user } = db.register({ email: 'moi@etax.vn', password: 'x', fullName: 'Mới', phone: '0911222333' });
    db.createOrder(user.id, db.listPlans()[0].id);
    expect(db.stats().pendingOrders).toBe(2);
    db.adminConfirmOrder('u-admin', db.listOrders('u-khach').find((x) => x.code === 'ET482915')!.id);
    expect(db.stats()).toEqual({ revenueThisMonth: 500000, pendingOrders: 1, activeLicenses: 2, newCustomersThisMonth: 1 });
  });

  it('adminGetUser: kèm đơn và bản quyền; admin hoặc id lạ thì NOT_FOUND', () => {
    const { db } = setup();
    const d = db.adminGetUser('u-khach');
    expect(d.orders).toHaveLength(2);
    expect(d.licenses).toHaveLength(1);
    expect('password' in d).toBe(false);
    expectError(() => db.adminGetUser('x'), 404, 'NOT_FOUND');
    expectError(() => db.adminGetUser('u-admin'), 404, 'NOT_FOUND');
  });
});
