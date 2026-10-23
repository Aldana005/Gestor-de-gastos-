using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Gastito.Entities.Utils;
using Microsoft.EntityFrameworkCore;

namespace Gastito.DataAccess.Stores;

public class TransactionStore : ITransactionStore
{
    private readonly AppDbContext _context;

    public TransactionStore(AppDbContext context)
    {
        _context = context;
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
        DateTime? desdeUtc = desde.HasValue ? DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc) : null;
        DateTime? hastaUtc = hasta.HasValue ? DateTime.SpecifyKind(hasta.Value.Date, DateTimeKind.Utc) : null;

        // 1. Movimientos Eventuales
        var eventualQuery = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Tarjeta)
            .Include(t => t.HistorialMontos)
            .Where(t => t.Frecuencia == Frecuencia.Eventual)
            .AsQueryable();

        if (tipo.HasValue)
        {
            eventualQuery = eventualQuery.Where(t => t.Tipo == tipo.Value);
        }

        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            eventualQuery = eventualQuery.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        var eventualRaw = await eventualQuery.ToListAsync(ct);
        var eventualItems = new List<Transaction>();

        foreach (var item in eventualRaw)
        {
            var tieneCuotas = item.CantidadCuotas.HasValue && item.CantidadCuotas.Value > 1 && item.MontoTotal.HasValue;
            if (tieneCuotas && (desdeUtc.HasValue || hastaUtc.HasValue))
            {
                // En consultas con rango, proyectar la cuota que cae en el período (Sección 3.5 y 3.7)
                var primeraCuota = item.FechaPrimeraCuota ?? item.Fecha;
                for (int c = 1; c <= item.CantidadCuotas!.Value; c++)
                {
                    var fechaCuota = CuotaCalculator.CalcularFechaCuota(primeraCuota, c);
                    var matchesRange = (!desdeUtc.HasValue || fechaCuota >= desdeUtc.Value) &&
                                       (!hastaUtc.HasValue || fechaCuota <= hastaUtc.Value);
                    if (matchesRange)
                    {
                        var montoCuota = CuotaCalculator.CalcularValorCuota(item.MontoTotal!.Value, item.CantidadCuotas.Value, c);
                        eventualItems.Add(new Transaction
                        {
                            Id = item.Id,
                            Tipo = item.Tipo,
                            Frecuencia = item.Frecuencia,
                            Monto = montoCuota,
                            Moneda = item.Moneda,
                            Fecha = fechaCuota,
                            Descripcion = item.Descripcion,
                            Notas = item.Notas,
                            CategoryId = item.CategoryId,
                            Category = item.Category,
                            MedioPago = item.MedioPago,
                            TarjetaId = item.TarjetaId,
                            Tarjeta = item.Tarjeta,
                            FechaInicio = item.FechaInicio,
                            FechaFin = item.FechaFin,
                            MontoTotal = item.MontoTotal,
                            CantidadCuotas = item.CantidadCuotas,
                            CuotasPagadas = item.CuotasPagadas,
                            FechaPrimeraCuota = item.FechaPrimeraCuota,
                            FechaPrimeraCuotaPendiente = item.FechaPrimeraCuotaPendiente,
                            CreatedAtUtc = item.CreatedAtUtc,
                            HistorialMontos = item.HistorialMontos
                        });
                    }
                }
            }
            else
            {
                var matchesRange = (!desdeUtc.HasValue || item.Fecha >= desdeUtc.Value) &&
                                   (!hastaUtc.HasValue || item.Fecha <= hastaUtc.Value);
                if (matchesRange)
                {
                    eventualItems.Add(item);
                }
            }
        }

        // 2. Movimientos Fijos (recurrentes: aparecen una vez por mes en el período activo)
        var fixedQuery = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Tarjeta)
            .Include(t => t.HistorialMontos)
            .Where(t => t.Frecuencia == Frecuencia.Fijo)
            .AsQueryable();

        if (tipo.HasValue)
        {
            fixedQuery = fixedQuery.Where(t => t.Tipo == tipo.Value);
        }

        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            fixedQuery = fixedQuery.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        var fixedTransactions = await fixedQuery.ToListAsync(ct);
        var fixedOccurrences = new List<Transaction>();

        var defaultNow = DateTime.UtcNow;
        var queryStart = desdeUtc ?? (hastaUtc.HasValue ? hastaUtc.Value.AddMonths(-2) : new DateTime(defaultNow.Year, defaultNow.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        var queryEnd = hastaUtc ?? (desdeUtc.HasValue ? desdeUtc.Value.AddMonths(2) : new DateTime(defaultNow.Year, defaultNow.Month, DateTime.DaysInMonth(defaultNow.Year, defaultNow.Month), 23, 59, 59, DateTimeKind.Utc));

        foreach (var fixedTx in fixedTransactions)
        {
            var fInicio = fixedTx.FechaInicio ?? fixedTx.Fecha;
            var fFin = fixedTx.FechaFin;

            // Rango efectivo de meses a evaluar
            var monthCursor = new DateTime(Math.Max(queryStart.Year, fInicio.Year), 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (queryStart > fInicio)
            {
                monthCursor = new DateTime(queryStart.Year, queryStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                monthCursor = new DateTime(fInicio.Year, fInicio.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            var limitDate = queryEnd;
            if (fFin.HasValue && fFin.Value < limitDate)
            {
                limitDate = fFin.Value;
            }

            var endMonth = new DateTime(limitDate.Year, limitDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            while (monthCursor <= endMonth)
            {
                var originalDay = fInicio.Day;
                var daysInMonth = DateTime.DaysInMonth(monthCursor.Year, monthCursor.Month);
                var day = Math.Min(originalDay, daysInMonth);
                var occurrenceDate = new DateTime(monthCursor.Year, monthCursor.Month, day, 0, 0, 0, DateTimeKind.Utc);

                var isWithinRange = (!desdeUtc.HasValue || occurrenceDate >= desdeUtc.Value) &&
                                    (!hastaUtc.HasValue || occurrenceDate <= hastaUtc.Value) &&
                                    occurrenceDate >= fInicio &&
                                    (!fFin.HasValue || occurrenceDate <= fFin.Value);

                if (isWithinRange)
                {
                    // Buscar monto vigente para este mes
                    var monthStart = new DateTime(monthCursor.Year, monthCursor.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var effectiveMonto = fixedTx.HistorialMontos
                        .Where(h => h.VigenteDesde <= monthStart)
                        .OrderByDescending(h => h.VigenteDesde)
                        .ThenByDescending(h => h.Id)
                        .FirstOrDefault()?.Monto ?? fixedTx.Monto;

                    fixedOccurrences.Add(new Transaction
                    {
                        Id = fixedTx.Id,
                        Tipo = fixedTx.Tipo,
                        Frecuencia = fixedTx.Frecuencia,
                        Monto = effectiveMonto,
                        Moneda = fixedTx.Moneda,
                        Fecha = occurrenceDate,
                        Descripcion = fixedTx.Descripcion,
                        Notas = fixedTx.Notas,
                        CategoryId = fixedTx.CategoryId,
                        Category = fixedTx.Category,
                        MedioPago = fixedTx.MedioPago,
                        TarjetaId = fixedTx.TarjetaId,
                        Tarjeta = fixedTx.Tarjeta,
                        FechaInicio = fixedTx.FechaInicio,
                        FechaFin = fixedTx.FechaFin,
                        MontoTotal = fixedTx.MontoTotal,
                        CantidadCuotas = fixedTx.CantidadCuotas,
                        CuotasPagadas = fixedTx.CuotasPagadas,
                        FechaPrimeraCuota = fixedTx.FechaPrimeraCuota,
                        FechaPrimeraCuotaPendiente = fixedTx.FechaPrimeraCuotaPendiente,
                        CreatedAtUtc = fixedTx.CreatedAtUtc,
                        HistorialMontos = fixedTx.HistorialMontos
                    });
                }

                monthCursor = monthCursor.AddMonths(1);
            }
        }

        var allItems = eventualItems.Concat(fixedOccurrences)
            .OrderByDescending(t => t.Fecha)
            .ThenByDescending(t => t.Id)
            .ToList();

        var totalCount = allItems.Count;

        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 1) tamanoPagina = 10;

        var pagedItems = allItems
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return (pagedItems, totalCount);
    }

    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Tarjeta)
            .Include(t => t.HistorialMontos)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken ct = default)
    {
        await _context.Transactions.AddAsync(transaction, ct);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(transaction).Reference(t => t.Category).LoadAsync(ct);
        if (transaction.TarjetaId.HasValue)
        {
            await _context.Entry(transaction).Reference(t => t.Tarjeta).LoadAsync(ct);
        }

        return transaction;
    }

    public async Task<Transaction> UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        var entry = _context.Entry(transaction);
        if (entry.State == EntityState.Detached)
        {
            _context.Transactions.Update(transaction);
        }

        await _context.SaveChangesAsync(ct);

        await _context.Entry(transaction).Reference(t => t.Category).LoadAsync(ct);
        if (transaction.TarjetaId.HasValue)
        {
            await _context.Entry(transaction).Reference(t => t.Tarjeta).LoadAsync(ct);
        }

        return transaction;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (transaction != null)
        {
            // Borrado lógico (Soft Delete - Sección 3.8)
            transaction.IsDeleted = true;
            transaction.DeletedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }
}
