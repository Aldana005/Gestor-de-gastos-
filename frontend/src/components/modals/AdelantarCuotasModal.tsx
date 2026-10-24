import React, { useState } from 'react';
import { X, CalendarClock, History } from 'lucide-react';
import { PendienteItemOutput } from '../../api/types';
import { formatCurrencyARS, formatDateAR } from '../../utils/formatters';
import { useMovimientoMutations } from '../../hooks/useMutations';

interface AdelantarCuotasModalProps {
  isOpen: boolean;
  item: PendienteItemOutput | null;
  onClose: () => void;
  onSuccess: () => void;
}

export const AdelantarCuotasModal: React.FC<AdelantarCuotasModalProps> = ({
  isOpen,
  item,
  onClose,
  onSuccess,
}) => {
  const [cantidad, setCantidad] = useState<number>(1);
  const [showHistory, setShowHistory] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const { adelantarCuotas } = useMovimientoMutations();

  if (!isOpen || !item) return null;

  const totalCuotas = item.cantidadCuotas || 1;
  const pagadas = item.cuotasPagadas || 0;
  const restantes = Math.max(0, totalCuotas - pagadas);
  const valorCuota = item.valorCuota || 0;
  const montoAdelanto = cantidad * valorCuota;
  const cuotaDesde = pagadas + 1;
  const cuotaHasta = pagadas + cantidad;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (cantidad < 1 || cantidad > restantes) {
      setError(`Podés adelantar entre 1 y ${restantes} cuotas.`);
      return;
    }

    try {
      await adelantarCuotas.mutateAsync({
        id: item.id,
        cantidad,
      });
      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err.message || 'Error al procesar el adelanto.');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-md animate-fade-in"
      role="dialog"
      aria-modal="true"
      aria-labelledby="adelantar-modal-title"
    >
      <div className="glass-card max-w-md w-full p-6 border-brand-300/30 bg-brand-900/95 shadow-2xl relative">
        <button
          type="button"
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-text-secondary hover:text-text-primary rounded-full hover:bg-brand-800/50"
          aria-label="Cerrar modal"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="flex items-center gap-3 mb-4">
          <div className="w-11 h-11 rounded-full bg-brand-500/20 text-brand-500 flex items-center justify-center">
            <CalendarClock className="w-6 h-6" />
          </div>
          <div>
            <h3 id="adelantar-modal-title" className="text-lg font-bold text-text-primary">
              Adelantar Cuotas
            </h3>
            <p className="text-xs text-text-secondary">{item.descripcion}</p>
          </div>
        </div>

        {/* Resumen de la compra */}
        <div className="p-3.5 rounded-2xl bg-brand-800/50 border border-brand-300/15 mb-4 space-y-2 text-xs">
          <div className="flex justify-between items-center">
            <span className="text-text-secondary">Progreso actual:</span>
            <span className="font-semibold text-brand-100 bg-brand-700/60 px-2.5 py-0.5 rounded-full border border-brand-300/20">
              {item.progreso || `${pagadas}/${totalCuotas}`} cuotas
            </span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-text-secondary">Cuotas pendientes:</span>
            <span className="font-semibold text-text-primary">{restantes} cuotas</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-text-secondary">Valor por cuota:</span>
            <span className="font-semibold text-brand-500">{formatCurrencyARS(valorCuota)}</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-text-secondary">Monto pendiente total:</span>
            <span className="font-semibold text-expense">{formatCurrencyARS(item.montoPendiente)}</span>
          </div>
        </div>

        {error && (
          <div className="mb-4 p-3 rounded-xl bg-expense/15 border border-expense/30 text-xs text-expense">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="cantidad-cuotas" className="block text-xs font-semibold text-text-secondary mb-1.5">
              ¿Cuántas cuotas querés adelantar? (Mínimo 1, Máximo {restantes})
            </label>
            <div className="flex items-center gap-3">
              <input
                id="cantidad-cuotas"
                type="number"
                min={1}
                max={restantes}
                value={cantidad}
                onChange={(e) => setCantidad(Math.max(1, Math.min(restantes, parseInt(e.target.value, 10) || 1)))}
                className="glass-input text-center text-lg font-bold text-brand-100 w-28"
              />
              <div className="flex-1 text-xs text-text-muted">
                Adelanta desde la cuota <strong>{cuotaDesde}</strong> hasta la <strong>{cuotaHasta}</strong>.
              </div>
            </div>
          </div>

          {/* Monto a adelantar calculado */}
          <div className="p-3 rounded-xl bg-brand-500/15 border border-brand-500/30 flex justify-between items-center">
            <span className="text-xs font-medium text-brand-100">Total del adelanto:</span>
            <span className="text-base font-extrabold text-brand-500">
              {formatCurrencyARS(montoAdelanto)}
            </span>
          </div>

          {/* Botón para ver historial de adelantos */}
          {item.historialPagos && item.historialPagos.length > 0 && (
            <div>
              <button
                type="button"
                onClick={() => setShowHistory(!showHistory)}
                className="w-full text-xs text-brand-300 hover:text-brand-100 flex items-center justify-center gap-1.5 py-1.5 transition-colors"
              >
                <History className="w-3.5 h-3.5" />
                <span>{showHistory ? 'Ocultar historial de adelantos' : `Ver historial de adelantos (${item.historialPagos.length})`}</span>
              </button>

              {showHistory && (
                <div className="mt-2 max-h-36 overflow-y-auto space-y-1.5 p-2 rounded-xl bg-brand-900/60 border border-brand-300/10 text-xs">
                  {item.historialPagos.map((p) => (
                    <div
                      key={p.id}
                      className="flex justify-between items-center p-2 rounded-lg bg-brand-800/40 text-[11px]"
                    >
                      <div>
                        <p className="font-semibold text-text-primary capitalize">{p.rangoCuotas}</p>
                        <p className="text-text-muted">{formatDateAR(p.fechaPagoUtc)}</p>
                      </div>
                      <span className="font-bold text-income">{formatCurrencyARS(p.monto)}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          <div className="flex justify-end gap-2 pt-3 border-t border-brand-300/10">
            <button
              type="button"
              onClick={onClose}
              disabled={adelantarCuotas.isPending}
              className="btn-secondary text-xs py-2 px-4"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={adelantarCuotas.isPending || restantes <= 0}
              className="btn-primary text-xs py-2 px-5"
            >
              {adelantarCuotas.isPending ? 'Procesando...' : 'Confirmar Adelanto'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
