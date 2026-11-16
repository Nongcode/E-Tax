import { describe, expect, it } from 'vitest';
import {
  MACHINE_ID_MESSAGE,
  isValidMachineId,
  isValidPhone,
  isValidTaxCode,
  normalizeMachineId,
} from './validators';

describe('normalizeMachineId', () => {
  it('bỏ khoảng trắng, xuống dòng và viết HOA', () => {
    expect(normalizeMachineId(' 3fa2 c91b7e0d4a55\n')).toBe('3FA2C91B7E0D4A55');
  });
});

describe('isValidMachineId', () => {
  it('chấp nhận mã 16 ký tự hex sau khi chuẩn hoá', () => {
    expect(isValidMachineId(' 3fa2c91b7e0d4a55\n')).toBe(true);
  });

  it('từ chối mã có gạch ngang', () => {
    expect(isValidMachineId('3FA2-C91B-7E0D-4A55')).toBe(false);
  });

  it('từ chối mã 15 ký tự', () => {
    expect(isValidMachineId('3FA2C91B7E0D4A5')).toBe(false);
  });

  it('từ chối ký tự ngoài 0-9, A-F', () => {
    expect(isValidMachineId('3FA2C91B7E0D4A5G')).toBe(false);
  });

  it('có thông báo lỗi chuẩn', () => {
    expect(MACHINE_ID_MESSAGE).toBe('Mã máy phải gồm 16 ký tự 0-9, A-F');
  });
});

describe('isValidTaxCode', () => {
  it('chấp nhận MST 10 số', () => {
    expect(isValidTaxCode('0101234567')).toBe(true);
  });

  it('chấp nhận MST dạng 10 số + -001', () => {
    expect(isValidTaxCode('0101234567-001')).toBe(true);
  });

  it('từ chối MST 9 số', () => {
    expect(isValidTaxCode('010123456')).toBe(false);
  });
});

describe('isValidPhone', () => {
  it('chấp nhận SĐT 10 số bắt đầu bằng 0', () => {
    expect(isValidPhone('0912345678')).toBe(true);
  });

  it('từ chối SĐT không có số 0 đầu', () => {
    expect(isValidPhone('912345678')).toBe(false);
  });
});
