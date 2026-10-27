import React, { useState, useEffect } from 'react';
import { X, Tag } from 'lucide-react';
import { TipoMovimiento } from '../../api/types';
import { useMovimientoMutations } from '../../hooks/useMutations';

interface CategoryModalProps {
  isOpen: boolean;
  defaultTipo: TipoMovimiento;
  onClose: () => void;
  onSuccess: (newCatId: number, tipo?: TipoMovimiento) => void;
}

export const CategoryModal: React.FC<CategoryModalProps> = ({
  isOpen,
  defaultTipo,
  onClose,
  onSuccess,
}) => {
  const [nombre, setNombre] = useState('');
  const [tipo, setTipo] = useState<TipoMovimiento>(defaultTipo);
  const [error, setError] = useState<string | null>(null);

  const { createCategoria } = useMovimientoMutations();

  useEffect(() => {
    if (isOpen) {
      setNombre('');
      setTipo(defaultTipo);
      setError(null);
    }
  }, [isOpen, defaultTipo]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!nombre.trim()) {
      setError('El nombre de la categoría es obligatorio.');
      return;
    }

    try {
      const created = await createCategoria.mutateAsync({
        nombre: nombre.trim(),
        tipo,
      });
      setNombre('');
      onSuccess(created.id, created.tipo);
      onClose();
    } catch (err: any) {
      setError(err.message || 'Error al crear la categoría.');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fade-in"
      role="dialog"
      aria-modal="true"
      aria-labelledby="cat-modal-title"
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
            <Tag className="w-5 h-5" />
          </div>
          <div>
            <h3 id="cat-modal-title" className="text-lg font-bold text-text-primary">
              Nueva Categoría
            </h3>
            <p className="text-xs text-text-secondary">Personalizá tus gastos o ingresos</p>
          </div>
        </div>

        {error && (
          <div className="mb-4 p-3 rounded-xl bg-expense/15 border border-expense/30 text-xs text-expense">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="cat-nombre" className="block text-xs font-semibold text-text-secondary mb-1.5">
              Nombre de la categoría *
            </label>
            <input
              id="cat-nombre"
              type="text"
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              placeholder="Ej: Gimnasio, Mascotas..."
              className="glass-input"
              autoFocus
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-text-secondary mb-1.5">
              Tipo de movimiento *
            </label>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => setTipo(TipoMovimiento.Gasto)}
                className={`py-2 px-3 rounded-xl text-xs font-semibold border transition-all ${
                  tipo === TipoMovimiento.Gasto
                    ? 'bg-expense/20 border-expense text-expense'
                    : 'bg-brand-800/40 border-brand-300/20 text-text-secondary'
                }`}
              >
                − Gasto
              </button>
              <button
                type="button"
                onClick={() => setTipo(TipoMovimiento.Ingreso)}
                className={`py-2 px-3 rounded-xl text-xs font-semibold border transition-all ${
                  tipo === TipoMovimiento.Ingreso
                    ? 'bg-income/20 border-income text-income'
                    : 'bg-brand-800/40 border-brand-300/20 text-text-secondary'
                }`}
              >
                + Ingreso
              </button>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-3 border-t border-brand-300/10">
            <button
              type="button"
              onClick={onClose}
              disabled={createCategoria.isPending}
              className="btn-secondary text-xs py-2 px-4"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={createCategoria.isPending}
              className="btn-primary text-xs py-2 px-5"
            >
              {createCategoria.isPending ? 'Guardando...' : 'Crear Categoría'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
