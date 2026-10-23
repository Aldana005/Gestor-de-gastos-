using Gastito.Entities.Enums;

namespace Gastito.Entities.Models;

public class Transaction
{
    public int Id { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public Frecuencia Frecuencia { get; set; }
    public decimal Monto { get; set; }
    public Moneda Moneda { get; set; } = Moneda.ARS;
    public DateTime Fecha { get; set; } // Fecha calendario como UTC a las 00:00:00
    public string Descripcion { get; set; } = string.Empty;
    public string? Notas { get; set; }

    // Categoría
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Medio de pago (solo gastos)
    public MedioPago? MedioPago { get; set; }
    public int? TarjetaId { get; set; }
    public Tarjeta? Tarjeta { get; set; }

    // Movimientos fijos (recurrentes)
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // Gastos en cuotas
    public decimal? MontoTotal { get; set; }
    public int? CantidadCuotas { get; set; }
    public int? CuotasPagadas { get; set; }
    public DateTime? FechaPrimeraCuota { get; set; }
    public DateTime? FechaPrimeraCuotaPendiente { get; set; } // Calendario corrido por adelantos

    // Auditoría y Soft Delete
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAtUtc { get; set; }

    // Relaciones hijas
    public ICollection<HistorialMonto> HistorialMontos { get; set; } = new List<HistorialMonto>();
    public ICollection<PagoCuota> PagosCuota { get; set; } = new List<PagoCuota>();
}
