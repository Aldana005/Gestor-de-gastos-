using Gastito.Entities.Enums;

namespace Gastito.Presentation.Inputs.Categories;

public class CategoryCreateInput
{
    public string Nombre { get; set; } = string.Empty;
    public TipoMovimiento Tipo { get; set; }
}
