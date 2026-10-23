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
            transaction.FechaPrimeraCuotaPendiente ??= transaction.FechaPrimeraCuota;

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
}
