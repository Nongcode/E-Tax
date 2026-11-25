import dayjs from 'dayjs';
import type { DbState } from './mockDb';

/** Dữ liệu mẫu. Mật khẩu lưu thô, chỉ dùng cho giả lập. */
export function createSeed(now: Date): DbState {
  const iso = (d: dayjs.Dayjs) => d.toDate().toISOString();
  const createdAt = iso(dayjs(now).subtract(9, 'month'));
  const paidDay = dayjs(now).subtract(8, 'month');
  const paidAt = iso(paidDay);
  const pendingDay = dayjs(now).subtract(1, 'hour');

  return {
    version: 1,
    users: [
      {
        id: 'u-admin',
        email: 'admin@etax.vn',
        password: 'Admin@123',
        fullName: 'Quản trị viên',
        phone: '0900000000',
        companyName: null,
        taxCode: null,
        role: 'admin',
        status: 'active',
        createdAt,
      },
      {
        id: 'u-khach',
        email: 'khach@etax.vn',
        password: 'Khach@123',
        fullName: 'Nguyễn Văn An',
        phone: '0912345678',
        companyName: 'Công ty TNHH Kế toán An Phát',
        taxCode: '0101234567',
        role: 'customer',
        status: 'active',
        createdAt,
      },
    ],
    plans: [
      {
        id: 'p3',
        name: 'Gói 3 tháng',
        description: 'Dùng thử ngắn hạn cho một máy kế toán.',
        durationMonths: 3,
        maxMachines: 1,
        maxMachineChanges: 1,
        price: 300000,
        featured: false,
        active: true,
        sortOrder: 1,
      },
      {
        id: 'p6',
        name: 'Gói 6 tháng',
        description: 'Phù hợp doanh nghiệp nhỏ dùng trong nửa năm.',
        durationMonths: 6,
        maxMachines: 1,
        maxMachineChanges: 2,
        price: 500000,
        featured: false,
        active: true,
        sortOrder: 2,
      },
      {
        id: 'p12',
        name: 'Gói 12 tháng',
        description: 'Lựa chọn phổ biến nhất, dùng được trên hai máy.',
        durationMonths: 12,
        maxMachines: 2,
        maxMachineChanges: 3,
        price: 900000,
        featured: true,
        active: true,
        sortOrder: 3,
      },
      {
        id: 'plife',
        name: 'Trọn đời',
        description: 'Mua một lần, dùng vĩnh viễn trên ba máy.',
        durationMonths: null,
        maxMachines: 3,
        maxMachineChanges: 5,
        price: 2500000,
        featured: false,
        active: true,
        sortOrder: 4,
      },
    ],
    orders: [
      {
        id: 'o-1',
        code: 'ET903311',
        userId: 'u-khach',
        planId: 'p12',
        planSnapshot: { name: 'Gói 12 tháng', durationMonths: 12, maxMachines: 2, maxMachineChanges: 3, price: 900000 },
        amount: 900000,
        status: 'paid',
        createdAt: paidAt,
        expiresAt: iso(paidDay.add(72, 'hour')),
        paidAt,
        confirmedBy: 'u-admin',
        note: null,
      },
      {
        id: 'o-2',
        code: 'ET482915',
        userId: 'u-khach',
        planId: 'p6',
        planSnapshot: { name: 'Gói 6 tháng', durationMonths: 6, maxMachines: 1, maxMachineChanges: 2, price: 500000 },
        amount: 500000,
        status: 'pending',
        createdAt: iso(pendingDay),
        expiresAt: iso(pendingDay.add(72, 'hour')),
        paidAt: null,
        confirmedBy: null,
        note: null,
      },
    ],
    licenses: [
      {
        id: 'l-1',
        orderId: 'o-1',
        userId: 'u-khach',
        planName: 'Gói 12 tháng',
        maxMachines: 2,
        maxMachineChanges: 3,
        machineChangesUsed: 0,
        startsAt: paidAt,
        expiresAt: iso(paidDay.add(12, 'month')),
        status: 'active',
      },
    ],
    activations: [
      {
        id: 'a-1',
        licenseId: 'l-1',
        machineId: '3FA2C91B7E0D4A55',
        machineName: 'Máy kế toán 1',
        key: 'ETAX-MOCK-3FA2C91B7E0D4A55-l-1',
        createdAt: paidAt,
        revokedAt: null,
      },
    ],
    bank: { bankBin: '970436', bankName: 'Vietcombank', accountNo: '0071000123456', accountName: 'CONG TY TNHH ETAX' },
    app: {
      version: '1.4.0',
      downloadUrl: '/downloads/E-Tax-1.4.0.zip',
      releasedAt: iso(dayjs(now).subtract(6, 'day')),
      notes: 'Giao diện mới, thanh trạng thái tổng tiền.',
    },
  };
}
