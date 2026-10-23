namespace Gastito.BusinessLogic.Utils;

public static class DateTimeUtils
{
    private static Func<DateTime>? _customUtcNow;

    public static void SetClock(Func<DateTime>? customUtcNow)
    {
        _customUtcNow = customUtcNow;
    }

    public static void ResetClock()
    {
        _customUtcNow = null;
    }

    public static DateTime UtcNow => _customUtcNow != null ? _customUtcNow() : DateTime.UtcNow;

    public static TimeZoneInfo GetArgentinaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
    }

    public static DateTime GetArgentinaNow()
    {
        var tz = GetArgentinaTimeZone();
        return TimeZoneInfo.ConvertTimeFromUtc(UtcNow, tz);
    }

    public static DateTime GetArgentinaToday()
    {
        return GetArgentinaNow().Date;
    }

    public static DateTime ToCalendarUtc(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }

    public static DateTime GetStartOfMonthUtc(DateTime date)
    {
        return DateTime.SpecifyKind(new DateTime(date.Year, date.Month, 1, 0, 0, 0), DateTimeKind.Utc);
    }

    public static DateTime GetStartOfNextMonthUtc(DateTime date)
    {
        return GetStartOfMonthUtc(date).AddMonths(1);
    }

    public static DateTime GetEndOfMonthUtc(DateTime date)
    {
        var days = DateTime.DaysInMonth(date.Year, date.Month);
        return new DateTime(date.Year, date.Month, days, 23, 59, 59, DateTimeKind.Utc);
    }
}
