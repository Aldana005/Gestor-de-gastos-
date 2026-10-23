using Gastito.Entities.Enums;

namespace Gastito.Presentation.Outputs.Tarjetas;

public class TarjetaOutput
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoTarjeta TipoTarjeta { get; set; }
}
