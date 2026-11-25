import dayjs from 'dayjs';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createMemoryStorage } from './memoryStorage';
import { createMockDb, MockError, STORAGE_KEY } from './mockDb';

let t = new Date('2026-11-16T03:00:00.000Z');

function setup() {
  t = new Date('2026-11-16T03:00:00.000Z');
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

const H = 3600 * 1000;
const NOT_PENDING = 'Đơn không còn ở trạng thái chờ thanh toán';

afterEach(() => vi.restoreAllMocks());

describe('mockDb orders', () => {
  it('R4: tạo đơn chụp giá, hạn 72h, kèm thông tin thanh toán', () => {
    const { db } = setup();
    const o = db.createOrder('u-khach', 'p6');
    expect(o.status).toBe('pending');
    expect(o.amount).toBe(500000);
    expect(o.planSnapshot.name).toBe('Gói 6 tháng');
    expect(o.expiresAt).toBe(new Date(t.getTime() + 72 * H).toISOString());
    expect(o.code).toMatch(/^ET\d{6}$/);
    expect(o.payment.transferContent).toBe(o.code);
    expect(o.payment.qrUrl).toContain('970436-0071000123456-compact2.png?amount=500000&addInfo=' + o.code);
    expect(o.licenseId).toBeNull();
  });

  it('R4: gói ngừng bán hoặc không tồn tại thì PLAN_NOT_FOUND', () => {
    const { db } = setup();
    const { id: _id, ...input } = db.listPlans().find((x) => x.id === 'p3')!;
    void _id;
    db.updatePlan('p3', { ...input, active: false });
    expectError(() => db.createOrder('u-khach', 'p3'), 404, 'PLAN_NOT_FOUND');
    expectError(() => db.createOrder('u-khach', 'zzz'), 404, 'PLAN_NOT_FOUND');
  });

  it('R4: đổi giá gói sau đó không ảnh hưởng đơn cũ', () => {
    const { db } = setup();
    const o = db.createOrder('u-khach', 'p6');
    const { id: _id, ...input } = db.listPlans().find((x) => x.id === 'p6')!;
    void _id;
    db.updatePlan('p6', { ...input, price: 999000 });
    expect(db.getOrder('u-khach', o.id).amount).toBe(500000);
  });

  it('R5: đơn quá hạn được coi là expired, không xác nhận / huỷ được', () => {
    const { db } = setup();
    const o = db.createOrder('u-khach', 'p6');
    t = new Date(t.getTime() + 73 * H);
    expect(db.getOrder('u-khach', o.id).status).toBe('expired');
    expectError(() => db.adminConfirmOrder('u-admin', o.id), 409, 'ORDER_NOT_PENDING', NOT_PENDING);
    expectError(() => db.cancelOrder('u-khach', o.id), 409, 'ORDER_NOT_PENDING', NOT_PENDING);
    expectError(() => db.adminCancelOrder(o.id), 409, 'ORDER_NOT_PENDING', NOT_PENDING);
  });

  it('huỷ đơn pending', () => {
    const { db } = setup();
    const o = db.createOrder('u-khach', 'p6');
    expect(db.cancelOrder('u-khach', o.id).status).toBe('cancelled');
    expect(db.adminCancelOrder('o-2', 'không chuyển').status).toBe('cancelled');
    expect(db.getOrder('u-khach', 'o-2').note).toBe('không chuyển');
  });

  it('R6: xác nhận đơn tạo bản quyền', () => {
    const { db } = setup();
    const o = db.createOrder('u-khach', 'p6');
    const { order, license } = db.adminConfirmOrder('u-admin', o.id, 'VCB FT1');
    expect(order.status).toBe('paid');
    expect(order.paidAt).toBe(t.toISOString());
    expect(order.confirmedBy).toBe('u-admin');
    expect(order.note).toBe('VCB FT1');
    expect(license.startsAt).toBe(t.toISOString());
    expect(license.expiresAt).toBe(dayjs(t).add(6, 'month').toISOString());
    expect(license.maxMachines).toBe(1);
    expect(license.maxMachineChanges).toBe(2);
    expect(license.activeMachines).toBe(0);
    expect(license.status).toBe('active');
    expect(db.getOrder('u-khach', o.id).licenseId).toBe(license.id);
    expectError(() => db.adminConfirmOrder('u-admin', o.id), 409, 'ORDER_NOT_PENDING');
  });

  it('R6: gói trọn đời không hết hạn', () => {
    const { db } = setup();
    const o = db.createOrder('u-khach', 'plife');
    expect(db.adminConfirmOrder('u-admin', o.id).license.expiresAt).toBeNull();
  });

  it('R6: cộng tháng cuối tháng', () => {
    const { db } = setup();
    // Dựng mốc và so sánh theo giờ máy để không lệch ngày ở múi giờ khác UTC.
    t = dayjs('2027-01-31T10:00:00').toDate();
    const plan = db.createPlan({
      name: '1 tháng',
      description: '',
      durationMonths: 1,
      maxMachines: 1,
      maxMachineChanges: 1,
      price: 100000,
      featured: false,
      active: true,
      sortOrder: 9,
    });
    const o = db.createOrder('u-khach', plan.id);
    const { license } = db.adminConfirmOrder('u-admin', o.id);
    expect(dayjs(license.expiresAt!).format('YYYY-MM-DD')).toBe('2027-02-28');
  });

  it('R15: người khác không thấy / huỷ được đơn', () => {
    const { db } = setup();
    const other = db.register({ email: 'la@etax.vn', password: 'La@123456', fullName: 'Lạ', phone: '0900000001' });
    expectError(() => db.getOrder(other.user.id, 'o-2'), 404, 'NOT_FOUND', 'Không tìm thấy dữ liệu');
    expectError(() => db.cancelOrder(other.user.id, 'o-2'), 404, 'NOT_FOUND');
    expectError(() => db.getOrder('u-khach', 'zzz'), 404, 'NOT_FOUND');
  });

  it('R17: mã đơn trùng thì sinh lại', () => {
    const { db } = setup();
    // 100000 + 0.4254611... * 900000 = 482915.5 -> ET482915 (trùng đơn mẫu)
    const dup = 382915.5 / 900000;
    const seq = [dup, dup, 0.5];
    let i = 0;
    const spy = vi.spyOn(Math, 'random').mockImplementation(() => seq[Math.min(i++, seq.length - 1)]);
    const o = db.createOrder('u-khach', 'p3');
    expect(spy.mock.calls.length).toBeGreaterThanOrEqual(2);
    expect(o.code).not.toBe('ET482915');
    expect(o.code).toMatch(/^ET\d{6}$/);
  });

  it('listOrders mới nhất trước', () => {
    const { db } = setup();
    t = new Date(t.getTime() + H);
    const o = db.createOrder('u-khach', 'p3');
    expect(db.listOrders('u-khach').map((x) => x.id)).toEqual([o.id, 'o-2', 'o-1']);
  });

  it('adminListOrders lọc theo trạng thái và q', () => {
    const { db } = setup();
    expect(db.adminListOrders({ status: 'pending' }).map((x) => x.code)).toContain('ET482915');
    expect(db.adminListOrders({ status: 'paid' }).map((x) => x.code)).not.toContain('ET482915');
    const byTax = db.adminListOrders({ q: '0101234567' });
    expect(byTax.length).toBe(2);
    expect(byTax[0].userEmail).toBe('khach@etax.vn');
    expect(db.adminListOrders({ q: 'et48' }).map((x) => x.code)).toEqual(['ET482915']);
    expect(db.adminListOrders({ q: 'KHACH@' }).length).toBe(2);
  });

  it('QR của đơn dùng công thức spec', () => {
    const { db } = setup();
    expect(db.getOrder('u-khach', 'o-2').payment.qrUrl).toBe(
      'https://img.vietqr.io/image/970436-0071000123456-compact2.png?amount=500000&addInfo=ET482915&accountName=CONG%20TY%20TNHH%20ETAX',
    );
  });

  it('storage thiếu bank (hoặc orders/licenses/activations/app) thì nạp lại seed', () => {
    const { storage, db } = setup();
    const s = JSON.parse(storage.getItem(STORAGE_KEY)!);
    delete s.bank;
    storage.setItem(STORAGE_KEY, JSON.stringify(s));
    expect(db.getBank().bankBin).toBe('970436');
    expect(JSON.parse(storage.getItem(STORAGE_KEY)!).bank).toBeDefined();
  });
});
