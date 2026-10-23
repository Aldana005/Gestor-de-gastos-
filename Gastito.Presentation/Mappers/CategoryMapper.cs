using Gastito.Entities.Models;
using Gastito.Presentation.Outputs.Categories;

namespace Gastito.Presentation.Mappers;

public static class CategoryMapper
{
    public static CategoryOutput ToOutput(this Category entity)
    {
        return new CategoryOutput
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            Tipo = entity.Tipo,
            EsSistema = entity.EsSistema
        };
    }

    public static List<CategoryOutput> ToOutputList(this IEnumerable<Category> entities)
    {
        return entities.Select(e => e.ToOutput()).ToList();
    }
}
