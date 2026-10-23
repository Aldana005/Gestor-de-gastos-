using Gastito.BusinessLogic.Models;
using Gastito.BusinessLogic.Utils;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Gastito.Entities.Utils;

namespace Gastito.BusinessLogic.Services;

public static class FinancialCalculationService
{
    public static List<(DateTime StartUtc, DateTime EndUtc)> GetMonthsInRange(DateTime desdeUtc, DateTime hastaUtc)
    {
        var months = new List<(DateTime StartUtc, DateTime EndUtc)>();
        var current = new DateTime(desdeUtc.Year, desdeUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endMonth = new DateTime(hastaUtc.Year, hastaUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (current <= endMonth)
        {
            var start = current;
            var end = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month), 23, 59, 59, DateTimeKind.Utc);
            months.Add((start, end));
            current = current.AddMonths(1);
        }

        return months;
    }

    public static decimal CalculateIngresos(
        List<Transaction> allIngresos,
        DateTime desdeUtc,
        DateTime hastaUtc,
        List<int>? categoriaIds = null)
    {
        decimal total = 0m;
        var months = GetMonthsInRange(desdeUtc, hastaUtc);

        // 1. Ingresos eventuales
        var eventuales = allIngresos
            .Where(t => t.Frecuencia == Frecuencia.Eventual)
            .Where(t => t.Fecha >= desdeUtc && t.Fecha <= hastaUtc);

        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            eventuales = eventuales.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        total += eventuales.Sum(t => t.Monto);

        // 2. Ingresos fijos (recurrentes mensuales)
        var fijos = allIngresos.Where(t => t.Frecuencia == Frecuencia.Fijo);
        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            fijos = fijos.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        foreach (var fijo in fijos)
        {
            var fInicio = fijo.FechaInicio ?? fijo.Fecha;
            var fFin = fijo.FechaFin;

            foreach (var (monthStart, monthEnd) in months)
            {
                var isActive = fInicio <= monthEnd && (!fFin.HasValue || fFin.Value >= monthStart);
                if (isActive)
                {
                    var vigente = fijo.HistorialMontos
                        .Where(h => h.VigenteDesde <= monthStart)
                        .OrderByDescending(h => h.VigenteDesde)
                        .FirstOrDefault();

                    total += vigente?.Monto ?? fijo.Monto;
                }
            }
        }

        return total;
    }

    public static (decimal TotalGastos, decimal MontoPendiente, Dictionary<int, decimal> GastosPorCategoria) CalculateGastos(
        List<Transaction> allGastos,
        DateTime desdeUtc,
        DateTime hastaUtc,
        List<int>? categoriaIds = null)
    {
        decimal totalGastos = 0m;
        decimal montoPendiente = 0m;
        var gastosPorCat = new Dictionary<int, decimal>();

        void AddCategoryExpense(int categoryId, decimal monto)
        {
            if (gastosPorCat.ContainsKey(categoryId))
            {
                gastosPorCat[categoryId] += monto;
            }
            else
            {
                gastosPorCat[categoryId] = monto;
            }
        }

        var months = GetMonthsInRange(desdeUtc, hastaUtc);

        // 1. Gastos eventuales sin cuotas
        var eventualesSinCuotas = allGastos
            .Where(t => t.Frecuencia == Frecuencia.Eventual && (!t.CantidadCuotas.HasValue || t.CantidadCuotas.Value <= 1))
            .Where(t => t.Fecha >= desdeUtc && t.Fecha <= hastaUtc);

        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            eventualesSinCuotas = eventualesSinCuotas.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        foreach (var gasto in eventualesSinCuotas)
        {
            totalGastos += gasto.Monto;
            AddCategoryExpense(gasto.CategoryId, gasto.Monto);
        }

        // 2. Gastos fijos (recurrentes mensuales)
        var fijos = allGastos.Where(t => t.Frecuencia == Frecuencia.Fijo);
        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            fijos = fijos.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        foreach (var fijo in fijos)
        {
            var fInicio = fijo.FechaInicio ?? fijo.Fecha;
            var fFin = fijo.FechaFin;

            foreach (var (monthStart, monthEnd) in months)
            {
                var isActive = fInicio <= monthEnd && (!fFin.HasValue || fFin.Value >= monthStart);
                if (isActive)
                {
                    var vigente = fijo.HistorialMontos
                        .Where(h => h.VigenteDesde <= monthStart)
                        .OrderByDescending(h => h.VigenteDesde)
                        .FirstOrDefault();

                    var montoMes = vigente?.Monto ?? fijo.Monto;
                    totalGastos += montoMes;
                    AddCategoryExpense(fijo.CategoryId, montoMes);
                }
            }
        }

        // 3. Compras en cuotas
        var comprasEnCuotas = allGastos
            .Where(t => t.Frecuencia == Frecuencia.Eventual && t.CantidadCuotas.HasValue && t.CantidadCuotas.Value > 1 && t.MontoTotal.HasValue);

        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            comprasEnCuotas = comprasEnCuotas.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        foreach (var compra in comprasEnCuotas)
        {
            var primeraCuota = compra.FechaPrimeraCuota ?? compra.Fecha;
            var cantidad = compra.CantidadCuotas!.Value;
            var pagadas = compra.CuotasPagadas ?? 0;

            // A. Adelantos registrados en PagoCuota que cayeron en el período
            if (compra.PagosCuota != null)
            {
                foreach (var pago in compra.PagosCuota)
                {
                    if (pago.FechaPagoUtc >= desdeUtc && pago.FechaPagoUtc <= hastaUtc)
                    {
                        totalGastos += pago.Monto;
                        AddCategoryExpense(compra.CategoryId, pago.Monto);
                    }
                }
            }

            // B. Cuotas programadas pendientes (no adelantadas) que caen en el período
            for (int c = pagadas + 1; c <= cantidad; c++)
            {
                var fechaCuota = CuotaCalculator.CalcularFechaDeCuotaConCorrimiento(
                    primeraCuota,
                    compra.FechaPrimeraCuotaPendiente,
                    pagadas,
                    c);

                if (fechaCuota >= desdeUtc && fechaCuota <= hastaUtc)
                {
                    var valorCuota = CuotaCalculator.CalcularValorCuota(compra.MontoTotal!.Value, cantidad, c);
                    totalGastos += valorCuota;
                    montoPendiente += valorCuota;
                    AddCategoryExpense(compra.CategoryId, valorCuota);
                }
            }
        }

        return (totalGastos, montoPendiente, gastosPorCat);
    }

    public static List<GastosPorCategoriaItemResult> BuildGastosPorCategoria(
        Dictionary<int, decimal> gastosPorCat,
        decimal totalGastos,
        List<Category> allCategories)
    {
        var categoryMap = allCategories.ToDictionary(c => c.Id, c => c.Nombre);
        var result = new List<GastosPorCategoriaItemResult>();

        foreach (var (catId, monto) in gastosPorCat.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value))
        {
            var nombre = categoryMap.TryGetValue(catId, out var n) ? n : $"Categoría #{catId}";
            var porcentaje = totalGastos > 0 ? Math.Round((monto / totalGastos) * 100, 2, MidpointRounding.AwayFromZero) : 0m;

            result.Add(new GastosPorCategoriaItemResult
            {
                CategoriaId = catId,
                CategoriaNombre = nombre,
                Monto = monto,
                Porcentaje = porcentaje
            });
        }

        return result;
    }

    public static List<ProximaDeudaItemResult> CalculateProximasDeudas(
        List<Transaction> allGastos,
        DateTime desdeUtc,
        DateTime hastaUtc,
        bool requireInstallmentInPeriod,
        List<int>? categoriaIds = null)
    {
        var cuotasQuery = allGastos
            .Where(t => t.Frecuencia == Frecuencia.Eventual &&
                        t.CantidadCuotas.HasValue &&
                        t.CantidadCuotas.Value > 1 &&
                        t.MontoTotal.HasValue &&
                        (t.CuotasPagadas ?? 0) < t.CantidadCuotas.Value);

        if (categoriaIds != null && categoriaIds.Count > 0)
        {
            cuotasQuery = cuotasQuery.Where(t => categoriaIds.Contains(t.CategoryId));
        }

        var candidatos = new List<(Transaction Tx, bool TerminaEnPeriodo, decimal ValorCuotaNext, DateTime FechaNext)>();

        foreach (var compra in cuotasQuery)
        {
            var primeraCuota = compra.FechaPrimeraCuota ?? compra.Fecha;
            var cantidad = compra.CantidadCuotas!.Value;
            var pagadas = compra.CuotasPagadas ?? 0;
            var nextIndex = pagadas + 1;

            var fechaNext = CuotaCalculator.CalcularFechaDeCuotaConCorrimiento(
                primeraCuota,
                compra.FechaPrimeraCuotaPendiente,
                pagadas,
                nextIndex);

            var valorCuotaNext = CuotaCalculator.CalcularValorCuota(compra.MontoTotal!.Value, cantidad, nextIndex);

            var fechaUltimaCuota = CuotaCalculator.CalcularFechaUltimaCuota(
                primeraCuota,
                compra.FechaPrimeraCuotaPendiente,
                cantidad,
                pagadas);

            bool terminaEnPeriodo = fechaUltimaCuota >= desdeUtc && fechaUltimaCuota <= hastaUtc;

            if (requireInstallmentInPeriod)
            {
                // Para Reportes: debe tener alguna cuota en el rango
                bool tieneCuotaEnRango = false;
                for (int c = 1; c <= cantidad; c++)
                {
                    var fc = CuotaCalculator.CalcularFechaDeCuotaConCorrimiento(
                        primeraCuota,
                        compra.FechaPrimeraCuotaPendiente,
                        pagadas,
                        c);

                    if (fc >= desdeUtc && fc <= hastaUtc)
                    {
                        tieneCuotaEnRango = true;
                        break;
                    }
                }

                if (!tieneCuotaEnRango) continue;
            }

            candidatos.Add((compra, terminaEnPeriodo, valorCuotaNext, fechaNext));
        }

        // Orden de Pendientes (Sección 3.7):
        // 1. Primero las que terminan dentro del rango
        // 2. Después por cuotas pagadas de mayor a menor (3/6 antes que 1/6)
        // 3. En caso de empate, por valor de cuota de mayor a menor
        var ordenados = candidatos
            .OrderByDescending(x => x.TerminaEnPeriodo)
            .ThenByDescending(x => x.Tx.CuotasPagadas ?? 0)
            .ThenByDescending(x => x.ValorCuotaNext)
            .ThenByDescending(x => x.Tx.Id)
            .Select(x => new ProximaDeudaItemResult
            {
                Id = x.Tx.Id,
                Descripcion = x.Tx.Descripcion,
                Categoria = x.Tx.Category?.Nombre ?? "",
                CuotasPagadas = x.Tx.CuotasPagadas ?? 0,
                CantidadCuotas = x.Tx.CantidadCuotas!.Value,
                ValorCuota = x.ValorCuotaNext,
                MesProximaCuota = $"{x.FechaNext.Year}-{x.FechaNext.Month:D2}"
            })
            .ToList();

        return ordenados;
    }
}
