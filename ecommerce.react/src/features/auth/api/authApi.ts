import { apiClient, clearAuth } from '../../../shared/lib/axios';
import type { AuthResponse, User } from '../types';

export const authApi = {
  login: async (credentials: Record<string, string>): Promise<AuthResponse> => {
    const { data } = await apiClient.post<AuthResponse>('/api/Auth/login', credentials);
    return data;
  },

  register: async (credentials: Record<string, string>): Promise<User> => {
    const { data } = await apiClient.post<User>('/api/Auth/register', credentials);
    return data;
  },

  getMe: async (): Promise<User> => {
    const { data } = await apiClient.get<User>('/api/Auth/me');
    return data;
  },

  logout: async (): Promise<void> => {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      try {
        await apiClient.post('/api/Auth/logout', { refreshToken });
      } catch (error) {
        console.error('Logout failed', error);
      }
    }
    clearAuth();
  },
};