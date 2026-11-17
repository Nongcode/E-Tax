import dayjs from 'dayjs';
import type {
  Activation,
  AdminLicense,
  AdminLicenseDetail,
  License,
  LicenseDetail,
  LicenseStatus,
  User,
} from '../api/types';
import { isValidMachineId, MACHINE_ID_MESSAGE, normalizeMachineId } from '../shared/validators';
import { MockError, validationError } from './errors';
import type { DbState } from './mockDb';

export type StoredLicense = DbState['licenses'][number];

const notFound = () => new MockError(404, 'NOT_FOUND', 'Không tìm thấy dữ liệu');

/** R7: license active có expiresAt < now thì được coi là expired khi đọc. */
export function effectiveLicenseStatus(l: StoredLicense, now: Date): LicenseStatus {
  const expired = l.status === 'active' && l.expiresAt !== null && new Date(l.expiresAt).getTime() < now.getTime();
  return expired ? 'expired' : l.status;
}

/** Dựng License công khai: trạng thái suy ra (R7) và activeMachines tính khi đọc. */
export function toPublicLicense(s: DbState, l: StoredLicense, now: Date): License {
  return {
    ...l,
    status: effectiveLicenseStatus(l, now),
    activeMachines: s.activations.filter((a) => a.licenseId === l.id && a.revokedAt === null).length,
  };
}

/** R15: license của chính user; của người khác hoặc không có đều NOT_FOUND. */
function findOwnLicense(s: DbState, userId: string, id: string): StoredLicense {
  const l = s.licenses.find((x) => x.id === id && x.userId === userId);
  if (!l) throw notFound();
  return l;
}

function findLicense(s: DbState, id: string): StoredLicense {
  const l = s.licenses.find((x) => x.id === id);
  if (!l) throw notFound();
  return l;
}

export function listLicenses(s: DbState, now: Date, userId: string): License[] {
  return s.licenses.filter((l) => l.userId === userId).map((l) => toPublicLicense(s, l, now));
}

export function getLicense(s: DbState, now: Date, userId: string, id: string): LicenseDetail {
  const l = findOwnLicense(s, userId, id);
  return {
    ...toPublicLicense(s, l, now),
    activations: s.activations.filter((a) => a.licenseId === l.id && a.revokedAt === null),
  };
}

export function addActivation(
  s: DbState,
  now: Date,
  userId: string,
  licenseId: string,
  i: { machineId: string; machineName: string },
): Activation {
  const l = findOwnLicense(s, userId, licenseId);
  if (!isValidMachineId(i.machineId)) throw new MockError(400, 'INVALID_MACHINE_ID', MACHINE_ID_MESSAGE);
  const machineId = normalizeMachineId(i.machineId);
  const machineName = typeof i.machineName === 'string' ? i.machineName.trim() : '';
  if (!machineName) throw new MockError(400, 'INVALID_MACHINE_NAME', 'Vui lòng nhập tên máy');
  // Thứ tự theo bảng R: R9 (inactive) trước R10 (máy đã có) trước R11 (hết chỗ).
  if (effectiveLicenseStatus(l, now) !== 'active') {
    throw new MockError(409, 'LICENSE_INACTIVE', 'Bản quyền đã hết hạn hoặc bị thu hồi');
  }
  const live = s.activations.filter((a) => a.licenseId === l.id && a.revokedAt === null);
  const existing = live.find((a) => a.machineId === machineId);
  if (existing) return existing;
  if (live.length >= l.maxMachines) {
    throw new MockError(409, 'MACHINE_LIMIT', 'Đã dùng hết số máy của gói, hãy gỡ một máy trước');
  }
  const act: Activation = {
    id: crypto.randomUUID(),
    licenseId: l.id,
    machineId,
    machineName,
    key: `ETAX-MOCK-${machineId}-${l.id}`,
    createdAt: now.toISOString(),
    revokedAt: null,
  };
  s.activations.push(act);
  return act;
}

export function removeActivation(s: DbState, now: Date, userId: string, licenseId: string, actId: string): void {
  const l = findOwnLicense(s, userId, licenseId);
  const act = s.activations.find((a) => a.id === actId && a.licenseId === l.id && a.revokedAt === null);
  if (!act) throw notFound();
  if (l.machineChangesUsed >= l.maxMachineChanges) {
    throw new MockError(409, 'CHANGE_LIMIT', 'Đã hết số lần đổi máy của gói');
  }
  act.revokedAt = now.toISOString();
  l.machineChangesUsed += 1;
}

export function adminListLicenses(s: DbState, now: Date, f: { status?: LicenseStatus; q?: string }): AdminLicense[] {
  const q = f.q?.trim().toLowerCase();
  const qMachine = f.q ? normalizeMachineId(f.q) : '';
  return s.licenses.flatMap((l) => {
    const view = toPublicLicense(s, l, now);
    if (f.status && view.status !== f.status) return [];
    const u = s.users.find((x) => x.id === l.userId);
    if (q) {
      const code = s.orders.find((o) => o.id === l.orderId)?.code ?? '';
      const machineHit =
        qMachine !== '' &&
        s.activations.some((a) => a.licenseId === l.id && a.revokedAt === null && a.machineId.includes(qMachine));
      const hit = (u?.email ?? '').toLowerCase().includes(q) || code.toLowerCase().includes(q) || machineHit;
      if (!hit) return [];
    }
    return [{ ...view, userEmail: u?.email ?? '', userFullName: u?.fullName ?? '' }];
  });
}

export function adminGetLicense(s: DbState, now: Date, id: string): AdminLicenseDetail {
  const l = findLicense(s, id);
  const u = s.users.find((x) => x.id === l.userId);
  if (!u) throw notFound();
  const user: User & { password?: string } = { ...u };
  delete user.password;
  return {
    ...toPublicLicense(s, l, now),
    activations: s.activations.filter((a) => a.licenseId === l.id),
    user,
  };
}

export function revokeLicense(s: DbState, now: Date, id: string): License {
  const l = findLicense(s, id);
  l.status = 'revoked';
  for (const a of s.activations) {
    if (a.licenseId === l.id && a.revokedAt === null) a.revokedAt = now.toISOString();
  }
  return toPublicLicense(s, l, now);
}

export function extendLicense(s: DbState, now: Date, id: string, months: number): License {
  if (!Number.isInteger(months) || months < 1 || months > 120) throw validationError('Số tháng gia hạn không hợp lệ');
  const l = findLicense(s, id);
  // R22: bản quyền đã thu hồi không gia hạn được.
  if (l.status === 'revoked') throw new MockError(409, 'LICENSE_REVOKED', 'Bản quyền đã bị thu hồi, không thể gia hạn');
  if (l.expiresAt === null) throw new MockError(409, 'LICENSE_PERMANENT', 'Bản quyền trọn đời không cần gia hạn');
  const base = Math.max(new Date(l.expiresAt).getTime(), now.getTime());
  l.expiresAt = dayjs(base).add(months, 'month').toISOString();
  return toPublicLicense(s, l, now);
}
