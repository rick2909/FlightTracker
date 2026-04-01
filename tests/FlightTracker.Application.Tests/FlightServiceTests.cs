using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class FlightServiceTests
{
    [Fact]
    public async Task GetUpcomingFlightsAsync_FiltersAndOrdersByDeparture()
    {
        var fromUtc = new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc);
        var flights = new List<Flight>
        {
            new() { Id = 1, DepartureTimeUtc = fromUtc.AddHours(-1) },
            new() { Id = 2, DepartureTimeUtc = fromUtc.AddHours(2) },
            new() { Id = 3, DepartureTimeUtc = fromUtc.AddHours(1) },
            new() { Id = 4, DepartureTimeUtc = fromUtc.AddHours(6) }
        };

        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(flights);

        var logger = new Mock<ILogger<FlightService>>();
        var service = new FlightService(repo.Object, logger.Object);

        var result = await service.GetUpcomingFlightsAsync(fromUtc, TimeSpan.FromHours(4));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Collection(result.Value!,
            flight => Assert.Equal(3, flight.Id),
            flight => Assert.Equal(2, flight.Id));
    }

    [Fact]
    public async Task GetFlightByIdAsync_ReturnsFlight_WhenRepositorySucceeds()
    {
        var flight = new Flight { Id = 42, FlightNumber = "FT042" };
        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flight);

        var service = CreateService(repo);

        var result = await service.GetFlightByIdAsync(42);

        Assert.True(result.IsSuccess);
        Assert.Same(flight, result.Value);
    }

    [Fact]
    public async Task GetFlightByIdAsync_ReturnsFailure_WhenRepositoryThrows()
    {
        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var service = CreateService(repo);

        var result = await service.GetFlightByIdAsync(42);

        Assert.True(result.IsFailure);
        Assert.Equal("flight.by_id.load_failed", result.ErrorCode);
        Assert.Equal("Unable to load the requested flight.", result.ErrorMessage);
    }

    [Fact]
    public async Task AddFlightAsync_ReturnsCreatedFlight_WhenRepositorySucceeds()
    {
        var flight = new Flight { FlightNumber = "FT100" };
        var created = new Flight { Id = 100, FlightNumber = "FT100" };
        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.AddAsync(flight, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var service = CreateService(repo);

        var result = await service.AddFlightAsync(flight);

        Assert.True(result.IsSuccess);
        Assert.Same(created, result.Value);
    }

    [Fact]
    public async Task UpdateFlightAsync_ReturnsFailure_WhenRepositoryThrows()
    {
        var flight = new Flight { Id = 3, FlightNumber = "FT300" };
        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.UpdateAsync(flight, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cannot update"));

        var service = CreateService(repo);

        var result = await service.UpdateFlightAsync(flight);

        Assert.True(result.IsFailure);
        Assert.Equal("flight.update.failed", result.ErrorCode);
        Assert.Equal("Unable to update the flight.", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteFlightAsync_DeletesFlight_WhenRepositorySucceeds()
    {
        var repo = new Mock<IFlightRepository>();
        var service = CreateService(repo);

        var result = await service.DeleteFlightAsync(8);

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.DeleteAsync(8, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static FlightService CreateService(Mock<IFlightRepository> repo)
    {
        var logger = new Mock<ILogger<FlightService>>();
        return new FlightService(repo.Object, logger.Object);
    }
}
