import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { ApiError, apiFetch, configureClient, shouldRetry } from './client';

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

  it('401 muộn sau khi token đã đổi (đăng xuất) KHÔNG gọi onUnauthorized', async () => {
    server.use(
      http.get('http://localhost/api/ping', () =>
        HttpResponse.json({ code: 'UNAUTHORIZED', message: 'Phiên hết hạn' }, { status: 401 }),
      ),
    );
    let token: string | null = 'abc';
    const onUnauthorized = vi.fn();
    configureClient({ getToken: () => token, onUnauthorized });
    const p = apiFetch('/ping');
    token = null;
    await expect(p).rejects.toBeInstanceOf(ApiError);
    expect(onUnauthorized).not.toHaveBeenCalled();
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

  it('2xx không phải JSON thành ApiError UNKNOWN', async () => {
    server.use(http.get('http://localhost/api/ping', () => HttpResponse.text('<html>proxy</html>')));
    await expect(apiFetch('/ping')).rejects.toMatchObject({
      status: 200,
      code: 'UNKNOWN',
      message: 'Phản hồi không hợp lệ từ máy chủ',
    });
  });
});

describe('shouldRetry', () => {
  it('không thử lại lỗi 4xx, lỗi khác thử lại 1 lần', () => {
    expect(shouldRetry(0, new ApiError(400, 'X', 'x'))).toBe(false);
    expect(shouldRetry(0, new ApiError(404, 'X', 'x'))).toBe(false);
    expect(shouldRetry(0, new ApiError(499, 'X', 'x'))).toBe(false);
    expect(shouldRetry(0, new ApiError(500, 'X', 'x'))).toBe(true);
    expect(shouldRetry(0, new ApiError(0, 'NETWORK', 'x'))).toBe(true);
    expect(shouldRetry(0, new Error('x'))).toBe(true);
    expect(shouldRetry(1, new ApiError(500, 'X', 'x'))).toBe(false);
  });
});
