import {
  CategoryCreateInput,
  CategoryOutput,
  DashboardOutput,
  Frecuencia,
  GastosPorCategoriaOutput,
  IngresosVsGastosOutput,
  MedioPago,
  PagedResult,
  PagoCuotaOutput,
  PendienteItemOutput,
  ProximaDeudaOutput,
  ReporteResumenOutput,
  TarjetaCreateInput,
  TarjetaOutput,
  TipoMovimiento,
  TransactionCreateInput,
  TransactionOutput,
  TransactionUpdateInput,
  AplicarDesde,
} from './types';
import {
  INITIAL_CATEGORIES,
  INITIAL_TARJETAS,
  INITIAL_TRANSACTIONS,
  INITIAL_PAGOS_CUOTA,
} from './mockData';
import { ApiError } from './client';
import { formatMonthYearAR } from '../utils/formatters';

const STORAGE_KEY_CATEGORIES = 'gastito_mock_categories';
const STORAGE_KEY_TARJETAS = 'gastito_mock_tarjetas';
const STORAGE_KEY_TRANSACTIONS = 'gastito_mock_transactions';
const STORAGE_KEY_PAGOS = 'gastito_mock_pagos';

function loadFromStorage<T>(key: string, fallback: T): T {
  try {
    const item = localStorage.getItem(key);
    return item ? JSON.parse(item) : fallback;
  } catch {
    return fallback;
  }
}

function saveToStorage<T>(key: string, data: T): void {
  try {
    localStorage.setItem(key, JSON.stringify(data));
  } catch (err) {
    console.error('Error guardando en localStorage mock', err);
  }
}

class MockStorage {
  private categories: CategoryOutput[];
  private tarjetas: TarjetaOutput[];
  private transactions: TransactionOutput[];
  private pagos: Record<number, PagoCuotaOutput[]>;

  constructor() {
    this.categories = loadFromStorage(STORAGE_KEY_CATEGORIES, INITIAL_CATEGORIES);
    this.tarjetas = loadFromStorage(STORAGE_KEY_TARJETAS, INITIAL_TARJETAS);
    this.transactions = loadFromStorage(STORAGE_KEY_TRANSACTIONS, INITIAL_TRANSACTIONS);
    this.pagos = loadFromStorage(STORAGE_KEY_PAGOS, INITIAL_PAGOS_CUOTA);
  }

  private persist() {
    saveToStorage(STORAGE_KEY_CATEGORIES, this.categories);
    saveToStorage(STORAGE_KEY_TARJETAS, this.tarjetas);
    saveToStorage(STORAGE_KEY_TRANSACTIONS, this.transactions);
    saveToStorage(STORAGE_KEY_PAGOS, this.pagos);
  }

  public resetData() {
    this.categories = [...INITIAL_CATEGORIES];
    this.tarjetas = [...INITIAL_TARJETAS];
    this.transactions = [...INITIAL_TRANSACTIONS];
    this.pagos = { ...INITIAL_PAGOS_CUOTA };
    this.persist();
  }

  // --- Auth ---
  public async login(usuario: string, contrasena: string) {
    await this.delay(200);
    if (usuario === 'admin' && contrasena === 'admin') {
      const token = 'mock-jwt-token-gastito-admin-authorized';
      const expiraEnUtc = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
      return { token, expiraEnUtc };
    }
    throw new ApiError(401, 'Credenciales inválidas. En desarrollo utilice admin / admin.');
  }

  // --- Categorías ---
  public async getCategorias(tipo?: TipoMovimiento): Promise<CategoryOutput[]> {
    await this.delay(100);
    if (tipo !== undefined && tipo !== null) {
      return this.categories.filter((c) => c.tipo === Number(tipo));
    }
    return [...this.categories];
  }

  public async createCategoria(input: CategoryCreateInput): Promise<CategoryOutput> {
    await this.delay(150);
    const exists = this.categories.some(
      (c) => c.nombre.toLowerCase() === input.nombre.trim().toLowerCase() && c.tipo === input.tipo
    );
    if (exists) {
      throw new ApiError(400, 'Ya existe una categoría con ese nombre para este tipo.');
    }
    const newCat: CategoryOutput = {
      id: Math.max(0, ...this.categories.map((c) => c.id)) + 1,
      nombre: input.nombre.trim(),
      tipo: input.tipo,
      esSistema: false,
    };
    this.categories.push(newCat);
    this.persist();
    return newCat;
  }

  public async updateCategoria(id: number, input: CategoryCreateInput): Promise<CategoryOutput> {
    await this.delay(150);
    const cat = this.categories.find((c) => c.id === id);
    if (!cat) throw new ApiError(404, 'Categoría no encontrada');
    if (cat.esSistema) throw new ApiError(400, 'Las categorías del sistema no se pueden modificar.');

    cat.nombre = input.nombre.trim();
    cat.tipo = input.tipo;
    this.persist();
    return cat;
  }

  public async deleteCategoria(id: number): Promise<void> {
    await this.delay(150);
    const cat = this.categories.find((c) => c.id === id);
    if (!cat) throw new ApiError(404, 'Categoría no encontrada');
    if (cat.esSistema) throw new ApiError(400, 'Las categorías del sistema no se pueden eliminar.');

    this.categories = this.categories.filter((c) => c.id !== id);
    this.persist();
  }

  // --- Tarjetas ---
  public async getTarjetas(): Promise<TarjetaOutput[]> {
    await this.delay(100);
    return [...this.tarjetas];
  }

  public async createTarjeta(input: TarjetaCreateInput): Promise<TarjetaOutput> {
    await this.delay(150);
    const newTarjeta: TarjetaOutput = {
      id: Math.max(0, ...this.tarjetas.map((t) => t.id)) + 1,
      nombre: input.nombre.trim(),
      tipoTarjeta: input.tipoTarjeta,
    };
    this.tarjetas.push(newTarjeta);
    this.persist();
    return newTarjeta;
  }

  public async updateTarjeta(id: number, input: TarjetaCreateInput): Promise<TarjetaOutput> {
    await this.delay(150);
    const tarjeta = this.tarjetas.find((t) => t.id === id);
    if (!tarjeta) throw new ApiError(404, 'Tarjeta no encontrada');
    tarjeta.nombre = input.nombre.trim();
    tarjeta.tipoTarjeta = input.tipoTarjeta;
    this.persist();
    return tarjeta;
  }

  public async deleteTarjeta(id: number): Promise<void> {
    await this.delay(150);
    this.tarjetas = this.tarjetas.filter((t) => t.id !== id);
    this.persist();
  }

  // --- Movimientos ---
  public async getMovimientos(params: {
    desde?: string;
    hasta?: string;
    tipo?: TipoMovimiento;
    categoriaIds?: number[];
    pagina?: number;
    tamanoPagina?: number;
  }): Promise<PagedResult<TransactionOutput>> {
    await this.delay(150);
    let items = this.expandFixedTransactions(this.transactions, params.desde, params.hasta);

    if (params.tipo) {
      items = items.filter((t) => t.tipo === Number(params.tipo));
    }
    if (params.categoriaIds && params.categoriaIds.length > 0) {
      const ids = params.categoriaIds.map(Number);
      items = items.filter((t) => ids.includes(t.categoryId));
    }
    if (params.desde) {
      const d = params.desde.slice(0, 10);
      items = items.filter((t) => t.fecha.slice(0, 10) >= d);
    }
    if (params.hasta) {
      const h = params.hasta.slice(0, 10);
      items = items.filter((t) => t.fecha.slice(0, 10) <= h);
    }

    // Ordenar de más reciente a más antiguo
    items.sort((a, b) => (b.fecha > a.fecha ? 1 : b.fecha < a.fecha ? -1 : b.id - a.id));

    const totalRegistros = items.length;
    const pagina = params.pagina || 1;
    const tamanoPagina = params.tamanoPagina || 10;
    const totalPaginas = Math.ceil(totalRegistros / tamanoPagina);
    const pagedItems = items.slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina);

    return {
      pagina,
      tamanoPagina,
      totalRegistros,
      totalPaginas,
      items: pagedItems,
    };
  }

  public async getMovimientoById(id: number): Promise<TransactionOutput> {
    await this.delay(100);
    const found = this.transactions.find((t) => t.id === id);
    if (!found) throw new ApiError(404, `No se encontró el movimiento con ID ${id}.`);
    return { ...found };
  }

  public async createMovimiento(input: TransactionCreateInput): Promise<TransactionOutput> {
    await this.delay(200);
    const cat = this.categories.find((c) => c.id === input.categoryId);
    const tarjeta = input.tarjetaId ? this.tarjetas.find((t) => t.id === input.tarjetaId) : undefined;

    const isCuotas = input.tipo === TipoMovimiento.Gasto && Boolean(input.cantidadCuotas && input.cantidadCuotas >= 2);
    let valorCuota: number | null = null;
    let cuotasRestantes: number | null = null;
    let montoPendiente: number | null = null;
    let montoEfectivo = input.monto;

    if (isCuotas && input.montoTotal && input.cantidadCuotas) {
      valorCuota = Math.round((input.montoTotal / input.cantidadCuotas) * 100) / 100;
      montoEfectivo = input.montoTotal;
      cuotasRestantes = input.cantidadCuotas;
      montoPendiente = input.montoTotal;
    }

    const newTx: TransactionOutput = {
      id: Math.max(0, ...this.transactions.map((t) => t.id)) + 1,
      tipo: input.tipo,
      frecuencia: input.frecuencia,
      monto: montoEfectivo,
      moneda: 1,
      fecha: input.fecha,
      descripcion: input.descripcion.trim(),
      notas: input.notas?.trim() || null,
      categoryId: input.categoryId,
      categoryNombre: cat ? cat.nombre : 'General',
      medioPago: input.medioPago || null,
      tarjetaId: input.tarjetaId || null,
      tarjetaNombre: tarjeta ? tarjeta.nombre : null,
      createdAtUtc: new Date().toISOString(),
      fechaInicio: input.frecuencia === Frecuencia.Fijo ? (input.fechaInicio || input.fecha) : null,
      fechaFin: input.fechaFin || null,
      montoTotal: isCuotas ? input.montoTotal : null,
      cantidadCuotas: isCuotas ? input.cantidadCuotas : null,
      cuotasPagadas: isCuotas ? 0 : null,
      cuotasRestantes: isCuotas ? cuotasRestantes : null,
      valorCuota: isCuotas ? valorCuota : null,
      valorUltimaCuota: isCuotas ? valorCuota : null,
      montoPendiente: isCuotas ? montoPendiente : null,
      fechaPrimeraCuota: isCuotas ? input.fechaPrimeraCuota : null,
      fechaPrimeraCuotaPendiente: isCuotas ? input.fechaPrimeraCuota : null,
      historialMontos: input.frecuencia === Frecuencia.Fijo ? [
        {
          id: 1,
          transactionId: 0,
          monto: input.monto,
          fechaInicioUtc: new Date().toISOString(),
          fechaFinUtc: null,
        }
      ] : [],
    };

    this.transactions.push(newTx);
    this.persist();
    return newTx;
  }

  public async updateMovimiento(
    id: number,
    input: TransactionUpdateInput,
    aplicarDesde?: AplicarDesde
  ): Promise<TransactionOutput> {
    await this.delay(200);
    const tx = this.transactions.find((t) => t.id === id);
    if (!tx) throw new ApiError(404, `No se encontró el movimiento con ID ${id}.`);

    // Regla: Si compra en cuotas tiene cuotas pagadas o adelantadas, bloquear montoTotal y cantidadCuotas
    if (tx.cantidadCuotas && (tx.cuotasPagadas || 0) > 0) {
      if (input.montoTotal !== undefined && input.montoTotal !== tx.montoTotal) {
        throw new ApiError(400, 'No se puede modificar el monto total de una compra con cuotas ya pagadas o adelantadas.');
      }
      if (input.cantidadCuotas !== undefined && input.cantidadCuotas !== tx.cantidadCuotas) {
        throw new ApiError(400, 'No se puede modificar la cantidad de cuotas de una compra con cuotas ya pagadas o adelantadas.');
      }
    }

    const cat = this.categories.find((c) => c.id === input.categoryId);
    const tarjeta = input.tarjetaId ? this.tarjetas.find((t) => t.id === input.tarjetaId) : undefined;

    // Regla: Fijo con cambio de monto
    if (tx.frecuencia === Frecuencia.Fijo && input.monto !== tx.monto) {
      const mode = aplicarDesde || input.aplicarDesde || AplicarDesde.MesActual;
      tx.historialMontos = tx.historialMontos || [];
      const now = new Date();
      tx.historialMontos.push({
        id: tx.historialMontos.length + 1,
        transactionId: id,
        monto: input.monto,
        fechaInicioUtc: mode === AplicarDesde.MesSiguiente
          ? new Date(now.getFullYear(), now.getMonth() + 1, 1).toISOString()
          : new Date(now.getFullYear(), now.getMonth(), 1).toISOString(),
        fechaFinUtc: null,
      });
    }

    tx.tipo = input.tipo;
    tx.frecuencia = input.frecuencia;
    tx.monto = input.monto;
    tx.fecha = input.fecha;
    tx.descripcion = input.descripcion.trim();
    tx.notas = input.notas?.trim() || null;
    tx.categoryId = input.categoryId;
    tx.categoryNombre = cat ? cat.nombre : tx.categoryNombre;
    tx.medioPago = input.medioPago || null;
    tx.tarjetaId = input.tarjetaId || null;
    tx.tarjetaNombre = tarjeta ? tarjeta.nombre : null;
    tx.fechaFin = input.fechaFin || null;

    this.persist();
    return { ...tx };
  }

  public async deleteMovimiento(id: number): Promise<void> {
    await this.delay(150);
    this.transactions = this.transactions.filter((t) => t.id !== id);
    this.persist();
  }

  // --- Cuotas: Adelantar y Pagos ---
  public async adelantarCuotas(id: number, cantidad: number): Promise<PagoCuotaOutput> {
    await this.delay(250);
    const tx = this.transactions.find((t) => t.id === id);
    if (!tx) throw new ApiError(404, `No se encontró la compra con ID ${id}.`);
    if (!tx.cantidadCuotas || !tx.valorCuota) {
      throw new ApiError(400, 'El movimiento no es una compra en cuotas.');
    }

    const cuotasRestantes = (tx.cantidadCuotas || 0) - (tx.cuotasPagadas || 0);
    if (cantidad < 1 || cantidad > cuotasRestantes) {
      throw new ApiError(400, `La cantidad de cuotas a adelantar debe estar entre 1 y ${cuotasRestantes}.`);
    }

    const cuotaDesde = (tx.cuotasPagadas || 0) + 1;
    const cuotaHasta = cuotaDesde + cantidad - 1;
    const monto = cantidad * tx.valorCuota;

    const pago: PagoCuotaOutput = {
      id: Math.floor(Math.random() * 100000),
      transactionId: id,
      cantidadCuotas: cantidad,
      monto,
      cuotaDesde,
      cuotaHasta,
      rangoCuotas: `cuotas ${cuotaDesde} a ${cuotaHasta}`,
      fechaPagoUtc: new Date().toISOString(),
    };

    if (!this.pagos[id]) {
      this.pagos[id] = [];
    }
    this.pagos[id].push(pago);

    tx.cuotasPagadas = (tx.cuotasPagadas || 0) + cantidad;
    tx.cuotasRestantes = (tx.cantidadCuotas || 0) - tx.cuotasPagadas;
    tx.montoPendiente = Math.max(0, (tx.montoTotal || 0) - tx.cuotasPagadas * tx.valorCuota);

    if (tx.cuotasPagadas >= (tx.cantidadCuotas || 0)) {
      tx.fechaPrimeraCuotaPendiente = undefined;
    } else {
      const now = new Date();
      const proximoMes = new Date(now.getFullYear(), now.getMonth() + 1, 1);
      const origDateStr = tx.fechaPrimeraCuota || tx.fecha;
      const origDay = parseInt(origDateStr.slice(8, 10), 10) || 1;
      const maxDays = new Date(proximoMes.getFullYear(), proximoMes.getMonth() + 1, 0).getDate();
      const day = Math.min(origDay, maxDays);
      const dayStr = String(day).padStart(2, '0');
      const monthStr = String(proximoMes.getMonth() + 1).padStart(2, '0');
      tx.fechaPrimeraCuotaPendiente = `${proximoMes.getFullYear()}-${monthStr}-${dayStr}`;
    }

    this.persist();
    return pago;
  }

  public async getPagosCuota(id: number): Promise<PagoCuotaOutput[]> {
    await this.delay(100);
    return this.pagos[id] ? [...this.pagos[id]] : [];
  }

  // --- Pendientes ---
  public async getPendientes(params: {
    desde?: string;
    hasta?: string;
    categoriaIds?: number[];
    montoMin?: number;
    montoMax?: number;
  }): Promise<PendienteItemOutput[]> {
    await this.delay(150);
    const result: PendienteItemOutput[] = [];

    // 1. Gastos eventuales sin cuotas pendientes de pago
    const eventuales = this.transactions.filter(
      (t) =>
        t.tipo === TipoMovimiento.Gasto &&
        t.frecuencia === Frecuencia.Eventual &&
        !t.cantidadCuotas &&
        (t.descripcion.toLowerCase().includes('plomería') ||
         t.descripcion.toLowerCase().includes('reparación') ||
         t.descripcion.toLowerCase().includes('pendiente') ||
         t.id === 203 ||
         t.id === 204)
    );

    for (const ev of eventuales) {
      result.push({
        id: ev.id,
        descripcion: ev.descripcion,
        categoryId: ev.categoryId,
        categoryNombre: ev.categoryNombre,
        fecha: ev.fecha,
        monto: ev.monto,
        esCuota: false,
      });
    }

    // 2. Compras en cuotas con cuotas pendientes
    const cuotas = this.transactions.filter(
      (t) =>
        t.tipo === TipoMovimiento.Gasto &&
        Boolean(t.cantidadCuotas && t.cantidadCuotas >= 2) &&
        (t.cuotasPagadas || 0) < (t.cantidadCuotas || 0)
    );

    for (const c of cuotas) {
      const pagadas = c.cuotasPagadas || 0;
      const totalCuotas = c.cantidadCuotas || 1;
      const restantes = totalCuotas - pagadas;
      const pagosHistorial = this.pagos[c.id] || [];

      result.push({
        id: c.id,
        descripcion: c.descripcion,
        categoryId: c.categoryId,
        categoryNombre: c.categoryNombre,
        fecha: c.fecha,
        monto: c.valorCuota || (c.montoTotal ? c.montoTotal / totalCuotas : 0),
        esCuota: true,
        montoTotal: c.montoTotal,
        cantidadCuotas: totalCuotas,
        cuotasPagadas: pagadas,
        cuotasRestantes: restantes,
        valorCuota: c.valorCuota,
        montoPendiente: c.montoPendiente || (restantes * (c.valorCuota || 0)),
        progreso: `${pagadas}/${totalCuotas}`,
        terminaEnPeriodo: restantes <= 3,
        fechaPrimeraCuotaPendiente: c.fechaPrimeraCuotaPendiente || '2026-11-10',
        historialPagos: pagosHistorial,
      });
    }

    // Filtros de categoría y montos
    let filtered = result;
    if (params.categoriaIds && params.categoriaIds.length > 0) {
      const ids = params.categoriaIds.map(Number);
      filtered = filtered.filter((i) => ids.includes(i.categoryId));
    }
    if (params.montoMin !== undefined && params.montoMin !== null) {
      filtered = filtered.filter((i) => (i.esCuota ? (i.montoPendiente || i.monto) : i.monto) >= params.montoMin!);
    }
    if (params.montoMax !== undefined && params.montoMax !== null) {
      filtered = filtered.filter((i) => (i.esCuota ? (i.montoPendiente || i.monto) : i.monto) <= params.montoMax!);
    }

    return filtered;
  }

  // --- Dashboard ---
  public async getDashboard(meses: number = 3): Promise<DashboardOutput> {
    await this.delay(200);
    const actualYear = 2026;
    const actualMonth = 10; // Octubre 2026

    // Totales del mes actual (Octubre 2026)
    const currentMonthTxs = this.expandFixedTransactions(
      this.transactions,
      '2026-10-01',
      '2026-10-31'
    ).filter((t) => t.fecha.startsWith('2026-10'));

    let totalIngresos = 0;
    let totalGastos = 0;
    const gastosCatMap: Record<number, { nombre: string; monto: number }> = {};

    for (const tx of currentMonthTxs) {
      if (tx.tipo === TipoMovimiento.Ingreso) {
        totalIngresos += tx.monto;
      } else {
        totalGastos += tx.monto;
        if (!gastosCatMap[tx.categoryId]) {
          gastosCatMap[tx.categoryId] = { nombre: tx.categoryNombre, monto: 0 };
        }
        gastosCatMap[tx.categoryId].monto += tx.monto;
      }
    }

    // Agregar cuota mensual de compras en cuotas que vencen en octubre
    for (const tx of this.transactions) {
      if (tx.cantidadCuotas && (tx.cuotasPagadas || 0) < (tx.cantidadCuotas || 0)) {
        if (!gastosCatMap[tx.categoryId]) {
          gastosCatMap[tx.categoryId] = { nombre: tx.categoryNombre, monto: 0 };
        }
        gastosCatMap[tx.categoryId].monto += tx.valorCuota || 0;
        totalGastos += tx.valorCuota || 0;
      }
    }

    // Gastos por categoría: solo las que tienen monto > 0
    const gastosPorCategoria: GastosPorCategoriaOutput[] = Object.entries(gastosCatMap)
      .map(([catId, data]) => ({
        categoriaId: Number(catId),
        categoriaNombre: data.nombre,
        monto: data.monto,
        porcentaje: totalGastos > 0 ? Math.round((data.monto / totalGastos) * 1000) / 10 : 0,
      }))
      .filter((g) => g.monto > 0)
      .sort((a, b) => b.monto - a.monto);

    // Monto Pendiente total (deudas en cuotas restantes + eventuales pendientes)
    let montoPendiente = 0;
    for (const tx of this.transactions) {
      if (tx.cantidadCuotas && (tx.cuotasPagadas || 0) < (tx.cantidadCuotas || 0)) {
        montoPendiente += tx.montoPendiente || (tx.cantidadCuotas - (tx.cuotasPagadas || 0)) * (tx.valorCuota || 0);
      } else if (tx.id === 203 || tx.id === 204) {
        montoPendiente += tx.monto;
      }
    }

    // Ingresos vs Gastos para N meses (meses: 3 a 12)
    const ingresosVsGastos: IngresosVsGastosOutput[] = [];
    const validMeses = Math.min(12, Math.max(3, meses));

    for (let i = validMeses - 1; i >= 0; i--) {
      let targetMonth = actualMonth - i;
      let targetYear = actualYear;
      while (targetMonth <= 0) {
        targetMonth += 12;
        targetYear -= 1;
      }

      const mStr = String(targetMonth).padStart(2, '0');
      const periodo = `${targetYear}-${mStr}`;
      const startOfMonth = `${periodo}-01`;
      const endOfMonth = `${periodo}-31`;

      const monthTxs = this.expandFixedTransactions(this.transactions, startOfMonth, endOfMonth)
        .filter((t) => t.fecha.startsWith(periodo));

      let mIng = 0;
      let mGas = 0;
      for (const t of monthTxs) {
        if (t.tipo === TipoMovimiento.Ingreso) mIng += t.monto;
        else mGas += t.monto;
      }

      // Añadir cuotas activas para ese mes simulado
      mGas += 220000; // promedio cuotas activas

      ingresosVsGastos.push({
        anio: targetYear,
        mes: targetMonth,
        periodo,
        totalIngresos: mIng > 0 ? mIng : 850000 + (targetMonth % 2 === 0 ? 150000 : 0),
        totalGastos: mGas > 0 ? mGas : 320000 + (targetMonth * 12000),
      });
    }

    // Próximas Deudas
    const proximasDeudas: ProximaDeudaOutput[] = this.transactions
      .filter((t) => t.cantidadCuotas && (t.cuotasPagadas || 0) < (t.cantidadCuotas || 0))
      .map((t) => ({
        id: t.id,
        descripcion: t.descripcion,
        categoria: t.categoryNombre,
        cuotasPagadas: t.cuotasPagadas || 0,
        cantidadCuotas: t.cantidadCuotas || 1,
        valorCuota: t.valorCuota || 0,
        mesProximaCuota: formatMonthYearAR('2026-11-01'),
      }));

    return {
      totalIngresos,
      totalGastos,
      montoPendiente,
      gastosPorCategoria,
      ingresosVsGastos,
      proximasDeudas,
    };
  }

  // --- Reportes Resumen ---
  public async getReporteResumen(params: {
    desde?: string;
    hasta?: string;
    categoriaIds?: number[];
  }): Promise<ReporteResumenOutput> {
    await this.delay(200);
    const desde = params.desde || '2026-10-01';
    const hasta = params.hasta || '2026-10-31';

    let txs = this.expandFixedTransactions(this.transactions, desde, hasta);
    txs = txs.filter((t) => t.fecha.slice(0, 10) >= desde.slice(0, 10) && t.fecha.slice(0, 10) <= hasta.slice(0, 10));

    if (params.categoriaIds && params.categoriaIds.length > 0) {
      const ids = params.categoriaIds.map(Number);
      txs = txs.filter((t) => ids.includes(t.categoryId));
    }

    let totalIngresos = 0;
    let totalGastos = 0;
    const catMap: Record<number, { nombre: string; monto: number }> = {};

    for (const t of txs) {
      if (t.tipo === TipoMovimiento.Ingreso) {
        totalIngresos += t.monto;
      } else {
        totalGastos += t.monto;
        if (!catMap[t.categoryId]) {
          catMap[t.categoryId] = { nombre: t.categoryNombre, monto: 0 };
        }
        catMap[t.categoryId].monto += t.monto;
      }
    }

    const gastosPorCategoria: GastosPorCategoriaOutput[] = Object.entries(catMap)
      .map(([catId, data]) => ({
        categoriaId: Number(catId),
        categoriaNombre: data.nombre,
        monto: data.monto,
        porcentaje: totalGastos > 0 ? Math.round((data.monto / totalGastos) * 1000) / 10 : 0,
      }))
      .filter((g) => g.monto > 0)
      .sort((a, b) => b.monto - a.monto);

    const proximasDeudas: ProximaDeudaOutput[] = this.transactions
      .filter((t) => t.cantidadCuotas && (t.cuotasPagadas || 0) < (t.cantidadCuotas || 0))
      .map((t) => ({
        id: t.id,
        descripcion: t.descripcion,
        categoria: t.categoryNombre,
        cuotasPagadas: t.cuotasPagadas || 0,
        cantidadCuotas: t.cantidadCuotas || 1,
        valorCuota: t.valorCuota || 0,
        mesProximaCuota: formatMonthYearAR('2026-11-01'),
      }));

    return {
      desde,
      hasta,
      totalIngresos,
      totalGastos,
      gastosPorCategoria,
      proximasDeudas,
    };
  }

  // --- Helpers de Cuotas y Expansión ---
  private calcularFechaCuota(baseDateStr: string, numeroCuota: number): string {
    const parts = baseDateStr.split('-');
    const year = parseInt(parts[0], 10);
    const month = parseInt(parts[1], 10) - 1;
    const day = parseInt(parts[2], 10);

    const targetDate = new Date(year, month + (numeroCuota - 1), 1);
    const maxDays = new Date(targetDate.getFullYear(), targetDate.getMonth() + 1, 0).getDate();
    const targetDay = Math.min(day, maxDays);

    const yStr = targetDate.getFullYear();
    const mStr = String(targetDate.getMonth() + 1).padStart(2, '0');
    const dStr = String(targetDay).padStart(2, '0');
    return `${yStr}-${mStr}-${dStr}`;
  }

  private calcularFechaCuotaConCorrimiento(
    fechaPrimeraCuota: string,
    fechaPrimeraCuotaPendiente: string | null | undefined,
    cuotasPagadas: number,
    numeroCuota: number
  ): string {
    if (numeroCuota <= cuotasPagadas || !fechaPrimeraCuotaPendiente) {
      return this.calcularFechaCuota(fechaPrimeraCuota, numeroCuota);
    }
    const offset = numeroCuota - (cuotasPagadas + 1);
    return this.calcularFechaCuota(fechaPrimeraCuotaPendiente, offset + 1);
  }

  private expandFixedTransactions(
    list: TransactionOutput[],
    desde?: string,
    hasta?: string
  ): TransactionOutput[] {
    const expanded: TransactionOutput[] = [];
    const targetMonths = ['2026-05', '2026-06', '2026-07', '2026-08', '2026-09', '2026-10', '2026-11', '2026-12', '2027-01'];

    for (const t of list) {
      // 1. Movimientos Fijos recurrentes
      if (t.frecuencia === Frecuencia.Fijo) {
        const origDay = t.fecha.slice(8, 10) || '01';
        for (const ym of targetMonths) {
          const itemDate = `${ym}-${origDay}`;
          if (desde && itemDate < desde.slice(0, 10)) continue;
          if (hasta && itemDate > hasta.slice(0, 10)) continue;

          expanded.push({
            ...t,
            id: t.id * 1000 + parseInt(ym.replace('-', ''), 10),
            fecha: itemDate,
          });
        }
        continue;
      }

      // 2. Compras en cuotas
      if (t.cantidadCuotas && t.cantidadCuotas >= 2 && t.montoTotal) {
        const primeraCuota = t.fechaPrimeraCuota || t.fecha;
        const cantidad = t.cantidadCuotas;
        const pagadas = t.cuotasPagadas || 0;
        const valorCuota = t.valorCuota || Math.round((t.montoTotal / cantidad) * 100) / 100;

        // A. Compra informativa para Camino 1 (primera cuota en mes posterior)
        const isCamino1 = Boolean(
          t.fechaPrimeraCuota && t.fechaPrimeraCuota.slice(0, 7) > t.fecha.slice(0, 7)
        );
        if (isCamino1) {
          const compraDate = t.fecha.slice(0, 10);
          const inRange = (!desde || compraDate >= desde.slice(0, 10)) &&
                          (!hasta || compraDate <= hasta.slice(0, 10));
          if (inRange) {
            expanded.push({
              ...t,
              monto: 0,
              descripcion: `${t.descripcion} (${cantidad} cuotas)`,
            });
          }
        }

        // B. Movimientos individuales por cada cuota adelantada/pagada en su fecha de pago
        const pagos = this.pagos[t.id] || [];
        for (const p of pagos) {
          const pagoDate = p.fechaPagoUtc.slice(0, 10);
          const inRange = (!desde || pagoDate >= desde.slice(0, 10)) &&
                          (!hasta || pagoDate <= hasta.slice(0, 10));
          if (inRange) {
            for (let c = p.cuotaDesde; c <= p.cuotaHasta; c++) {
              expanded.push({
                ...t,
                id: -(p.id * 1000 + c),
                fecha: pagoDate,
                monto: valorCuota,
                descripcion: `Cuota ${c}/${cantidad} - ${t.descripcion} (Adelanto)`,
              });
            }
          }
        }

        // C. Cuotas pendientes no saldadas (con corrimiento consecutivo hacia adelante)
        for (let c = pagadas + 1; c <= cantidad; c++) {
          const fc = this.calcularFechaCuotaConCorrimiento(
            primeraCuota,
            t.fechaPrimeraCuotaPendiente,
            pagadas,
            c
          );
          const inRange = (!desde || fc >= desde.slice(0, 10)) &&
                          (!hasta || fc <= hasta.slice(0, 10));
          if (inRange) {
            expanded.push({
              ...t,
              id: -(t.id * 10000 + c),
              fecha: fc,
              monto: valorCuota,
              descripcion: `Cuota ${c}/${cantidad} - ${t.descripcion}`,
            });
          }
        }
        continue;
      }

      // 3. Movimiento regular eventual
      const itemDate = t.fecha.slice(0, 10);
      const inRange = (!desde || itemDate >= desde.slice(0, 10)) &&
                      (!hasta || itemDate <= hasta.slice(0, 10));
      if (inRange) {
        expanded.push({ ...t });
      }
    }

    return expanded;
  }

  private delay(ms: number) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}

export const mockService = new MockStorage();
