import type { ApiErrorBody } from './types';

export class ApiError extends Error {
  status: number;
  code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
  }
}

interface ClientOptions {
  getToken: () => string | null;
  onUnauthorized: () => void;
}

let options: ClientOptions = { getToken: () => null, onUnauthorized: () => {} };

export function configureClient(opts: ClientOptions): void {
  options = opts;
}

interface FetchInit {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
}

function isErrorBody(v: unknown): v is ApiErrorBody {
  return (
    typeof v === 'object' &&
    v !== null &&
    typeof (v as ApiErrorBody).code === 'string' &&
    typeof (v as ApiErrorBody).message === 'string'
  );
}

export async function apiFetch<T>(path: string, init: FetchInit = {}): Promise<T> {
  const token = options.getToken();
  const headers: Record<string, string> = {};
  if (init.body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers['Authorization'] = `Bearer ${token}`;

  let res: Response;
  try {
    res = await fetch((import.meta.env.VITE_API_URL ?? '') + '/api' + path, {
      method: init.method ?? 'GET',
      headers,
      body: init.body !== undefined ? JSON.stringify(init.body) : undefined,
    });
  } catch {
    throw new ApiError(0, 'NETWORK', 'Không kết nối được máy chủ');
  }

  if (res.ok) {
    if (res.status === 204) return undefined as T;
    return (await res.json()) as T;
  }

  let body: unknown;
  try {
    body = await res.json();
  } catch {
    body = undefined;
  }
  // Chỉ coi là "phiên hết hạn" khi request đã gửi kèm token.
  if (res.status === 401 && token) options.onUnauthorized();
  if (isErrorBody(body)) throw new ApiError(res.status, body.code, body.message);
  throw new ApiError(res.status, 'UNKNOWN', `Có lỗi xảy ra (HTTP ${res.status})`);
}
