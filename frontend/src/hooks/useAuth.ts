import { useState, useEffect, useCallback } from 'react';
import { getStoredToken, clearStoredAuth } from '../api/client';
import { apiService } from '../api/apiService';

export function useAuth() {
  const [token, setToken] = useState<string | null>(getStoredToken());
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handleStorage = () => {
      setToken(getStoredToken());
    };
    window.addEventListener('storage', handleStorage);
    return () => window.removeEventListener('storage', handleStorage);
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
    window.location.href = '/login';
  }, []);

  return {
    isAuthenticated: Boolean(token),
    token,
    loading,
    error,
    login,
    logout,
  };
}
