using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.BusinessLogic.Models;
using Gastito.BusinessLogic.Services;
using Gastito.BusinessLogic.Utils;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;

namespace Gastito.BusinessLogic.Managers;

public class ReportesManager : IReportesManager
{
    private readonly ITransactionStore _transactionStore;
    private readonly ICategoryStore _categoryStore;

    public ReportesManager(ITransactionStore transactionStore, ICategoryStore categoryStore)
    {
        _transactionStore = transactionStore;
        _categoryStore = categoryStore;
    }

    public async Task<ReporteResumenResult> GetResumenAsync(
        DateTime? desde,
        DateTime? hasta,
        List<int>? categoriaIds,
        CancellationToken ct = default)
    {
        var today = DateTimeUtils.GetArgentinaToday();
        var desdeUtc = desde.HasValue
            ? DateTimeUtils.ToCalendarUtc(desde.Value)
            : DateTimeUtils.GetStartOfMonthUtc(today);

        var hastaUtc = hasta.HasValue
            ? new DateTime(hasta.Value.Year, hasta.Value.Month, hasta.Value.Day, 23, 59, 59, DateTimeKind.Utc)
            : DateTimeUtils.GetEndOfMonthUtc(today);

        if (hastaUtc < desdeUtc)
        {
            throw new BusinessException("La fecha hasta no puede ser anterior a la fecha desde.");
        }

        var allIngresos = await _transactionStore.GetRawAsync(TipoMovimiento.Ingreso, ct);
        var allGastos = await _transactionStore.GetRawAsync(TipoMovimiento.Gasto, ct);
        var allCategories = await _categoryStore.GetAllAsync(ct: ct);

        // 1. Totales y distribución del período
        var totalIngresos = FinancialCalculationService.CalculateIngresos(allIngresos, desdeUtc, hastaUtc, categoriaIds);
        var (totalGastos, _, gastosPorCat) = FinancialCalculationService.CalculateGastos(allGastos, desdeUtc, hastaUtc, categoriaIds);
        var gastosPorCategoria = FinancialCalculationService.BuildGastosPorCategoria(gastosPorCat, totalGastos, allCategories);

        // 2. Próximas deudas del período
        var proximasDeudas = FinancialCalculationService.CalculateProximasDeudas(
            allGastos,
            desdeUtc,
            hastaUtc,
            requireInstallmentInPeriod: true,
            categoriaIds);

        return new ReporteResumenResult
        {
            Desde = desdeUtc,
            Hasta = hastaUtc,
            TotalIngresos = totalIngresos,
            TotalGastos = totalGastos,
            GastosPorCategoria = gastosPorCategoria,
            ProximasDeudas = proximasDeudas
        };
    }
}
