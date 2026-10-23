namespace Gastito.Presentation.Outputs.Common;

public class PagedResult<T>
{
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas => TamanoPagina > 0 ? (int)Math.Ceiling((double)TotalRegistros / TamanoPagina) : 0;
    public List<T> Items { get; set; } = new();
}
