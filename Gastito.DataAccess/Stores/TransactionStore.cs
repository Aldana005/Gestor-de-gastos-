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
            .Include(t => t.PagosCuota)
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
                var primeraCuota = item.FechaPrimeraCuota ?? item.Fecha;
                var cantidad = item.CantidadCuotas!.Value;
                var pagadas = item.CuotasPagadas ?? 0;

                // A. Registro de compra en su mes de carga para Camino 1 (cuando la 1ª cuota empieza en un mes posterior):
                // En el mes de compra figura el registro informativo con Monto = 0m, sin inflar los gastos del mes.
                var esCamino1 = item.FechaPrimeraCuota.HasValue &&
                                (item.FechaPrimeraCuota.Value.Year > item.Fecha.Year ||
                                 (item.FechaPrimeraCuota.Value.Year == item.Fecha.Year && item.FechaPrimeraCuota.Value.Month > item.Fecha.Month));

                if (esCamino1)
                {
                    var compraMatchesRange = (!desdeUtc.HasValue || item.Fecha >= desdeUtc.Value) &&
                                             (!hastaUtc.HasValue || item.Fecha <= hastaUtc.Value);
                    if (compraMatchesRange)
                    {
                        eventualItems.Add(new Transaction
                        {
                            Id = item.Id,
                            Tipo = item.Tipo,
                            Frecuencia = item.Frecuencia,
                            Monto = 0m,
                            Moneda = item.Moneda,
                            Fecha = item.Fecha,
                            Descripcion = $"{item.Descripcion} ({cantidad} cuotas)",
                            Notas = item.Notas,
                            CategoryId = item.CategoryId,
                            Category = item.Category,
                            MedioPago = item.MedioPago,
                            TarjetaId = item.TarjetaId,
                            Tarjeta = item.Tarjeta,
                            MontoTotal = item.MontoTotal,
                            CantidadCuotas = item.CantidadCuotas,
                            CuotasPagadas = item.CuotasPagadas,
                            FechaPrimeraCuota = item.FechaPrimeraCuota,
                            FechaPrimeraCuotaPendiente = item.FechaPrimeraCuotaPendiente,
                            CreatedAtUtc = item.CreatedAtUtc
                        });
                    }
                }

                // B. Movimientos individuales por cada cuota adelantada/pagada en PagoCuota:
                // Se proyectan con la fecha en que se realizó el pago efectivo.
                if (item.PagosCuota != null)
                {
                    foreach (var pago in item.PagosCuota)
                    {
                        var pagoMatches = (!desdeUtc.HasValue || pago.FechaPagoUtc >= desdeUtc.Value) &&
                                          (!hastaUtc.HasValue || pago.FechaPagoUtc <= hastaUtc.Value);
                        if (pagoMatches)
                        {
                            for (int c = pago.CuotaDesde; c <= pago.CuotaHasta; c++)
                            {
                                var montoCuota = CuotaCalculator.CalcularValorCuota(item.MontoTotal!.Value, cantidad, c);
                                eventualItems.Add(new Transaction
                                {
                                    Id = item.Id,
                                    Tipo = item.Tipo,
                                    Frecuencia = item.Frecuencia,
                                    Monto = montoCuota,
                                    Moneda = item.Moneda,
                                    Fecha = pago.FechaPagoUtc,
                                    Descripcion = $"Cuota {c}/{cantidad} - {item.Descripcion} (Adelanto)",
                                    Notas = item.Notas,
                                    CategoryId = item.CategoryId,
                                    Category = item.Category,
                                    MedioPago = item.MedioPago,
                                    TarjetaId = item.TarjetaId,
                                    Tarjeta = item.Tarjeta,
                                    MontoTotal = item.MontoTotal,
                                    CantidadCuotas = item.CantidadCuotas,
                                    CuotasPagadas = item.CuotasPagadas,
                                    FechaPrimeraCuota = item.FechaPrimeraCuota,
                                    FechaPrimeraCuotaPendiente = item.FechaPrimeraCuotaPendiente,
                                    CreatedAtUtc = pago.FechaPagoUtc
                                });
                            }
                        }
                    }
                }

                // C. Cuotas programadas pendientes (no saldadas ni adelantadas):
                // Se proyectan mes a mes a partir de cuotasPagadas + 1 con el corrimiento consecutivo.
                for (int c = pagadas + 1; c <= cantidad; c++)
                {
                    var fechaCuota = CuotaCalculator.CalcularFechaDeCuotaConCorrimiento(primeraCuota, item.FechaPrimeraCuotaPendiente, pagadas, c);
                    var matchesRange = (!desdeUtc.HasValue || fechaCuota >= desdeUtc.Value) &&
                                       (!hastaUtc.HasValue || fechaCuota <= hastaUtc.Value);
                    if (matchesRange)
                    {
                        var montoCuota = CuotaCalculator.CalcularValorCuota(item.MontoTotal!.Value, cantidad, c);
                        eventualItems.Add(new Transaction
                        {
                            Id = item.Id,
                            Tipo = item.Tipo,
                            Frecuencia = item.Frecuencia,
                            Monto = montoCuota,
                            Moneda = item.Moneda,
                            Fecha = fechaCuota,
                            Descripcion = $"Cuota {c}/{cantidad} - {item.Descripcion}",
                            Notas = item.Notas,
                            CategoryId = item.CategoryId,
                            Category = item.Category,
                            MedioPago = item.MedioPago,
                            TarjetaId = item.TarjetaId,
                            Tarjeta = item.Tarjeta,
                            MontoTotal = item.MontoTotal,
                            CantidadCuotas = item.CantidadCuotas,
                            CuotasPagadas = item.CuotasPagadas,
                            FechaPrimeraCuota = item.FechaPrimeraCuota,
                            FechaPrimeraCuotaPendiente = item.FechaPrimeraCuotaPendiente,
                            CreatedAtUtc = item.CreatedAtUtc
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
            var earliestHistorial = fixedTx.HistorialMontos?.Count > 0
                ? fixedTx.HistorialMontos.Min(h => h.VigenteDesde)
                : (DateTime?)null;

            var fInicio = fixedTx.FechaInicio ?? fixedTx.Fecha;
            if (earliestHistorial.HasValue && earliestHistorial.Value < fInicio)
            {
                fInicio = earliestHistorial.Value;
            }
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
                var originalDay = fixedTx.Fecha.Day;
                var daysInMonth = DateTime.DaysInMonth(monthCursor.Year, monthCursor.Month);
                var day = Math.Min(originalDay, daysInMonth);
                var occurrenceDate = new DateTime(monthCursor.Year, monthCursor.Month, day, 0, 0, 0, DateTimeKind.Utc);

                var isWithinRange = (!desdeUtc.HasValue || occurrenceDate >= desdeUtc.Value) &&
                                    (!hastaUtc.HasValue || occurrenceDate <= hastaUtc.Value) &&
                                    monthCursor >= new DateTime(fInicio.Year, fInicio.Month, 1, 0, 0, 0, DateTimeKind.Utc) &&
                                    (!fFin.HasValue || occurrenceDate <= fFin.Value);

                if (isWithinRange)
                {
                    // Buscar monto vigente para este mes
                    var monthStart = new DateTime(monthCursor.Year, monthCursor.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var effectiveMonto = (fixedTx.HistorialMontos ?? Enumerable.Empty<HistorialMonto>())
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
                        HistorialMontos = fixedTx.HistorialMontos ?? new List<HistorialMonto>()
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
            .Include(t => t.PagosCuota)
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

    public async Task<PagoCuota> AddPagoCuotaAsync(PagoCuota pago, CancellationToken ct = default)
    {
        await _context.Set<PagoCuota>().AddAsync(pago, ct);
        await _context.SaveChangesAsync(ct);
        return pago;
    }

    public async Task<List<PagoCuota>> GetPagosCuotaAsync(int transactionId, CancellationToken ct = default)
    {
        return await _context.Set<PagoCuota>()
            .AsNoTracking()
            .Where(p => p.TransactionId == transactionId)
            .OrderBy(p => p.FechaPagoUtc)
            .ToListAsync(ct);
    }

    public async Task<List<Transaction>> GetRawAsync(TipoMovimiento? tipo = null, CancellationToken ct = default)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Tarjeta)
            .Include(t => t.HistorialMontos)
            .Include(t => t.PagosCuota)
            .AsQueryable();

        if (tipo.HasValue)
        {
            query = query.Where(t => t.Tipo == tipo.Value);
        }

        return await query.ToListAsync(ct);
    }
}
