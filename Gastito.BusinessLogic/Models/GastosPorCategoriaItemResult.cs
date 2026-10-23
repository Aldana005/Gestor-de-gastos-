namespace Gastito.BusinessLogic.Models;

public class GastosPorCategoriaItemResult
{
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal Porcentaje { get; set; }
}
