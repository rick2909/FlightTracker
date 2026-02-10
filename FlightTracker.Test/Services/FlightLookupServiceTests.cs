using System;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using Moq;
using Xunit;

namespace FlightTracker.Test.Services;

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

        Assert.Same(flight, result);
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

        Assert.Same(flight, result);
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
        Assert.Same(providerFlight, result);
        provider.Verify(p => p.GetFlightByNumberAsync(flightNumber, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveFlightAsync_ReturnsNull_WhenFlightNumberEmpty()
    {
        var repo = new Mock<IFlightRepository>();
        var provider = new Mock<IFlightDataProvider>();
        var service = new FlightLookupService(repo.Object, provider.Object);

        var result = await service.ResolveFlightAsync(" ", new DateOnly(2025, 01, 01));

        Assert.Null(result);
        repo.Verify(r => r.GetByFlightNumberAndDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        provider.Verify(p => p.GetFlightByNumberAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
