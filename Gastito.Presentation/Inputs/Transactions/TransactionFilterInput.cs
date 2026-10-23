using Gastito.Entities.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Inputs.Transactions;

public class TransactionFilterInput
{
    [FromQuery(Name = "desde")]
    public DateTime? Desde { get; set; }

    [FromQuery(Name = "hasta")]
    public DateTime? Hasta { get; set; }

    [FromQuery(Name = "tipo")]
    public TipoMovimiento? Tipo { get; set; }

    [FromQuery(Name = "categoriaIds")]
    public List<int>? CategoriaIds { get; set; }

    [FromQuery(Name = "pagina")]
    public int Pagina { get; set; } = 1;

    [FromQuery(Name = "tamanoPagina")]
    public int TamanoPagina { get; set; } = 10;
}
