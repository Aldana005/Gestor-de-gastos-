using Gastito.Entities.Enums;

namespace Gastito.Entities.Models;

public class TelegramConversacion
{
    public long ChatId { get; set; }
    public TelegramEstadoConversacion Estado { get; set; } = TelegramEstadoConversacion.Ninguno;
    public TipoMovimiento? Tipo { get; set; }
    public decimal? Monto { get; set; }
    public MedioPago? MedioPago { get; set; }
    public int? TarjetaId { get; set; }
    public int? CantidadCuotas { get; set; }
    public int? CategoryId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime UltimaActualizacionUtc { get; set; } = DateTime.UtcNow;

    public void Reset()
    {
        Estado = TelegramEstadoConversacion.Ninguno;
        Tipo = null;
        Monto = null;
        MedioPago = null;
        TarjetaId = null;
        CantidadCuotas = null;
        CategoryId = null;
        Descripcion = null;
        UltimaActualizacionUtc = DateTime.UtcNow;
    }
}
