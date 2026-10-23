using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;

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

        return await _transactionStore.CreateAsync(transaction, ct);
    }

    public async Task<Transaction> UpdateAsync(int id, Transaction updatedData, CancellationToken ct = default)
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
            if (updatedData.Monto != existing.Monto ||
                updatedData.MontoTotal != existing.MontoTotal ||
                updatedData.CantidadCuotas != existing.CantidadCuotas)
            {
                throw new BusinessException("No se puede modificar el monto total ni la cantidad de cuotas de una compra que ya tiene cuotas pagadas o adelantadas.");
            }
        }

        await ValidateAndNormalizeTransactionAsync(updatedData, ct);

        // Actualizar propiedades editables
        existing.Tipo = updatedData.Tipo;
        existing.Frecuencia = updatedData.Frecuencia;
        existing.Monto = updatedData.Monto;
        existing.Fecha = updatedData.Fecha;
        existing.Descripcion = updatedData.Descripcion.Trim();
        existing.Notas = string.IsNullOrWhiteSpace(updatedData.Notas) ? null : updatedData.Notas.Trim();
        existing.CategoryId = updatedData.CategoryId;
        existing.MedioPago = updatedData.MedioPago;
        existing.TarjetaId = updatedData.TarjetaId;

        // Si fuera el caso de cuotas/fijos futuros, preservar o actualizar
        if (updatedData.FechaInicio.HasValue) existing.FechaInicio = updatedData.FechaInicio;
        if (updatedData.FechaFin.HasValue) existing.FechaFin = updatedData.FechaFin;
        if (updatedData.MontoTotal.HasValue) existing.MontoTotal = updatedData.MontoTotal;
        if (updatedData.CantidadCuotas.HasValue) existing.CantidadCuotas = updatedData.CantidadCuotas;

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
