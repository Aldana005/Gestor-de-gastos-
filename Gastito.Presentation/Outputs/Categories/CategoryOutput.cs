using Gastito.Entities.Enums;

namespace Gastito.Presentation.Outputs.Categories;

public class CategoryOutput
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoMovimiento Tipo { get; set; }
    public bool EsSistema { get; set; }
}
