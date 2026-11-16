import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { ApiError, apiFetch, configureClient } from './client';

const server = setupServer();

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
beforeEach(() => {
  configureClient({ getToken: () => null, onUnauthorized: () => {} });
});

describe('apiFetch', () => {
  it('gửi Bearer token khi có', async () => {
    server.use(
      http.get('http://localhost/api/ping', ({ request }) =>
        HttpResponse.json({ ok: true, auth: request.headers.get('Authorization') }),
      ),
    );
    configureClient({ getToken: () => 'abc', onUnauthorized: () => {} });
    const res = await apiFetch<{ ok: boolean; auth: string }>('/ping');
    expect(res.auth).toBe('Bearer abc');
  });

  it('lỗi JSON {code,message} thành ApiError', async () => {
    server.use(
      http.post('http://localhost/api/auth/register', () =>
        HttpResponse.json({ code: 'EMAIL_TAKEN', message: 'Email đã được đăng ký' }, { status: 409 }),
      ),
    );
    await expect(apiFetch('/auth/register', { method: 'POST', body: { a: 1 } })).rejects.toMatchObject({
      status: 409,
      code: 'EMAIL_TAKEN',
      message: 'Email đã được đăng ký',
    });
  });

  it('401 khi có token gọi onUnauthorized', async () => {
    server.use(
      http.get('http://localhost/api/me', () =>
        HttpResponse.json({ code: 'UNAUTHORIZED', message: 'Phiên hết hạn' }, { status: 401 }),
      ),
    );
    const onUnauthorized = vi.fn();
    configureClient({ getToken: () => 'abc', onUnauthorized });
    const err = await apiFetch('/me').catch((e: unknown) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect((err as ApiError).status).toBe(401);
    expect(onUnauthorized).toHaveBeenCalledTimes(1);
  });

  it('401 khi chưa có token (đăng nhập sai) KHÔNG gọi onUnauthorized', async () => {
    server.use(
      http.post('http://localhost/api/auth/login', () =>
        HttpResponse.json({ code: 'INVALID_CREDENTIALS', message: 'Sai email hoặc mật khẩu' }, { status: 401 }),
      ),
    );
    const onUnauthorized = vi.fn();
    configureClient({ getToken: () => null, onUnauthorized });
    await expect(apiFetch('/auth/login', { method: 'POST', body: {} })).rejects.toMatchObject({
      code: 'INVALID_CREDENTIALS',
    });
    expect(onUnauthorized).not.toHaveBeenCalled();
  });

  it('lỗi mạng thành ApiError NETWORK', async () => {
    server.use(http.get('http://localhost/api/x', () => HttpResponse.error()));
    await expect(apiFetch('/x')).rejects.toMatchObject({
      status: 0,
      code: 'NETWORK',
      message: 'Không kết nối được máy chủ',
    });
  });

  it('lỗi không phải JSON', async () => {
    server.use(http.get('http://localhost/api/x', () => new HttpResponse('boom', { status: 500 })));
    await expect(apiFetch('/x')).rejects.toMatchObject({
      status: 500,
      code: 'UNKNOWN',
      message: 'Có lỗi xảy ra (HTTP 500)',
    });
  });

  it('204 trả undefined', async () => {
    server.use(http.delete('http://localhost/api/x', () => new HttpResponse(null, { status: 204 })));
    await expect(apiFetch('/x', { method: 'DELETE' })).resolves.toBeUndefined();
  });
});
