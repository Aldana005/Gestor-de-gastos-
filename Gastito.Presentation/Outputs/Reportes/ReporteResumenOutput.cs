using Gastito.Presentation.Outputs.Dashboard;

namespace Gastito.Presentation.Outputs.Reportes;

public class ReporteResumenOutput
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public List<GastosPorCategoriaOutput> GastosPorCategoria { get; set; } = new();
    public List<ProximaDeudaOutput> ProximasDeudas { get; set; } = new();
}
