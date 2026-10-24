import React from 'react';
import { AlertCircle, RotateCcw } from 'lucide-react';

interface ErrorStateProps {
  title?: string;
  message?: string;
  onRetry?: () => void;
  className?: string;
}

export const ErrorState: React.FC<ErrorStateProps> = ({
  title = 'Ha ocurrido un error',
  message = 'No se pudieron cargar los datos. Por favor, reintente en unos instantes.',
  onRetry,
  className = '',
}) => {
  return (
    <div
      className={`glass-card p-6 text-center border-expense/30 bg-expense-bg/20 flex flex-col items-center justify-center space-y-3 ${className}`}
      role="alert"
    >
      <div className="w-12 h-12 rounded-full bg-expense/20 border border-expense/40 flex items-center justify-center text-expense">
        <AlertCircle className="w-6 h-6" aria-hidden="true" />
      </div>
      <h3 className="text-base font-semibold text-expense">{title}</h3>
      <p className="text-sm text-text-secondary max-w-sm mx-auto">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="btn-secondary mt-1 text-sm border-expense/30 hover:bg-expense/10 text-text-primary"
        >
          <RotateCcw className="w-4 h-4 mr-1.5" />
          Reintentar
        </button>
      )}
    </div>
  );
};
