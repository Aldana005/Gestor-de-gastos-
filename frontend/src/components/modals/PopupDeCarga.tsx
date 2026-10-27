import React, { useState, useEffect } from 'react';
import {
  X,
  PlusCircle,
  CreditCard,
  Banknote,
  ArrowRightLeft,
  Calendar,
  AlertCircle,
  Tag,
  CheckCircle2,
} from 'lucide-react';
import {
  TipoMovimiento,
  Frecuencia,
  MedioPago,
  TipoTarjeta,
  AplicarDesde,
  TransactionOutput,
} from '../../api/types';
import { useCategorias, useTarjetas } from '../../hooks/useQueries';
import { useMovimientoMutations } from '../../hooks/useMutations';
import { CategoryModal } from './CategoryModal';
import { TarjetaModal } from './TarjetaModal';
import {
  getTodayCalendarString,
  toCalendarDateString,
  getNextMonthFirstDayString,
  formatMonthYearAR,
} from '../../utils/formatters';

interface PopupDeCargaProps {
  isOpen: boolean;
  onClose: () => void;
  editingTransaction?: TransactionOutput | null;
  onSuccess?: () => void;
}

export const PopupDeCarga: React.FC<PopupDeCargaProps> = ({
  isOpen,
  onClose,
  editingTransaction = null,
  onSuccess,
}) => {
  const isEditing = Boolean(editingTransaction);

  // Form States
  const [tipo, setTipo] = useState<TipoMovimiento>(TipoMovimiento.Gasto);
  const [frecuencia, setFrecuencia] = useState<Frecuencia>(Frecuencia.Eventual);
  const [monto, setMonto] = useState<string>('');
  const [descripcion, setDescripcion] = useState<string>('');
  const [notas, setNotas] = useState<string>('');
  const [categoryId, setCategoryId] = useState<number | ''>('');
  const [fecha, setFecha] = useState<string>(getTodayCalendarString());
  const [fechaFin, setFechaFin] = useState<string>('');

  // Gasto States
  const [medioPago, setMedioPago] = useState<MedioPago>(MedioPago.Tarjeta);
  const [tarjetaId, setTarjetaId] = useState<number | ''>('');
  const [tieneCuotas, setTieneCuotas] = useState<boolean>(false);
  const [montoTotal, setMontoTotal] = useState<string>('');
  const [cantidadCuotas, setCantidadCuotas] = useState<string>('3');
  const [fechaPrimeraCuota, setFechaPrimeraCuota] = useState<string>(getTodayCalendarString());

  // Regla Fijo editando monto
  const [aplicarDesde, setAplicarDesde] = useState<AplicarDesde>(AplicarDesde.MesActual);
  const [originalMonto, setOriginalMonto] = useState<number | null>(null);

  // Secondary modals
  const [isCategoryModalOpen, setIsCategoryModalOpen] = useState(false);
  const [isTarjetaModalOpen, setIsTarjetaModalOpen] = useState(false);

  // Error states
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  // Queries & Mutations
  const { data: categorias = [] } = useCategorias(tipo);
  const { data: tarjetas = [] } = useTarjetas();
  const { createMovimiento, updateMovimiento } = useMovimientoMutations();

  // Selected Tarjeta details
  const selectedTarjeta = tarjetas.find((t) => t.id === Number(tarjetaId));
  const isCreditCard = selectedTarjeta?.tipoTarjeta === TipoTarjeta.Credito;

  // Initialize or reset on open / edit
  useEffect(() => {
    if (editingTransaction) {
      setTipo(editingTransaction.tipo);
      setFrecuencia(editingTransaction.frecuencia);
      setMonto(String(editingTransaction.monto || ''));
      setOriginalMonto(editingTransaction.monto);
      setDescripcion(editingTransaction.descripcion || '');
      setNotas(editingTransaction.notas || '');
      setCategoryId(editingTransaction.categoryId);
      setFecha(toCalendarDateString(editingTransaction.fecha));
      setFechaFin(editingTransaction.fechaFin ? toCalendarDateString(editingTransaction.fechaFin) : '');
      setMedioPago(editingTransaction.medioPago || MedioPago.Tarjeta);
      setTarjetaId(editingTransaction.tarjetaId || '');
      const hasInstallments = Boolean(editingTransaction.cantidadCuotas && editingTransaction.cantidadCuotas >= 2);
      setTieneCuotas(hasInstallments);
      setMontoTotal(editingTransaction.montoTotal ? String(editingTransaction.montoTotal) : '');
      setCantidadCuotas(editingTransaction.cantidadCuotas ? String(editingTransaction.cantidadCuotas) : '3');
      setFechaPrimeraCuota(
        editingTransaction.fechaPrimeraCuota
          ? toCalendarDateString(editingTransaction.fechaPrimeraCuota)
          : getTodayCalendarString()
      );
    } else {
      setTipo(TipoMovimiento.Gasto);
      setFrecuencia(Frecuencia.Eventual);
      setMonto('');
      setOriginalMonto(null);
      setDescripcion('');
      setNotas('');
      setCategoryId('');
      setFecha(getTodayCalendarString());
      setFechaFin('');
      setMedioPago(MedioPago.Tarjeta);
      setTarjetaId(tarjetas.length > 0 ? tarjetas[0].id : '');
      setTieneCuotas(false);
      setMontoTotal('');
      setCantidadCuotas('3');
      setFechaPrimeraCuota(getTodayCalendarString());
      setAplicarDesde(AplicarDesde.MesActual);
    }
    setFormError(null);
    setFieldErrors({});
  }, [editingTransaction, isOpen, tarjetas]);

  // Si no hay tarjeta seleccionada y se elige tarjeta, preseleccionar la primera
  useEffect(() => {
    if (medioPago === MedioPago.Tarjeta && !tarjetaId && tarjetas.length > 0) {
      setTarjetaId(tarjetas[0].id);
    }
  }, [medioPago, tarjetaId, tarjetas]);

  // Si cambia de tarjeta y no es crédito, apagar cuotas
  useEffect(() => {
    if (!isCreditCard) {
      setTieneCuotas(false);
    }
  }, [isCreditCard]);

  // Si tiene cuotas, no admite fijos
  useEffect(() => {
    if (tieneCuotas) {
      setFrecuencia(Frecuencia.Eventual);
    }
  }, [tieneCuotas]);

  if (!isOpen) return null;

  // Validación de cuotas bloqueadas en edición
  const hasPaidInstallments = Boolean(
    isEditing &&
      editingTransaction?.cantidadCuotas &&
      (editingTransaction.cuotasPagadas || 0) > 0
  );

  // Validación de cambio de monto en fijos
  const showAplicarDesdeSelector = Boolean(
    isEditing &&
      editingTransaction?.frecuencia === Frecuencia.Fijo &&
      originalMonto !== null &&
      parseFloat(monto) !== originalMonto &&
      !isNaN(parseFloat(monto))
  );

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!descripcion.trim()) {
      errors.descripcion = 'La descripción es obligatoria.';
    }

    if (!categoryId) {
      errors.categoryId = 'Seleccioná una categoría.';
    }

    if (tipo === TipoMovimiento.Gasto && tieneCuotas && isCreditCard) {
      const parsedMontoTotal = parseFloat(montoTotal);
      if (isNaN(parsedMontoTotal) || parsedMontoTotal <= 0) {
        errors.montoTotal = 'Ingresá un monto total válido mayor a $ 0.';
      }
      const parsedCuotas = parseInt(cantidadCuotas, 10);
      if (isNaN(parsedCuotas) || parsedCuotas < 2) {
        errors.cantidadCuotas = 'La cantidad de cuotas debe ser mínimo 2.';
      }
      if (!fechaPrimeraCuota) {
        errors.fechaPrimeraCuota = 'Ingresá la fecha de la primera cuota.';
      }
    } else {
      const parsedMonto = parseFloat(monto);
      if (isNaN(parsedMonto) || parsedMonto <= 0) {
        errors.monto = 'Ingresá un monto válido mayor a $ 0.';
      }
    }

    if (tipo === TipoMovimiento.Gasto && medioPago === MedioPago.Tarjeta && !tarjetaId) {
      errors.tarjetaId = 'Seleccioná una tarjeta o creá una nueva.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!validateForm()) return;

    try {
      if (isEditing && editingTransaction) {
        await updateMovimiento.mutateAsync({
          id: editingTransaction.id,
          input: {
            tipo,
            frecuencia,
            monto: tieneCuotas ? parseFloat(montoTotal) : parseFloat(monto),
            fecha,
            descripcion: descripcion.trim(),
            notas: notas.trim() || null,
            categoryId: Number(categoryId),
            medioPago: tipo === TipoMovimiento.Gasto ? medioPago : null,
            tarjetaId: tipo === TipoMovimiento.Gasto && medioPago === MedioPago.Tarjeta ? Number(tarjetaId) : null,
            fechaInicio: frecuencia === Frecuencia.Fijo ? fecha : null,
            fechaFin: frecuencia === Frecuencia.Fijo && fechaFin ? fechaFin : null,
            aplicarDesde: showAplicarDesdeSelector ? aplicarDesde : null,
            montoTotal: tieneCuotas ? parseFloat(montoTotal) : null,
            cantidadCuotas: tieneCuotas ? parseInt(cantidadCuotas, 10) : null,
            fechaPrimeraCuota: tieneCuotas ? fechaPrimeraCuota : null,
          },
          aplicarDesde: showAplicarDesdeSelector ? aplicarDesde : undefined,
        });
      } else {
        await createMovimiento.mutateAsync({
          tipo,
          frecuencia,
          monto: tieneCuotas ? parseFloat(montoTotal) : parseFloat(monto),
          fecha,
          descripcion: descripcion.trim(),
          notas: notas.trim() || null,
          categoryId: Number(categoryId),
          medioPago: tipo === TipoMovimiento.Gasto ? medioPago : null,
          tarjetaId: tipo === TipoMovimiento.Gasto && medioPago === MedioPago.Tarjeta ? Number(tarjetaId) : null,
          fechaInicio: frecuencia === Frecuencia.Fijo ? fecha : null,
          fechaFin: frecuencia === Frecuencia.Fijo && fechaFin ? fechaFin : null,
          montoTotal: tieneCuotas ? parseFloat(montoTotal) : null,
          cantidadCuotas: tieneCuotas ? parseInt(cantidadCuotas, 10) : null,
          fechaPrimeraCuota: tieneCuotas ? fechaPrimeraCuota : null,
        });
      }

      onSuccess?.();
      onClose();
    } catch (err: any) {
      if (err.problemDetails?.errors) {
        const serverErrors: Record<string, string> = {};
        for (const [k, v] of Object.entries(err.problemDetails.errors)) {
          serverErrors[k.toLowerCase()] = Array.isArray(v) ? (v as string[]).join(' ') : String(v);
        }
        setFieldErrors(serverErrors);
      }
      setFormError(err.message || 'Error al guardar el movimiento.');
    }
  };

  return (
    <>
      <div
        className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/80 backdrop-blur-md animate-fade-in"
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-carga-title"
      >
        <div className="glass-card w-full sm:max-w-xl max-h-[92vh] sm:max-h-[88vh] flex flex-col bg-brand-900/95 border-brand-300/30 rounded-t-3xl sm:rounded-3xl shadow-2xl overflow-hidden animate-slide-up sm:animate-fade-in">
          {/* Header del Modal */}
          <div className="flex items-center justify-between p-4 sm:p-5 border-b border-brand-300/15">
            <div>
              <h2 id="modal-carga-title" className="text-lg sm:text-xl font-bold text-text-primary">
                {isEditing ? 'Editar Movimiento' : 'Registrar Movimiento'}
              </h2>
              <p className="text-xs text-text-secondary">Moneda: Pesos Argentinos (ARS)</p>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="p-2 text-text-secondary hover:text-text-primary rounded-full hover:bg-brand-800/60 min-w-touch min-h-touch flex items-center justify-center"
              aria-label="Cerrar ventana de carga"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Selector de Pestañas: Ingresos / Gastos */}
          <div className="p-4 sm:px-6 pb-2">
            <div className="grid grid-cols-2 p-1 rounded-2xl bg-brand-800/60 border border-brand-300/15">
              <button
                type="button"
                onClick={() => {
                  setTipo(TipoMovimiento.Gasto);
                  setCategoryId('');
                }}
                disabled={isEditing}
                className={`py-2.5 px-4 rounded-xl text-sm font-bold flex items-center justify-center gap-2 transition-all min-h-touch ${
                  tipo === TipoMovimiento.Gasto
                    ? 'bg-expense text-white shadow-lg'
                    : 'text-text-secondary hover:text-text-primary'
                }`}
              >
                <span>− Gasto</span>
              </button>
              <button
                type="button"
                onClick={() => {
                  setTipo(TipoMovimiento.Ingreso);
                  setTieneCuotas(false);
                  setCategoryId('');
                }}
                disabled={isEditing}
                className={`py-2.5 px-4 rounded-xl text-sm font-bold flex items-center justify-center gap-2 transition-all min-h-touch ${
                  tipo === TipoMovimiento.Ingreso
                    ? 'bg-income text-brand-900 shadow-lg'
                    : 'text-text-secondary hover:text-text-primary'
                }`}
              >
                <span>+ Ingreso</span>
              </button>
            </div>
          </div>

          {/* Formulario scrollable */}
          <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
            {formError && (
              <div className="p-3.5 rounded-xl bg-expense/15 border border-expense/30 flex items-start gap-2.5 text-xs text-expense">
                <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                <span>{formError}</span>
              </div>
            )}

            {/* SECCIÓN ESPECÍFICA PARA GASTOS: Medio de Pago */}
            {tipo === TipoMovimiento.Gasto && (
              <div className="space-y-3 p-3.5 rounded-2xl bg-brand-800/40 border border-brand-300/15">
                <label className="block text-xs font-semibold text-text-secondary">
                  1. Medio de Pago *
                </label>
                <div className="grid grid-cols-3 gap-2">
                  <button
                    type="button"
                    onClick={() => setMedioPago(MedioPago.Tarjeta)}
                    className={`py-2 px-2.5 rounded-xl text-xs font-medium border flex items-center justify-center gap-1.5 transition-all min-h-touch ${
                      medioPago === MedioPago.Tarjeta
                        ? 'bg-brand-500/25 border-brand-500 text-brand-100 font-bold'
                        : 'bg-brand-900/40 border-brand-300/20 text-text-secondary'
                    }`}
                  >
                    <CreditCard className="w-3.5 h-3.5" />
                    <span>Tarjeta</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setMedioPago(MedioPago.Efectivo);
                      setTieneCuotas(false);
                    }}
                    className={`py-2 px-2.5 rounded-xl text-xs font-medium border flex items-center justify-center gap-1.5 transition-all min-h-touch ${
                      medioPago === MedioPago.Efectivo
                        ? 'bg-brand-500/25 border-brand-500 text-brand-100 font-bold'
                        : 'bg-brand-900/40 border-brand-300/20 text-text-secondary'
                    }`}
                  >
                    <Banknote className="w-3.5 h-3.5" />
                    <span>Efectivo</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setMedioPago(MedioPago.Transferencia);
                      setTieneCuotas(false);
                    }}
                    className={`py-2 px-2.5 rounded-xl text-xs font-medium border flex items-center justify-center gap-1.5 transition-all min-h-touch ${
                      medioPago === MedioPago.Transferencia
                        ? 'bg-brand-500/25 border-brand-500 text-brand-100 font-bold'
                        : 'bg-brand-900/40 border-brand-300/20 text-text-secondary'
                    }`}
                  >
                    <ArrowRightLeft className="w-3.5 h-3.5" />
                    <span>Transfer.</span>
                  </button>
                </div>

                {/* Si es Tarjeta: elegir tarjeta + crear nueva */}
                {medioPago === MedioPago.Tarjeta && (
                  <div className="pt-2 border-t border-brand-300/10 space-y-2">
                    <div className="flex items-center justify-between">
                      <label htmlFor="tarjeta-select" className="text-xs font-medium text-text-secondary">
                        Seleccionar Tarjeta *
                      </label>
                      <button
                        type="button"
                        onClick={() => setIsTarjetaModalOpen(true)}
                        className="text-xs text-brand-500 hover:text-brand-300 flex items-center gap-1 font-semibold"
                      >
                        <PlusCircle className="w-3.5 h-3.5" />
                        <span>Crear nueva</span>
                      </button>
                    </div>
                    <select
                      id="tarjeta-select"
                      value={tarjetaId}
                      onChange={(e) => setTarjetaId(Number(e.target.value))}
                      className="glass-input text-xs"
                    >
                      <option value="" disabled className="bg-brand-900">
                        -- Elegir tarjeta --
                      </option>
                      {tarjetas.map((t) => (
                        <option key={t.id} value={t.id} className="bg-brand-900">
                          {t.nombre} ({t.tipoTarjeta === TipoTarjeta.Credito ? 'Crédito' : 'Débito'})
                        </option>
                      ))}
                    </select>
                    {fieldErrors.tarjetaId && (
                      <p className="text-xs text-expense">{fieldErrors.tarjetaId}</p>
                    )}

                    {/* Si la tarjeta es de Crédito: ¿Tiene cuotas? Sí / No */}
                    {isCreditCard && (
                      <div className="pt-3 border-t border-brand-300/10 space-y-3">
                        <div className="flex items-center justify-between">
                          <span className="text-xs font-semibold text-text-primary">
                            ¿Tiene cuotas?
                          </span>
                          <div className="flex gap-1.5 p-1 rounded-xl bg-brand-900/60 border border-brand-300/15">
                            <button
                              type="button"
                              disabled={hasPaidInstallments}
                              onClick={() => setTieneCuotas(true)}
                              className={`px-3 py-1 rounded-lg text-xs font-bold transition-all ${
                                tieneCuotas
                                  ? 'bg-brand-500 text-brand-900'
                                  : 'text-text-secondary hover:text-text-primary'
                              }`}
                            >
                              Sí
                            </button>
                            <button
                              type="button"
                              disabled={hasPaidInstallments}
                              onClick={() => setTieneCuotas(false)}
                              className={`px-3 py-1 rounded-lg text-xs font-bold transition-all ${
                                !tieneCuotas
                                  ? 'bg-brand-500 text-brand-900'
                                  : 'text-text-secondary hover:text-text-primary'
                              }`}
                            >
                              No
                            </button>
                          </div>
                        </div>

                        {/* Alerta de bloqueo por cuotas ya pagadas */}
                        {hasPaidInstallments && (
                          <div className="p-2.5 rounded-xl bg-warning-bg/30 border border-warning/40 text-xs text-warning flex items-start gap-2">
                            <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                            <span>
                              Esta compra ya tiene cuotas pagadas o adelantadas. El monto total y cantidad de cuotas están protegidos y no pueden modificarse.
                            </span>
                          </div>
                        )}

                        {/* Campos de cuotas: Monto Total, Cantidad de cuotas y Fecha primera cuota */}
                        {tieneCuotas && (
                          <div className="p-3 rounded-xl bg-brand-900/60 border border-brand-300/20 space-y-3">
                            <div>
                              <label htmlFor="monto-total" className="block text-xs font-medium text-text-secondary mb-1">
                                Monto total de la compra (ARS) *
                              </label>
                              <div className="relative">
                                <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-brand-300 font-bold text-sm">
                                  $
                                </span>
                                <input
                                  id="monto-total"
                                  type="number"
                                  step="0.01"
                                  disabled={hasPaidInstallments}
                                  value={montoTotal}
                                  onChange={(e) => setMontoTotal(e.target.value)}
                                  placeholder="0,00"
                                  className="glass-input pl-8 font-bold text-base"
                                />
                              </div>
                              {fieldErrors.montoTotal && (
                                <p className="text-xs text-expense mt-1">{fieldErrors.montoTotal}</p>
                              )}
                            </div>

                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                              <div>
                                <label htmlFor="cant-cuotas" className="block text-xs font-medium text-text-secondary mb-1">
                                  Cantidad de cuotas (mín. 2) *
                                </label>
                                <input
                                  id="cant-cuotas"
                                  type="number"
                                  min={2}
                                  max={60}
                                  disabled={hasPaidInstallments}
                                  value={cantidadCuotas}
                                  onChange={(e) => setCantidadCuotas(e.target.value)}
                                  className="glass-input font-bold text-center"
                                />
                                {fieldErrors.cantidadCuotas && (
                                  <p className="text-xs text-expense mt-1">{fieldErrors.cantidadCuotas}</p>
                                )}
                              </div>
                              <div>
                                <label htmlFor="primera-cuota" className="block text-xs font-medium text-text-secondary mb-1">
                                  Fecha exacta 1ª cuota *
                                </label>
                                <input
                                  id="primera-cuota"
                                  type="date"
                                  disabled={hasPaidInstallments}
                                  value={fechaPrimeraCuota}
                                  onChange={(e) => setFechaPrimeraCuota(e.target.value)}
                                  className="glass-input text-xs"
                                />
                                {fieldErrors.fechaPrimeraCuota && (
                                  <p className="text-xs text-expense mt-1">{fieldErrors.fechaPrimeraCuota}</p>
                                )}
                              </div>
                            </div>

                            {/* Selector rápido: Camino 1 vs Camino 2 */}
                            <div className="space-y-1.5 pt-2 border-t border-brand-300/10">
                              <label className="block text-xs font-semibold text-text-secondary">
                                ¿Cuándo empieza a correr la 1ª cuota?
                              </label>
                              <div className="grid grid-cols-2 gap-2">
                                <button
                                  type="button"
                                  disabled={hasPaidInstallments}
                                  onClick={() => setFechaPrimeraCuota(fecha)}
                                  className={`py-2 px-3 rounded-xl text-xs font-semibold border transition-all ${
                                    fechaPrimeraCuota.slice(0, 7) <= fecha.slice(0, 7)
                                      ? 'bg-brand-500/25 border-brand-500 text-brand-100 shadow-sm'
                                      : 'bg-brand-800/40 border-brand-300/15 text-text-secondary hover:text-text-primary'
                                  }`}
                                >
                                  Empieza este mes
                                </button>
                                <button
                                  type="button"
                                  disabled={hasPaidInstallments}
                                  onClick={() => setFechaPrimeraCuota(getNextMonthFirstDayString(fecha))}
                                  className={`py-2 px-3 rounded-xl text-xs font-semibold border transition-all ${
                                    fechaPrimeraCuota.slice(0, 7) > fecha.slice(0, 7)
                                      ? 'bg-brand-500/25 border-brand-500 text-brand-100 shadow-sm'
                                      : 'bg-brand-800/40 border-brand-300/15 text-text-secondary hover:text-text-primary'
                                  }`}
                                >
                                  Empieza el mes que viene
                                </button>
                              </div>
                              <p className="text-[11px] text-brand-300 capitalize pt-0.5">
                                📅 Primera cuota programada: <strong>{formatMonthYearAR(fechaPrimeraCuota)}</strong>
                              </p>
                            </div>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}

            {/* Frecuencia: Fijo / Eventual (Fijo NO admite cuotas) */}
            <div className="space-y-1.5">
              <label className="block text-xs font-semibold text-text-secondary">
                Frecuencia del movimiento *
              </label>
              <div className="grid grid-cols-2 gap-2">
                <button
                  type="button"
                  onClick={() => setFrecuencia(Frecuencia.Eventual)}
                  className={`py-2 px-3 rounded-xl text-xs font-semibold border transition-all min-h-touch ${
                    frecuencia === Frecuencia.Eventual
                      ? 'bg-brand-500/20 border-brand-500 text-brand-100'
                      : 'bg-brand-800/40 border-brand-300/15 text-text-secondary'
                  }`}
                >
                  Eventual (único)
                </button>
                <button
                  type="button"
                  disabled={tieneCuotas}
                  onClick={() => setFrecuencia(Frecuencia.Fijo)}
                  className={`py-2 px-3 rounded-xl text-xs font-semibold border transition-all min-h-touch ${
                    frecuencia === Frecuencia.Fijo
                      ? 'bg-brand-500/20 border-brand-500 text-brand-100'
                      : 'bg-brand-800/40 border-brand-300/15 text-text-secondary'
                  } ${tieneCuotas ? 'opacity-40 cursor-not-allowed' : ''}`}
                >
                  Fijo (mensual recurrente)
                </button>
              </div>
              {tieneCuotas && (
                <p className="text-[11px] text-text-muted">
                  * Las compras en cuotas son gastos eventuales y no admiten frecuencia fija.
                </p>
              )}
            </div>

            {/* CAMPO MONTO SIMPLE (Se oculta si tiene cuotas) */}
            {!tieneCuotas && (
              <div>
                <label htmlFor="monto-simple" className="block text-xs font-semibold text-text-secondary mb-1">
                  Monto (ARS) *
                </label>
                <div className="relative">
                  <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-brand-300 font-bold text-base">
                    $
                  </span>
                  <input
                    id="monto-simple"
                    type="number"
                    step="0.01"
                    value={monto}
                    onChange={(e) => setMonto(e.target.value)}
                    placeholder="0,00"
                    className="glass-input pl-8 font-extrabold text-lg text-brand-100"
                  />
                </div>
                {fieldErrors.monto && <p className="text-xs text-expense mt-1">{fieldErrors.monto}</p>}
              </div>
            )}

            {/* SELECTOR "Desde este mes / Desde el mes siguiente" (Al editar fijo con cambio de monto) */}
            {showAplicarDesdeSelector && (
              <div className="p-3.5 rounded-2xl bg-warning-bg/25 border border-warning/40 space-y-2">
                <div className="flex items-center gap-2 text-warning text-xs font-bold">
                  <AlertCircle className="w-4 h-4" />
                  <span>Modificación de monto en movimiento fijo</span>
                </div>
                <p className="text-xs text-text-secondary">
                  El monto cambió de ${originalMonto} a ${monto}. ¿Desde cuándo rige este nuevo valor?
                </p>
                <div className="grid grid-cols-2 gap-2 pt-1">
                  <button
                    type="button"
                    onClick={() => setAplicarDesde(AplicarDesde.MesActual)}
                    className={`py-2 px-2.5 rounded-xl text-xs font-semibold border transition-all ${
                      aplicarDesde === AplicarDesde.MesActual
                        ? 'bg-warning text-brand-900 border-warning font-bold'
                        : 'bg-brand-900/60 border-brand-300/20 text-text-secondary'
                    }`}
                  >
                    Desde este mes
                  </button>
                  <button
                    type="button"
                    onClick={() => setAplicarDesde(AplicarDesde.MesSiguiente)}
                    className={`py-2 px-2.5 rounded-xl text-xs font-semibold border transition-all ${
                      aplicarDesde === AplicarDesde.MesSiguiente
                        ? 'bg-warning text-brand-900 border-warning font-bold'
                        : 'bg-brand-900/60 border-brand-300/20 text-text-secondary'
                    }`}
                  >
                    Desde el mes siguiente
                  </button>
                </div>
                <p className="text-[11px] text-text-muted">
                  Los meses anteriores conservarán el monto original histórico.
                </p>
              </div>
            )}

            {/* Categoría (+ Crear Nueva) */}
            <div>
              <div className="flex items-center justify-between mb-1">
                <label htmlFor="cat-select" className="text-xs font-semibold text-text-secondary">
                  Categoría *
                </label>
                <button
                  type="button"
                  onClick={() => setIsCategoryModalOpen(true)}
                  className="text-xs text-brand-500 hover:text-brand-300 flex items-center gap-1 font-semibold"
                >
                  <PlusCircle className="w-3.5 h-3.5" />
                  <span>Crear nueva</span>
                </button>
              </div>
              <select
                id="cat-select"
                value={categoryId}
                onChange={(e) => setCategoryId(Number(e.target.value))}
                className="glass-input text-xs"
              >
                <option value="" disabled className="bg-brand-900">
                  -- Seleccionar categoría --
                </option>
                {categorias.map((c) => (
                  <option key={c.id} value={c.id} className="bg-brand-900">
                    {c.nombre} {c.esSistema ? '(Sistema)' : ''}
                  </option>
                ))}
              </select>
              {fieldErrors.categoryId && (
                <p className="text-xs text-expense mt-1">{fieldErrors.categoryId}</p>
              )}
            </div>

            {/* Fecha del movimiento & Fecha de fin (si es fijo) */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label htmlFor="fecha-mov" className="block text-xs font-semibold text-text-secondary mb-1">
                  Fecha *
                </label>
                <input
                  id="fecha-mov"
                  type="date"
                  value={fecha}
                  onChange={(e) => setFecha(e.target.value)}
                  className="glass-input text-xs"
                />
              </div>

              {frecuencia === Frecuencia.Fijo && (
                <div>
                  <label htmlFor="fecha-fin" className="block text-xs font-semibold text-text-secondary mb-1">
                    Fecha de fin (opcional)
                  </label>
                  <input
                    id="fecha-fin"
                    type="date"
                    value={fechaFin}
                    onChange={(e) => setFechaFin(e.target.value)}
                    className="glass-input text-xs"
                  />
                </div>
              )}
            </div>

            {/* Descripción */}
            <div>
              <label htmlFor="desc-input" className="block text-xs font-semibold text-text-secondary mb-1">
                Descripción *
              </label>
              <input
                id="desc-input"
                type="text"
                value={descripcion}
                onChange={(e) => setDescripcion(e.target.value)}
                placeholder="Ej: Compra mensual, Sueldo, Farmacia..."
                className="glass-input text-sm"
              />
              {fieldErrors.descripcion && (
                <p className="text-xs text-expense mt-1">{fieldErrors.descripcion}</p>
              )}
            </div>

            {/* Notas opcionales */}
            <div>
              <label htmlFor="notas-input" className="block text-xs font-semibold text-text-secondary mb-1">
                Notas adicionales (opcional)
              </label>
              <textarea
                id="notas-input"
                rows={2}
                value={notas}
                onChange={(e) => setNotas(e.target.value)}
                placeholder="Detalles adicionales, recordatorios..."
                className="glass-input text-xs resize-none"
              />
            </div>
          </form>

          {/* Footer de Acciones */}
          <div className="p-4 sm:p-5 border-t border-brand-300/15 flex items-center justify-end gap-3 bg-brand-900/90">
            <button
              type="button"
              onClick={onClose}
              disabled={createMovimiento.isPending || updateMovimiento.isPending}
              className="btn-secondary text-sm py-2 px-4"
            >
              Cancelar
            </button>
            <button
              type="button"
              onClick={handleSubmit}
              disabled={createMovimiento.isPending || updateMovimiento.isPending}
              className="btn-primary text-sm py-2 px-6"
            >
              {createMovimiento.isPending || updateMovimiento.isPending ? (
                'Guardando...'
              ) : isEditing ? (
                'Actualizar Movimiento'
              ) : (
                'Guardar Movimiento'
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Modales secundarios */}
      <CategoryModal
        isOpen={isCategoryModalOpen}
        defaultTipo={tipo}
        onClose={() => setIsCategoryModalOpen(false)}
        onSuccess={(newId) => setCategoryId(newId)}
      />

      <TarjetaModal
        isOpen={isTarjetaModalOpen}
        onClose={() => setIsTarjetaModalOpen(false)}
        onSuccess={(newId) => setTarjetaId(newId)}
      />
    </>
  );
};
