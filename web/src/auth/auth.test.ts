import { describe, expect, it } from 'vitest';
import type { User } from '../api/types';
import { loadSession } from './session';
import { resolveAfterLogin } from './redirect';

const customer: User = {
  id: 'u1',
  email: 'a@b.vn',
  fullName: 'A',
  phone: '0912345678',
  companyName: null,
  taxCode: null,
  role: 'customer',
  status: 'active',
  createdAt: '2025-01-01T00:00:00.000Z',
};
const admin: User = { ...customer, role: 'admin' };

describe('resolveAfterLogin', () => {
  it('mặc định theo vai trò', () => {
    expect(resolveAfterLogin(customer, null)).toBe('/tai-khoan');
    expect(resolveAfterLogin(admin, null)).toBe('/quan-tri');
  });
  it('giữ next nội bộ', () => {
    expect(resolveAfterLogin(customer, '/tai-khoan/mua-goi?goi=p6')).toBe('/tai-khoan/mua-goi?goi=p6');
  });
  it('chặn next độc hại', () => {
    expect(resolveAfterLogin(customer, '//evil.com')).toBe('/tai-khoan');
    expect(resolveAfterLogin(customer, 'https://evil.com')).toBe('/tai-khoan');
    expect(resolveAfterLogin(customer, '/\\evil.com')).toBe('/tai-khoan');
  });
});

describe('loadSession', () => {
  it('JSON hỏng thì xoá key và trả null', () => {
    localStorage.setItem('etax-session', '{hỏng');
    expect(loadSession()).toBeNull();
    expect(localStorage.getItem('etax-session')).toBeNull();
  });
});
