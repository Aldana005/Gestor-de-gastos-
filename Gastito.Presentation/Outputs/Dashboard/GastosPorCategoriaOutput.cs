namespace Gastito.Presentation.Outputs.Dashboard;

public class GastosPorCategoriaOutput
{
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal Porcentaje { get; set; }
}
