namespace Gastito.Entities.Models;

public class PagoCuota
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public int CantidadCuotas { get; set; }
    public decimal Monto { get; set; }
    public int CuotaDesde { get; set; }
    public int CuotaHasta { get; set; }
    public DateTime FechaPagoUtc { get; set; } = DateTime.UtcNow;
}
