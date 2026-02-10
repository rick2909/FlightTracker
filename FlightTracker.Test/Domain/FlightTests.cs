using System;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using Xunit;

namespace FlightTracker.Test.Domain;

public class FlightTests
{
    [Fact]
    public void HasSameScheduleAndRoute_ReturnsTrue_ForEquivalentData()
    {
        var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
        var left = CreateBaseFlight(baseTime, "FT100");
        var right = CreateBaseFlight(baseTime, "ft100");

        var result = left.HasSameScheduleAndRoute(right);

        Assert.True(result);
    }

    [Fact]
    public void HasSameScheduleAndRoute_ReturnsFalse_WhenOtherIsNull()
    {
        var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
        var left = CreateBaseFlight(baseTime, "FT100");

        var result = left.HasSameScheduleAndRoute(null);

        Assert.False(result);
    }

    [Fact]
    public void HasSameScheduleAndRoute_ReturnsFalse_WhenRouteDiffers()
    {
        var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
        var left = CreateBaseFlight(baseTime, "FT100");
        var right = CreateBaseFlight(baseTime, "FT100");
        right.ArrivalAirportId = 999;

        var result = left.HasSameScheduleAndRoute(right);

        Assert.False(result);
    }

    [Fact]
    public void HasSameScheduleAndRoute_ReturnsFalse_WhenFlightNumberDiffers()
    {
        var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
        var left = CreateBaseFlight(baseTime, "FT100");
        var right = CreateBaseFlight(baseTime, "FT200");

        var result = left.HasSameScheduleAndRoute(right);

        Assert.False(result);
    }

    [Fact]
    public void HasSameScheduleAndRoute_ReturnsFalse_WhenTimesDiffer()
    {
        var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
        var left = CreateBaseFlight(baseTime, "FT100");
        var right = CreateBaseFlight(baseTime.AddMinutes(5), "FT100");

        var result = left.HasSameScheduleAndRoute(right);

        Assert.False(result);
    }

    [Fact]
    public void DefaultStatus_IsScheduled()
    {
        var flight = new Flight();

        Assert.Equal(FlightStatus.Scheduled, flight.Status);
    }

    [Fact]
    public void Collections_AreInitialized()
    {
        var flight = new Flight();

        Assert.NotNull(flight.UserFlights);
        Assert.NotNull(flight.Tags);
    }

    private static Flight CreateBaseFlight(DateTime baseTime, string flightNumber) => new()
    {
        FlightNumber = flightNumber,
        DepartureTimeUtc = baseTime,
        ArrivalTimeUtc = baseTime.AddHours(6),
        DepartureAirportId = 1,
        ArrivalAirportId = 2
    };
}
