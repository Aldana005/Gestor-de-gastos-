namespace Gastito.Entities.Utils;

public static class CuotaCalculator
{
    public static decimal CalcularValorCuotaBase(decimal montoTotal, int cantidadCuotas)
    {
        if (cantidadCuotas <= 0) return montoTotal;
        return Math.Round(montoTotal / cantidadCuotas, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalcularValorUltimaCuota(decimal montoTotal, int cantidadCuotas)
    {
        if (cantidadCuotas <= 1) return montoTotal;
        var cuotaBase = CalcularValorCuotaBase(montoTotal, cantidadCuotas);
        return montoTotal - (cuotaBase * (cantidadCuotas - 1));
    }

    public static decimal CalcularValorCuota(decimal montoTotal, int cantidadCuotas, int numeroCuota)
    {
        if (cantidadCuotas <= 1) return montoTotal;
        if (numeroCuota == cantidadCuotas)
        {
            return CalcularValorUltimaCuota(montoTotal, cantidadCuotas);
        }
        return CalcularValorCuotaBase(montoTotal, cantidadCuotas);
    }

    public static decimal CalcularMontoPendiente(decimal montoTotal, int cantidadCuotas, int cuotasPagadas)
    {
        if (cuotasPagadas >= cantidadCuotas) return 0m;
        var cuotaBase = CalcularValorCuotaBase(montoTotal, cantidadCuotas);
        return montoTotal - (cuotaBase * cuotasPagadas);
    }

    public static DateTime CalcularFechaCuota(DateTime fechaPrimeraCuota, int numeroCuota)
    {
        if (numeroCuota <= 1)
        {
            return DateTime.SpecifyKind(fechaPrimeraCuota.Date, DateTimeKind.Utc);
        }

        var offsetMeses = numeroCuota - 1;
        var targetMonth = fechaPrimeraCuota.AddMonths(offsetMeses);
        var originalDay = fechaPrimeraCuota.Day;
        var maxDaysInTargetMonth = DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month);
        var day = Math.Min(originalDay, maxDaysInTargetMonth);

        return new DateTime(targetMonth.Year, targetMonth.Month, day, 0, 0, 0, DateTimeKind.Utc);
    }
}
