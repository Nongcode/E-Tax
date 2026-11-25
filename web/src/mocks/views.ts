import type { Order, OrderStatus, User } from '../api/types';

/** R5: đơn pending quá hạn được coi là expired khi đọc, không ghi lại. */
export function effectiveOrderStatus(o: Order, now: Date): OrderStatus {
  return o.status === 'pending' && new Date(o.expiresAt).getTime() < now.getTime() ? 'expired' : o.status;
}

/** Bỏ mật khẩu trước khi trả user ra ngoài. */
export function toPublicUser(u: User & { password: string }): User {
  const pub: User & { password?: string } = { ...u };
  delete pub.password;
  return pub;
}
