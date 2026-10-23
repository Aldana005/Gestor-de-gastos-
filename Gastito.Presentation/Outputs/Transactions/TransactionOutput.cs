using Gastito.Entities.Enums;

namespace Gastito.Presentation.Outputs.Transactions;

public class TransactionOutput
{
    public int Id { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public Frecuencia Frecuencia { get; set; }
    public decimal Monto { get; set; }
    public Moneda Moneda { get; set; }
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Notas { get; set; }

    public int CategoryId { get; set; }
    public string CategoryNombre { get; set; } = string.Empty;

    public MedioPago? MedioPago { get; set; }
    public int? TarjetaId { get; set; }
    public string? TarjetaNombre { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    // Campos de fijos y cuotas para compatibilidad de vistas
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal? MontoTotal { get; set; }
    public int? CantidadCuotas { get; set; }
    public int? CuotasPagadas { get; set; }
    public DateTime? FechaPrimeraCuota { get; set; }
    public DateTime? FechaPrimeraCuotaPendiente { get; set; }

    public decimal? ValorCuota { get; set; }
    public decimal? ValorUltimaCuota { get; set; }
    public int? CuotasRestantes { get; set; }
    public decimal? MontoPendiente { get; set; }

    public List<HistorialMontoOutput> HistorialMontos { get; set; } = new();
}
