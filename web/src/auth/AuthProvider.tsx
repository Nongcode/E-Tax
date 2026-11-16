import { useCallback, useMemo, useRef, useState, type ReactNode } from 'react';
import { App } from 'antd';
import { useLocation, useNavigate } from 'react-router-dom';
import * as authApi from '../api/auth';
import { configureClient } from '../api/client';
import type { LoginInput, RegisterInput, User } from '../api/types';
import { AuthContext, type AuthContextValue } from './context';
import { clearSession, loadSession, saveSession } from './session';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [initial] = useState(loadSession);
  const [user, setUserState] = useState<User | null>(initial?.user ?? null);
  const tokenRef = useRef<string | null>(initial?.token ?? null);
  const navigate = useNavigate();
  const location = useLocation();
  const { message } = App.useApp();

  const locationRef = useRef(location);
  const navigateRef = useRef(navigate);
  const messageRef = useRef(message);
  // Ref luôn trỏ giá trị mới nhất để callback đã đăng ký với client không bị cũ.
  locationRef.current = location;
  navigateRef.current = navigate;
  messageRef.current = message;

  // Cấu hình ngay khi render để request đầu tiên của effect con đã có getToken.
  configureClient({
    getToken: () => tokenRef.current,
    onUnauthorized: () => {
      clearSession();
      tokenRef.current = null;
      setUserState(null);
      const { pathname, search } = locationRef.current;
      navigateRef.current('/dang-nhap?next=' + encodeURIComponent(pathname + search));
      messageRef.current.warning('Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại');
    },
  });

  const start = useCallback((res: { token: string; user: User }) => {
    tokenRef.current = res.token;
    saveSession(res);
    setUserState(res.user);
    return res.user;
  }, []);

  const login = useCallback(async (i: LoginInput) => start(await authApi.login(i)), [start]);
  const register = useCallback(async (i: RegisterInput) => start(await authApi.register(i)), [start]);

  const logout = useCallback(() => {
    clearSession();
    tokenRef.current = null;
    setUserState(null);
    navigateRef.current('/');
  }, []);

  const setUser = useCallback((u: User) => {
    setUserState(u);
    if (tokenRef.current) saveSession({ token: tokenRef.current, user: u });
  }, []);

  const refreshMe = useCallback(async () => {
    setUser(await authApi.getMe());
  }, [setUser]);

  const value = useMemo<AuthContextValue>(
    () => ({ user, login, register, logout, refreshMe, setUser }),
    [user, login, register, logout, refreshMe, setUser],
  );
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
