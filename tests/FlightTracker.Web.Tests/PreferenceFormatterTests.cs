using System;
using System.Text.RegularExpressions;
using FlightTracker.Domain.Enums;
using FlightTracker.Web.Formatting;
using Xunit;

namespace FlightTracker.Web.Tests;

public class PreferenceFormatterTests
{
    [Fact]
    public void FormatDistanceFromMiles_ConvertsToConfiguredUnit()
    {
        var kilometers = PreferenceFormatter.FormatDistanceFromMiles(
            100,
            DistanceUnit.Kilometers);
        var nautical = PreferenceFormatter.FormatDistanceFromMiles(
            100,
            DistanceUnit.NauticalMiles);
        var miles = PreferenceFormatter.FormatDistanceFromMiles(
            100,
            DistanceUnit.Miles);

        Assert.Equal("161 km", kilometers);
        Assert.Equal("87 nmi", nautical);
        Assert.Equal("100 mi", miles);
    }

    [Theory]
    [InlineData(DateFormat.DayMonthYear, "^09\\D03\\D2026$")]
    [InlineData(DateFormat.MonthDayYear, "^03\\D09\\D2026$")]
    [InlineData(DateFormat.YearMonthDay, "^2026\\D03\\D09$")]
    public void FormatDate_UsesExpectedPattern(
        DateFormat format,
        string expectedPattern)
    {
        var value = new DateTime(2026, 3, 9, 15, 10, 0, DateTimeKind.Utc);

        var result = PreferenceFormatter.FormatDate(value, format);

        Assert.Matches(new Regex(expectedPattern), result);
    }

    [Fact]
    public void FormatUtcDateTime_Uses24HourOr12HourPattern()
    {
        var value = new DateTime(2026, 3, 9, 15, 10, 0, DateTimeKind.Utc);

        var twentyFourHour = PreferenceFormatter.FormatUtcDateTime(
            value,
            DateFormat.YearMonthDay,
            TimeFormat.TwentyFourHour);

        var twelveHour = PreferenceFormatter.FormatUtcDateTime(
            value,
            DateFormat.YearMonthDay,
            TimeFormat.TwelveHour);

        Assert.Equal("2026-03-09 15:10 UTC", twentyFourHour);
        Assert.StartsWith("2026-03-09 03:10", twelveHour);
        Assert.EndsWith(" UTC", twelveHour);
    }

    [Fact]
    public void FormatLocalDateTime_FallsBackToUtc_WhenTimezoneMissing()
    {
        var value = new DateTime(2026, 3, 9, 15, 10, 0, DateTimeKind.Utc);

        var result = PreferenceFormatter.FormatLocalDateTime(
            value,
            null,
            DateFormat.YearMonthDay,
            TimeFormat.TwentyFourHour);

        Assert.Equal("2026-03-09 15:10 UTC", result);
    }

    [Fact]
    public void FormatLocalDateTime_FormatsOffset_WhenTimezoneIsValid()
    {
        var value = new DateTime(2026, 3, 9, 15, 10, 0, DateTimeKind.Utc);

        var result = PreferenceFormatter.FormatLocalDateTime(
            value,
            "UTC",
            DateFormat.YearMonthDay,
            TimeFormat.TwentyFourHour);

        Assert.Equal("2026-03-09 15:10 (UTC+00:00)", result);
    }
}