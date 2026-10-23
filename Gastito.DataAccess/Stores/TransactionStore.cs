using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
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
        var query = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Tarjeta)
            .AsQueryable();

        if (desde.HasValue)
        {
            var desdeUtc = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);
            query = query.Where(t => t.Fecha >= desdeUtc);
        }

        if (hasta.HasValue)
        {
            var hastaUtc = DateTime.SpecifyKind(hasta.Value.Date, DateTimeKind.Utc);
            query = query.Where(t => t.Fecha <= hastaUtc);
        }

        if (tipo.HasValue)
        {
            query = query.Where(t => t.Tipo == tipo.Value);
        }

        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            query = query.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        var totalCount = await query.CountAsync(ct);

        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 1) tamanoPagina = 10;

        var items = await query
            .OrderByDescending(t => t.Fecha)
            .ThenByDescending(t => t.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Tarjeta)
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
