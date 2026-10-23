using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Gastito.DataAccess.Stores;

public class TarjetaStore : ITarjetaStore
{
    private readonly AppDbContext _context;

    public TarjetaStore(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Tarjeta>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Tarjetas
            .AsNoTracking()
            .OrderBy(t => t.Nombre)
            .ToListAsync(ct);
    }

    public async Task<Tarjeta?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Tarjetas.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tarjeta> CreateAsync(Tarjeta tarjeta, CancellationToken ct = default)
    {
        await _context.Tarjetas.AddAsync(tarjeta, ct);
        await _context.SaveChangesAsync(ct);
        return tarjeta;
    }

    public async Task<Tarjeta> UpdateAsync(Tarjeta tarjeta, CancellationToken ct = default)
    {
        _context.Tarjetas.Update(tarjeta);
        await _context.SaveChangesAsync(ct);
        return tarjeta;
    }

    public async Task DeleteAsync(Tarjeta tarjeta, CancellationToken ct = default)
    {
        _context.Tarjetas.Remove(tarjeta);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> HasTransactionsAsync(int tarjetaId, CancellationToken ct = default)
    {
        return await _context.Transactions
            .IgnoreQueryFilters()
            .AnyAsync(t => t.TarjetaId == tarjetaId, ct);
    }

    public async Task<bool> ExistsByNameAsync(string nombre, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = nombre.Trim().ToLower();
        return await _context.Tarjetas
            .AsNoTracking()
            .AnyAsync(t => t.Nombre.ToLower() == normalized && (!excludeId.HasValue || t.Id != excludeId.Value), ct);
    }
}
