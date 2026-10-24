import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Lock, User, ArrowRight, ShieldCheck, AlertCircle } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';

export const Login: React.FC = () => {
  const navigate = useNavigate();
  const { login, loading, error: authError } = useAuth();

  const [usuario, setUsuario] = useState('admin');
  const [contrasena, setContrasena] = useState('admin');
  const [localError, setLocalError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLocalError(null);

    if (!usuario.trim() || !contrasena.trim()) {
      setLocalError('Por favor ingresá usuario y contraseña.');
      return;
    }

    try {
      await login(usuario.trim(), contrasena.trim());
      navigate('/');
    } catch (err: any) {
      setLocalError(err.message || 'Error al iniciar sesión.');
    }
  };

  const handleFillDevCredentials = () => {
    setUsuario('admin');
    setContrasena('admin');
    setLocalError(null);
  };

  const errorMessage = localError || authError;

  return (
    <div className="min-h-screen min-h-[100dvh] flex items-center justify-center p-4 bg-gradient-to-b from-brand-900 via-brand-900 to-brand-800 text-text-primary selection:bg-brand-500 selection:text-brand-900">
      <div className="w-full max-w-md">
        {/* Card principal con efecto vidrio Nexora */}
        <div className="glass-card p-6 sm:p-8 border-brand-300/25 bg-brand-900/80 shadow-2xl space-y-6 animate-fade-in">
          {/* Logo provisorio & encabezado */}
          <div className="text-center space-y-2">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-brand-500/20 border border-brand-500/40 text-brand-500 shadow-glow mb-2">
              <span className="text-2xl font-black tracking-tight">G</span>
            </div>
            <h1 className="text-3xl font-extrabold tracking-wider bg-gradient-to-r from-brand-100 via-brand-500 to-brand-300 bg-clip-text text-transparent">
              GASTITO
            </h1>
            <p className="text-xs sm:text-sm text-text-secondary">
              Gestor de ingresos, gastos y cuotas personales
            </p>
          </div>

          {/* Mensaje de error si falla */}
          {errorMessage && (
            <div
              className="p-3.5 rounded-2xl bg-expense/15 border border-expense/30 flex items-start gap-2.5 text-xs text-expense animate-fade-in"
              role="alert"
            >
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{errorMessage}</span>
            </div>
          )}

          {/* Formulario */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label
                htmlFor="login-usuario"
                className="block text-xs font-semibold text-text-secondary mb-1.5"
              >
                Usuario
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-brand-300">
                  <User className="w-4 h-4" />
                </div>
                <input
                  id="login-usuario"
                  type="text"
                  autoComplete="username"
                  required
                  value={usuario}
                  onChange={(e) => setUsuario(e.target.value)}
                  placeholder="admin"
                  className="glass-input pl-10"
                />
              </div>
            </div>

            <div>
              <label
                htmlFor="login-contrasena"
                className="block text-xs font-semibold text-text-secondary mb-1.5"
              >
                Contraseña
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-brand-300">
                  <Lock className="w-4 h-4" />
                </div>
                <input
                  id="login-contrasena"
                  type="password"
                  autoComplete="current-password"
                  required
                  value={contrasena}
                  onChange={(e) => setContrasena(e.target.value)}
                  placeholder="••••••••"
                  className="glass-input pl-10"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="btn-primary w-full mt-2 text-sm font-bold min-h-[48px]"
            >
              {loading ? (
                <span>Ingresando...</span>
              ) : (
                <>
                  <span>Ingresar</span>
                  <ArrowRight className="w-4 h-4 ml-1" />
                </>
              )}
            </button>
          </form>

          {/* Credenciales de desarrollo */}
          <div className="p-3.5 rounded-2xl bg-brand-800/40 border border-brand-300/15 text-xs text-text-secondary flex items-center justify-between">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-income" />
              <span>
                Acceso demo: <strong>admin</strong> / <strong>admin</strong>
              </span>
            </div>
            <button
              type="button"
              onClick={handleFillDevCredentials}
              className="text-xs text-brand-500 hover:text-brand-300 font-semibold underline underline-offset-2"
            >
              Autocompletar
            </button>
          </div>
        </div>

        <p className="text-center text-xs text-text-muted mt-6">
          Moneda exclusiva: Pesos Argentinos (ARS) · Single-Tenant
        </p>
      </div>
    </div>
  );
};
