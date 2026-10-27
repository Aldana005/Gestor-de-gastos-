import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  Home,
  Clock,
  BarChart3,
  ChevronLeft,
  ChevronRight,
  Coins,
} from 'lucide-react';
import { useSidebar } from '../../context/SidebarContext';

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
  const { isCollapsed, toggleSidebar } = useSidebar();

  return (
    <aside
      className={`hidden md:flex flex-col border-r border-brand-300/15 bg-brand-900/60 backdrop-blur-md min-h-[calc(100vh-61px)] shrink-0 transition-all duration-300 ease-in-out relative ${
        isCollapsed ? 'w-20 p-3 items-center' : 'w-56 p-4'
      }`}
      aria-label="Navegación lateral escritorio"
    >
      {/* Cabecera del sidebar: Título y botón colapsar */}
      <div
        className={`flex items-center mb-4 ${
          isCollapsed ? 'justify-center w-full' : 'justify-between w-full'
        }`}
      >
        {!isCollapsed && (
          <p className="px-2 text-xs font-semibold uppercase tracking-wider text-text-muted select-none">
            Principal
          </p>
        )}
        <button
          type="button"
          onClick={toggleSidebar}
          title={isCollapsed ? 'Expandir barra lateral' : 'Colapsar barra lateral'}
          className="p-1.5 rounded-xl text-text-muted hover:text-brand-300 hover:bg-brand-800/60 border border-brand-300/10 hover:border-brand-300/25 transition-all flex items-center justify-center cursor-pointer"
          aria-label={isCollapsed ? 'Expandir barra lateral' : 'Colapsar barra lateral'}
        >
          {isCollapsed ? (
            <ChevronRight className="w-4 h-4 text-brand-300" />
          ) : (
            <ChevronLeft className="w-4 h-4 text-brand-300" />
          )}
        </button>
      </div>

      {/* Lista de navegación */}
      <nav className="space-y-2 w-full" aria-label="Menú principal">
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `group relative flex items-center rounded-2xl font-medium text-sm transition-all duration-200 min-h-touch ${
                  isCollapsed ? 'justify-center px-0 w-11 h-11 mx-auto' : 'gap-3 px-3.5 py-2.5'
                } ${
                  isActive
                    ? 'bg-brand-500 text-brand-900 font-semibold shadow-glow'
                    : 'text-text-secondary hover:text-text-primary hover:bg-brand-800/40'
                }`
              }
            >
              {({ isActive }) => (
                <>
                  <Icon
                    className={`w-5 h-5 shrink-0 transition-transform ${
                      isActive ? 'text-brand-900 stroke-[2.5]' : 'text-brand-300 group-hover:scale-110'
                    }`}
                    aria-hidden="true"
                  />
                  {!isCollapsed && (
                    <span className="truncate">{item.label}</span>
                  )}

                  {/* Tooltip flyout en modo colapsado (según referencia del usuario) */}
                  {isCollapsed && (
                    <div className="absolute left-full ml-3 px-3 py-1.5 rounded-xl bg-brand-800 text-xs font-semibold text-text-primary shadow-2xl border border-brand-300/25 whitespace-nowrap opacity-0 group-hover:opacity-100 group-hover:translate-x-0 -translate-x-2 pointer-events-none transition-all duration-200 z-50">
                      {item.label}
                      <div className="absolute right-full top-1/2 -translate-y-1/2 border-4 border-transparent border-r-brand-800" />
                    </div>
                  )}
                </>
              )}
            </NavLink>
          );
        })}
      </nav>

      {/* Sección inferior: Moneda del sistema */}
      <div className="mt-auto w-full pt-4">
        {isCollapsed ? (
          <div className="group relative flex justify-center">
            <div className="w-10 h-10 rounded-2xl bg-brand-800/60 border border-brand-300/15 flex items-center justify-center text-brand-300 hover:text-brand-100 transition-colors cursor-default">
              <Coins className="w-4 h-4 text-income" />
            </div>
            {/* Tooltip flyout */}
            <div className="absolute left-full ml-3 px-3 py-1.5 rounded-xl bg-brand-800 text-xs font-medium text-text-primary shadow-2xl border border-brand-300/25 whitespace-nowrap opacity-0 group-hover:opacity-100 group-hover:translate-x-0 -translate-x-2 pointer-events-none transition-all duration-200 z-50">
              <p className="font-semibold text-text-primary">Moneda del sistema</p>
              <p className="text-brand-300 text-[11px]">Pesos Argentinos (ARS)</p>
              <div className="absolute right-full top-1/2 -translate-y-1/2 border-4 border-transparent border-r-brand-800" />
            </div>
          </div>
        ) : (
          <div className="p-3 rounded-2xl bg-brand-800/40 border border-brand-300/15 text-xs text-text-secondary">
            <p className="font-semibold text-text-primary mb-1 text-[11px]">Moneda del sistema</p>
            <p className="flex items-center gap-1.5 text-brand-300 text-[11px]">
              <span className="w-2 h-2 rounded-full bg-income shrink-0" />
              <span className="truncate">Pesos Argentinos (ARS)</span>
            </p>
          </div>
        )}
      </div>
    </aside>
  );
};
