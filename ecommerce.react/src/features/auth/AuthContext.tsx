import { createContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import type { User } from './types';
import { authApi } from './api/authApi';
import { setMemoryAccessToken } from '../../shared/lib/axios';

export interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: Record<string, string>) => Promise<void>;
  register: (credentials: Record<string, string>) => Promise<void>;
  logout: () => Promise<void>;
}

// eslint-disable-next-line react-refresh/only-export-components
export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const initAuth = async () => {
      try {
        // If there's a refresh token, trying to get me will trigger a refresh via interceptor
        // if we don't have an access token.
        const hasRefreshToken = !!localStorage.getItem('refreshToken');
        if (hasRefreshToken) {
          const userData = await authApi.getMe();
          setUser(userData);
        }
      } catch (error) {
        console.error('Failed to initialize auth', error);
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    };

    void initAuth();

    const handleUnauthorized = () => {
      setUser(null);
    };

    window.addEventListener('auth:unauthorized', handleUnauthorized);
    return () => {
      window.removeEventListener('auth:unauthorized', handleUnauthorized);
    };
  }, []);

  const login = async (credentials: Record<string, string>) => {
    const data = await authApi.login(credentials);
    setMemoryAccessToken(data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    
    // Fetch user after login
    const userData = await authApi.getMe();
    setUser(userData);
  };

  const register = async (credentials: Record<string, string>) => {
    await authApi.register(credentials);
    // After register, you typically need to login because register returns User, not tokens
    // OR if the API returns a User but no tokens, we force the user to login.
    // The API returned UserResponse.
  };

  const logout = async () => {
    await authApi.logout();
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};