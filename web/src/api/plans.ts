import { apiFetch } from './client';
import type { AppRelease, Plan } from './types';

export const getPlans = () => apiFetch<Plan[]>('/plans');
export const getAppLatest = () => apiFetch<AppRelease>('/app/latest');
