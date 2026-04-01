using System;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class FlightLookupServiceTests
{
    [Fact]
    public async Task ResolveFlightAsync_TrimsAndUppercasesFlightNumber()
    {
        const string normalized = "FT300";
        var date = new DateOnly(2025, 01, 01);
        var flight = new Flight { Id = 30, FlightNumber = normalized };

        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.GetByFlightNumberAndDateAsync(normalized, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flight);

        var provider = new Mock<IFlightDataProvider>();
        var service = new FlightLookupService(repo.Object, provider.Object);

        var result = await service.ResolveFlightAsync("  ft300 ", date);

        Assert.True(result.IsSuccess);
        Assert.Same(flight, result.Value);
        repo.Verify(r => r.GetByFlightNumberAndDateAsync(normalized, date, It.IsAny<CancellationToken>()), Times.Once);
        provider.Verify(p => p.GetFlightByNumberAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveFlightAsync_ReturnsLocal_WhenFound()
    {
        const string flightNumber = "FT100";
        var date = new DateOnly(2025, 01, 01);
        var flight = new Flight { Id = 10, FlightNumber = flightNumber };

        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.GetByFlightNumberAndDateAsync(flightNumber, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flight);

        var provider = new Mock<IFlightDataProvider>();

        var service = new FlightLookupService(repo.Object, provider.Object);

        var result = await service.ResolveFlightAsync(flightNumber, date);

        Assert.True(result.IsSuccess);
        Assert.Same(flight, result.Value);
        provider.Verify(p => p.GetFlightByNumberAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveFlightAsync_UsesProvider_WhenLocalMissing()
    {
        const string flightNumber = "FT200";
        var date = new DateOnly(2025, 01, 01);
        var providerFlight = new Flight { Id = 20, FlightNumber = flightNumber };

        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.GetByFlightNumberAndDateAsync(flightNumber, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Flight?)null);

        var provider = new Mock<IFlightDataProvider>();
        provider.Setup(p => p.GetFlightByNumberAsync(flightNumber, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerFlight);

        var service = new FlightLookupService(repo.Object, provider.Object);

        var result = await service.ResolveFlightAsync(flightNumber, date);
        Assert.True(result.IsSuccess);
        Assert.Same(providerFlight, result.Value);
        provider.Verify(p => p.GetFlightByNumberAsync(flightNumber, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveFlightAsync_ReturnsNull_WhenFlightNumberEmpty()
    {
        var repo = new Mock<IFlightRepository>();
        var provider = new Mock<IFlightDataProvider>();
        var service = new FlightLookupService(repo.Object, provider.Object);

        var result = await service.ResolveFlightAsync(" ", new DateOnly(2025, 01, 01));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        repo.Verify(r => r.GetByFlightNumberAndDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        provider.Verify(p => p.GetFlightByNumberAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchCandidatesByDesignatorAsync_ReturnsLatestDatabaseFlightPerNumber()
    {
        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.SearchByFlightNumberAsync("FT123", It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Flight
                {
                    Id = 1,
                    FlightNumber = "FT123",
                    Status = FlightStatus.Scheduled,
                    DepartureTimeUtc = new DateTime(2026, 03, 01, 08, 00, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2026, 03, 01, 11, 00, 00, DateTimeKind.Utc)
                },
                new Flight
                {
                    Id = 2,
                    FlightNumber = "FT123",
                    Status = FlightStatus.Scheduled,
                    DepartureTimeUtc = new DateTime(2026, 03, 02, 08, 00, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2026, 03, 02, 11, 00, 00, DateTimeKind.Utc)
                }
            });

        var provider = new Mock<IFlightDataProvider>();
        var routeClient = new Mock<IFlightRouteLookupClient>();
        var airportRepo = new Mock<IAirportRepository>();
        var airlineRepo = new Mock<IAirlineRepository>();

        var service = new FlightLookupService(
            repo.Object,
            provider.Object,
            routeClient.Object,
            airportRepo.Object,
            airlineRepo.Object);

        var result = await service.SearchCandidatesByDesignatorAsync("ft123");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!);
        Assert.Equal(2, result.Value![0].FlightId);
        Assert.True(result.Value![0].IsFromDatabase);
    }

    [Fact]
    public async Task SearchCandidatesByDesignatorAsync_UsesIcaoFallbackWhenIataLookupFails()
    {
        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.SearchByFlightNumberAsync("KL641", It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Flight>());

        var provider = new Mock<IFlightDataProvider>();
        var routeClient = new Mock<IFlightRouteLookupClient>();
        routeClient.Setup(r => r.GetFlightRouteAsync("KL641", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlightRouteLookupResult?)null);
        routeClient.Setup(r => r.GetFlightRouteAsync("KLM641", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightRouteLookupResult(
                "KLM641",
                new FlightRouteAirline("KLM", "KLM", "KL", "Netherlands", "NL", "KLM"),
                new FlightRouteAirport("AMS", "EHAM", "Schiphol", "Amsterdam", "Netherlands", "NL", null, null, null),
                new FlightRouteAirport("JFK", "KJFK", "John F. Kennedy International", "New York", "United States", "US", null, null, null)));

        var airportRepo = new Mock<IAirportRepository>();
        var airlineRepo = new Mock<IAirlineRepository>();
        airlineRepo.Setup(r => r.GetByIataAsync("KL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Airline { Id = 8, IataCode = "KL", IcaoCode = "KLM", Name = "KLM" });

        var service = new FlightLookupService(
            repo.Object,
            provider.Object,
            routeClient.Object,
            airportRepo.Object,
            airlineRepo.Object);

        var result = await service.SearchCandidatesByDesignatorAsync("KL641");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!);
        Assert.False(result.Value![0].IsFromDatabase);
        Assert.Equal("KLM641", result.Value![0].Callsign);

        routeClient.Verify(r => r.GetFlightRouteAsync("KL641", It.IsAny<CancellationToken>()), Times.Once);
        routeClient.Verify(r => r.GetFlightRouteAsync("KLM641", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchCandidatesByDesignatorAsync_DoesNotAddExternalDuplicateWhenDatabaseHasFlight()
    {
        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.SearchByFlightNumberAsync("FT300", It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Flight
                {
                    Id = 33,
                    FlightNumber = "FT300",
                    Status = FlightStatus.Scheduled,
                    DepartureTimeUtc = new DateTime(2026, 03, 03, 07, 00, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2026, 03, 03, 10, 00, 00, DateTimeKind.Utc)
                }
            });

        var provider = new Mock<IFlightDataProvider>();
        var routeClient = new Mock<IFlightRouteLookupClient>();
        routeClient.Setup(r => r.GetFlightRouteAsync("FT300", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightRouteLookupResult(
                "FT300",
                null,
                null,
                null));

        var airportRepo = new Mock<IAirportRepository>();
        var airlineRepo = new Mock<IAirlineRepository>();

        var service = new FlightLookupService(
            repo.Object,
            provider.Object,
            routeClient.Object,
            airportRepo.Object,
            airlineRepo.Object);

        var result = await service.SearchCandidatesByDesignatorAsync("FT300");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!);
        Assert.True(result.Value![0].IsFromDatabase);
        Assert.Equal(33, result.Value![0].FlightId);
    }
}
