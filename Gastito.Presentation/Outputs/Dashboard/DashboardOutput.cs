namespace Gastito.Presentation.Outputs.Dashboard;

public class DashboardOutput
{
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal MontoPendiente { get; set; }
    public List<GastosPorCategoriaOutput> GastosPorCategoria { get; set; } = new();
    public List<IngresosVsGastosOutput> IngresosVsGastos { get; set; } = new();
    public List<ProximaDeudaOutput> ProximasDeudas { get; set; } = new();
}
