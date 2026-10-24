import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  TrendingUp,
  TrendingDown,
  Clock,
  BarChart3,
  ChevronRight,
  Filter,
  CreditCard,
  PieChart as PieChartIcon,
} from 'lucide-react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Legend,
} from 'recharts';
import { useDashboard } from '../hooks/useQueries';
import { formatCurrencyARS, formatMonthYearAR } from '../utils/formatters';
import { SkeletonCard, SkeletonList } from '../components/common/SkeletonCard';
import { ErrorState } from '../components/common/ErrorState';
import { EmptyState } from '../components/common/EmptyState';

// 7 Tonalidades de la paleta oficial Небо (Holst) + variaciones con contraste AA
const CATEGORY_COLORS = [
  '#4FB3DA', // Brand 500 (Cian claro)
  '#1786B4', // Brand 600
  '#98D5EE', // Brand 300
  '#3DDC97', // Verde menta
  '#FFC857', // Ámbar
  '#FF6B6B', // Coral
  '#2BC48A', // Esmeralda suave
  '#62C6EC', // Azul cielo
  '#0F5E83', // Brand 700
];

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const [mesesFiltro, setMesesFiltro] = useState<number>(3);
  const [isFilterMenuOpen, setIsFilterMenuOpen] = useState<boolean>(false);

  const { data, isLoading, isError, error, refetch } = useDashboard(mesesFiltro);

  if (isLoading) {
    return (
      <div className="space-y-6 animate-fade-in">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <SkeletonCard className="h-80" />
          <SkeletonCard className="h-80" />
        </div>
        <SkeletonList count={2} />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <ErrorState
        title="Error al cargar el Dashboard"
        message={error instanceof Error ? error.message : 'No se pudo obtener la información financiera.'}
        onRetry={() => refetch()}
      />
    );
  }

  const { totalIngresos, totalGastos, montoPendiente, gastosPorCategoria, ingresosVsGastos, proximasDeudas } = data;

  // Opciones de selector de meses (3 a 12)
  const mesesOptions = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  // Tooltip personalizado para el gráfico de torta
  const CustomPieTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const item = payload[0].payload;
      return (
        <div className="glass-card !bg-brand-900/95 p-2.5 border-brand-300/30 text-xs shadow-xl">
          <p className="font-bold text-text-primary">{item.categoriaNombre}</p>
          <p className="text-brand-500 font-extrabold">{formatCurrencyARS(item.monto)}</p>
          <p className="text-text-muted">{item.porcentaje}% del total</p>
        </div>
      );
    }
    return null;
  };

  // Tooltip personalizado para el gráfico de líneas
  const CustomLineTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      return (
        <div className="glass-card !bg-brand-900/95 p-3 border-brand-300/30 text-xs shadow-xl space-y-1">
          <p className="font-bold text-brand-300">{formatMonthYearAR(label)}</p>
          {payload.map((entry: any, index: number) => (
            <p key={`item-${index}`} className="flex items-center justify-between gap-3 font-semibold" style={{ color: entry.color }}>
              <span>{entry.name}:</span>
              <span>{formatCurrencyARS(entry.value)}</span>
            </p>
          ))}
        </div>
      );
    }
    return null;
  };

  return (
    <div className="space-y-6 sm:space-y-8 animate-fade-in">
      {/* Título de bienvenida e información del mes */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-text-primary tracking-tight">
            Dashboard
          </h1>
          <p className="text-xs sm:text-sm text-text-secondary">
            Resumen consolidado de <strong className="text-brand-100">{formatMonthYearAR(new Date())}</strong>
          </p>
        </div>
      </div>

      {/* 1. LAS 4 TARJETAS PRINCIPALES DEL MES ACTUAL */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Tarjeta 1: Total Ingresos — Este mes */}
        <div className="glass-card p-5 relative overflow-hidden flex flex-col justify-between">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold text-text-secondary uppercase tracking-wider">
              Total Ingresos
            </span>
            <div className="w-9 h-9 rounded-full bg-income-bg border border-income-border flex items-center justify-center text-income shadow-sm">
              <TrendingUp className="w-5 h-5" aria-hidden="true" />
            </div>
          </div>
          <div>
            <p className="text-2xl sm:text-3xl font-extrabold text-income tracking-tight">
              +{formatCurrencyARS(totalIngresos)}
            </p>
            <p className="text-[11px] text-text-muted mt-1">Este mes</p>
          </div>
        </div>

        {/* Tarjeta 2: Total Gastos — Este mes */}
        <div className="glass-card p-5 relative overflow-hidden flex flex-col justify-between">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold text-text-secondary uppercase tracking-wider">
              Total Gastos
            </span>
            <div className="w-9 h-9 rounded-full bg-expense-bg border border-expense-border flex items-center justify-center text-expense shadow-sm">
              <TrendingDown className="w-5 h-5" aria-hidden="true" />
            </div>
          </div>
          <div>
            <p className="text-2xl sm:text-3xl font-extrabold text-expense tracking-tight">
              −{formatCurrencyARS(totalGastos)}
            </p>
            <p className="text-[11px] text-text-muted mt-1">Este mes</p>
          </div>
        </div>

        {/* Tarjeta 3: Monto Pendiente (Lleva a Pendientes) */}
        <button
          type="button"
          onClick={() => navigate('/pendientes')}
          className="glass-card glass-card-hover p-5 text-left flex flex-col justify-between group cursor-pointer focus-visible:ring-2 focus-visible:ring-brand-500"
          aria-label="Ver movimientos pendientes"
        >
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold text-warning uppercase tracking-wider">
              Monto Pendiente
            </span>
            <div className="w-9 h-9 rounded-full bg-warning-bg border border-warning-border flex items-center justify-center text-warning group-hover:scale-110 transition-transform">
              <Clock className="w-5 h-5" aria-hidden="true" />
            </div>
          </div>
          <div>
            <p className="text-2xl sm:text-3xl font-extrabold text-warning tracking-tight">
              {formatCurrencyARS(montoPendiente)}
            </p>
            <p className="text-[11px] text-brand-300 flex items-center gap-1 mt-1 group-hover:text-brand-100 transition-colors">
              <span>Ir a Pendientes</span>
              <ChevronRight className="w-3.5 h-3.5" />
            </p>
          </div>
        </button>

        {/* Tarjeta 4: Reportes (Lleva a Reportes) */}
        <button
          type="button"
          onClick={() => navigate('/reportes')}
          className="glass-card glass-card-hover p-5 text-left flex flex-col justify-between group cursor-pointer focus-visible:ring-2 focus-visible:ring-brand-500"
          aria-label="Ver reportes y últimos movimientos"
        >
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold text-brand-300 uppercase tracking-wider">
              Reportes
            </span>
            <div className="w-9 h-9 rounded-full bg-brand-500/20 border border-brand-500/30 flex items-center justify-center text-brand-500 group-hover:scale-110 transition-transform">
              <BarChart3 className="w-5 h-5" aria-hidden="true" />
            </div>
          </div>
          <div>
            <p className="text-lg sm:text-xl font-bold text-text-primary tracking-tight">
              Análisis y Movimientos
            </p>
            <p className="text-[11px] text-brand-300 flex items-center gap-1 mt-1 group-hover:text-brand-100 transition-colors">
              <span>Ver detalle y filtros</span>
              <ChevronRight className="w-3.5 h-3.5" />
            </p>
          </div>
        </button>
      </div>

      {/* 2. GRÁFICOS: Gastos por Categoría (Donut) & Ingresos vs Gastos (Líneas) */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* GRÁFICO DE TORTA: Gastos por Categoría */}
        <div className="glass-card p-5 sm:p-6 flex flex-col justify-between space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-base sm:text-lg font-bold text-text-primary flex items-center gap-2">
                <PieChartIcon className="w-5 h-5 text-brand-500" />
                <span>Gastos por Categoría</span>
              </h2>
              <p className="text-xs text-text-secondary">Distribución de gastos del mes actual</p>
            </div>
          </div>

          {gastosPorCategoria.length === 0 ? (
            <div className="py-12 text-center text-text-muted text-sm">
              No hay gastos registrados en el mes actual.
            </div>
          ) : (
            <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
              {/* Gráfico Donut de 360px compatible */}
              <div className="w-full sm:w-1/2 h-56 min-w-[200px] flex items-center justify-center">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart aria-label="Gráfico circular de gastos por categoría">
                    <Pie
                      data={gastosPorCategoria}
                      cx="50%"
                      cy="50%"
                      innerRadius={55}
                      outerRadius={85}
                      paddingAngle={3}
                      dataKey="monto"
                      nameKey="categoriaNombre"
                    >
                      {gastosPorCategoria.map((entry, index) => (
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

              {/* Leyenda y listado con porcentajes y contraste AA */}
              <div className="w-full sm:w-1/2 space-y-2 max-h-56 overflow-y-auto pr-1">
                {gastosPorCategoria.map((item, index) => (
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

        {/* GRÁFICO DE LÍNEAS: Ingresos vs Gastos con filtro de meses (3 a 12) */}
        <div className="glass-card p-5 sm:p-6 flex flex-col justify-between space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-base sm:text-lg font-bold text-text-primary flex items-center gap-2">
                <BarChart3 className="w-5 h-5 text-brand-500" />
                <span>Ingresos vs Gastos</span>
              </h2>
              <p className="text-xs text-text-secondary">Evolución mensual histórica</p>
            </div>

            {/* Botón de Filtro de Meses (3 a 12) */}
            <div className="relative">
              <button
                type="button"
                onClick={() => setIsFilterMenuOpen(!isFilterMenuOpen)}
                className="btn-secondary text-xs py-1.5 px-3 min-h-[36px] flex items-center gap-1.5"
                aria-expanded={isFilterMenuOpen}
                aria-label="Filtrar cantidad de meses para este gráfico"
              >
                <Filter className="w-3.5 h-3.5 text-brand-500" />
                <span>Meses: <strong>{mesesFiltro}</strong></span>
              </button>

              {isFilterMenuOpen && (
                <div className="absolute right-0 top-full mt-2 w-48 rounded-2xl bg-brand-900/95 border border-brand-300/30 shadow-2xl p-2 z-20 animate-fade-in">
                  <p className="px-3 py-1 text-[11px] font-semibold text-text-muted uppercase">
                    Seleccionar rango
                  </p>
                  <div className="grid grid-cols-3 gap-1 mt-1">
                    {mesesOptions.map((m) => (
                      <button
                        key={m}
                        type="button"
                        onClick={() => {
                          setMesesFiltro(m);
                          setIsFilterMenuOpen(false);
                        }}
                        className={`py-1.5 px-2 rounded-xl text-xs font-semibold transition-all ${
                          mesesFiltro === m
                            ? 'bg-brand-500 text-brand-900 font-bold'
                            : 'text-text-secondary hover:text-text-primary hover:bg-brand-800/60'
                        }`}
                      >
                        {m}m
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Gráfico de Líneas Responsive */}
          <div className="w-full h-56 min-w-[200px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={ingresosVsGastos} margin={{ top: 10, right: 10, left: -15, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(152, 213, 238, 0.1)" />
                <XAxis
                  dataKey="periodo"
                  stroke="#98D5EE"
                  fontSize={11}
                  tickLine={false}
                  tickFormatter={(val) => {
                    const match = val.match(/-(\d{2})$/);
                    const mesesCortos = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
                    return match ? mesesCortos[parseInt(match[1], 10) - 1] : val;
                  }}
                />
                <YAxis
                  stroke="#98D5EE"
                  fontSize={10}
                  tickLine={false}
                  tickFormatter={(val) => `$${(val / 1000).toFixed(0)}k`}
                />
                <Tooltip content={<CustomLineTooltip />} />
                <Legend
                  verticalAlign="top"
                  align="right"
                  height={30}
                  iconSize={10}
                  wrapperStyle={{ fontSize: '11px', color: '#E1F2FA' }}
                />
                <Line
                  type="monotone"
                  dataKey="totalIngresos"
                  name="Ingresos"
                  stroke="#3DDC97"
                  strokeWidth={2.5}
                  dot={{ r: 3, fill: '#3DDC97' }}
                  activeDot={{ r: 5 }}
                />
                <Line
                  type="monotone"
                  dataKey="totalGastos"
                  name="Gastos"
                  stroke="#FF6B6B"
                  strokeWidth={2.5}
                  dot={{ r: 3, fill: '#FF6B6B' }}
                  activeDot={{ r: 5 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      {/* 3. PRÓXIMAS DEUDAS (Lista con cuotas sin pagar + botón Ver más) */}
      <div className="glass-card p-5 sm:p-6 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-base sm:text-lg font-bold text-text-primary flex items-center gap-2">
              <CreditCard className="w-5 h-5 text-warning" />
              <span>Próximas Deudas</span>
            </h2>
            <p className="text-xs text-text-secondary">Compras en cuotas con vencimientos pendientes</p>
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

        {proximasDeudas.length === 0 ? (
          <EmptyState
            title="Sin deudas pendientes"
            description="¡Excelente! No tenés cuotas pendientes para los próximos meses."
            className="!py-6"
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {proximasDeudas.map((deuda) => (
              <div
                key={deuda.id}
                onClick={() => navigate('/pendientes')}
                className="p-4 rounded-2xl bg-brand-800/40 border border-brand-300/15 hover:border-brand-500/40 transition-all cursor-pointer flex items-center justify-between group"
              >
                <div className="space-y-1 pr-3">
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-sm text-text-primary group-hover:text-brand-100 transition-colors">
                      {deuda.descripcion}
                    </span>
                    <span className="pill-chip bg-brand-700/60 text-brand-300 text-[10px] py-0.5 px-2 border border-brand-300/20">
                      {deuda.categoria}
                    </span>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-text-muted">
                    <span>Próxima: <strong className="text-text-secondary">{deuda.mesProximaCuota}</strong></span>
                    <span>•</span>
                    <span className="font-semibold text-warning">
                      Progreso: {deuda.cuotasPagadas}/{deuda.cantidadCuotas}
                    </span>
                  </div>
                </div>

                <div className="text-right shrink-0">
                  <p className="text-sm font-extrabold text-warning">
                    {formatCurrencyARS(deuda.valorCuota)}
                  </p>
                  <p className="text-[10px] text-text-muted">por cuota</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
