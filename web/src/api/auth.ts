import { apiFetch } from './client';
import type { AuthResponse, ChangePasswordInput, LoginInput, ProfileInput, RegisterInput, User } from './types';

export const register = (i: RegisterInput) => apiFetch<AuthResponse>('/auth/register', { method: 'POST', body: i });
export const login = (i: LoginInput) => apiFetch<AuthResponse>('/auth/login', { method: 'POST', body: i });
export const getMe = () => apiFetch<User>('/auth/me');
export const forgotPassword = (email: string) =>
  apiFetch<void>('/auth/forgot-password', { method: 'POST', body: { email } });
export const updateProfile = (i: ProfileInput) => apiFetch<User>('/auth/profile', { method: 'PUT', body: i });
export const changePassword = (i: ChangePasswordInput) =>
  apiFetch<void>('/auth/change-password', { method: 'POST', body: i });
