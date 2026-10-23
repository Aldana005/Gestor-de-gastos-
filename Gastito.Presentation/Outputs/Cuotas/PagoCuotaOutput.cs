namespace Gastito.Presentation.Outputs.Cuotas;

public class PagoCuotaOutput
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int CantidadCuotas { get; set; }
    public decimal Monto { get; set; }
    public int CuotaDesde { get; set; }
    public int CuotaHasta { get; set; }
    public string RangoCuotas => $"cuotas {CuotaDesde} a {CuotaHasta}";
    public DateTime FechaPagoUtc { get; set; }
}
