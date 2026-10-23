using Gastito.Entities.Enums;

namespace Gastito.Presentation.Inputs.Transactions;

public class TransactionCreateInput
{
    public TipoMovimiento Tipo { get; set; }
    public Frecuencia Frecuencia { get; set; } = Frecuencia.Eventual;
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Notas { get; set; }
    public int CategoryId { get; set; }
    public MedioPago? MedioPago { get; set; }
    public int? TarjetaId { get; set; }
}
