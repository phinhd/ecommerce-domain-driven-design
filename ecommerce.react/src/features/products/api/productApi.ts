import { apiClient } from '../../../shared/lib/axios';
import type { Product, PagedResponse } from '../types';

export const productApi = {
  getProducts: async (params: { page: number; pageSize: number; search?: string; mine?: boolean; excludeMine?: boolean }): Promise<PagedResponse<Product>> => {
    const queryParams: Record<string, string | number | boolean> = { page: params.page, pageSize: params.pageSize };
    if (params.search?.trim()) {
      queryParams.search = params.search.trim();
    }
    if (params.mine !== undefined) queryParams.mine = params.mine;
    if (params.excludeMine !== undefined) queryParams.excludeMine = params.excludeMine;
    const { data } = await apiClient.get<PagedResponse<Product>>('/api/products', { params: queryParams });
    return data;
  },

  createProduct: async (product: Omit<Product, 'id' | 'createdByUserId'>): Promise<Product> => {
    const { data } = await apiClient.post<Product>('/api/products', product);
    return data;
  },

  updateProduct: async (id: string, product: Partial<Product>): Promise<Product> => {
    const { data } = await apiClient.put<Product>(`/api/products/${id}`, product);
    return data;
  },

  deleteProduct: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/products/${id}`);
  },
};