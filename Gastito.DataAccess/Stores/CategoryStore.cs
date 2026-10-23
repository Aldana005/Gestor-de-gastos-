using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Gastito.DataAccess.Stores;

public class CategoryStore : ICategoryStore
{
    private readonly AppDbContext _context;

    public CategoryStore(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllAsync(TipoMovimiento? tipo = null, CancellationToken ct = default)
    {
        var query = _context.Categories.AsNoTracking().AsQueryable();

        if (tipo.HasValue)
        {
            query = query.Where(c => c.Tipo == tipo.Value);
        }

        return await query.OrderBy(c => c.Nombre).ToListAsync(ct);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category> CreateAsync(Category category, CancellationToken ct = default)
    {
        await _context.Categories.AddAsync(category, ct);
        await _context.SaveChangesAsync(ct);
        return category;
    }

    public async Task<Category> UpdateAsync(Category category, CancellationToken ct = default)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(ct);
        return category;
    }

    public async Task DeleteAsync(Category category, CancellationToken ct = default)
    {
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> HasTransactionsAsync(int categoryId, CancellationToken ct = default)
    {
        // Verifica si existen movimientos asociados (incluso soft-deleted para preservar integridad)
        return await _context.Transactions
            .IgnoreQueryFilters()
            .AnyAsync(t => t.CategoryId == categoryId, ct);
    }

    public async Task<bool> ExistsByNameAsync(string nombre, TipoMovimiento tipo, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = nombre.Trim().ToLower();
        return await _context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Tipo == tipo && c.Nombre.ToLower() == normalized && (!excludeId.HasValue || c.Id != excludeId.Value), ct);
    }
}
