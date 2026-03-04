using FlightTracker.Domain.Enums;
using TimeZoneConverter;

namespace FlightTracker.Web.Formatting;

public static class PreferenceFormatter
{
    public static string FormatDistanceFromMiles(
        int miles,
        DistanceUnit distanceUnit)
    {
        return distanceUnit switch
        {
            DistanceUnit.Kilometers
                => $"{miles * 1.609344:N0} km",
            DistanceUnit.NauticalMiles
                => $"{miles * 0.868976:N0} nmi",
            _ => $"{miles:N0} mi"
        };
    }

    public static string FormatDate(
        DateTime dateTimeUtc,
        DateFormat dateFormat)
    {
        return dateTimeUtc.ToString(GetDatePattern(dateFormat));
    }

    public static string FormatUtcDateTime(
        DateTime dateTimeUtc,
        DateFormat dateFormat,
        TimeFormat timeFormat)
    {
        var datePattern = GetDatePattern(dateFormat);
        var timePattern = GetTimePattern(timeFormat);
        return $"{dateTimeUtc.ToString($"{datePattern} {timePattern}")} UTC";
    }

    public static string FormatLocalDateTime(
        DateTime dateTimeUtc,
        string? timeZoneId,
        DateFormat dateFormat,
        TimeFormat timeFormat)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return FormatUtcDateTime(dateTimeUtc, dateFormat, timeFormat);
        }

        try
        {
            TimeZoneInfo timeZoneInfo;
            try
            {
                timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZoneId);
            }
            catch
            {
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }

            var utc = DateTime.SpecifyKind(dateTimeUtc, DateTimeKind.Utc);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZoneInfo);
            var offset = timeZoneInfo.GetUtcOffset(local);
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var hh = Math.Abs(offset.Hours).ToString("00");
            var mm = Math.Abs(offset.Minutes).ToString("00");
            var datePattern = GetDatePattern(dateFormat);
            var timePattern = GetTimePattern(timeFormat);

            return $"{local.ToString($"{datePattern} {timePattern}")} (UTC{sign}{hh}:{mm})";
        }
        catch
        {
            return FormatUtcDateTime(dateTimeUtc, dateFormat, timeFormat);
        }
    }

    public static string GetDatePattern(DateFormat dateFormat)
    {
        return dateFormat switch
        {
            DateFormat.DayMonthYear => "dd/MM/yyyy",
            DateFormat.MonthDayYear => "MM/dd/yyyy",
            _ => "yyyy-MM-dd"
        };
    }

    private static string GetTimePattern(TimeFormat timeFormat)
    {
        return timeFormat switch
        {
            TimeFormat.TwelveHour => "hh:mm tt",
            _ => "HH:mm"
        };
    }
}