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
    public async Task GetFlightsAsync_ReturnsEmpty_WhenCodeMissing()
    {
        var repo = new Mock<IFlightRepository>();
        var live = new Mock<IAirportLiveService>();
        var service = new AirportOverviewService(repo.Object, live.Object);

        var result = await service.GetFlightsAsync(" ", null, live: false, limit: 10);

        Assert.Empty(result.Departing);
        Assert.Empty(result.Arriving);
        repo.Verify(r => r.SearchByRouteAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFlightsAsync_DefaultsLimit_WhenNonPositive()
    {
        var now = new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc);
        var departing = Enumerable.Range(1, 120)
            .Select(i => new Flight { Id = i, FlightNumber = $"FT{i}", DepartureTimeUtc = now.AddMinutes(i) })
            .ToList();
        var arriving = Enumerable.Range(1, 120)
            .Select(i => new Flight { Id = i + 200, FlightNumber = $"AR{i}", DepartureTimeUtc = now.AddMinutes(i) })
            .ToList();

        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.SearchByRouteAsync("JFK", null, It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(departing);
        repo.Setup(r => r.SearchByRouteAsync(null, "JFK", It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(arriving);

        var live = new Mock<IAirportLiveService>();
        var service = new AirportOverviewService(repo.Object, live.Object);

        var result = await service.GetFlightsAsync("JFK", null, live: false, limit: 0);

        Assert.Equal(100, result.Departing.Count());
        Assert.Equal(100, result.Arriving.Count());
    }

    [Fact]
    public async Task GetFlightsAsync_ReturnsArrivingOnly_WhenDirArrivingAndLiveTrue()
    {
        var now = new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc);
        var liveArriving = new List<LiveFlightDto>
        {
            new()
            {
                FlightNumber = "FT77",
                DepartureIata = "LAX",
                ArrivalIata = "JFK",
                ArrivalScheduledUtc = now.AddHours(5)
            }
        };

        var repo = new Mock<IFlightRepository>();
        repo.Setup(r => r.SearchByRouteAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Flight>());

        var live = new Mock<IAirportLiveService>();
        live.Setup(l => l.GetDeparturesAsync("JFK", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LiveFlightDto>());
        live.Setup(l => l.GetArrivalsAsync("JFK", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(liveArriving);

        var service = new AirportOverviewService(repo.Object, live.Object);

        var result = await service.GetFlightsAsync("JFK", "arriving", live: true, limit: 10);

        Assert.Empty(result.Departing);
        Assert.Single(result.Arriving);
        Assert.Equal("FT77", result.Arriving.First().FlightNumber);
    }

    [Fact]
    public async Task GetFlightsAsync_DedupesIgnoringCaseAndWhitespace()
    {
        var now = new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc);
        var dbDeparting = new List<Flight>
        {
            new()
            {
                Id = 20,
                FlightNumber = "ft10",
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
                FlightNumber = " FT10 ",
                DepartureIata = " jfk ",
                ArrivalIata = " lax ",
                DepartureScheduledUtc = now.AddMinutes(10),
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
        Assert.Equal(20, items[0].Id);
    }

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
