namespace Gastito.BusinessLogic.Models;

public class IngresosVsGastosItemResult
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Periodo { get; set; } = string.Empty; // "2026-10"
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
}
