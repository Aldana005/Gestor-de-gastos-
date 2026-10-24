import { apiClient, setStoredAuth, clearStoredAuth } from './client';
import { mockService } from './mockService';
import {
  AuthLoginResponse,
  CategoryCreateInput,
  CategoryOutput,
  DashboardOutput,
  PagedResult,
  PagoCuotaOutput,
  PendienteItemOutput,
  ReporteResumenOutput,
  TarjetaCreateInput,
  TarjetaOutput,
  TipoMovimiento,
  TransactionCreateInput,
  TransactionOutput,
  TransactionUpdateInput,
  AplicarDesde,
} from './types';

export const isUsingMock = (): boolean => {
  const envUrl = import.meta.env.VITE_API_BASE_URL;
  const forceMock = import.meta.env.VITE_USE_MOCKS;
  if (forceMock === 'true' || forceMock === true) return true;
  if (!envUrl || envUrl === '' || envUrl === 'mock') return true;
  return false;
};

export const apiService = {
  isMock: isUsingMock,

  auth: {
    login: async (usuario: string, contrasena: string): Promise<AuthLoginResponse> => {
      if (isUsingMock()) {
        const res = await mockService.login(usuario, contrasena);
        setStoredAuth(res.token, res.expiraEnUtc);
        return res;
      }
      const res = await apiClient<AuthLoginResponse>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ usuario, contrasena }),
      });
      setStoredAuth(res.token, res.expiraEnUtc);
      return res;
    },
    logout: () => {
      clearStoredAuth();
    },
  },

  categorias: {
    getAll: async (tipo?: TipoMovimiento): Promise<CategoryOutput[]> => {
      if (isUsingMock()) {
        return mockService.getCategorias(tipo);
      }
      const q = tipo ? `?tipo=${tipo}` : '';
      return apiClient<CategoryOutput[]>(`/categorias${q}`);
    },
    create: async (input: CategoryCreateInput): Promise<CategoryOutput> => {
      if (isUsingMock()) {
        return mockService.createCategoria(input);
      }
      return apiClient<CategoryOutput>('/categorias', {
        method: 'POST',
        body: JSON.stringify(input),
      });
    },
    update: async (id: number, input: CategoryCreateInput): Promise<CategoryOutput> => {
      if (isUsingMock()) {
        return mockService.updateCategoria(id, input);
      }
      return apiClient<CategoryOutput>(`/categorias/${id}`, {
        method: 'PUT',
        body: JSON.stringify(input),
      });
    },
    delete: async (id: number): Promise<void> => {
      if (isUsingMock()) {
        return mockService.deleteCategoria(id);
      }
      return apiClient<void>(`/categorias/${id}`, {
        method: 'DELETE',
      });
    },
  },

  tarjetas: {
    getAll: async (): Promise<TarjetaOutput[]> => {
      if (isUsingMock()) {
        return mockService.getTarjetas();
      }
      return apiClient<TarjetaOutput[]>('/tarjetas');
    },
    create: async (input: TarjetaCreateInput): Promise<TarjetaOutput> => {
      if (isUsingMock()) {
        return mockService.createTarjeta(input);
      }
      return apiClient<TarjetaOutput>('/tarjetas', {
        method: 'POST',
        body: JSON.stringify(input),
      });
    },
    update: async (id: number, input: TarjetaCreateInput): Promise<TarjetaOutput> => {
      if (isUsingMock()) {
        return mockService.updateTarjeta(id, input);
      }
      return apiClient<TarjetaOutput>(`/tarjetas/${id}`, {
        method: 'PUT',
        body: JSON.stringify(input),
      });
    },
    delete: async (id: number): Promise<void> => {
      if (isUsingMock()) {
        return mockService.deleteTarjeta(id);
      }
      return apiClient<void>(`/tarjetas/${id}`, {
        method: 'DELETE',
      });
    },
  },

  movimientos: {
    getPaged: async (params: {
      desde?: string;
      hasta?: string;
      tipo?: TipoMovimiento;
      categoriaIds?: number[];
      pagina?: number;
      tamanoPagina?: number;
    }): Promise<PagedResult<TransactionOutput>> => {
      if (isUsingMock()) {
        return mockService.getMovimientos(params);
      }
      const query = new URLSearchParams();
      if (params.desde) query.append('desde', params.desde);
      if (params.hasta) query.append('hasta', params.hasta);
      if (params.tipo) query.append('tipo', String(params.tipo));
      if (params.categoriaIds && params.categoriaIds.length > 0) {
        params.categoriaIds.forEach((id) => query.append('categoriaIds', String(id)));
      }
      if (params.pagina) query.append('pagina', String(params.pagina));
      if (params.tamanoPagina) query.append('tamanoPagina', String(params.tamanoPagina));

      const qs = query.toString() ? `?${query.toString()}` : '';
      return apiClient<PagedResult<TransactionOutput>>(`/movimientos${qs}`);
    },
    getById: async (id: number): Promise<TransactionOutput> => {
      if (isUsingMock()) {
        return mockService.getMovimientoById(id);
      }
      return apiClient<TransactionOutput>(`/movimientos/${id}`);
    },
    create: async (input: TransactionCreateInput): Promise<TransactionOutput> => {
      if (isUsingMock()) {
        return mockService.createMovimiento(input);
      }
      return apiClient<TransactionOutput>('/movimientos', {
        method: 'POST',
        body: JSON.stringify(input),
      });
    },
    update: async (
      id: number,
      input: TransactionUpdateInput,
      aplicarDesde?: AplicarDesde
    ): Promise<TransactionOutput> => {
      if (isUsingMock()) {
        return mockService.updateMovimiento(id, input, aplicarDesde);
      }
      const qs = aplicarDesde ? `?aplicarDesde=${aplicarDesde}` : '';
      return apiClient<TransactionOutput>(`/movimientos/${id}${qs}`, {
        method: 'PUT',
        body: JSON.stringify({ ...input, aplicarDesde }),
      });
    },
    delete: async (id: number): Promise<void> => {
      if (isUsingMock()) {
        return mockService.deleteMovimiento(id);
      }
      return apiClient<void>(`/movimientos/${id}`, {
        method: 'DELETE',
      });
    },
    adelantarCuotas: async (id: number, cantidad: number): Promise<PagoCuotaOutput> => {
      if (isUsingMock()) {
        return mockService.adelantarCuotas(id, cantidad);
      }
      return apiClient<PagoCuotaOutput>(`/movimientos/${id}/cuotas/adelantar`, {
        method: 'POST',
        body: JSON.stringify({ cantidad }),
      });
    },
    getPagos: async (id: number): Promise<PagoCuotaOutput[]> => {
      if (isUsingMock()) {
        return mockService.getPagosCuota(id);
      }
      return apiClient<PagoCuotaOutput[]>(`/movimientos/${id}/cuotas/pagos`);
    },
  },

  pendientes: {
    getAll: async (params: {
      desde?: string;
      hasta?: string;
      categoriaIds?: number[];
      montoMin?: number;
      montoMax?: number;
    } = {}): Promise<PendienteItemOutput[]> => {
      if (isUsingMock()) {
        return mockService.getPendientes(params);
      }
      const query = new URLSearchParams();
      if (params.desde) query.append('desde', params.desde);
      if (params.hasta) query.append('hasta', params.hasta);
      if (params.categoriaIds && params.categoriaIds.length > 0) {
        params.categoriaIds.forEach((id) => query.append('categoriaIds', String(id)));
      }
      if (params.montoMin !== undefined) query.append('montoMin', String(params.montoMin));
      if (params.montoMax !== undefined) query.append('montoMax', String(params.montoMax));

      const qs = query.toString() ? `?${query.toString()}` : '';
      return apiClient<PendienteItemOutput[]>(`/pendientes${qs}`);
    },
  },

  dashboard: {
    get: async (meses: number = 3): Promise<DashboardOutput> => {
      if (isUsingMock()) {
        return mockService.getDashboard(meses);
      }
      return apiClient<DashboardOutput>(`/dashboard?meses=${meses}`);
    },
  },

  reportes: {
    getResumen: async (params: {
      desde?: string;
      hasta?: string;
      categoriaIds?: number[];
    } = {}): Promise<ReporteResumenOutput> => {
      if (isUsingMock()) {
        return mockService.getReporteResumen(params);
      }
      const query = new URLSearchParams();
      if (params.desde) query.append('desde', params.desde);
      if (params.hasta) query.append('hasta', params.hasta);
      if (params.categoriaIds && params.categoriaIds.length > 0) {
        params.categoriaIds.forEach((id) => query.append('categoriaIds', String(id)));
      }
      const qs = query.toString() ? `?${query.toString()}` : '';
      return apiClient<ReporteResumenOutput>(`/reportes/resumen${qs}`);
    },
  },

  mock: {
    reset: () => {
      mockService.resetData();
    },
  },
};
