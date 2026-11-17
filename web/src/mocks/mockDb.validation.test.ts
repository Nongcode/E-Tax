import { describe, expect, it } from 'vitest';
import type { PlanInput } from '../api/types';
import { createMemoryStorage } from './memoryStorage';
import { createMockDb, MockError } from './mockDb';

const t = new Date('2026-11-16T03:00:00.000Z');
const KEY = 'etax-mockdb-v1';

function setup() {
  const storage = createMemoryStorage();
  const db = createMockDb(storage, () => t);
  db.reset();
  return { db, storage };
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

const validPlan: PlanInput = {
  name: 'Gói thử',
  description: '',
  durationMonths: 3,
  maxMachines: 1,
  maxMachineChanges: 0,
  price: 0,
  featured: false,
  active: true,
  sortOrder: 9,
};

describe('mockDb kiểm tra đầu vào (R21–R24)', () => {
  it('R23: gia hạn với số tháng không hợp lệ', () => {
    const { db } = setup();
    for (const m of [0, -1, 1.5, Number.NaN, 121]) {
      expectError(() => db.extendLicense('l-1', m), 400, 'VALIDATION_ERROR', 'Số tháng gia hạn không hợp lệ');
    }
    expect(db.extendLicense('l-1', 120).id).toBe('l-1');
  });

  it('R22: không gia hạn bản quyền đã thu hồi', () => {
    const { db } = setup();
    db.revokeLicense('l-1');
    expectError(() => db.extendLicense('l-1', 1), 409, 'LICENSE_REVOKED', 'Bản quyền đã bị thu hồi, không thể gia hạn');
  });

  it('R23: trạng thái tài khoản chỉ nhận active/locked', () => {
    const { db } = setup();
    expectError(
      () => db.adminSetUserStatus('u-admin', 'u-khach', 'banned' as never),
      400,
      'VALIDATION_ERROR',
      'Trạng thái tài khoản không hợp lệ',
    );
  });

  it('R21: không khoá tài khoản quản trị khác', () => {
    const { db, storage } = setup();
    const state = JSON.parse(storage.getItem(KEY)!);
    const admin = state.users.find((u: { id: string }) => u.id === 'u-admin');
    state.users.push({ ...admin, id: 'u-admin-2', email: 'admin2@etax.vn' });
    storage.setItem(KEY, JSON.stringify(state));
    expectError(
      () => db.adminSetUserStatus('u-admin', 'u-admin-2', 'locked'),
      409,
      'CANNOT_LOCK_ADMIN',
      'Không thể khoá tài khoản quản trị',
    );
  });

  it('R23: gói sai dữ liệu thì VALIDATION_ERROR, cả khi tạo lẫn sửa', () => {
    const { db } = setup();
    const bad: Partial<PlanInput>[] = [
      { name: '  ' },
      { price: -1 },
      { price: 1.5 },
      { maxMachines: 0 },
      { maxMachineChanges: -1 },
      { durationMonths: 0 },
      { durationMonths: 2.5 },
    ];
    for (const b of bad) {
      expectError(() => db.createPlan({ ...validPlan, ...b }), 400, 'VALIDATION_ERROR');
      expectError(() => db.updatePlan('p3', { ...validPlan, ...b }), 400, 'VALIDATION_ERROR');
    }
    expect(db.createPlan({ ...validPlan, durationMonths: null }).durationMonths).toBeNull();
  });

  it('R24: thiếu tên máy hoặc không phải chuỗi thì INVALID_MACHINE_NAME, không phải lỗi hệ thống', () => {
    const { db } = setup();
    for (const machineName of [undefined, 123, null]) {
      expectError(
        () => db.addActivation('u-khach', 'l-1', { machineId: 'AAAABBBBCCCCDDDD', machineName: machineName as never }),
        400,
        'INVALID_MACHINE_NAME',
        'Vui lòng nhập tên máy',
      );
    }
  });

  it('admin tìm theo mã máy chỉ khớp máy đang dùng', () => {
    const { db } = setup();
    expect(db.adminListLicenses({ q: '3FA2C91B' })).toHaveLength(1);
    const act = db.getLicense('u-khach', 'l-1').activations[0];
    db.removeActivation('u-khach', 'l-1', act.id);
    expect(db.adminListLicenses({ q: '3FA2C91B' })).toHaveLength(0);
  });

});
