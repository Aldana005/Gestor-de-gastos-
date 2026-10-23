using Gastito.Entities.Models;
using Gastito.Presentation.Inputs.Transactions;
using Gastito.Presentation.Outputs.Common;
using Gastito.Presentation.Outputs.Transactions;

namespace Gastito.Presentation.Mappers;

public static class TransactionMapper
{
    public static TransactionOutput ToOutput(this Transaction entity)
    {
        return new TransactionOutput
        {
            Id = entity.Id,
            Tipo = entity.Tipo,
            Frecuencia = entity.Frecuencia,
            Monto = entity.Monto,
            Moneda = entity.Moneda,
            Fecha = entity.Fecha,
            Descripcion = entity.Descripcion,
            Notas = entity.Notas,
            CategoryId = entity.CategoryId,
            CategoryNombre = entity.Category?.Nombre ?? string.Empty,
            MedioPago = entity.MedioPago,
            TarjetaId = entity.TarjetaId,
            TarjetaNombre = entity.Tarjeta?.Nombre,
            CreatedAtUtc = entity.CreatedAtUtc,
            FechaInicio = entity.FechaInicio,
            FechaFin = entity.FechaFin,
            MontoTotal = entity.MontoTotal,
            CantidadCuotas = entity.CantidadCuotas,
            CuotasPagadas = entity.CuotasPagadas,
            FechaPrimeraCuota = entity.FechaPrimeraCuota,
            FechaPrimeraCuotaPendiente = entity.FechaPrimeraCuotaPendiente,
            HistorialMontos = entity.HistorialMontos?
                .OrderBy(h => h.VigenteDesde)
                .Select(h => new HistorialMontoOutput
                {
                    Id = h.Id,
                    Monto = h.Monto,
                    VigenteDesde = h.VigenteDesde,
                    CreatedAtUtc = h.CreatedAtUtc
                }).ToList() ?? new()
        };
    }

    public static List<TransactionOutput> ToOutputList(this IEnumerable<Transaction> entities)
    {
        return entities.Select(e => e.ToOutput()).ToList();
    }

    public static PagedResult<TransactionOutput> ToPagedResult(
        this IEnumerable<Transaction> entities,
        int totalCount,
        int pagina,
        int tamanoPagina)
    {
        return new PagedResult<TransactionOutput>
        {
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = totalCount,
            Items = entities.ToOutputList()
        };
    }

    public static Transaction ToEntity(this TransactionCreateInput input)
    {
        return new Transaction
        {
            Tipo = input.Tipo,
            Frecuencia = input.Frecuencia,
            Monto = input.Monto,
            Fecha = input.Fecha,
            Descripcion = input.Descripcion,
            Notas = input.Notas,
            CategoryId = input.CategoryId,
            MedioPago = input.MedioPago,
            TarjetaId = input.TarjetaId,
            FechaInicio = input.FechaInicio,
            FechaFin = input.FechaFin
        };
    }

    public static Transaction ToEntity(this TransactionUpdateInput input)
    {
        return new Transaction
        {
            Tipo = input.Tipo,
            Frecuencia = input.Frecuencia,
            Monto = input.Monto,
            Fecha = input.Fecha,
            Descripcion = input.Descripcion,
            Notas = input.Notas,
            CategoryId = input.CategoryId,
            MedioPago = input.MedioPago,
            TarjetaId = input.TarjetaId,
            FechaInicio = input.FechaInicio,
            FechaFin = input.FechaFin
        };
    }
}
