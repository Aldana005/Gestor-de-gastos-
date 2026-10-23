using Gastito.Entities.Models;

namespace Gastito.DataAccess.Interfaces;

public interface ITarjetaStore
{
    Task<List<Tarjeta>> GetAllAsync(CancellationToken ct = default);
    Task<Tarjeta?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Tarjeta> CreateAsync(Tarjeta tarjeta, CancellationToken ct = default);
    Task<Tarjeta> UpdateAsync(Tarjeta tarjeta, CancellationToken ct = default);
    Task DeleteAsync(Tarjeta tarjeta, CancellationToken ct = default);
    Task<bool> HasTransactionsAsync(int tarjetaId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string nombre, int? excludeId = null, CancellationToken ct = default);
}
