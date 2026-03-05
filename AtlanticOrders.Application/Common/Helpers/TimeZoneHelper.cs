namespace AtlanticOrders.Application.Common.Helpers;

public static class TimeZoneHelper
{
    private static readonly TimeZoneInfo LimaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

    public static DateTime ConvertToLima(DateTime utcDate)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDate, LimaTimeZone);
    }
}