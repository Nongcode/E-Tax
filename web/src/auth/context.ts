import { createContext, useContext } from 'react';
import type { LoginInput, RegisterInput, User } from '../api/types';

export interface AuthContextValue {
  user: User | null;
  login(i: LoginInput): Promise<User>;
  register(i: RegisterInput): Promise<User>;
  logout(): void;
  refreshMe(): Promise<void>;
  setUser(u: User): void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth phải nằm trong AuthProvider');
  return ctx;
}
