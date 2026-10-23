using Gastito.Entities.Enums;
using Gastito.Entities.Models;

namespace Gastito.BusinessLogic.Interfaces;

public interface ITransactionManager
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

    Task<Transaction> UpdateAsync(int id, Transaction updatedData, AplicarDesde? aplicarDesde = null, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<PagoCuota> AdelantarCuotasAsync(int transactionId, int cantidad, CancellationToken ct = default);

    Task<List<PagoCuota>> GetPagosCuotaAsync(int transactionId, CancellationToken ct = default);

    Task<List<Transaction>> GetPendientesAsync(
        DateTime? desde,
        DateTime? hasta,
        List<int>? categoriaIds,
        decimal? montoMin,
        decimal? montoMax,
        CancellationToken ct = default);
}
