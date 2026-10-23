using Gastito.BusinessLogic.Models;

namespace Gastito.BusinessLogic.Interfaces;

public interface IReportesManager
{
    Task<ReporteResumenResult> GetResumenAsync(DateTime? desde, DateTime? hasta, List<int>? categoriaIds, CancellationToken ct = default);
}
