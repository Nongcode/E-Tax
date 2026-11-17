import { beforeEach, describe, expect, it, vi } from 'vitest';
import { configureClient } from './client';
import { setupMockServer } from '../test/server';
import { addActivation, removeActivation, getLicenses } from './licenses';
import { forgotPassword, login } from './auth';
import { createOrder, getOrder, getOrders } from './orders';
import { getPlans } from './plans';
import { confirmOrder, getAdminOrders, getStats } from './admin';

setupMockServer();

let token: string | null = null;

beforeEach(() => {
  token = null;
  configureClient({ getToken: () => token, onUnauthorized: vi.fn() });
});

async function loginAs(email: string, password: string) {
  const r = await login({ email, password });
  token = r.token;
  return r;
}

describe('api qua MSW', () => {
  it('công khai xem gói, chưa đăng nhập thì 401', async () => {
    expect(await getPlans()).toHaveLength(4);
    await expect(getOrders()).rejects.toMatchObject({ status: 401, code: 'UNAUTHORIZED' });
  });

  it('khách tạo đơn p6 rồi có 3 đơn', async () => {
    await loginAs('khach@etax.vn', 'Khach@123');
    const o = await createOrder('p6');
    expect(o.amount).toBe(500000);
    expect(await getOrders()).toHaveLength(3);
  });

  it('khách gọi API admin bị 403', async () => {
    await loginAs('khach@etax.vn', 'Khach@123');
    await expect(getStats()).rejects.toMatchObject({
      status: 403,
      code: 'FORBIDDEN',
      message: 'Bạn không có quyền thực hiện thao tác này',
    });
  });

  it('admin xác nhận đơn, khách thấy 2 bản quyền', async () => {
    await loginAs('admin@etax.vn', 'Admin@123');
    const pending = await getAdminOrders({ status: 'pending' });
    const r = await confirmOrder(pending[0].id, 'ok');
    expect(r.order.status).toBe('paid');
    await expect(getOrders()).rejects.toMatchObject({ status: 403 });
    await loginAs('khach@etax.vn', 'Khach@123');
    expect(await getLicenses()).toHaveLength(2);
  });

  it('kích hoạt mã máy sai, gỡ máy 204, quên mật khẩu 204', async () => {
    await loginAs('khach@etax.vn', 'Khach@123');
    await expect(addActivation('l-1', { machineId: 'zz', machineName: 'A' })).rejects.toMatchObject({
      status: 400,
      code: 'INVALID_MACHINE_ID',
    });
    await expect(removeActivation('l-1', 'a-1')).resolves.toBeUndefined();
    await expect(forgotPassword('khong-co@x.vn')).resolves.toBeUndefined();
  });

  it('bỏ tham số rỗng khi lọc đơn admin', async () => {
    await loginAs('admin@etax.vn', 'Admin@123');
    const seen: string[] = [];
    const orig = globalThis.fetch;
    const spy = vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
      seen.push(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url);
      return orig(input, init);
    });
    const list = await getAdminOrders({ status: 'pending', q: '' });
    spy.mockRestore();
    expect(seen[0]).toContain('status=pending');
    expect(seen[0]).not.toContain('q=');
    expect(list.map((o) => o.code)).toContain('ET482915');
  });

  it('R23: body JSON hỏng thì 400 VALIDATION_ERROR, không phải lỗi máy chủ', async () => {
    const res = await fetch('http://localhost/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: '{bad',
    });
    expect(res.status).toBe(400);
    expect(await res.json()).toEqual({ code: 'VALIDATION_ERROR', message: 'Dữ liệu gửi lên không hợp lệ' });
  });

  it('mã hoá tham số đường dẫn', async () => {
    await loginAs('khach@etax.vn', 'Khach@123');
    const seen: string[] = [];
    const orig = globalThis.fetch;
    const spy = vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
      seen.push(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url);
      return orig(input, init);
    });
    await getOrder('a/../b').catch(() => undefined);
    spy.mockRestore();
    expect(seen[0]).toContain('/api/orders/a%2F..%2Fb');
  });
});
