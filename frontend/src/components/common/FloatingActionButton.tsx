import React from 'react';
import { Plus } from 'lucide-react';

interface FloatingActionButtonProps {
  onClick: () => void;
  ariaLabel?: string;
}

export const FloatingActionButton: React.FC<FloatingActionButtonProps> = ({
  onClick,
  ariaLabel = 'Registrar nuevo ingreso o gasto',
}) => {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={ariaLabel}
      className="fixed bottom-20 right-5 md:bottom-8 md:right-8 z-40 w-14 h-14 rounded-full bg-brand-500 text-brand-900 font-bold flex items-center justify-center shadow-floating hover:bg-brand-300 hover:scale-105 active:scale-95 transition-all duration-200 focus-visible:ring-4 focus-visible:ring-brand-500/50"
    >
      <Plus className="w-7 h-7 stroke-[2.5]" aria-hidden="true" />
    </button>
  );
};
