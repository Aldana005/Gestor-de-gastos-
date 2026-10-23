using Gastito.Entities.Enums;
using Gastito.Entities.Models;

namespace Gastito.DataAccess.Interfaces;

public interface ICategoryStore
{
    Task<List<Category>> GetAllAsync(TipoMovimiento? tipo = null, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Category> CreateAsync(Category category, CancellationToken ct = default);
    Task<Category> UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(Category category, CancellationToken ct = default);
    Task<bool> HasTransactionsAsync(int categoryId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string nombre, TipoMovimiento tipo, int? excludeId = null, CancellationToken ct = default);
}
