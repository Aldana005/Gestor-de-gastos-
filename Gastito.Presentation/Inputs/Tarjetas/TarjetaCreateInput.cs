using Gastito.Entities.Enums;

namespace Gastito.Presentation.Inputs.Tarjetas;

public class TarjetaCreateInput
{
    public string Nombre { get; set; } = string.Empty;
    public TipoTarjeta TipoTarjeta { get; set; }
}
