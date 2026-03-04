using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Services.Interfaces.Analytics;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class PassportServiceTests
{
    [Fact]
    public async Task GetPassportDataAsync_ComputesCoreAggregates_ForFlownFlightsOnly()
    {
        var userFlightRepo = new Mock<IUserFlightRepository>();
        var flightRepo = new Mock<IFlightRepository>();
        var mapFlightService = new Mock<IMapFlightService>();
        var distanceCalculator = new Mock<IDistanceCalculator>();
        var flightStatsService = new Mock<IFlightStatsService>();

        var dep1 = new Airport { IataCode = "JFK", Country = "US" };
        var arr1 = new Airport { IataCode = "AMS", Country = "NL" };
        var dep2 = new Airport { IataCode = "AMS", Country = "NL" };
        var arr2 = new Airport { IataCode = "JFK", Country = "US" };

        var userFlights = new List<UserFlight>
        {
            new()
            {
                UserId = 7,
                FlightId = 100,
                DidFly = true,
                FlightClass = FlightClass.Business,
                Flight = new Flight
                {
                    Id = 100,
                    FlightNumber = "FT100",
                    DepartureTimeUtc = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2025, 1, 10, 18, 0, 0, DateTimeKind.Utc),
                    DepartureAirport = dep1,
                    ArrivalAirport = arr1,
                    OperatingAirline = new Airline { Name = "Contoso Air" },
                    Aircraft = new Aircraft { Model = "A320" }
                }
            },
            new()
            {
                UserId = 7,
                FlightId = 101,
                DidFly = true,
                FlightClass = FlightClass.Business,
                Flight = new Flight
                {
                    Id = 101,
                    FlightNumber = "FT101",
                    DepartureTimeUtc = new DateTime(2025, 3, 15, 11, 0, 0, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2025, 3, 15, 19, 0, 0, DateTimeKind.Utc),
                    DepartureAirport = dep2,
                    ArrivalAirport = arr2,
                    OperatingAirline = new Airline { Name = "Contoso Air" },
                    Aircraft = new Aircraft { Model = "A320" }
                }
            },
            new()
            {
                UserId = 7,
                FlightId = 102,
                DidFly = false,
                FlightClass = FlightClass.First,
                Flight = new Flight
                {
                    Id = 102,
                    FlightNumber = "FT102",
                    DepartureAirport = dep1,
                    ArrivalAirport = arr1,
                    OperatingAirline = new Airline { Name = "Other Air" },
                    Aircraft = new Aircraft { Model = "B737" }
                }
            }
        };

        userFlightRepo.Setup(r => r.GetUserFlightsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userFlights);

        distanceCalculator.Setup(dc => dc.CalculateGreatCircleKm(dep1, arr1)).Returns(1000);
        distanceCalculator.Setup(dc => dc.CalculateGreatCircleKm(dep2, arr2)).Returns(500);

        mapFlightService.Setup(s => s.GetUserMapFlightsAsync(7, 1000, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<MapFlightDto>>.Success(new List<MapFlightDto>
            {
                new() { FlightId = 100, DepartureAirportCode = "JFK", ArrivalAirportCode = "AMS" }
            }));

        var sut = new PassportService(
            userFlightRepo.Object,
            flightRepo.Object,
            mapFlightService.Object,
            distanceCalculator.Object,
            flightStatsService.Object);

        var result = await sut.GetPassportDataAsync(7);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var dto = result.Value!;
        Assert.Equal(2, dto.TotalFlights);
        Assert.Equal(932, dto.TotalMiles);
        Assert.Equal(621, dto.LongestFlightMiles);
        Assert.Equal(311, dto.ShortestFlightMiles);
        Assert.Equal("Contoso Air", dto.FavoriteAirline);
        Assert.Equal("A320", dto.MostFlownAircraftType);
        Assert.Equal("Business", dto.FavoriteClass);

        Assert.Equal(new[] { "AMS", "JFK" }, dto.AirportsVisited);
        Assert.Equal(new[] { "NL", "US" }, dto.CountriesVisitedIso2);
        Assert.Equal(2, dto.FlightsPerYear[2025]);
        Assert.Equal(2, dto.FlightsByAirline["Contoso Air"]);
        Assert.Equal(2, dto.FlightsByAircraftType["A320"]);
        Assert.Single(dto.Routes);
    }

    [Fact]
    public async Task GetPassportDataAsync_ReturnsFailure_WhenRoutesLoadFails()
    {
        var userFlightRepo = new Mock<IUserFlightRepository>();
        var flightRepo = new Mock<IFlightRepository>();
        var mapFlightService = new Mock<IMapFlightService>();
        var distanceCalculator = new Mock<IDistanceCalculator>();
        var flightStatsService = new Mock<IFlightStatsService>();

        userFlightRepo.Setup(r => r.GetUserFlightsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFlight>());

        mapFlightService.Setup(s => s.GetUserMapFlightsAsync(5, 1000, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<MapFlightDto>>.Failure(
                "route provider unavailable",
                "map.routes.failed"));

        var sut = new PassportService(
            userFlightRepo.Object,
            flightRepo.Object,
            mapFlightService.Object,
            distanceCalculator.Object,
            flightStatsService.Object);

        var result = await sut.GetPassportDataAsync(5);

        Assert.True(result.IsFailure);
        Assert.Equal("map.routes.failed", result.ErrorCode);
        Assert.Equal("route provider unavailable", result.ErrorMessage);
    }

    [Fact]
    public async Task GetPassportDetailsAsync_ReturnsFailure_WhenStatsServiceFails()
    {
        var userFlightRepo = new Mock<IUserFlightRepository>();
        var flightRepo = new Mock<IFlightRepository>();
        var mapFlightService = new Mock<IMapFlightService>();
        var distanceCalculator = new Mock<IDistanceCalculator>();
        var flightStatsService = new Mock<IFlightStatsService>();

        flightStatsService.Setup(s => s.GetPassportDetailsAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PassportDetailsDto>.Failure(
                "stats unavailable",
                "stats.failed"));

        var sut = new PassportService(
            userFlightRepo.Object,
            flightRepo.Object,
            mapFlightService.Object,
            distanceCalculator.Object,
            flightStatsService.Object);

        var result = await sut.GetPassportDetailsAsync(11);

        Assert.True(result.IsFailure);
        Assert.Equal("stats.failed", result.ErrorCode);
        Assert.Equal("stats unavailable", result.ErrorMessage);
    }
}