import type { User } from '../api/types';

// eslint-disable-next-line no-control-regex
const UNSAFE = /[\u0000-\u001F\u007F\\]/;

/** Chỉ chấp nhận đường dẫn nội bộ; chặn `//host`, gạch ngược, ký tự điều khiển, URL tuyệt đối. */
export function resolveAfterLogin(user: User, next: string | null): string {
  if (next && !UNSAFE.test(next) && next.startsWith('/') && !next.startsWith('//')) return next;
  return user.role === 'admin' ? '/quan-tri' : '/tai-khoan';
}
