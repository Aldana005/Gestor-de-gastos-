using Gastito.Entities.Models;
using Gastito.Entities.Utils;
using Gastito.Presentation.Inputs.Transactions;
using Gastito.Presentation.Outputs.Common;
using Gastito.Presentation.Outputs.Transactions;

namespace Gastito.Presentation.Mappers;

public static class TransactionMapper
{
    public static TransactionOutput ToOutput(this Transaction entity)
    {
        decimal? valorCuota = null;
        decimal? valorUltimaCuota = null;
        int? cuotasRestantes = null;
        decimal? montoPendiente = null;

        if (entity.MontoTotal.HasValue && entity.CantidadCuotas.HasValue && entity.CantidadCuotas.Value > 1)
        {
            valorCuota = CuotaCalculator.CalcularValorCuotaBase(entity.MontoTotal.Value, entity.CantidadCuotas.Value);
            valorUltimaCuota = CuotaCalculator.CalcularValorUltimaCuota(entity.MontoTotal.Value, entity.CantidadCuotas.Value);
            cuotasRestantes = Math.Max(0, entity.CantidadCuotas.Value - (entity.CuotasPagadas ?? 0));
            montoPendiente = CuotaCalculator.CalcularMontoPendiente(entity.MontoTotal.Value, entity.CantidadCuotas.Value, entity.CuotasPagadas ?? 0);
        }

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
            ValorCuota = valorCuota,
            ValorUltimaCuota = valorUltimaCuota,
            CuotasRestantes = cuotasRestantes,
            MontoPendiente = montoPendiente,
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
            FechaFin = input.FechaFin,
            MontoTotal = input.MontoTotal,
            CantidadCuotas = input.CantidadCuotas,
            FechaPrimeraCuota = input.FechaPrimeraCuota
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
            FechaFin = input.FechaFin,
            MontoTotal = input.MontoTotal,
            CantidadCuotas = input.CantidadCuotas,
            FechaPrimeraCuota = input.FechaPrimeraCuota
        };
    }
}
