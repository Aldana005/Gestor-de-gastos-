import { ProblemDetails } from './types';

const TOKEN_KEY = 'gastito_auth_token';
const EXPIRY_KEY = 'gastito_token_expiry';

export const getStoredToken = (): string | null => {
  return localStorage.getItem(TOKEN_KEY);
};

export const setStoredAuth = (token: string, expiraEnUtc: string) => {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(EXPIRY_KEY, expiraEnUtc);
  window.dispatchEvent(new Event('auth-change'));
};

export const clearStoredAuth = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EXPIRY_KEY);
  window.dispatchEvent(new Event('auth-change'));
};

export class ApiError extends Error {
  public status: number;
  public problemDetails?: ProblemDetails;

  constructor(status: number, message: string, problemDetails?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problemDetails = problemDetails;
  }
}

const getApiBaseUrl = (): string => {
  const envUrl = import.meta.env.VITE_API_BASE_URL;
  if (!envUrl || envUrl === 'mock') {
    return '/api/v1';
  }
  return envUrl.endsWith('/') ? envUrl.slice(0, -1) : envUrl;
};

export async function apiClient<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const baseUrl = getApiBaseUrl();
  const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
  const url = `${baseUrl}${cleanEndpoint}`;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> || {}),
  };

  const token = getStoredToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    clearStoredAuth();
    // Redirigir al login si no estamos ya allí
    if (window.location.pathname !== '/login') {
      window.location.href = '/login';
    }
    throw new ApiError(401, 'Sesión expirada o no autorizada');
  }

  if (response.status === 204) {
    return {} as T;
  }

  let data: any = null;
  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    data = await response.json();
  }

  if (!response.ok) {
    const problem: ProblemDetails = data || {};
    let message = problem.detail;
    if (!message && problem.errors) {
      message = Object.entries(problem.errors)
        .map(([_, errs]) => (Array.isArray(errs) ? errs.join(', ') : String(errs)))
        .join('. ');
    }
    if (!message) {
      message = problem.title || `Error HTTP ${response.status}`;
    }
    throw new ApiError(response.status, message, problem);
  }

  return data as T;
}
