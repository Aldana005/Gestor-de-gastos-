import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  TrendingUp,
  TrendingDown,
  Filter,
  Calendar,
  CreditCard,
  Edit2,
  Trash2,
  ChevronLeft,
  ChevronRight,
  PieChart as PieChartIcon,
  ListFilter,
  ArrowUpRight,
  ArrowDownLeft,
  Tag,
} from 'lucide-react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
} from 'recharts';
import {
  useReportesResumen,
  useMovimientos,
  useCategorias,
} from '../hooks/useQueries';
import { useMovimientoMutations } from '../hooks/useMutations';
import { useModalCarga } from '../context/ModalContext';
import { formatCurrencyARS, formatDateAR } from '../utils/formatters';
import { SkeletonCard, SkeletonList } from '../components/common/SkeletonCard';
import { ErrorState } from '../components/common/ErrorState';
import { EmptyState } from '../components/common/EmptyState';
import { ConfirmDialog } from '../components/common/ConfirmDialog';
import { TipoMovimiento, TransactionOutput } from '../api/types';

const CATEGORY_COLORS = [
  '#4FB3DA',
  '#1786B4',
  '#98D5EE',
  '#3DDC97',
  '#FFC857',
  '#FF6B6B',
  '#2BC48A',
  '#62C6EC',
  '#0F5E83',
];

export const Reportes: React.FC = () => {
  const navigate = useNavigate();
  const { openEditModal } = useModalCarga();
  const { deleteMovimiento } = useMovimientoMutations();

  // Estados de filtros (por defecto: mes actual completo)
  const [desde, setDesde] = useState<string>('2026-10-01');
  const [hasta, setHasta] = useState<string>('2026-10-31');
  const [selectedCategoriaIds, setSelectedCategoriaIds] = useState<number[]>([]);
  const [pagina, setPagina] = useState<number>(1);
  const tamanoPagina = 8;

  // Estado para diálogo de confirmación de borrado
  const [deletingTx, setDeletingTx] = useState<TransactionOutput | null>(null);

  // Queries
  const { data: categorias = [] } = useCategorias();
  const {
    data: resumenData,
    isLoading: isLoadingResumen,
    isError: isErrorResumen,
    refetch: refetchResumen,
  } = useReportesResumen({
    desde,
    hasta,
    categoriaIds: selectedCategoriaIds.length > 0 ? selectedCategoriaIds : undefined,
  });

  const {
    data: movimientosData,
    isLoading: isLoadingMovimientos,
    isError: isErrorMovimientos,
    refetch: refetchMovimientos,
  } = useMovimientos({
    desde,
    hasta,
    categoriaIds: selectedCategoriaIds.length > 0 ? selectedCategoriaIds : undefined,
    pagina,
    tamanoPagina,
  });

  // Toggle para selección múltiple de categorías
  const toggleCategoria = (catId: number) => {
    setSelectedCategoriaIds((prev) => {
      const exists = prev.includes(catId);
      const next = exists ? prev.filter((id) => id !== catId) : [...prev, catId];
      setPagina(1);
      return next;
    });
  };

  const handleClearCategoryFilters = () => {
    setSelectedCategoriaIds([]);
    setPagina(1);
  };

  // Acciones rápidas de fecha
  const setQuickRange = (range: 'mesActual' | 'ultimos3Meses' | 'anoActual') => {
    setPagina(1);
    if (range === 'mesActual') {
      setDesde('2026-10-01');
      setHasta('2026-10-31');
    } else if (range === 'ultimos3Meses') {
      setDesde('2026-08-01');
      setHasta('2026-10-31');
    } else if (range === 'anoActual') {
      setDesde('2026-01-01');
      setHasta('2026-10-31');
    }
  };

  // Confirmar eliminación
  const handleConfirmDelete = async () => {
    if (!deletingTx) return;
    try {
      await deleteMovimiento.mutateAsync(deletingTx.id);
      setDeletingTx(null);
    } catch (err) {
      console.error('Error al borrar movimiento', err);
    }
  };

  // Tooltip personalizado para el gráfico de torta
  const CustomPieTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const item = payload[0].payload;
      return (
        <div className="glass-card !bg-brand-900/95 p-2.5 border-brand-300/30 text-xs shadow-xl">
          <p className="font-bold text-text-primary">{item.categoriaNombre}</p>
          <p className="text-brand-500 font-extrabold">{formatCurrencyARS(item.monto)}</p>
          <p className="text-text-muted">{item.porcentaje}% del período</p>
        </div>
      );
    }
    return null;
  };

  const isLoading = isLoadingResumen || isLoadingMovimientos;
  const isError = isErrorResumen || isErrorMovimientos;

  return (
    <div className="space-y-6 sm:space-y-8 animate-fade-in">
      {/* Encabezado */}
      <div>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-text-primary tracking-tight">
          Reportes y Movimientos
        </h1>
        <p className="text-xs sm:text-sm text-text-secondary">
          Filtrá tus operaciones por categorías y período de fechas
        </p>
      </div>

      {/* 1. BARRA DE FILTROS INTERACTIVOS */}
      <div className="glass-card p-4 sm:p-6 space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-brand-500" />
            <h2 className="text-sm font-bold text-text-primary">Filtros del Reporte</h2>
          </div>

          {selectedCategoriaIds.length > 0 && (
            <button
              type="button"
              onClick={handleClearCategoryFilters}
              className="text-xs text-brand-500 hover:text-brand-300 font-semibold"
            >
              Limpiar categorías ({selectedCategoriaIds.length})
            </button>
          )}
        </div>

        {/* Filtros de Fecha */}
        <div className="space-y-2">
          <label className="block text-xs font-semibold text-text-secondary">
            Rango de fechas *
          </label>
          <div className="flex flex-wrap items-center gap-2 sm:gap-3">
            <div className="flex items-center gap-2 flex-1 min-w-[240px]">
              <input
                type="date"
                value={desde}
                onChange={(e) => {
                  setDesde(e.target.value);
                  setPagina(1);
                }}
                className="glass-input text-xs"
                aria-label="Fecha desde"
              />
              <span className="text-text-muted text-xs">hasta</span>
              <input
                type="date"
                value={hasta}
                onChange={(e) => {
                  setHasta(e.target.value);
                  setPagina(1);
                }}
                className="glass-input text-xs"
                aria-label="Fecha hasta"
              />
            </div>

            {/* Accesos rápidos de rango */}
            <div className="flex items-center gap-1.5">
              <button
                type="button"
                onClick={() => setQuickRange('mesActual')}
                className="pill-chip bg-brand-800/60 hover:bg-brand-700/80 text-[11px] text-brand-300 border border-brand-300/15"
              >
                Mes Actual
              </button>
              <button
                type="button"
                onClick={() => setQuickRange('ultimos3Meses')}
                className="pill-chip bg-brand-800/60 hover:bg-brand-700/80 text-[11px] text-brand-300 border border-brand-300/15"
              >
                3 Meses
              </button>
              <button
                type="button"
                onClick={() => setQuickRange('anoActual')}
                className="pill-chip bg-brand-800/60 hover:bg-brand-700/80 text-[11px] text-brand-300 border border-brand-300/15"
              >
                Año 2026
              </button>
            </div>
          </div>
        </div>

        {/* Filtro de Categorías con Selección Múltiple (Píldoras) */}
        <div className="space-y-2 pt-2 border-t border-brand-300/10">
          <label className="block text-xs font-semibold text-text-secondary">
            Filtrar por categorías (selección múltiple):
          </label>
          <div className="flex flex-wrap gap-1.5 max-h-28 overflow-y-auto pr-1">
            {categorias.map((cat) => {
              const isSelected = selectedCategoriaIds.includes(cat.id);
              return (
                <button
                  key={cat.id}
                  type="button"
                  onClick={() => toggleCategoria(cat.id)}
                  className={`pill-chip text-xs border transition-all ${
                    isSelected
                      ? 'bg-brand-500 text-brand-900 border-brand-500 font-bold shadow-glow scale-105'
                      : 'bg-brand-900/60 border-brand-300/20 text-text-secondary hover:text-text-primary hover:bg-brand-800/60'
                  }`}
                >
                  <span>{cat.nombre}</span>
                  {isSelected && <span className="text-[10px] ml-1">✕</span>}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Estados de Carga y Error */}
      {isError && (
        <ErrorState
          title="Error al cargar los reportes"
          onRetry={() => {
            refetchResumen();
            refetchMovimientos();
          }}
        />
      )}

      {isLoading && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <SkeletonCard />
            <SkeletonCard />
          </div>
          <SkeletonCard className="h-64" />
          <SkeletonList count={4} />
        </div>
      )}

      {!isLoading && !isError && resumenData && (
        <>
          {/* 2. TARJETAS DE TOTALES DEL PERÍODO */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="glass-card p-5 flex items-center justify-between">
              <div>
                <span className="text-xs font-semibold text-text-secondary uppercase tracking-wider">
                  Total Ingresos del Período
                </span>
                <p className="text-2xl sm:text-3xl font-extrabold text-income tracking-tight mt-1">
                  +{formatCurrencyARS(resumenData.totalIngresos)}
                </p>
                <p className="text-[11px] text-text-muted mt-1">
                  Desde {formatDateAR(desde)} hasta {formatDateAR(hasta)}
                </p>
              </div>
              <div className="w-12 h-12 rounded-2xl bg-income-bg border border-income-border flex items-center justify-center text-income shadow-sm">
                <TrendingUp className="w-6 h-6" />
              </div>
            </div>

            <div className="glass-card p-5 flex items-center justify-between">
              <div>
                <span className="text-xs font-semibold text-text-secondary uppercase tracking-wider">
                  Total Gastos del Período
                </span>
                <p className="text-2xl sm:text-3xl font-extrabold text-expense tracking-tight mt-1">
                  −{formatCurrencyARS(resumenData.totalGastos)}
                </p>
                <p className="text-[11px] text-text-muted mt-1">
                  Desde {formatDateAR(desde)} hasta {formatDateAR(hasta)}
                </p>
              </div>
              <div className="w-12 h-12 rounded-2xl bg-expense-bg border border-expense-border flex items-center justify-center text-expense shadow-sm">
                <TrendingDown className="w-6 h-6" />
              </div>
            </div>
          </div>

          {/* 3. GRÁFICO DE TORTA DE GASTOS DEL PERÍODO */}
          <div className="glass-card p-5 sm:p-6 space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-base sm:text-lg font-bold text-text-primary flex items-center gap-2">
                  <PieChartIcon className="w-5 h-5 text-brand-500" />
                  <span>Distribución de Gastos del Período</span>
                </h2>
                <p className="text-xs text-text-secondary">
                  Gastos agrupados por categoría según los filtros aplicados
                </p>
              </div>
            </div>

            {resumenData.gastosPorCategoria.length === 0 ? (
              <div className="py-8 text-center text-text-muted text-xs">
                No hay gastos para los filtros y fechas seleccionadas.
              </div>
            ) : (
              <div className="flex flex-col sm:flex-row items-center justify-between gap-6">
                <div className="w-full sm:w-1/2 h-56 min-w-[200px] flex items-center justify-center">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={resumenData.gastosPorCategoria}
                        cx="50%"
                        cy="50%"
                        innerRadius={55}
                        outerRadius={85}
                        paddingAngle={3}
                        dataKey="monto"
                        nameKey="categoriaNombre"
                      >
                        {resumenData.gastosPorCategoria.map((entry, index) => (
                          <Cell
                            key={`cell-${entry.categoriaId}`}
                            fill={CATEGORY_COLORS[index % CATEGORY_COLORS.length]}
                            stroke="#04202E"
                            strokeWidth={2}
                          />
                        ))}
                      </Pie>
                      <Tooltip content={<CustomPieTooltip />} />
                    </PieChart>
                  </ResponsiveContainer>
                </div>

                <div className="w-full sm:w-1/2 space-y-2 max-h-56 overflow-y-auto pr-1">
                  {resumenData.gastosPorCategoria.map((item, index) => (
                    <div
                      key={item.categoriaId}
                      className="flex items-center justify-between text-xs p-1.5 rounded-lg hover:bg-brand-800/40 transition-colors"
                    >
                      <div className="flex items-center gap-2 truncate pr-2">
                        <span
                          className="w-3 h-3 rounded-full shrink-0"
                          style={{ backgroundColor: CATEGORY_COLORS[index % CATEGORY_COLORS.length] }}
                        />
                        <span className="text-text-primary font-medium truncate">
                          {item.categoriaNombre}
                        </span>
                      </div>
                      <div className="text-right shrink-0">
                        <span className="font-bold text-text-primary mr-2">
                          {formatCurrencyARS(item.monto)}
                        </span>
                        <span className="text-[11px] text-brand-300 font-semibold bg-brand-800/80 px-1.5 py-0.5 rounded-md">
                          {item.porcentaje}%
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* 4. ÚLTIMOS MOVIMIENTOS PAGINADOS (Con Editar y Borrar) */}
          <div className="glass-card p-5 sm:p-6 space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-base sm:text-lg font-bold text-text-primary flex items-center gap-2">
                  <ListFilter className="w-5 h-5 text-brand-500" />
                  <span>Últimos Movimientos</span>
                </h2>
                <p className="text-xs text-text-secondary">
                  Operaciones del período ({movimientosData?.totalRegistros || 0} registros)
                </p>
              </div>
            </div>

            {!movimientosData || movimientosData.items.length === 0 ? (
              <EmptyState
                title="Sin movimientos"
                description="No se encontraron movimientos registrados para los filtros seleccionados."
                className="!py-8"
              />
            ) : (
              <div className="space-y-2.5">
                {movimientosData.items.map((mov) => {
                  const isIngreso = mov.tipo === TipoMovimiento.Ingreso;
                  return (
                    <div
                      key={mov.id}
                      className="p-3.5 sm:p-4 rounded-2xl bg-brand-800/40 border border-brand-300/15 hover:border-brand-300/30 transition-all flex items-center justify-between gap-3 group"
                    >
                      <div className="flex items-center gap-3 min-w-0">
                        <div
                          className={`w-10 h-10 rounded-2xl flex items-center justify-center shrink-0 ${
                            isIngreso
                              ? 'bg-income-bg text-income border border-income-border'
                              : 'bg-expense-bg text-expense border border-expense-border'
                          }`}
                        >
                          {isIngreso ? (
                            <ArrowUpRight className="w-5 h-5" />
                          ) : (
                            <ArrowDownLeft className="w-5 h-5" />
                          )}
                        </div>

                        <div className="min-w-0 space-y-0.5">
                          <p className="font-bold text-sm text-text-primary truncate">
                            {mov.descripcion}
                          </p>
                          <div className="flex flex-wrap items-center gap-1.5 text-xs text-text-muted">
                            <span className="pill-chip bg-brand-900/60 text-brand-300 text-[10px] py-0 px-2 border border-brand-300/20">
                              {mov.categoryNombre}
                            </span>
                            <span>•</span>
                            <span>{formatDateAR(mov.fecha)}</span>
                            {mov.cantidadCuotas && (
                              <span className="text-warning font-semibold text-[11px]">
                                • {mov.cantidadCuotas} cuotas
                              </span>
                            )}
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center gap-3 shrink-0">
                        <div className="text-right">
                          <p
                            className={`text-sm sm:text-base font-extrabold ${
                              isIngreso ? 'text-income' : 'text-expense'
                            }`}
                          >
                            {isIngreso ? '+' : '−'}
                            {formatCurrencyARS(mov.monto)}
                          </p>
                          {mov.tarjetaNombre && (
                            <p className="text-[10px] text-text-muted truncate max-w-[100px]">
                              {mov.tarjetaNombre}
                            </p>
                          )}
                        </div>

                        {/* Botones de acción: Editar y Borrar */}
                        <div className="flex items-center gap-1 border-l border-brand-300/15 pl-2">
                          <button
                            type="button"
                            onClick={() => openEditModal(mov)}
                            className="p-2 text-text-secondary hover:text-brand-500 rounded-xl hover:bg-brand-700/50 transition-colors min-w-touch min-h-touch flex items-center justify-center"
                            aria-label={`Editar movimiento ${mov.descripcion}`}
                            title="Editar movimiento"
                          >
                            <Edit2 className="w-4 h-4" />
                          </button>
                          <button
                            type="button"
                            onClick={() => setDeletingTx(mov)}
                            className="p-2 text-text-secondary hover:text-expense rounded-xl hover:bg-brand-700/50 transition-colors min-w-touch min-h-touch flex items-center justify-center"
                            aria-label={`Borrar movimiento ${mov.descripcion}`}
                            title="Borrar movimiento"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </div>
                    </div>
                  );
                })}

                {/* Controles de Paginación */}
                {movimientosData.totalPaginas > 1 && (
                  <div className="flex items-center justify-between pt-3 border-t border-brand-300/10 text-xs">
                    <span className="text-text-muted">
                      Página <strong>{movimientosData.pagina}</strong> de{' '}
                      <strong>{movimientosData.totalPaginas}</strong>
                    </span>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        disabled={movimientosData.pagina <= 1}
                        onClick={() => setPagina((p) => Math.max(1, p - 1))}
                        className="btn-secondary text-xs py-1.5 px-3 min-h-[38px] flex items-center gap-1"
                      >
                        <ChevronLeft className="w-4 h-4" />
                        <span>Anterior</span>
                      </button>
                      <button
                        type="button"
                        disabled={movimientosData.pagina >= movimientosData.totalPaginas}
                        onClick={() => setPagina((p) => p + 1)}
                        className="btn-secondary text-xs py-1.5 px-3 min-h-[38px] flex items-center gap-1"
                      >
                        <span>Siguiente</span>
                        <ChevronRight className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>

          {/* 5. PRÓXIMAS DEUDAS DEL PERÍODO (Con Ver más) */}
          <div className="glass-card p-5 sm:p-6 space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-base sm:text-lg font-bold text-text-primary flex items-center gap-2">
                  <CreditCard className="w-5 h-5 text-warning" />
                  <span>Próximas Deudas del Período</span>
                </h2>
                <p className="text-xs text-text-secondary">Compras en cuotas proyectadas</p>
              </div>

              <button
                type="button"
                onClick={() => navigate('/pendientes')}
                className="btn-secondary text-xs py-1.5 px-3 min-h-[38px] flex items-center gap-1 font-semibold text-brand-300 hover:text-brand-100"
              >
                <span>Ver más</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>

            {resumenData.proximasDeudas.length === 0 ? (
              <div className="py-6 text-center text-text-muted text-xs">
                No hay deudas en cuotas proyectadas para este período.
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {resumenData.proximasDeudas.map((deuda) => (
                  <div
                    key={deuda.id}
                    onClick={() => navigate('/pendientes')}
                    className="p-3.5 rounded-2xl bg-brand-800/40 border border-brand-300/15 hover:border-brand-500/40 transition-all cursor-pointer flex items-center justify-between"
                  >
                    <div>
                      <p className="font-bold text-xs text-text-primary">{deuda.descripcion}</p>
                      <p className="text-[11px] text-text-muted">
                        Próxima cuota: <strong className="text-text-secondary">{deuda.mesProximaCuota}</strong>
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-xs font-bold text-warning">{formatCurrencyARS(deuda.valorCuota)}</p>
                      <p className="text-[10px] text-brand-300 font-medium">Progreso {deuda.cuotasPagadas}/{deuda.cantidadCuotas}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </>
      )}

      {/* Diálogo de confirmación para eliminar movimiento */}
      <ConfirmDialog
        isOpen={Boolean(deletingTx)}
        title="¿Eliminar movimiento?"
        description={`¿Estás seguro de que querés borrar "${deletingTx?.descripcion}" (${formatCurrencyARS(deletingTx?.monto)})? Esta acción no se puede deshacer.`}
        confirmText="Eliminar"
        cancelText="Cancelar"
        isDestructive={true}
        isLoading={deleteMovimiento.isPending}
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeletingTx(null)}
      />
    </div>
  );
};
