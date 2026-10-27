import React from 'react';
import { LogOut, Database, RefreshCw, PanelLeftClose, PanelLeftOpen } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { useSidebar } from '../../context/SidebarContext';
import { apiService } from '../../api/apiService';

export const Header: React.FC = () => {
  const { logout } = useAuth();
  const { isCollapsed, toggleSidebar } = useSidebar();
  const isMock = apiService.isMock();

  const handleResetMock = () => {
    if (confirm('¿Restablecer los datos mock a su estado original?')) {
      apiService.mock.reset();
      window.location.reload();
    }
  };

  return (
    <header className="sticky top-0 z-30 w-full backdrop-blur-md bg-brand-900/75 border-b border-brand-300/15 px-4 sm:px-6 py-3 transition-colors">
      <div className="max-w-[1440px] mx-auto flex items-center justify-between">
        {/* Logo y botón colapsar barra lateral */}
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={toggleSidebar}
            title={isCollapsed ? 'Expandir barra lateral' : 'Colapsar barra lateral'}
            className="hidden md:flex p-1.5 rounded-xl text-text-muted hover:text-brand-300 hover:bg-brand-800/60 border border-brand-300/10 transition-colors cursor-pointer"
            aria-label="Alternar barra lateral"
          >
            {isCollapsed ? (
              <PanelLeftOpen className="w-5 h-5 text-brand-300" />
            ) : (
              <PanelLeftClose className="w-5 h-5 text-brand-300" />
            )}
          </button>

          <div className="flex items-center gap-2">
            <span className="font-extrabold tracking-wider text-xl sm:text-2xl bg-gradient-to-r from-brand-100 via-brand-500 to-brand-300 bg-clip-text text-transparent select-none">
              GASTITO
            </span>
          </div>

          {/* Badge de datos Mock vs API real */}
          {isMock ? (
            <button
              type="button"
              onClick={handleResetMock}
              title="Click para reiniciar datos simulados"
              className="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium bg-brand-800/80 text-brand-300 border border-brand-500/30 hover:bg-brand-700/80 transition-colors"
            >
              <Database className="w-3 h-3 text-brand-500" />
              <span>Datos Mock</span>
              <RefreshCw className="w-2.5 h-2.5 opacity-70" />
            </button>
          ) : (
            <span className="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium bg-income-bg text-income border border-income-border">
              <span className="w-1.5 h-1.5 rounded-full bg-income animate-pulse" />
              <span>API Conectada</span>
            </span>
          )}
        </div>

        {/* Acciones de cabecera: Cerrar sesión */}
        <div className="flex items-center gap-2">
          {isMock && (
            <button
              type="button"
              onClick={handleResetMock}
              title="Reiniciar datos demo"
              className="sm:hidden p-2 text-brand-300 hover:text-brand-100 rounded-full hover:bg-brand-800/50 transition-colors min-w-touch min-h-touch flex items-center justify-center"
              aria-label="Reiniciar datos demo"
            >
              <RefreshCw className="w-4 h-4" />
            </button>
          )}

          <button
            type="button"
            onClick={logout}
            className="flex items-center gap-2 px-3 py-2 text-sm text-text-secondary hover:text-text-primary hover:bg-brand-800/60 rounded-xl transition-colors min-h-touch min-w-touch"
            aria-label="Cerrar sesión"
            title="Cerrar sesión"
          >
            <LogOut className="w-5 h-5 text-expense/90" aria-hidden="true" />
            <span className="hidden md:inline font-medium">Salir</span>
          </button>
        </div>
      </div>
    </header>
  );
};
