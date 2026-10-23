using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;

namespace Gastito.BusinessLogic.Managers;

public class TarjetaManager : ITarjetaManager
{
    private readonly ITarjetaStore _tarjetaStore;

    public TarjetaManager(ITarjetaStore tarjetaStore)
    {
        _tarjetaStore = tarjetaStore;
    }

    public async Task<List<Tarjeta>> GetAllAsync(CancellationToken ct = default)
    {
        return await _tarjetaStore.GetAllAsync(ct);
    }

    public async Task<Tarjeta?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _tarjetaStore.GetByIdAsync(id, ct);
    }

    public async Task<Tarjeta> CreateAsync(string nombre, TipoTarjeta tipoTarjeta, CancellationToken ct = default)
    {
        var cleanName = nombre.Trim();

        if (await _tarjetaStore.ExistsByNameAsync(cleanName, null, ct))
        {
            throw new BusinessException($"Ya existe una tarjeta con el nombre '{cleanName}'.");
        }

        var tarjeta = new Tarjeta
        {
            Nombre = cleanName,
            TipoTarjeta = tipoTarjeta
        };

        return await _tarjetaStore.CreateAsync(tarjeta, ct);
    }

    public async Task<Tarjeta> UpdateAsync(int id, string nuevoNombre, TipoTarjeta tipoTarjeta, CancellationToken ct = default)
    {
        var tarjeta = await _tarjetaStore.GetByIdAsync(id, ct);
        if (tarjeta == null)
        {
            throw new BusinessException($"No se encontró la tarjeta con ID {id}.");
        }

        var cleanName = nuevoNombre.Trim();
        if (await _tarjetaStore.ExistsByNameAsync(cleanName, id, ct))
        {
            throw new BusinessException($"Ya existe otra tarjeta con el nombre '{cleanName}'.");
        }

        tarjeta.Nombre = cleanName;
        tarjeta.TipoTarjeta = tipoTarjeta;

        return await _tarjetaStore.UpdateAsync(tarjeta, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var tarjeta = await _tarjetaStore.GetByIdAsync(id, ct);
        if (tarjeta == null)
        {
            throw new BusinessException($"No se encontró la tarjeta con ID {id}.");
        }

        if (await _tarjetaStore.HasTransactionsAsync(id, ct))
        {
            throw new BusinessException("No se puede eliminar la tarjeta porque tiene movimientos asociados.");
        }

        await _tarjetaStore.DeleteAsync(tarjeta, ct);
    }
}
