using Gastito.Entities.Models;
using Gastito.Entities.Utils;
using Gastito.Presentation.Outputs.Cuotas;

namespace Gastito.Presentation.Mappers;

public static class CuotaMapper
{
    public static PagoCuotaOutput ToOutput(this PagoCuota entity)
    {
        return new PagoCuotaOutput
        {
            Id = entity.Id,
            TransactionId = entity.TransactionId,
            CantidadCuotas = entity.CantidadCuotas,
            Monto = entity.Monto,
            CuotaDesde = entity.CuotaDesde,
            CuotaHasta = entity.CuotaHasta,
            FechaPagoUtc = entity.FechaPagoUtc
        };
    }

    public static List<PagoCuotaOutput> ToOutputList(this IEnumerable<PagoCuota> entities)
    {
        return entities.Select(e => e.ToOutput()).ToList();
    }

    public static PendienteItemOutput ToPendienteOutput(this Transaction tx, DateTime desde, DateTime hasta)
    {
        var esCuota = tx.CantidadCuotas.HasValue && tx.CantidadCuotas.Value > 1 && tx.MontoTotal.HasValue;

        if (!esCuota)
        {
            return new PendienteItemOutput
            {
                Id = tx.Id,
                Descripcion = tx.Descripcion,
                CategoryId = tx.CategoryId,
                CategoryNombre = tx.Category?.Nombre ?? string.Empty,
                Fecha = tx.Fecha,
                Monto = tx.Monto,
                EsCuota = false
            };
        }

        var primeraCuota = tx.FechaPrimeraCuota ?? tx.Fecha;
        var cantidad = tx.CantidadCuotas!.Value;
        var pagadas = tx.CuotasPagadas ?? 0;
        var valorCuota = CuotaCalculator.CalcularValorCuotaBase(tx.MontoTotal!.Value, cantidad);
        var montoPendiente = CuotaCalculator.CalcularMontoPendiente(tx.MontoTotal.Value, cantidad, pagadas);
        var fechaUltima = CuotaCalculator.CalcularFechaUltimaCuota(primeraCuota, tx.FechaPrimeraCuotaPendiente, cantidad, pagadas);
        var terminaEnPeriodo = fechaUltima >= desde && fechaUltima <= hasta;

        return new PendienteItemOutput
        {
            Id = tx.Id,
            Descripcion = tx.Descripcion,
            CategoryId = tx.CategoryId,
            CategoryNombre = tx.Category?.Nombre ?? string.Empty,
            Fecha = tx.Fecha,
            Monto = valorCuota,
            EsCuota = true,
            MontoTotal = tx.MontoTotal,
            CantidadCuotas = cantidad,
            CuotasPagadas = pagadas,
            CuotasRestantes = Math.Max(0, cantidad - pagadas),
            ValorCuota = valorCuota,
            MontoPendiente = montoPendiente,
            Progreso = $"{pagadas}/{cantidad}",
            TerminaEnPeriodo = terminaEnPeriodo,
            FechaUltimaCuota = fechaUltima,
            FechaPrimeraCuotaPendiente = tx.FechaPrimeraCuotaPendiente,
            HistorialPagos = tx.PagosCuota?.Select(p => p.ToOutput()).ToList() ?? new()
        };
    }

    public static List<PendienteItemOutput> ToPendienteOutputList(this IEnumerable<Transaction> transactions, DateTime desde, DateTime hasta)
    {
        return transactions.Select(t => t.ToPendienteOutput(desde, hasta)).ToList();
    }
}
