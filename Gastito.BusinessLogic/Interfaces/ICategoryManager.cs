using Gastito.Entities.Enums;
using Gastito.Entities.Models;

namespace Gastito.BusinessLogic.Interfaces;

public interface ICategoryManager
{
    Task<List<Category>> GetAllAsync(TipoMovimiento? tipo = null, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Category> CreateAsync(string nombre, TipoMovimiento tipo, CancellationToken ct = default);
    Task<Category> UpdateAsync(int id, string nuevoNombre, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
