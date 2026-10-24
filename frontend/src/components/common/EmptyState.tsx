import React from 'react';
import { LucideIcon, Inbox } from 'lucide-react';

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  className?: string;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon: Icon = Inbox,
  title,
  description,
  action,
  className = '',
}) => {
  return (
    <div
      className={`glass-card p-8 text-center flex flex-col items-center justify-center space-y-3 ${className}`}
      role="status"
    >
      <div className="w-14 h-14 rounded-full bg-brand-800/60 border border-brand-300/20 flex items-center justify-center text-brand-300 shadow-glow">
        <Icon className="w-7 h-7" aria-hidden="true" />
      </div>
      <h3 className="text-lg font-semibold text-text-primary">{title}</h3>
      {description && (
        <p className="text-sm text-text-secondary max-w-sm mx-auto leading-relaxed">
          {description}
        </p>
      )}
      {action && (
        <button
          type="button"
          onClick={action.onClick}
          className="btn-primary mt-2 text-sm"
        >
          {action.label}
        </button>
      )}
    </div>
  );
};
