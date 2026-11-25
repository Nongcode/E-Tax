import dayjs from 'dayjs';

const vndFormatter = new Intl.NumberFormat('vi-VN');

/** Tiền VND: 1200000 -> "1.200.000 ₫" */
export function formatVnd(n: number): string {
  return `${vndFormatter.format(n)} ₫`;
}

/** Ngày DD/MM/YYYY theo giờ địa phương; null/undefined -> "Trọn đời"; rỗng/sai định dạng -> "—" */
export function formatDate(iso: string | null | undefined): string {
  if (iso == null) return 'Trọn đời';
  const d = dayjs(iso);
  return iso && d.isValid() ? d.format('DD/MM/YYYY') : '—';
}

/** Ngày giờ DD/MM/YYYY HH:mm theo giờ địa phương; rỗng/sai định dạng -> "—" */
export function formatDateTime(iso: string): string {
  const d = dayjs(iso);
  return iso && d.isValid() ? d.format('DD/MM/YYYY HH:mm') : '—';
}
