using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;

namespace Gastito.BusinessLogic.Managers;

public class CategoryManager : ICategoryManager
{
    private readonly ICategoryStore _categoryStore;

    public CategoryManager(ICategoryStore categoryStore)
    {
        _categoryStore = categoryStore;
    }

    public async Task<List<Category>> GetAllAsync(TipoMovimiento? tipo = null, CancellationToken ct = default)
    {
        return await _categoryStore.GetAllAsync(tipo, ct);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _categoryStore.GetByIdAsync(id, ct);
    }

    public async Task<Category> CreateAsync(string nombre, TipoMovimiento tipo, CancellationToken ct = default)
    {
        var cleanName = nombre.Trim();

        if (await _categoryStore.ExistsByNameAsync(cleanName, tipo, null, ct))
        {
            throw new BusinessException($"Ya existe una categoría con el nombre '{cleanName}' para este tipo de movimiento.");
        }

        var category = new Category
        {
            Nombre = cleanName,
            Tipo = tipo,
            EsSistema = false
        };

        return await _categoryStore.CreateAsync(category, ct);
    }

    public async Task<Category> UpdateAsync(int id, string nuevoNombre, CancellationToken ct = default)
    {
        var category = await _categoryStore.GetByIdAsync(id, ct);
        if (category == null)
        {
            throw new BusinessException($"No se encontró la categoría con ID {id}.");
        }

        // Regla: Las categorías del seed no se pueden editar ni borrar (Sección 3.2 y 14)
        if (category.EsSistema)
        {
            throw new BusinessException("Las categorías iniciales del sistema no se pueden modificar.");
        }

        var cleanName = nuevoNombre.Trim();
        if (await _categoryStore.ExistsByNameAsync(cleanName, category.Tipo, id, ct))
        {
            throw new BusinessException($"Ya existe otra categoría con el nombre '{cleanName}' para este tipo de movimiento.");
        }

        category.Nombre = cleanName;
        return await _categoryStore.UpdateAsync(category, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _categoryStore.GetByIdAsync(id, ct);
        if (category == null)
        {
            throw new BusinessException($"No se encontró la categoría con ID {id}.");
        }

        // Regla: Las categorías del seed no se pueden editar ni borrar (Sección 3.2 y 14)
        if (category.EsSistema)
        {
            throw new BusinessException("Las categorías iniciales del sistema no se pueden eliminar.");
        }

        // Regla: Una categoría propia no se puede borrar si tiene movimientos (Sección 3.2 y 14)
        if (await _categoryStore.HasTransactionsAsync(id, ct))
        {
            throw new BusinessException("No se puede eliminar la categoría porque tiene movimientos asociados.");
        }

        await _categoryStore.DeleteAsync(category, ct);
    }
}
