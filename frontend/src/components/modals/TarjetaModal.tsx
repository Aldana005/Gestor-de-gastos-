import React, { useState, useEffect } from 'react';
import { X, CreditCard } from 'lucide-react';
import { TipoTarjeta } from '../../api/types';
import { useMovimientoMutations } from '../../hooks/useMutations';

interface TarjetaModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (newTarjetaId: number) => void;
}

export const TarjetaModal: React.FC<TarjetaModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
}) => {
  const [nombre, setNombre] = useState('');
  const [tipoTarjeta, setTipoTarjeta] = useState<TipoTarjeta>(TipoTarjeta.Credito);
  const [error, setError] = useState<string | null>(null);

  const { createTarjeta } = useMovimientoMutations();

  useEffect(() => {
    if (isOpen) {
      setNombre('');
      setTipoTarjeta(TipoTarjeta.Credito);
      setError(null);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!nombre.trim()) {
      setError('El nombre de la tarjeta es obligatorio.');
      return;
    }

    try {
      const created = await createTarjeta.mutateAsync({
        nombre: nombre.trim(),
        tipoTarjeta,
      });
      setNombre('');
      onSuccess(created.id);
      onClose();
    } catch (err: any) {
      setError(err.message || 'Error al crear la tarjeta.');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fade-in"
      role="dialog"
      aria-modal="true"
      aria-labelledby="card-modal-title"
    >
      <div className="glass-card max-w-sm w-full p-6 border-brand-300/30 bg-brand-900/95 shadow-2xl relative">
        <button
          type="button"
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-text-secondary hover:text-text-primary rounded-full hover:bg-brand-800/50"
          aria-label="Cerrar modal"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="flex items-center gap-3 mb-4">
          <div className="w-10 h-10 rounded-full bg-brand-500/20 text-brand-500 flex items-center justify-center">
            <CreditCard className="w-5 h-5" />
          </div>
          <div>
            <h3 id="card-modal-title" className="text-lg font-bold text-text-primary">
              Nueva Tarjeta
            </h3>
            <p className="text-xs text-text-secondary">Agregá tu tarjeta de crédito o débito</p>
          </div>
        </div>

        {error && (
          <div className="mb-4 p-3 rounded-xl bg-expense/15 border border-expense/30 text-xs text-expense">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="card-nombre" className="block text-xs font-semibold text-text-secondary mb-1.5">
              Nombre o Banco de la tarjeta *
            </label>
            <input
              id="card-nombre"
              type="text"
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              placeholder="Ej: Visa Macro, Mastercard Galicia..."
              className="glass-input"
              autoFocus
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-text-secondary mb-1.5">
              Tipo de tarjeta *
            </label>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => setTipoTarjeta(TipoTarjeta.Credito)}
                className={`py-2 px-3 rounded-xl text-xs font-semibold border transition-all ${
                  tipoTarjeta === TipoTarjeta.Credito
                    ? 'bg-brand-500/20 border-brand-500 text-brand-100'
                    : 'bg-brand-800/40 border-brand-300/20 text-text-secondary'
                }`}
              >
                Crédito
              </button>
              <button
                type="button"
                onClick={() => setTipoTarjeta(TipoTarjeta.Debito)}
                className={`py-2 px-3 rounded-xl text-xs font-semibold border transition-all ${
                  tipoTarjeta === TipoTarjeta.Debito
                    ? 'bg-brand-500/20 border-brand-500 text-brand-100'
                    : 'bg-brand-800/40 border-brand-300/20 text-text-secondary'
                }`}
              >
                Débito
              </button>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-3 border-t border-brand-300/10">
            <button
              type="button"
              onClick={onClose}
              disabled={createTarjeta.isPending}
              className="btn-secondary text-xs py-2 px-4"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={createTarjeta.isPending}
              className="btn-primary text-xs py-2 px-5"
            >
              {createTarjeta.isPending ? 'Guardando...' : 'Crear Tarjeta'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
