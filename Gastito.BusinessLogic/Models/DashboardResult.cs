namespace Gastito.BusinessLogic.Models;

public class DashboardResult
{
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal MontoPendiente { get; set; }
    public List<GastosPorCategoriaItemResult> GastosPorCategoria { get; set; } = new();
    public List<IngresosVsGastosItemResult> IngresosVsGastos { get; set; } = new();
    public List<ProximaDeudaItemResult> ProximasDeudas { get; set; } = new();
}
