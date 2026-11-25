import { apiFetch } from './client';
import type { Order, OrderDetail } from './types';

export const createOrder = (planId: string) => apiFetch<OrderDetail>('/orders', { method: 'POST', body: { planId } });
export const getOrders = () => apiFetch<Order[]>('/orders');
export const getOrder = (id: string) => apiFetch<OrderDetail>(`/orders/${encodeURIComponent(id)}`);
export const cancelOrder = (id: string) => apiFetch<Order>(`/orders/${encodeURIComponent(id)}/cancel`, { method: 'POST' });
