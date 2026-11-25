export const MACHINE_ID_MESSAGE = 'Mã máy phải gồm 16 ký tự 0-9, A-F';

/** Bỏ mọi khoảng trắng rồi viết HOA */
export function normalizeMachineId(s: string): string {
  return s.replace(/\s/g, '').toUpperCase();
}

export function isValidMachineId(s: string): boolean {
  return /^[0-9A-F]{16}$/.test(normalizeMachineId(s));
}

/** Mã số thuế: 10 số, hoặc 10 số + "-" + 3 số chi nhánh */
export function isValidTaxCode(s: string): boolean {
  return /^\d{10}(-\d{3})?$/.test(s);
}

/** Số điện thoại: 10 số, bắt đầu bằng 0 */
export function isValidPhone(s: string): boolean {
  return /^0\d{9}$/.test(s);
}
