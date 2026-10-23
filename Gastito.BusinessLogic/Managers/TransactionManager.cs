using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.BusinessLogic.Utils;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Gastito.Entities.Utils;

namespace Gastito.BusinessLogic.Managers;

public class TransactionManager : ITransactionManager
{
    private readonly ITransactionStore _transactionStore;
    private readonly ICategoryStore _categoryStore;
    private readonly ITarjetaStore _tarjetaStore;

    public TransactionManager(
        ITransactionStore transactionStore,
        ICategoryStore categoryStore,
        ITarjetaStore tarjetaStore)
    {
        _transactionStore = transactionStore;
        _categoryStore = categoryStore;
        _tarjetaStore = tarjetaStore;
    }

    public async Task<(List<Transaction> Items, int TotalCount)> GetPagedAsync(
        DateTime? desde,
        DateTime? hasta,
        TipoMovimiento? tipo,
        List<int>? categoriaIds,
        int pagina,
        int tamanoPagina,
        CancellationToken ct = default)
    {
        return await _transactionStore.GetPagedAsync(desde, hasta, tipo, categoriaIds, pagina, tamanoPagina, ct);
    }

    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _transactionStore.GetByIdAsync(id, ct);
    }

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken ct = default)
    {
        await ValidateAndNormalizeTransactionAsync(transaction, ct);

        // Auditoría inicial y soft delete
        transaction.CreatedAtUtc = DateTime.UtcNow;
        transaction.IsDeleted = false;
        transaction.DeletedAtUtc = null;

        // Para movimientos fijos (recurrentes), registrar el monto inicial en HistorialMonto
        if (transaction.Frecuencia == Frecuencia.Fijo)
        {
            var fInicio = transaction.FechaInicio ?? transaction.Fecha;
            var vigenteDesde = DateTimeUtils.GetStartOfMonthUtc(fInicio);

            transaction.HistorialMontos.Add(new HistorialMonto
            {
                Monto = transaction.Monto,
                VigenteDesde = vigenteDesde,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return await _transactionStore.CreateAsync(transaction, ct);
    }

    public async Task<Transaction> UpdateAsync(int id, Transaction updatedData, AplicarDesde? aplicarDesde = null, CancellationToken ct = default)
    {
        var existing = await _transactionStore.GetByIdAsync(id, ct);
        if (existing == null)
        {
            throw new BusinessException($"No se encontró el movimiento con ID {id}.");
        }

        // Restricción Sección 3.3 y 10:
        // Si un gasto en cuotas ya tiene cuotas pagadas o adelantadas, no se pueden cambiar el monto total ni la cantidad de cuotas; solo descripción, categoría y notas.
        if ((existing.CuotasPagadas ?? 0) > 0)
        {
            var isCuota = existing.CantidadCuotas.HasValue && existing.CantidadCuotas.Value > 1;
            if (isCuota)
            {
                if (updatedData.MontoTotal.HasValue && updatedData.MontoTotal.Value != existing.MontoTotal ||
                    updatedData.CantidadCuotas.HasValue && updatedData.CantidadCuotas.Value != existing.CantidadCuotas)
                {
                    throw new BusinessException("No se puede modificar el monto total ni la cantidad de cuotas de una compra que ya tiene cuotas pagadas o adelantadas.");
                }
            }
            else
            {
                if (updatedData.Monto != existing.Monto)
                {
                    throw new BusinessException("No se puede modificar el monto total ni la cantidad de cuotas de una compra que ya tiene cuotas pagadas o adelantadas.");
                }
            }
        }

        await ValidateAndNormalizeTransactionAsync(updatedData, ct);

        // Si es movimiento fijo y cambia el monto, registrar en HistorialMonto (Sección 3.4)
        if (updatedData.Frecuencia == Frecuencia.Fijo && updatedData.Monto != existing.Monto)
        {
            var opcion = aplicarDesde ?? AplicarDesde.MesActual;
            var today = DateTimeUtils.GetArgentinaToday();
            var vigenteDesde = opcion == AplicarDesde.MesActual
                ? DateTimeUtils.GetStartOfMonthUtc(today)
                : DateTimeUtils.GetStartOfNextMonthUtc(today);

            if (opcion == AplicarDesde.MesSiguiente)
            {
                // Asegurarse de que el mes actual conserve el monto previo en el historial
                var currentMonthStart = DateTimeUtils.GetStartOfMonthUtc(today);
                var currentMonthRecord = existing.HistorialMontos.FirstOrDefault(h => h.VigenteDesde == currentMonthStart);
                if (currentMonthRecord == null)
                {
                    existing.HistorialMontos.Add(new HistorialMonto
                    {
                        TransactionId = existing.Id,
                        Monto = existing.Monto,
                        VigenteDesde = currentMonthStart,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            var targetRecord = existing.HistorialMontos.FirstOrDefault(h => h.VigenteDesde == vigenteDesde);
            if (targetRecord != null)
            {
                targetRecord.Monto = updatedData.Monto;
            }
            else
            {
                existing.HistorialMontos.Add(new HistorialMonto
                {
                    TransactionId = existing.Id,
                    Monto = updatedData.Monto,
                    VigenteDesde = vigenteDesde,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (opcion == AplicarDesde.MesActual)
            {
                existing.Monto = updatedData.Monto;
            }
        }
        else
        {
            existing.Monto = updatedData.Monto;
        }

        // Actualizar propiedades editables
        existing.Tipo = updatedData.Tipo;
        existing.Frecuencia = updatedData.Frecuencia;
        existing.Fecha = updatedData.Fecha;
        existing.Descripcion = updatedData.Descripcion.Trim();
        existing.Notas = string.IsNullOrWhiteSpace(updatedData.Notas) ? null : updatedData.Notas.Trim();
        existing.CategoryId = updatedData.CategoryId;
        existing.MedioPago = updatedData.MedioPago;
        existing.TarjetaId = updatedData.TarjetaId;

        // Fijos y cuotas
        existing.FechaInicio = updatedData.FechaInicio;
        existing.FechaFin = updatedData.FechaFin;
        if (updatedData.MontoTotal.HasValue) existing.MontoTotal = updatedData.MontoTotal;
        if (updatedData.CantidadCuotas.HasValue) existing.CantidadCuotas = updatedData.CantidadCuotas;
        if (updatedData.FechaPrimeraCuota.HasValue)
        {
            existing.FechaPrimeraCuota = DateTime.SpecifyKind(updatedData.FechaPrimeraCuota.Value.Date, DateTimeKind.Utc);
            existing.FechaPrimeraCuotaPendiente ??= existing.FechaPrimeraCuota;
        }

        if (existing.CantidadCuotas.HasValue && existing.CantidadCuotas.Value > 1 && existing.MontoTotal.HasValue)
        {
            existing.Monto = CuotaCalculator.CalcularValorCuotaBase(existing.MontoTotal.Value, existing.CantidadCuotas.Value);
        }

        return await _transactionStore.UpdateAsync(existing, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _transactionStore.GetByIdAsync(id, ct);
        if (existing == null)
        {
            throw new BusinessException($"No se encontró el movimiento con ID {id}.");
        }

        await _transactionStore.DeleteAsync(id, ct);
    }

    private async Task ValidateAndNormalizeTransactionAsync(Transaction transaction, CancellationToken ct)
    {
        // 1. Validaciones y normalizaciones de movimientos fijos (Sección 3.4 y 10)
        if (transaction.Frecuencia == Frecuencia.Fijo)
        {
            if ((transaction.CantidadCuotas.HasValue && transaction.CantidadCuotas.Value > 1) ||
                (transaction.MontoTotal.HasValue && transaction.MontoTotal.Value > 0))
            {
                throw new BusinessException("Los movimientos fijos no pueden tener cuotas.");
            }

            transaction.FechaInicio ??= transaction.Fecha;
            transaction.FechaInicio = DateTime.SpecifyKind(transaction.FechaInicio.Value.Date, DateTimeKind.Utc);

            if (transaction.FechaFin.HasValue)
            {
                transaction.FechaFin = DateTime.SpecifyKind(transaction.FechaFin.Value.Date, DateTimeKind.Utc);
                if (transaction.FechaFin.Value < transaction.FechaInicio.Value)
                {
                    throw new BusinessException("La fecha de fin no puede ser anterior a la fecha de inicio.");
                }
            }
        }

        // 2. Validación y cálculo de compras en cuotas (Sección 3.5 y 10)
        var esCompraEnCuotas = (transaction.CantidadCuotas.HasValue && transaction.CantidadCuotas.Value > 1) ||
                               (transaction.MontoTotal.HasValue && transaction.MontoTotal.Value > 0);

        if (esCompraEnCuotas)
        {
            if (transaction.Tipo != TipoMovimiento.Gasto)
            {
                throw new BusinessException("Las compras en cuotas solo pueden ser de tipo Gasto.");
            }

            if (transaction.Frecuencia != Frecuencia.Eventual)
            {
                throw new BusinessException("Las compras en cuotas solo pueden tener frecuencia Eventual.");
            }

            if (!transaction.CantidadCuotas.HasValue || transaction.CantidadCuotas.Value <= 1)
            {
                throw new BusinessException("La cantidad de cuotas debe ser mayor a 1.");
            }

            if (!transaction.MontoTotal.HasValue || transaction.MontoTotal.Value <= 0)
            {
                throw new BusinessException("El monto total es obligatorio y debe ser mayor a cero para compras en cuotas.");
            }

            if (transaction.MedioPago != MedioPago.Tarjeta || !transaction.TarjetaId.HasValue)
            {
                throw new BusinessException("Las compras en cuotas deben pagarse con tarjeta de crédito.");
            }

            var tarjeta = await _tarjetaStore.GetByIdAsync(transaction.TarjetaId.Value, ct);
            if (tarjeta == null)
            {
                throw new BusinessException($"No se encontró la tarjeta con ID {transaction.TarjetaId.Value}.");
            }

            if (tarjeta.TipoTarjeta != TipoTarjeta.Credito)
            {
                throw new BusinessException("Las compras en cuotas solo pueden realizarse con tarjetas de crédito.");
            }

            transaction.CuotasPagadas ??= 0;
            transaction.FechaPrimeraCuota ??= transaction.Fecha;
            transaction.FechaPrimeraCuota = DateTime.SpecifyKind(transaction.FechaPrimeraCuota.Value.Date, DateTimeKind.Utc);
            // FechaPrimeraCuotaPendiente permanece null a menos que un adelanto corra el calendario (Sección 3.6)

            // Se calcula y asigna el valor de la cuota base
            transaction.Monto = CuotaCalculator.CalcularValorCuotaBase(transaction.MontoTotal.Value, transaction.CantidadCuotas.Value);
        }

        if (transaction.Monto <= 0)
        {
            throw new BusinessException("El monto debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(transaction.Descripcion))
        {
            throw new BusinessException("La descripción es obligatoria.");
        }

        transaction.Descripcion = transaction.Descripcion.Trim();
        transaction.Notas = string.IsNullOrWhiteSpace(transaction.Notas) ? null : transaction.Notas.Trim();

        // Normalización de Fecha según Sección 5:
        // "Fecha del movimiento: día calendario elegido, guardado como UTC a las 00:00:00"
        transaction.Fecha = DateTime.SpecifyKind(transaction.Fecha.Date, DateTimeKind.Utc);

        // Validación de categoría (Sección 3.2 y 10: "La categoría es del mismo tipo que el movimiento")
        var category = await _categoryStore.GetByIdAsync(transaction.CategoryId, ct);
        if (category == null)
        {
            throw new BusinessException($"No se encontró la categoría con ID {transaction.CategoryId}.");
        }

        if (category.Tipo != transaction.Tipo)
        {
            throw new BusinessException($"La categoría '{category.Nombre}' es de tipo {category.Tipo} y no coincide con el movimiento que es de tipo {transaction.Tipo}.");
        }

        // Validación de Medio de Pago y Tarjeta (Sección 3.3, 4.2 y 10)
        if (transaction.Tipo == TipoMovimiento.Ingreso)
        {
            // Solo en gastos aplica medio de pago
            transaction.MedioPago = null;
            transaction.TarjetaId = null;
        }
        else if (transaction.Tipo == TipoMovimiento.Gasto)
        {
            if (!transaction.MedioPago.HasValue)
            {
                throw new BusinessException("El medio de pago es obligatorio para los gastos.");
            }

            if (transaction.MedioPago == MedioPago.Tarjeta)
            {
                if (!transaction.TarjetaId.HasValue)
                {
                    throw new BusinessException("Debe seleccionar una tarjeta cuando el medio de pago es Tarjeta.");
                }

                var tarjeta = await _tarjetaStore.GetByIdAsync(transaction.TarjetaId.Value, ct);
                if (tarjeta == null)
                {
                    throw new BusinessException($"No se encontró la tarjeta con ID {transaction.TarjetaId.Value}.");
                }
            }
            else
            {
                // Si es Efectivo o Transferencia, no lleva tarjeta
                transaction.TarjetaId = null;
            }
        }
    }

    public async Task<PagoCuota> AdelantarCuotasAsync(int transactionId, int cantidad, CancellationToken ct = default)
    {
        var existing = await _transactionStore.GetByIdAsync(transactionId, ct);
        if (existing == null)
        {
            throw new BusinessException($"No se encontró el movimiento con ID {transactionId}.");
        }

        if (!existing.CantidadCuotas.HasValue || existing.CantidadCuotas.Value <= 1 || !existing.MontoTotal.HasValue)
        {
            throw new BusinessException("El movimiento no es una compra en cuotas.");
        }

        if (cantidad <= 0)
        {
            throw new BusinessException("La cantidad de cuotas a adelantar debe ser mayor a 0.");
        }

        var cuotasPagadasActuales = existing.CuotasPagadas ?? 0;
        var cuotasRestantes = existing.CantidadCuotas.Value - cuotasPagadasActuales;

        if (cantidad > cuotasRestantes)
        {
            throw new BusinessException($"No se pueden adelantar más cuotas de las que faltan. Cuotas restantes: {cuotasRestantes}.");
        }

        // Calcular el monto total de las cuotas adelantadas
        var cuotaDesde = cuotasPagadasActuales + 1;
        var cuotaHasta = cuotasPagadasActuales + cantidad;
        decimal montoTotalAdelantado = 0m;
        for (int c = cuotaDesde; c <= cuotaHasta; c++)
        {
            montoTotalAdelantado += CuotaCalculator.CalcularValorCuota(existing.MontoTotal.Value, existing.CantidadCuotas.Value, c);
        }

        // Registrar PagoCuota
        var pago = new PagoCuota
        {
            TransactionId = existing.Id,
            CantidadCuotas = cantidad,
            Monto = montoTotalAdelantado,
            CuotaDesde = cuotaDesde,
            CuotaHasta = cuotaHasta,
            FechaPagoUtc = DateTime.UtcNow
        };
        await _transactionStore.AddPagoCuotaAsync(pago, ct);

        // Actualizar cuotas pagadas
        existing.CuotasPagadas = cuotasPagadasActuales + cantidad;

        // Corrimiento de calendario (Sección 3.6):
        // "Las cuotas restantes se corren: la próxima cuota pendiente pasa al mes siguiente al del pago y las demás la siguen mes a mes."
        // "Caso borde: si lo adelantado no supera las cuotas que ya correspondían hasta el mes del pago, el calendario no cambia."
        var fechaPago = DateTimeUtils.GetArgentinaToday();
        var primeraCuota = existing.FechaPrimeraCuota ?? existing.Fecha;

        // Cuántas cuotas correspondían hasta el mes actual del pago:
        var mesesHastaPago = ((fechaPago.Year - primeraCuota.Year) * 12) + (fechaPago.Month - primeraCuota.Month);
        var cuotasQueCorrespondian = Math.Min(existing.CantidadCuotas.Value, Math.Max(0, mesesHastaPago + 1));

        if (existing.CuotasPagadas.Value >= existing.CantidadCuotas.Value)
        {
            // Ya se pagaron todas las cuotas
            existing.FechaPrimeraCuotaPendiente = null;
        }
        else if (existing.CuotasPagadas.Value > cuotasQueCorrespondian)
        {
            // Superó las cuotas que correspondían: la próxima cuota pendiente pasa al mes siguiente al del pago
            var proximoMes = new DateTime(fechaPago.Year, fechaPago.Month, 1).AddMonths(1);
            var maxDays = DateTime.DaysInMonth(proximoMes.Year, proximoMes.Month);
            var day = Math.Min(primeraCuota.Day, maxDays);
            existing.FechaPrimeraCuotaPendiente = new DateTime(proximoMes.Year, proximoMes.Month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        else
        {
            // Caso borde: lo adelantado no supera lo que correspondía hasta este mes: el calendario no cambia
            var proximaPendienteIndex = existing.CuotasPagadas.Value + 1;
            existing.FechaPrimeraCuotaPendiente = CuotaCalculator.CalcularFechaCuota(primeraCuota, proximaPendienteIndex);
        }

        await _transactionStore.UpdateAsync(existing, ct);

        return pago;
    }

    public async Task<List<PagoCuota>> GetPagosCuotaAsync(int transactionId, CancellationToken ct = default)
    {
        return await _transactionStore.GetPagosCuotaAsync(transactionId, ct);
    }

    public async Task<List<Transaction>> GetPendientesAsync(
        DateTime? desde,
        DateTime? hasta,
        List<int>? categoriaIds,
        decimal? montoMin,
        decimal? montoMax,
        CancellationToken ct = default)
    {
        var today = DateTimeUtils.GetArgentinaToday();
        var desdeUtc = desde.HasValue
            ? DateTimeUtils.ToCalendarUtc(desde.Value)
            : DateTimeUtils.GetStartOfMonthUtc(today);

        var hastaUtc = hasta.HasValue
            ? DateTimeUtils.ToCalendarUtc(hasta.Value)
            : new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month), 23, 59, 59, DateTimeKind.Utc);

        var allGastos = await _transactionStore.GetRawAsync(TipoMovimiento.Gasto, ct);

        // Los fijos NO entran en Pendientes (Sección 3.4 y 3.7)
        var eventuales = allGastos.Where(t => t.Frecuencia == Frecuencia.Eventual).ToList();

        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            eventuales = eventuales.Where(t => categoriaIds.Contains(t.CategoryId)).ToList();
        }

        // 1. Gastos eventuales sin cuotas, ordenados de mayor a menor monto
        var sinCuotas = eventuales
            .Where(t => !t.CantidadCuotas.HasValue || t.CantidadCuotas.Value <= 1)
            .Where(t => t.Fecha >= desdeUtc && t.Fecha <= hastaUtc)
            .Where(t => (!montoMin.HasValue || t.Monto >= montoMin.Value) &&
                        (!montoMax.HasValue || t.Monto <= montoMax.Value))
            .OrderByDescending(t => t.Monto)
            .ThenByDescending(t => t.Id)
            .ToList();

        // 2. Compras en cuotas
        var comprasEnCuotas = eventuales
            .Where(t => t.CantidadCuotas.HasValue && t.CantidadCuotas.Value > 1 && t.MontoTotal.HasValue)
            .ToList();

        var cuotasEnPeriodo = new List<(Transaction Tx, bool TerminaEnPeriodo, decimal ValorCuota)>();

        foreach (var compra in comprasEnCuotas)
        {
            var primeraCuota = compra.FechaPrimeraCuota ?? compra.Fecha;
            var cantidad = compra.CantidadCuotas!.Value;
            var pagadas = compra.CuotasPagadas ?? 0;
            var valorCuota = CuotaCalculator.CalcularValorCuotaBase(compra.MontoTotal!.Value, cantidad);

            if (montoMin.HasValue && compra.MontoTotal!.Value < montoMin.Value && valorCuota < montoMin.Value) continue;
            if (montoMax.HasValue && compra.MontoTotal!.Value > montoMax.Value && valorCuota > montoMax.Value) continue;

            // Verificar si alguna cuota cae dentro del rango [desdeUtc, hastaUtc]
            bool tieneCuotaEnRango = false;
            for (int c = 1; c <= cantidad; c++)
            {
                var fechaCuota = CuotaCalculator.CalcularFechaDeCuotaConCorrimiento(primeraCuota, compra.FechaPrimeraCuotaPendiente, pagadas, c);
                if (fechaCuota >= desdeUtc && fechaCuota <= hastaUtc)
                {
                    tieneCuotaEnRango = true;
                    break;
                }
            }

            if (tieneCuotaEnRango)
            {
                // Verificar si la última cuota cae dentro del período (termina dentro del rango)
                var fechaUltimaCuota = CuotaCalculator.CalcularFechaUltimaCuota(primeraCuota, compra.FechaPrimeraCuotaPendiente, cantidad, pagadas);
                bool terminaEnPeriodo = fechaUltimaCuota >= desdeUtc && fechaUltimaCuota <= hastaUtc;

                cuotasEnPeriodo.Add((compra, terminaEnPeriodo, valorCuota));
            }
        }

        // Orden de compras en cuotas (Sección 3.7):
        // a. Primero las que terminan dentro del rango.
        // b. Después el resto, por cuotas pagadas de mayor a menor (3/6 antes que 1/6).
        // c. En caso de empate, por valor de la cuota de mayor a menor.
        var cuotasOrdenadas = cuotasEnPeriodo
            .OrderByDescending(x => x.TerminaEnPeriodo)
            .ThenByDescending(x => x.Tx.CuotasPagadas ?? 0)
            .ThenByDescending(x => x.ValorCuota)
            .ThenByDescending(x => x.Tx.Id)
            .Select(x => x.Tx)
            .ToList();

        var result = new List<Transaction>();
        result.AddRange(sinCuotas);
        result.AddRange(cuotasOrdenadas);

        return result;
    }
}
