import { apiFetch } from './client';
import type { Activation, License, LicenseDetail } from './types';

export const getLicenses = () => apiFetch<License[]>('/licenses');
export const getLicense = (id: string) => apiFetch<LicenseDetail>(`/licenses/${encodeURIComponent(id)}`);
export const addActivation = (licenseId: string, i: { machineId: string; machineName: string }) =>
  apiFetch<Activation>(`/licenses/${encodeURIComponent(licenseId)}/activations`, { method: 'POST', body: i });
export const removeActivation = (licenseId: string, actId: string) =>
  apiFetch<void>(`/licenses/${encodeURIComponent(licenseId)}/activations/${encodeURIComponent(actId)}`, { method: 'DELETE' });
