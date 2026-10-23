namespace Gastito.Presentation.Outputs.Cuotas;

public class PendienteItemOutput
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public bool EsCuota { get; set; }

    // Campos para compras en cuotas
    public decimal? MontoTotal { get; set; }
    public int? CantidadCuotas { get; set; }
    public int? CuotasPagadas { get; set; }
    public int? CuotasRestantes { get; set; }
    public decimal? ValorCuota { get; set; }
    public decimal? MontoPendiente { get; set; }
    public string? Progreso { get; set; }
    public bool TerminaEnPeriodo { get; set; }
    public DateTime? FechaUltimaCuota { get; set; }
    public DateTime? FechaPrimeraCuotaPendiente { get; set; }
    public List<PagoCuotaOutput> HistorialPagos { get; set; } = new();
}
