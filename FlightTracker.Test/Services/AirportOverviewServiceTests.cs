using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using Moq;
using Xunit;

namespace FlightTracker.Test.Services;

public class AirportOverviewServiceTests
{
    [Fact]
    public async Task GetFlightsAsync_ReturnsDepartingOnly_WhenDirDepartingAndLiveFalse()
    {
        var now = new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc);
        var departing = new List<Flight>
        {
            new() { Id = 1, FlightNumber = "FT1", DepartureTimeUtc = now.AddHours(-1) },
            new() { Id = 2, FlightNumber = "FT2", DepartureTimeUtc = now.AddHours(2) },
            new() { Id = 3, FlightNumber = "FT3", DepartureTimeUtc = now.AddHours(1) }
        };
        var arriving = new List<Flight>
        {
            new() { Id = 4, FlightNumber = "FT4", DepartureTimeUtc = now.AddHours(3) }
        };

        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.SearchByRouteAsync("JFK", null, It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(departing);
        repo.Setup(r => r.SearchByRouteAsync(null, "JFK", It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(arriving);

        var live = new Mock<IAirportLiveService>();

        var service = new AirportOverviewService(repo.Object, live.Object);

        var result = await service.GetFlightsAsync("JFK", "departing", live: false, limit: 2);

        Assert.Empty(result.Arriving);
        var items = result.Departing.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("FT2", items[0].FlightNumber);
        Assert.Equal("FT3", items[1].FlightNumber);
    }

    [Fact]
    public async Task GetFlightsAsync_MergesLiveAndDb_DedupesByRoute()
    {
        var now = new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc);
        var dbDeparting = new List<Flight>
        {
            new()
            {
                Id = 10,
                FlightNumber = "FT9",
                DepartureTimeUtc = now,
                ArrivalTimeUtc = now.AddHours(2),
                DepartureAirport = new Airport { IataCode = "JFK" },
                ArrivalAirport = new Airport { IataCode = "LAX" }
            }
        };

        var liveDeparting = new List<LiveFlightDto>
        {
            new()
            {
                FlightNumber = "FT9",
                DepartureIata = "JFK",
                ArrivalIata = "LAX",
                DepartureScheduledUtc = now.AddMinutes(5),
                ArrivalScheduledUtc = now.AddHours(2)
            }
        };

        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.SearchByRouteAsync("JFK", null, It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbDeparting);
        repo.Setup(r => r.SearchByRouteAsync(null, "JFK", It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Flight>());

        var live = new Mock<IAirportLiveService>();
        live.Setup(l => l.GetDeparturesAsync("JFK", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(liveDeparting);
        live.Setup(l => l.GetArrivalsAsync("JFK", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LiveFlightDto>());

        var service = new AirportOverviewService(repo.Object, live.Object);

        var result = await service.GetFlightsAsync("JFK", "departing", live: true, limit: 10);

        var items = result.Departing.ToList();
        Assert.Single(items);
        Assert.Equal("FT9", items[0].FlightNumber);
        Assert.Equal("JFK", items[0].DepartureCode);
        Assert.Equal("LAX", items[0].ArrivalCode);
        Assert.Equal(10, items[0].Id);
    }
}
