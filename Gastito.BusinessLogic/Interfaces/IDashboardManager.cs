using Gastito.BusinessLogic.Models;

namespace Gastito.BusinessLogic.Interfaces;

public interface IDashboardManager
{
    Task<DashboardResult> GetDashboardAsync(int meses = 3, CancellationToken ct = default);
}
