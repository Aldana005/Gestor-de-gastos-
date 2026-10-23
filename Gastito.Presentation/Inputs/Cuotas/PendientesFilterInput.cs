namespace Gastito.Presentation.Inputs.Cuotas;

public class PendientesFilterInput
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public List<int>? CategoriaIds { get; set; }
    public decimal? MontoMin { get; set; }
    public decimal? MontoMax { get; set; }
}
