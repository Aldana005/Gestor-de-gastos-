/**
 * Contratos de datos (API v1) - GASTITO
 * Alineado exactamente con la solución .NET 8 y DTOs de Gastito.Presentation
 */

export enum TipoMovimiento {
  Ingreso = 1,
  Gasto = 2,
}

export enum Frecuencia {
  Eventual = 1,
  Fijo = 2,
}

export enum MedioPago {
  Tarjeta = 1,
  Efectivo = 2,
  Transferencia = 3,
}

export enum TipoTarjeta {
  Credito = 1,
  Debito = 2,
}

export enum AplicarDesde {
  MesActual = 1,
  MesSiguiente = 2,
}

export interface CategoryOutput {
  id: number;
  nombre: string;
  tipo: TipoMovimiento;
  esSistema: boolean;
}

export interface CategoryCreateInput {
  nombre: string;
  tipo: TipoMovimiento;
}

export interface TarjetaOutput {
  id: number;
  nombre: string;
  tipoTarjeta: TipoTarjeta;
}

export interface TarjetaCreateInput {
  nombre: string;
  tipoTarjeta: TipoTarjeta;
}

export interface HistorialMontoOutput {
  id: number;
  transactionId: number;
  monto: number;
  fechaInicioUtc: string;
  fechaFinUtc?: string | null;
}

export interface TransactionOutput {
  id: number;
  tipo: TipoMovimiento;
  frecuencia: Frecuencia;
  monto: number;
  moneda: number;
  fecha: string;
  descripcion: string;
  notas?: string | null;
  categoryId: number;
  categoryNombre: string;
  medioPago?: MedioPago | null;
  tarjetaId?: number | null;
  tarjetaNombre?: string | null;
  createdAtUtc: string;

  // Fijos y Cuotas
  fechaInicio?: string | null;
  fechaFin?: string | null;
  montoTotal?: number | null;
  cantidadCuotas?: number | null;
  cuotasPagadas?: number | null;
  fechaPrimeraCuota?: string | null;
  fechaPrimeraCuotaPendiente?: string | null;
  valorCuota?: number | null;
  valorUltimaCuota?: number | null;
  cuotasRestantes?: number | null;
  montoPendiente?: number | null;
  historialMontos?: HistorialMontoOutput[];
}

export interface TransactionCreateInput {
  tipo: TipoMovimiento;
  frecuencia: Frecuencia;
  monto: number;
  fecha: string;
  descripcion: string;
  notas?: string | null;
  categoryId: number;
  medioPago?: MedioPago | null;
  tarjetaId?: number | null;
  fechaInicio?: string | null;
  fechaFin?: string | null;
  montoTotal?: number | null;
  cantidadCuotas?: number | null;
  fechaPrimeraCuota?: string | null;
}

export interface TransactionUpdateInput {
  tipo: TipoMovimiento;
  frecuencia: Frecuencia;
  monto: number;
  fecha: string;
  descripcion: string;
  notas?: string | null;
  categoryId: number;
  medioPago?: MedioPago | null;
  tarjetaId?: number | null;
  fechaInicio?: string | null;
  fechaFin?: string | null;
  aplicarDesde?: AplicarDesde | null;
  montoTotal?: number | null;
  cantidadCuotas?: number | null;
  fechaPrimeraCuota?: string | null;
}

export interface PagedResult<T> {
  pagina: number;
  tamanoPagina: number;
  totalRegistros: number;
  totalPaginas: number;
  items: T[];
}

export interface GastosPorCategoriaOutput {
  categoriaId: number;
  categoriaNombre: string;
  monto: number;
  porcentaje: number;
}

export interface IngresosVsGastosOutput {
  anio: number;
  mes: number;
  periodo: string; // "2026-10"
  totalIngresos: number;
  totalGastos: number;
}

export interface ProximaDeudaOutput {
  id: number;
  descripcion: string;
  categoria: string;
  cuotasPagadas: number;
  cantidadCuotas: number;
  valorCuota: number;
  mesProximaCuota: string;
}

export interface DashboardOutput {
  totalIngresos: number;
  totalGastos: number;
  montoPendiente: number;
  gastosPorCategoria: GastosPorCategoriaOutput[];
  ingresosVsGastos: IngresosVsGastosOutput[];
  proximasDeudas: ProximaDeudaOutput[];
}

export interface ReporteResumenOutput {
  desde: string;
  hasta: string;
  totalIngresos: number;
  totalGastos: number;
  gastosPorCategoria: GastosPorCategoriaOutput[];
  proximasDeudas: ProximaDeudaOutput[];
}

export interface PagoCuotaOutput {
  id: number;
  transactionId: number;
  cantidadCuotas: number;
  monto: number;
  cuotaDesde: number;
  cuotaHasta: number;
  rangoCuotas: string; // "cuotas 4 a 5"
  fechaPagoUtc: string;
}

export interface PendienteItemOutput {
  id: number;
  descripcion: string;
  categoryId: number;
  categoryNombre: string;
  fecha: string;
  monto: number;
  esCuota: boolean;

  // Campos para compras en cuotas
  montoTotal?: number | null;
  cantidadCuotas?: number | null;
  cuotasPagadas?: number | null;
  cuotasRestantes?: number | null;
  valorCuota?: number | null;
  montoPendiente?: number | null;
  progreso?: string | null; // "3/6"
  terminaEnPeriodo?: boolean;
  fechaUltimaCuota?: string | null;
  fechaPrimeraCuotaPendiente?: string | null;
  historialPagos?: PagoCuotaOutput[];
}

export interface AdelantarCuotasInput {
  cantidad: number;
}

export interface AuthLoginResponse {
  token: string;
  expiraEnUtc: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}
