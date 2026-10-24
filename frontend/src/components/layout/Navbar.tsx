import React from 'react';
import { NavLink } from 'react-router-dom';
import { Home, Clock, BarChart3 } from 'lucide-react';

export const NAV_ITEMS = [
  {
    to: '/',
    label: 'Inicio',
    icon: Home,
  },
  {
    to: '/pendientes',
    label: 'Pendientes',
    icon: Clock,
  },
  {
    to: '/reportes',
    label: 'Reportes',
    icon: BarChart3,
  },
];

export const BottomNav: React.FC = () => {
  return (
    <nav
      className="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-brand-900/90 backdrop-blur-lg border-t border-brand-300/15 pb-[env(safe-area-inset-bottom)] px-3 pt-2 shadow-2xl"
      aria-label="Navegación principal inferior"
    >
      <div className="grid grid-cols-3 max-w-md mx-auto">
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex flex-col items-center justify-center py-1.5 px-2 rounded-2xl min-h-[48px] min-w-touch transition-all duration-200 ${
                  isActive
                    ? 'text-brand-500 font-semibold'
                    : 'text-text-secondary hover:text-text-primary'
                }`
              }
            >
              {({ isActive }) => (
                <>
                  <div
                    className={`p-1 rounded-xl transition-all ${
                      isActive ? 'bg-brand-500/15 shadow-glow scale-105' : ''
                    }`}
                  >
                    <Icon className="w-5 h-5 stroke-[2.2]" aria-hidden="true" />
                  </div>
                  <span className="text-[11px] mt-0.5 tracking-tight">{item.label}</span>
                </>
              )}
            </NavLink>
          );
        })}
      </div>
    </nav>
  );
};

export const DesktopSidebar: React.FC = () => {
  return (
    <aside
      className="hidden md:flex flex-col w-64 border-r border-brand-300/15 bg-brand-900/60 backdrop-blur-md min-h-[calc(100vh-61px)] p-4 shrink-0"
      aria-label="Navegación lateral escritorio"
    >
      <div className="space-y-1.5">
        <p className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-text-muted">
          Principal
        </p>
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3.5 py-3 rounded-2xl font-medium text-sm transition-all duration-200 min-h-touch ${
                  isActive
                    ? 'bg-brand-500 text-brand-900 font-semibold shadow-glow translate-x-1'
                    : 'text-text-secondary hover:text-text-primary hover:bg-brand-800/40'
                }`
              }
            >
              {({ isActive }) => (
                <>
                  <Icon
                    className={`w-5 h-5 ${
                      isActive ? 'text-brand-900 stroke-[2.5]' : 'text-brand-300'
                    }`}
                    aria-hidden="true"
                  />
                  <span>{item.label}</span>
                </>
              )}
            </NavLink>
          );
        })}
      </div>

      <div className="mt-auto p-4 rounded-2xl bg-brand-800/40 border border-brand-300/15 text-xs text-text-secondary">
        <p className="font-semibold text-text-primary mb-1">Moneda del sistema</p>
        <p className="flex items-center gap-1.5 text-brand-300">
          <span className="w-2 h-2 rounded-full bg-income" />
          Pesos Argentinos (ARS)
        </p>
      </div>
    </aside>
  );
};
