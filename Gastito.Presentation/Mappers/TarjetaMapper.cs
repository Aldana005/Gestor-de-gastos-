using Gastito.Entities.Models;
using Gastito.Presentation.Outputs.Tarjetas;

namespace Gastito.Presentation.Mappers;

public static class TarjetaMapper
{
    public static TarjetaOutput ToOutput(this Tarjeta entity)
    {
        return new TarjetaOutput
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            TipoTarjeta = entity.TipoTarjeta
        };
    }

    public static List<TarjetaOutput> ToOutputList(this IEnumerable<Tarjeta> entities)
    {
        return entities.Select(e => e.ToOutput()).ToList();
    }
}
