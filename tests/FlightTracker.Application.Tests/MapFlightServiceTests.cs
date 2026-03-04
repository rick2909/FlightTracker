using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class MapFlightServiceTests
{
    [Fact]
    public async Task GetUserMapFlightsAsync_ReturnsOrderedPastThenUpcoming_AndRespectsLimits()
    {
        var now = new DateTime(2026, 03, 04, 12, 00, 00, DateTimeKind.Utc);
        var repo = new Mock<IUserFlightRepository>();
        var clock = new Mock<IClock>();

        clock.SetupGet(c => c.UtcNow).Returns(now);
        repo.Setup(r => r.GetUserFlightsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFlight>
            {
                BuildUserFlight(1, now.AddHours(-1), "JFK", "AMS"),
                BuildUserFlight(2, now.AddHours(-2), "LHR", "SFO"),
                BuildUserFlight(3, now.AddHours(-3), "CDG", "DXB"),
                BuildUserFlight(4, now.AddHours(2), "FRA", "MAD"),
                BuildUserFlight(5, now.AddHours(1), "SIN", "HKG"),
                new()
                {
                    FlightId = 99,
                    DidFly = true,
                    Flight = new Flight
                    {
                        DepartureAirport = null,
                        ArrivalAirport = new Airport { IataCode = "XXX" }
                    }
                }
            });

        var sut = new MapFlightService(repo.Object, clock.Object);

        var result = await sut.GetUserMapFlightsAsync(10, maxPast: 2, maxUpcoming: 1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var flights = result.Value!.ToList();
        Assert.Equal(3, flights.Count);

        Assert.Equal(1, flights[0].FlightId);
        Assert.Equal(2, flights[1].FlightId);
        Assert.Equal(5, flights[2].FlightId);

        Assert.False(flights[0].IsUpcoming);
        Assert.False(flights[1].IsUpcoming);
        Assert.True(flights[2].IsUpcoming);
    }

    [Fact]
    public async Task GetUserMapFlightsAsync_PropagatesCancellationToken()
    {
        var token = new CancellationTokenSource().Token;
        var repo = new Mock<IUserFlightRepository>();
        var clock = new Mock<IClock>();

        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);
        repo.Setup(r => r.GetUserFlightsAsync(77, token))
            .ReturnsAsync(new List<UserFlight>());

        var sut = new MapFlightService(repo.Object, clock.Object);

        await sut.GetUserMapFlightsAsync(77, cancellationToken: token);

        repo.Verify(r => r.GetUserFlightsAsync(77, token), Times.Once);
    }

    [Fact]
    public async Task GetUserMapFlightsAsync_ReturnsFailure_WhenRepositoryThrows()
    {
        var repo = new Mock<IUserFlightRepository>();
        var clock = new Mock<IClock>();

        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);
        repo.Setup(r => r.GetUserFlightsAsync(3, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("repo failed"));

        var sut = new MapFlightService(repo.Object, clock.Object);

        var result = await sut.GetUserMapFlightsAsync(3);

        Assert.True(result.IsFailure);
        Assert.Equal("map_flights.load.failed", result.ErrorCode);
        Assert.Equal("repo failed", result.ErrorMessage);
    }

    private static UserFlight BuildUserFlight(
        int flightId,
        DateTime departure,
        string departureCode,
        string arrivalCode)
    {
        return new UserFlight
        {
            FlightId = flightId,
            DidFly = true,
            Flight = new Flight
            {
                Id = flightId,
                FlightNumber = $"FT{flightId}",
                DepartureTimeUtc = departure,
                ArrivalTimeUtc = departure.AddHours(2),
                DepartureAirport = new Airport
                {
                    IataCode = departureCode,
                    Latitude = 1,
                    Longitude = 2
                },
                ArrivalAirport = new Airport
                {
                    IataCode = arrivalCode,
                    Latitude = 3,
                    Longitude = 4
                }
            }
        };
    }
}