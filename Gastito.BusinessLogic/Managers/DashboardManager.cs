using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.BusinessLogic.Models;
using Gastito.BusinessLogic.Services;
using Gastito.BusinessLogic.Utils;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;

namespace Gastito.BusinessLogic.Managers;

public class DashboardManager : IDashboardManager
{
    private readonly ITransactionStore _transactionStore;
    private readonly ICategoryStore _categoryStore;

    public DashboardManager(ITransactionStore transactionStore, ICategoryStore categoryStore)
    {
        _transactionStore = transactionStore;
        _categoryStore = categoryStore;
    }

    public async Task<DashboardResult> GetDashboardAsync(int meses = 3, CancellationToken ct = default)
    {
        if (meses < 3 || meses > 12)
        {
            throw new BusinessException("El parámetro meses debe estar comprendido entre 3 y 12.");
        }

        var today = DateTimeUtils.GetArgentinaToday();
        var startOfMonth = DateTimeUtils.GetStartOfMonthUtc(today);
        var endOfMonth = DateTimeUtils.GetEndOfMonthUtc(today);

        var allIngresos = await _transactionStore.GetRawAsync(TipoMovimiento.Ingreso, ct);
        var allGastos = await _transactionStore.GetRawAsync(TipoMovimiento.Gasto, ct);
        var allCategories = await _categoryStore.GetAllAsync(ct: ct);

        // 1. Totales y distribución del mes actual
        var totalIngresos = FinancialCalculationService.CalculateIngresos(allIngresos, startOfMonth, endOfMonth);
        var (totalGastos, montoPendiente, gastosPorCat) = FinancialCalculationService.CalculateGastos(allGastos, startOfMonth, endOfMonth);
        var gastosPorCategoria = FinancialCalculationService.BuildGastosPorCategoria(gastosPorCat, totalGastos, allCategories);

        // 2. Ingresos vs Gastos (gráfico de líneas con historial de 'meses' meses)
        var ingresosVsGastos = new List<IngresosVsGastosItemResult>();
        for (int i = meses - 1; i >= 0; i--)
        {
            var targetMonth = today.AddMonths(-i);
            var mStart = DateTimeUtils.GetStartOfMonthUtc(targetMonth);
            var mEnd = DateTimeUtils.GetEndOfMonthUtc(targetMonth);

            var mIngresos = FinancialCalculationService.CalculateIngresos(allIngresos, mStart, mEnd);
            var (mGastos, _, _) = FinancialCalculationService.CalculateGastos(allGastos, mStart, mEnd);

            ingresosVsGastos.Add(new IngresosVsGastosItemResult
            {
                Anio = targetMonth.Year,
                Mes = targetMonth.Month,
                Periodo = $"{targetMonth.Year}-{targetMonth.Month:D2}",
                TotalIngresos = mIngresos,
                TotalGastos = mGastos
            });
        }

        // 3. Próximas deudas (compras en cuotas con cuotas sin pagar, en el orden de Pendientes)
        var proximasDeudas = FinancialCalculationService.CalculateProximasDeudas(
            allGastos,
            startOfMonth,
            endOfMonth,
            requireInstallmentInPeriod: false);

        return new DashboardResult
        {
            TotalIngresos = totalIngresos,
            TotalGastos = totalGastos,
            MontoPendiente = montoPendiente,
            GastosPorCategoria = gastosPorCategoria,
            IngresosVsGastos = ingresosVsGastos,
            ProximasDeudas = proximasDeudas
        };
    }
}
