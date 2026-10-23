namespace Gastito.BusinessLogic.Models;

public class ProximaDeudaItemResult
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int CuotasPagadas { get; set; }
    public int CantidadCuotas { get; set; }
    public decimal ValorCuota { get; set; }
    public string MesProximaCuota { get; set; } = string.Empty; // "2026-10"
}
