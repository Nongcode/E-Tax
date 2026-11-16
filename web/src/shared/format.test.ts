import dayjs from 'dayjs';
import { describe, expect, it } from 'vitest';
import { formatDate, formatDateTime, formatVnd } from './format';

describe('formatVnd', () => {
  it('định dạng tiền VND với dấu chấm và ký hiệu ₫', () => {
    expect(formatVnd(1200000)).toBe('1.200.000 ₫');
    expect(formatVnd(0)).toBe('0 ₫');
  });
});

describe('formatDate', () => {
  it('định dạng DD/MM/YYYY theo giờ địa phương', () => {
    expect(formatDate('2026-03-02T00:00:00.000Z')).toBe(
      dayjs('2026-03-02T00:00:00.000Z').format('DD/MM/YYYY'),
    );
  });

  it('null trả về "Trọn đời"', () => {
    expect(formatDate(null)).toBe('Trọn đời');
  });

  it('undefined trả về "Trọn đời"', () => {
    expect(formatDate(undefined)).toBe('Trọn đời');
  });
});

describe('formatDateTime', () => {
  it('định dạng DD/MM/YYYY HH:mm theo giờ địa phương', () => {
    expect(formatDateTime('2026-03-02T08:05:00.000Z')).toBe(
      dayjs('2026-03-02T08:05:00.000Z').format('DD/MM/YYYY HH:mm'),
    );
  });
});
