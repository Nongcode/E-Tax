import type { User } from '../api/types';

const KEY = 'etax-session';

export interface Session {
  token: string;
  user: User;
}

export function loadSession(): Session | null {
  const raw = localStorage.getItem(KEY);
  if (raw === null) return null;
  try {
    const v: unknown = JSON.parse(raw);
    if (
      typeof v === 'object' &&
      v !== null &&
      typeof (v as Session).token === 'string' &&
      typeof (v as Session).user === 'object' &&
      (v as Session).user !== null
    ) {
      return v as Session;
    }
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
