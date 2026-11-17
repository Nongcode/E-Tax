import type { User } from '../api/types';

const KEY = 'etax-session';

export interface Session {
  token: string;
  user: User;
}

function isSession(v: unknown): v is Session {
  if (typeof v !== 'object' || v === null) return false;
  const { token, user } = v as Partial<Session>;
  if (typeof token !== 'string' || typeof user !== 'object' || user === null) return false;
  return (
    typeof user.id === 'string' &&
    typeof user.email === 'string' &&
    typeof user.fullName === 'string' &&
    (user.role === 'customer' || user.role === 'admin')
  );
}

export function loadSession(): Session | null {
  const raw = localStorage.getItem(KEY);
  if (raw === null) return null;
  try {
    const v: unknown = JSON.parse(raw);
    if (isSession(v)) return v;
  } catch {
    // rơi xuống xoá key
  }
  localStorage.removeItem(KEY);
  return null;
}

export function saveSession(s: Session): void {
  localStorage.setItem(KEY, JSON.stringify(s));
}

export function clearSession(): void {
  localStorage.removeItem(KEY);
}
