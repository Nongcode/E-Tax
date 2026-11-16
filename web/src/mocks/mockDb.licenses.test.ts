import dayjs from 'dayjs';
import { describe, expect, it } from 'vitest';
import { createMemoryStorage } from './memoryStorage';
import { createMockDb, MockError } from './mockDb';

let t = new Date('2026-11-16T03:00:00.000Z');

function setup() {
  t = new Date('2026-11-16T03:00:00.000Z');
  const db = createMockDb(createMemoryStorage(), () => t);
  return { db };
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

const U = 'u-khach';
const SEED_MACHINE = '3FA2C91B7E0D4A55';

describe('mockDb licenses', () => {
  it('R7: quá expiresAt thì listLicenses trả expired', () => {
    const { db } = setup();
    expect(db.listLicenses(U)[0].status).toBe('active');
    t = dayjs(db.listLicenses(U)[0].expiresAt).add(1, 'day').toDate();
    expect(db.listLicenses(U)[0].status).toBe('expired');
  });

  it('R8: chuẩn hoá mã máy, báo lỗi mã/tên sai (kể cả dán kèm khoảng trắng, chữ thường)', () => {
    const { db } = setup();
    const a = db.addActivation(U, 'l-1', { machineId: ' 1a2b3c4d5e6f7a8b\n', machineName: ' Máy 2 ' });
    expect(a.machineId).toBe('1A2B3C4D5E6F7A8B');
    expect(a.machineName).toBe('Máy 2');
    expect(a.key).toBe('ETAX-MOCK-1A2B3C4D5E6F7A8B-l-1');
    expectError(
      () => db.addActivation(U, 'l-1', { machineId: '1A2B-3C4D', machineName: 'x' }),
      400,
      'INVALID_MACHINE_ID',
      'Mã máy phải gồm 16 ký tự 0-9, A-F',
    );
    expectError(
      () => db.addActivation(U, 'l-1', { machineId: 'AAAAAAAAAAAAAAAA', machineName: '  ' }),
      400,
      'INVALID_MACHINE_NAME',
      'Vui lòng nhập tên máy',
    );
  });

  it('R9: license expired hoặc revoked thì LICENSE_INACTIVE', () => {
    const msg = 'Bản quyền đã hết hạn hoặc bị thu hồi';
    const { db } = setup();
    t = dayjs(t).add(400, 'day').toDate();
    expectError(() => db.addActivation(U, 'l-1', { machineId: 'AAAAAAAAAAAAAAAA', machineName: 'x' }), 409, 'LICENSE_INACTIVE', msg);
    const { db: db2 } = setup();
    db2.revokeLicense('l-1');
    expectError(() => db2.addActivation(U, 'l-1', { machineId: 'AAAAAAAAAAAAAAAA', machineName: 'x' }), 409, 'LICENSE_INACTIVE', msg);
  });

  it('R10: thêm lại máy đang dùng trả activation cũ', () => {
    const { db } = setup();
    const a = db.addActivation(U, 'l-1', { machineId: SEED_MACHINE.toLowerCase(), machineName: 'khác' });
    expect(a.id).toBe('a-1');
    expect(a.key).toBe('ETAX-MOCK-3FA2C91B7E0D4A55-l-1');
    expect(db.getLicense(U, 'l-1').activeMachines).toBe(1);
  });

  it('R11: máy thứ 2 được, máy thứ 3 MACHINE_LIMIT', () => {
    const { db } = setup();
    db.addActivation(U, 'l-1', { machineId: 'AAAAAAAAAAAAAAAA', machineName: 'B' });
    expect(db.getLicense(U, 'l-1').activeMachines).toBe(2);
    expectError(
      () => db.addActivation(U, 'l-1', { machineId: 'BBBBBBBBBBBBBBBB', machineName: 'C' }),
      409,
      'MACHINE_LIMIT',
      'Đã dùng hết số máy của gói, hãy gỡ một máy trước',
    );
  });

  it('R12: gỡ máy tăng số lần đổi, admin vẫn thấy máy đã gỡ, hết lượt thì CHANGE_LIMIT', () => {
    const { db } = setup();
    db.removeActivation(U, 'l-1', 'a-1');
    const d = db.getLicense(U, 'l-1');
    expect(d.machineChangesUsed).toBe(1);
    expect(d.activations).toHaveLength(0);
    const ad = db.adminGetLicense('l-1');
    expect(ad.activations.find((x) => x.id === 'a-1')?.revokedAt).toBe(t.toISOString());
    expect(ad.user.email).toBe('khach@etax.vn');
    expect('password' in ad.user).toBe(false);

    const b = db.addActivation(U, 'l-1', { machineId: 'AAAAAAAAAAAAAAAA', machineName: 'B' });
    db.removeActivation(U, 'l-1', b.id);
    const c = db.addActivation(U, 'l-1', { machineId: 'BBBBBBBBBBBBBBBB', machineName: 'C' });
    db.removeActivation(U, 'l-1', c.id);
    expect(db.getLicense(U, 'l-1').machineChangesUsed).toBe(3);
    const e = db.addActivation(U, 'l-1', { machineId: 'CCCCCCCCCCCCCCCC', machineName: 'D' });
    expectError(() => db.removeActivation(U, 'l-1', e.id), 409, 'CHANGE_LIMIT', 'Đã hết số lần đổi máy của gói');
  });

  it('R13: gia hạn cộng từ expiresAt, hoặc từ now nếu đã hết hạn; trọn đời bị từ chối', () => {
    const { db } = setup();
    const old = db.listLicenses(U)[0].expiresAt!;
    expect(db.extendLicense('l-1', 3).expiresAt).toBe(dayjs(old).add(3, 'month').toISOString());

    const { db: db2 } = setup();
    const old2 = db2.listLicenses(U)[0].expiresAt!;
    t = dayjs(old2).add(10, 'day').toDate();
    expect(db2.listLicenses(U)[0].status).toBe('expired');
    const ext = db2.extendLicense('l-1', 3);
    expect(ext.expiresAt).toBe(dayjs(t).add(3, 'month').toISOString());
    expect(ext.status).toBe('active');

    const { db: db3 } = setup();
    const order = db3.createOrder(U, 'plife');
    const { license } = db3.adminConfirmOrder('u-admin', order.id);
    expectError(() => db3.extendLicense(license.id, 3), 409, 'LICENSE_PERMANENT', 'Bản quyền trọn đời không cần gia hạn');
  });

  it('R14: thu hồi đặt revoked và gỡ mọi máy đang dùng', () => {
    const { db } = setup();
    const l = db.revokeLicense('l-1');
    expect(l.status).toBe('revoked');
    expect(l.activeMachines).toBe(0);
    expect(db.adminGetLicense('l-1').activations.every((a) => a.revokedAt === t.toISOString())).toBe(true);
  });

  it('R15: user khác hoặc id lạ thì NOT_FOUND', () => {
    const { db } = setup();
    const other = db.register({
      email: 'x@etax.vn',
      password: 'Abcdef12',
      fullName: 'X',
      phone: '0900000001',
      companyName: '',
      taxCode: '',
    }).user.id;
    expectError(() => db.getLicense(other, 'l-1'), 404, 'NOT_FOUND', 'Không tìm thấy dữ liệu');
    expectError(() => db.addActivation(other, 'l-1', { machineId: 'AAAAAAAAAAAAAAAA', machineName: 'x' }), 404, 'NOT_FOUND');
    expectError(() => db.removeActivation(other, 'l-1', 'a-1'), 404, 'NOT_FOUND');
    expectError(() => db.getLicense(U, 'zzz'), 404, 'NOT_FOUND');
    expect(db.listLicenses(other)).toEqual([]);
  });

  it('adminListLicenses lọc theo status và q (email, code đơn, mã máy)', () => {
    const { db } = setup();
    expect(db.adminListLicenses({ q: '3FA2C91B' }).map((l) => l.id)).toEqual(['l-1']);
    expect(db.adminListLicenses({ q: ' 3fa2c91b\n' }).map((l) => l.id)).toEqual(['l-1']);
    expect(db.adminListLicenses({ q: 'KHACH@' })[0].userEmail).toBe('khach@etax.vn');
    expect(db.adminListLicenses({ q: 'et903311' })).toHaveLength(1);
    expect(db.adminListLicenses({ q: 'nope' })).toHaveLength(0);
    expect(db.adminListLicenses({ status: 'revoked' })).toHaveLength(0);
    expect(db.adminListLicenses({ status: 'active' })).toHaveLength(1);
  });
});
