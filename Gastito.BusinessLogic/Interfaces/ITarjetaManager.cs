using Gastito.Entities.Enums;
using Gastito.Entities.Models;

namespace Gastito.BusinessLogic.Interfaces;

public interface ITarjetaManager
{
    Task<List<Tarjeta>> GetAllAsync(CancellationToken ct = default);
    Task<Tarjeta?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Tarjeta> CreateAsync(string nombre, TipoTarjeta tipoTarjeta, CancellationToken ct = default);
    Task<Tarjeta> UpdateAsync(int id, string nuevoNombre, TipoTarjeta tipoTarjeta, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
