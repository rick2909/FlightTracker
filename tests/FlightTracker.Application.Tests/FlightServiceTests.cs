using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Domain.Entities;
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

        var service = new FlightService(repo.Object);

        var result = await service.GetUpcomingFlightsAsync(fromUtc, TimeSpan.FromHours(4));

        Assert.Collection(result,
            flight => Assert.Equal(3, flight.Id),
            flight => Assert.Equal(2, flight.Id));
    }
}
