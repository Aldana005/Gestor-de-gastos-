using Gastito.BusinessLogic.Models;
using Gastito.Presentation.Outputs.Dashboard;
using Gastito.Presentation.Outputs.Reportes;

namespace Gastito.Presentation.Mappers;

public static class DashboardMapper
{
    public static GastosPorCategoriaOutput ToOutput(this GastosPorCategoriaItemResult item)
    {
        return new GastosPorCategoriaOutput
        {
            CategoriaId = item.CategoriaId,
            CategoriaNombre = item.CategoriaNombre,
            Monto = item.Monto,
            Porcentaje = item.Porcentaje
        };
    }

    public static IngresosVsGastosOutput ToOutput(this IngresosVsGastosItemResult item)
    {
        return new IngresosVsGastosOutput
        {
            Anio = item.Anio,
            Mes = item.Mes,
            Periodo = item.Periodo,
            TotalIngresos = item.TotalIngresos,
            TotalGastos = item.TotalGastos
        };
    }

    public static ProximaDeudaOutput ToOutput(this ProximaDeudaItemResult item)
    {
        return new ProximaDeudaOutput
        {
            Id = item.Id,
            Descripcion = item.Descripcion,
            Categoria = item.Categoria,
            CuotasPagadas = item.CuotasPagadas,
            CantidadCuotas = item.CantidadCuotas,
            ValorCuota = item.ValorCuota,
            MesProximaCuota = item.MesProximaCuota
        };
    }

    public static DashboardOutput ToOutput(this DashboardResult result)
    {
        return new DashboardOutput
        {
            TotalIngresos = result.TotalIngresos,
            TotalGastos = result.TotalGastos,
            MontoPendiente = result.MontoPendiente,
            GastosPorCategoria = result.GastosPorCategoria.Select(x => x.ToOutput()).ToList(),
            IngresosVsGastos = result.IngresosVsGastos.Select(x => x.ToOutput()).ToList(),
            ProximasDeudas = result.ProximasDeudas.Select(x => x.ToOutput()).ToList()
        };
    }

    public static ReporteResumenOutput ToOutput(this ReporteResumenResult result)
    {
        return new ReporteResumenOutput
        {
            Desde = result.Desde,
            Hasta = result.Hasta,
            TotalIngresos = result.TotalIngresos,
            TotalGastos = result.TotalGastos,
            GastosPorCategoria = result.GastosPorCategoria.Select(x => x.ToOutput()).ToList(),
            ProximasDeudas = result.ProximasDeudas.Select(x => x.ToOutput()).ToList()
        };
    }
}
