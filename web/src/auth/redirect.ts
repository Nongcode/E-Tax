import type { User } from '../api/types';

/** Chỉ chấp nhận đường dẫn nội bộ; chặn `//host`, `/\host`, URL tuyệt đối. */
export function resolveAfterLogin(user: User, next: string | null): string {
  if (next && next.startsWith('/') && !next.startsWith('//') && !next.startsWith('/\\')) return next;
  return user.role === 'admin' ? '/quan-tri' : '/tai-khoan';
}
