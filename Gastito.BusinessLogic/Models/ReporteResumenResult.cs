namespace Gastito.BusinessLogic.Models;

public class ReporteResumenResult
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public List<GastosPorCategoriaItemResult> GastosPorCategoria { get; set; } = new();
    public List<ProximaDeudaItemResult> ProximasDeudas { get; set; } = new();
}
