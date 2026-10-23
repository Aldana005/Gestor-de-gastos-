using Gastito.Entities.Enums;
using Gastito.Entities.Models;

namespace Gastito.DataAccess.Interfaces;

public interface ITransactionStore
{
    Task<(List<Transaction> Items, int TotalCount)> GetPagedAsync(
        DateTime? desde,
        DateTime? hasta,
        TipoMovimiento? tipo,
        List<int>? categoriaIds,
        int pagina,
        int tamanoPagina,
        CancellationToken ct = default);

    Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Transaction> CreateAsync(Transaction transaction, CancellationToken ct = default);

    Task<Transaction> UpdateAsync(Transaction transaction, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
