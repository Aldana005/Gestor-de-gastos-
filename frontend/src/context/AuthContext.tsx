import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { getStoredToken, clearStoredAuth } from '../api/client';
import { apiService } from '../api/apiService';
import { AuthLoginResponse } from '../api/types';

interface AuthContextType {
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
  login: (usuario: string, contrasena: string) => Promise<AuthLoginResponse>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(getStoredToken());
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handleAuthChange = () => {
      setToken(getStoredToken());
    };

    window.addEventListener('storage', handleAuthChange);
    window.addEventListener('auth-change', handleAuthChange);

    return () => {
      window.removeEventListener('storage', handleAuthChange);
      window.removeEventListener('auth-change', handleAuthChange);
    };
  }, []);

  const login = useCallback(async (usuario: string, contrasena: string) => {
    setLoading(true);
    setError(null);
    try {
      const res = await apiService.auth.login(usuario, contrasena);
      setToken(res.token);
      return res;
    } catch (err: any) {
      const msg = err.message || 'Error al iniciar sesión';
      setError(msg);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    apiService.auth.logout();
    clearStoredAuth();
    setToken(null);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        token,
        isAuthenticated: Boolean(token),
        loading,
        error,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
