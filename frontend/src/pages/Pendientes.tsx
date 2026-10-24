import React, { useState } from 'react';
import {
  Clock,
  Filter,
  Calendar,
  DollarSign,
  ArrowRight,
  History,
  CreditCard,
  AlertCircle,
  CheckCircle2,
  CalendarClock,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { usePendientes, useCategorias } from '../hooks/useQueries';
import { formatCurrencyARS, formatDateAR, formatMonthYearAR } from '../utils/formatters';
import { SkeletonList, SkeletonCard } from '../components/common/SkeletonCard';
import { ErrorState } from '../components/common/ErrorState';
import { EmptyState } from '../components/common/EmptyState';
import { AdelantarCuotasModal } from '../components/modals/AdelantarCuotasModal';
import { PendienteItemOutput } from '../api/types';

export const Pendientes: React.FC = () => {
  // Filtros
  const [desde, setDesde] = useState<string>('');
  const [hasta, setHasta] = useState<string>('');
  const [selectedCategoriaId, setSelectedCategoriaId] = useState<number | ''>('');
  const [montoMin, setMontoMin] = useState<string>('');
  const [montoMax, setMontoMax] = useState<string>('');

  // Estados de modales y despliegues
  const [selectedItemToAdvance, setSelectedItemToAdvance] = useState<PendienteItemOutput | null>(null);
  const [expandedHistoryIds, setExpandedHistoryIds] = useState<number[]>([]);
  const [showFilters, setShowFilters] = useState<boolean>(false);

  // Queries
  const { data: categorias = [] } = useCategorias();
  const {
    data: pendientes = [],
    isLoading,
    isError,
    error,
    refetch,
  } = usePendientes({
    desde: desde || undefined,
    hasta: hasta || undefined,
    categoriaIds: selectedCategoriaId ? [Number(selectedCategoriaId)] : undefined,
    montoMin: montoMin ? parseFloat(montoMin) : undefined,
    montoMax: montoMax ? parseFloat(montoMax) : undefined,
  });

  const toggleHistory = (id: number) => {
    setExpandedHistoryIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    );
  };

  const clearFilters = () => {
    setDesde('');
    setHasta('');
    setSelectedCategoriaId('');
    setMontoMin('');
    setMontoMax('');
  };

  const hasActiveFilters = Boolean(
    desde || hasta || selectedCategoriaId || montoMin || montoMax
  );

  return (
    <div className="space-y-6 sm:space-y-8 animate-fade-in">
      {/* Encabezado */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-text-primary tracking-tight flex items-center gap-2.5">
            <Clock className="w-7 h-7 text-warning" />
            <span>Pendientes y Cuotas</span>
          </h1>
          <p className="text-xs sm:text-sm text-text-secondary">
            Gastos eventuales no saldados y compras en cuotas activas
          </p>
        </div>

        <button
          type="button"
          onClick={() => setShowFilters(!showFilters)}
          className={`btn-secondary text-xs py-2 px-3.5 flex items-center gap-2 ${
            hasActiveFilters ? '!border-brand-500 !text-brand-100' : ''
          }`}
          aria-expanded={showFilters}
        >
          <Filter className="w-4 h-4 text-brand-500" />
          <span>Filtros {hasActiveFilters && '(Activos)'}</span>
          {showFilters ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
        </button>
      </div>

      {/* Panel de Filtros Desplegable */}
      {showFilters && (
        <div className="glass-card p-4 sm:p-5 space-y-4 animate-fade-in">
          <div className="flex items-center justify-between">
            <h2 className="text-xs font-bold uppercase tracking-wider text-text-secondary">
              Filtrar Pendientes
            </h2>
            {hasActiveFilters && (
              <button
                type="button"
                onClick={clearFilters}
                className="text-xs text-brand-500 hover:text-brand-300 font-semibold"
              >
                Limpiar todos
              </button>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            {/* Categoría */}
            <div>
              <label htmlFor="filter-cat" className="block text-xs font-semibold text-text-secondary mb-1">
                Categoría
              </label>
              <select
                id="filter-cat"
                value={selectedCategoriaId}
                onChange={(e) => setSelectedCategoriaId(e.target.value ? Number(e.target.value) : '')}
                className="glass-input text-xs"
              >
                <option value="" className="bg-brand-900">
                  Todas las categorías
                </option>
                {categorias.map((c) => (
                  <option key={c.id} value={c.id} className="bg-brand-900">
                    {c.nombre}
                  </option>
                ))}
              </select>
            </div>

            {/* Rango de Fechas */}
            <div>
              <label className="block text-xs font-semibold text-text-secondary mb-1">
                Rango de Fechas
              </label>
              <div className="grid grid-cols-2 gap-2">
                <input
                  type="date"
                  value={desde}
                  onChange={(e) => setDesde(e.target.value)}
                  placeholder="Desde"
                  className="glass-input text-xs"
                />
                <input
                  type="date"
                  value={hasta}
                  onChange={(e) => setHasta(e.target.value)}
                  placeholder="Hasta"
                  className="glass-input text-xs"
                />
              </div>
            </div>

            {/* Rango de Monto */}
            <div>
              <label className="block text-xs font-semibold text-text-secondary mb-1">
                Rango de Monto (ARS)
              </label>
              <div className="grid grid-cols-2 gap-2">
                <input
                  type="number"
                  placeholder="Mínimo $"
                  value={montoMin}
                  onChange={(e) => setMontoMin(e.target.value)}
                  className="glass-input text-xs"
                />
                <input
                  type="number"
                  placeholder="Máximo $"
                  value={montoMax}
                  onChange={(e) => setMontoMax(e.target.value)}
                  className="glass-input text-xs"
                />
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Estados de Carga y Error */}
      {isLoading && (
        <div className="space-y-4">
          <SkeletonCard />
          <SkeletonList count={4} />
        </div>
      )}

      {isError && (
        <ErrorState
          title="Error al cargar pendientes"
          message={error instanceof Error ? error.message : 'No se pudieron recuperar los pendientes.'}
          onRetry={() => refetch()}
        />
      )}

      {/* Lista de Pendientes (Orden estricto de la API sin reordenar en cliente) */}
      {!isLoading && !isError && (
        <>
          {pendientes.length === 0 ? (
            <EmptyState
              title="No hay gastos ni compras pendientes"
              description="¡Al día! No tenés compras en cuotas por saldar ni deudas pendientes registradas con los filtros actuales."
            />
          ) : (
            <div className="space-y-3">
              {pendientes.map((item) => {
                const isExpanded = expandedHistoryIds.includes(item.id);
                const hasHistory = item.historialPagos && item.historialPagos.length > 0;

                // 1. CASO COMPRA EN CUOTAS
                if (item.esCuota) {
                  return (
                    <div
                      key={`cuota-${item.id}`}
                      className="glass-card p-4 sm:p-5 border-brand-300/20 hover:border-brand-500/30 transition-all space-y-3 group"
                    >
                      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                        {/* Descripción y categoría */}
                        <div className="space-y-1">
                          <div className="flex items-center gap-2">
                            <span className="font-extrabold text-base text-text-primary">
                              {item.descripcion}
                            </span>
                            <span className="pill-chip bg-brand-700/60 text-brand-300 text-[10px] py-0 px-2 border border-brand-300/20">
                              {item.categoryNombre}
                            </span>
                          </div>

                          <div className="flex flex-wrap items-center gap-2 text-xs text-text-muted">
                            <span className="text-warning font-semibold">
                              Progreso: {item.progreso || `${item.cuotasPagadas}/${item.cantidadCuotas}`}
                            </span>
                            <span>•</span>
                            <span>
                              Próxima cuota: <strong className="text-text-secondary">Noviembre 2026</strong>
                            </span>
                            <span>•</span>
                            <span>Total compra: {formatCurrencyARS(item.montoTotal)}</span>
                          </div>
                        </div>

                        {/* Montos y botón "Adelantar" */}
                        <div className="flex items-center justify-between sm:justify-end gap-3 pt-2 sm:pt-0 border-t sm:border-t-0 border-brand-300/10">
                          <div className="text-left sm:text-right">
                            <p className="text-xs text-text-muted">Valor cuota</p>
                            <p className="text-base sm:text-lg font-black text-warning">
                              {formatCurrencyARS(item.valorCuota)}
                            </p>
                          </div>

                          {/* Botón Adelantar (Sin usar la palabra "Pagar") */}
                          <button
                            type="button"
                            onClick={() => setSelectedItemToAdvance(item)}
                            className="btn-primary text-xs py-2 px-4 shadow-md hover:scale-105 active:scale-95"
                          >
                            <CalendarClock className="w-3.5 h-3.5" />
                            <span>Adelantar</span>
                          </button>
                        </div>
                      </div>

                      {/* Barra de Progreso visual de cuotas */}
                      <div className="w-full bg-brand-900/60 rounded-full h-2 overflow-hidden border border-brand-300/15">
                        <div
                          className="bg-gradient-to-r from-brand-500 to-income h-2 rounded-full transition-all duration-500"
                          style={{
                            width: `${Math.min(
                              100,
                              ((item.cuotasPagadas || 0) / (item.cantidadCuotas || 1)) * 100
                            )}%`,
                          }}
                        />
                      </div>

                      {/* Acceso a historial de adelantos */}
                      {hasHistory && (
                        <div className="pt-1">
                          <button
                            type="button"
                            onClick={() => toggleHistory(item.id)}
                            className="text-xs text-brand-300 hover:text-brand-100 flex items-center gap-1.5 transition-colors font-medium"
                          >
                            <History className="w-3.5 h-3.5" />
                            <span>
                              {isExpanded
                                ? 'Ocultar historial de adelantos'
                                : `Ver historial de adelantos (${item.historialPagos!.length})`}
                            </span>
                          </button>

                          {isExpanded && (
                            <div className="mt-2.5 space-y-1.5 p-3 rounded-2xl bg-brand-900/70 border border-brand-300/15 text-xs animate-fade-in">
                              <p className="font-semibold text-text-secondary text-[11px] mb-1">
                                Registro de cuotas adelantadas:
                              </p>
                              {item.historialPagos!.map((p) => (
                                <div
                                  key={p.id}
                                  className="flex items-center justify-between p-2 rounded-xl bg-brand-800/40 text-xs"
                                >
                                  <div>
                                    <p className="font-bold text-text-primary capitalize">
                                      {p.rangoCuotas}
                                    </p>
                                    <p className="text-[10px] text-text-muted">
                                      {formatDateAR(p.fechaPagoUtc)}
                                    </p>
                                  </div>
                                  <div className="text-right">
                                    <span className="font-extrabold text-income">
                                      {formatCurrencyARS(p.monto)}
                                    </span>
                                    <p className="text-[10px] text-text-muted">
                                      {p.cantidadCuotas} {p.cantidadCuotas === 1 ? 'cuota' : 'cuotas'}
                                    </p>
                                  </div>
                                </div>
                              ))}
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  );
                }

                // 2. CASO GASTO EVENTUAL SIN CUOTAS (Aparece primero según la API)
                return (
                  <div
                    key={`eventual-${item.id}`}
                    className="glass-card p-4 sm:p-5 border-brand-300/15 flex items-center justify-between gap-3"
                  >
                    <div className="space-y-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-sm text-text-primary truncate">
                          {item.descripcion}
                        </span>
                        <span className="pill-chip bg-warning-bg/30 text-warning text-[10px] py-0 px-2 border border-warning/30">
                          Gasto pendiente
                        </span>
                      </div>
                      <div className="flex items-center gap-2 text-xs text-text-muted">
                        <span className="pill-chip bg-brand-900/60 text-brand-300 text-[10px] py-0 px-2 border border-brand-300/20">
                          {item.categoryNombre}
                        </span>
                        <span>•</span>
                        <span>Fecha: {formatDateAR(item.fecha)}</span>
                      </div>
                    </div>

                    <div className="text-right shrink-0">
                      <p className="text-xs text-text-muted">Monto adeudado</p>
                      <p className="text-base font-extrabold text-expense">
                        {formatCurrencyARS(item.monto)}
                      </p>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </>
      )}

      {/* Modal para Adelantar Cuotas */}
      <AdelantarCuotasModal
        isOpen={Boolean(selectedItemToAdvance)}
        item={selectedItemToAdvance}
        onClose={() => setSelectedItemToAdvance(null)}
        onSuccess={() => refetch()}
      />
    </div>
  );
};
