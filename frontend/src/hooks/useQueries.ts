import { useQuery } from '@tanstack/react-query';
import { apiService } from '../api/apiService';
import { TipoMovimiento } from '../api/types';

export const QUERY_KEYS = {
  dashboard: (meses: number) => ['dashboard', meses] as const,
  reportesResumen: (desde?: string, hasta?: string, categoriaIds?: number[]) =>
    ['reportes', 'resumen', desde, hasta, categoriaIds] as const,
  movimientos: (params: Record<string, any>) => ['movimientos', params] as const,
  movimiento: (id: number) => ['movimiento', id] as const,
  pendientes: (params: Record<string, any>) => ['pendientes', params] as const,
  categorias: (tipo?: TipoMovimiento) => ['categorias', tipo] as const,
  tarjetas: () => ['tarjetas'] as const,
  pagosCuota: (txId: number) => ['pagosCuota', txId] as const,
};

export function useDashboard(meses: number = 3) {
  return useQuery({
    queryKey: QUERY_KEYS.dashboard(meses),
    queryFn: () => apiService.dashboard.get(meses),
    staleTime: 1000 * 60 * 2, // 2 minutos
  });
}

export function useReportesResumen(params: {
  desde?: string;
  hasta?: string;
  categoriaIds?: number[];
}) {
  return useQuery({
    queryKey: QUERY_KEYS.reportesResumen(params.desde, params.hasta, params.categoriaIds),
    queryFn: () => apiService.reportes.getResumen(params),
    staleTime: 1000 * 60 * 2,
  });
}

export function useMovimientos(params: {
  desde?: string;
  hasta?: string;
  tipo?: TipoMovimiento;
  categoriaIds?: number[];
  pagina?: number;
  tamanoPagina?: number;
}) {
  return useQuery({
    queryKey: QUERY_KEYS.movimientos(params),
    queryFn: () => apiService.movimientos.getPaged(params),
    staleTime: 1000 * 60,
  });
}

export function usePendientes(params: {
  desde?: string;
  hasta?: string;
  categoriaIds?: number[];
  montoMin?: number;
  montoMax?: number;
} = {}) {
  return useQuery({
    queryKey: QUERY_KEYS.pendientes(params),
    queryFn: () => apiService.pendientes.getAll(params),
    staleTime: 1000 * 60,
  });
}

export function useCategorias(tipo?: TipoMovimiento) {
  return useQuery({
    queryKey: QUERY_KEYS.categorias(tipo),
    queryFn: () => apiService.categorias.getAll(tipo),
    staleTime: 1000 * 60 * 5,
  });
}

export function useTarjetas() {
  return useQuery({
    queryKey: QUERY_KEYS.tarjetas(),
    queryFn: () => apiService.tarjetas.getAll(),
    staleTime: 1000 * 60 * 5,
  });
}

export function usePagosCuota(txId: number | null) {
  return useQuery({
    queryKey: QUERY_KEYS.pagosCuota(txId || 0),
    queryFn: () => (txId ? apiService.movimientos.getPagos(txId) : Promise.resolve([])),
    enabled: Boolean(txId),
  });
}
