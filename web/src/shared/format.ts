import dayjs from 'dayjs';

const vndFormatter = new Intl.NumberFormat('vi-VN');

/** Tiền VND: 1200000 -> "1.200.000 ₫" */
export function formatVnd(n: number): string {
  return `${vndFormatter.format(n)} ₫`;
}

/** Ngày DD/MM/YYYY theo giờ địa phương; null/undefined -> "Trọn đời" */
export function formatDate(iso: string | null | undefined): string {
  if (iso == null) return 'Trọn đời';
  return dayjs(iso).format('DD/MM/YYYY');
}

/** Ngày giờ DD/MM/YYYY HH:mm theo giờ địa phương */
export function formatDateTime(iso: string): string {
  return dayjs(iso).format('DD/MM/YYYY HH:mm');
}
