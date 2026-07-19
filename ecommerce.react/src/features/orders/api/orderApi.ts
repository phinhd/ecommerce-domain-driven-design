import { apiClient } from '../../../shared/lib/axios';
import type { Order } from '../types';

export const orderApi = {
  createOrder: async (order: { productId: string; quantity: number }): Promise<Order> => {
    const { data } = await apiClient.post<Order>('/api/orders', order);
    return data;
  },
  getMyOrders: async (): Promise<Order[]> => {
    const { data } = await apiClient.get<Order[]>('/api/orders/my');
    return data;
  },
};