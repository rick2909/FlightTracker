using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class AirportServiceTests
{
    [Fact]
    public async Task GetAirportByCodeAsync_ReturnsWrappedAirport_WhenFound()
    {
        var airport = new Airport { Id = 11, IataCode = "AMS" };
        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.GetByCodeAsync("AMS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(airport);

        var service = new AirportService(repo.Object, new Mock<ITimeApiService>().Object);

        var result = await service.GetAirportByCodeAsync("AMS");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(11, result.Value!.Id);
    }

    [Fact]
    public async Task GetAllAirportsAsync_ReturnsWrappedList()
    {
        var airports = new List<Airport>
        {
            new() { Id = 1, IataCode = "JFK" },
            new() { Id = 2, IataCode = "LAX" }
        };

        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(airports);

        var service = new AirportService(repo.Object, new Mock<ITimeApiService>().Object);

        var result = await service.GetAllAirportsAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task AddAirportAsync_ReturnsWrappedCreatedAirport()
    {
        var toAdd = new Airport { Name = "Schiphol", IataCode = "AMS" };
        var created = new Airport { Id = 15, Name = "Schiphol", IataCode = "AMS" };

        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.AddAsync(toAdd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var service = new AirportService(repo.Object, new Mock<ITimeApiService>().Object);

        var result = await service.AddAirportAsync(toAdd);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(15, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateAirportAsync_ReturnsSuccessWrapper()
    {
        var airport = new Airport { Id = 10, IataCode = "CDG" };
        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.UpdateAsync(airport, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AirportService(repo.Object, new Mock<ITimeApiService>().Object);

        var result = await service.UpdateAirportAsync(airport);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAirportAsync_ReturnsSuccessWrapper()
    {
        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.DeleteAsync(7, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AirportService(repo.Object, new Mock<ITimeApiService>().Object);

        var result = await service.DeleteAirportAsync(7);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetTimeZoneIdByAirportCodeAsync_ReturnsNull_WhenAirportMissing()
    {
        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.GetByCodeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airport?)null);

        var timeApi = new Mock<ITimeApiService>();
        var service = new AirportService(repo.Object, timeApi.Object);

        var result = await service.GetTimeZoneIdByAirportCodeAsync("XXX");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        timeApi.Verify(t => t.GetTimeZoneIdAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.UpdateAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTimeZoneIdByAirportCodeAsync_DoesNotCallTimeApi_WhenCoordinatesMissing()
    {
        var airport = new Airport
        {
            Id = 4,
            IataCode = "ORD",
            Latitude = null,
            Longitude = null,
            TimeZoneId = null
        };

        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.GetByCodeAsync("ORD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(airport);

        var timeApi = new Mock<ITimeApiService>();
        var service = new AirportService(repo.Object, timeApi.Object);

        var result = await service.GetTimeZoneIdByAirportCodeAsync("ORD");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        timeApi.Verify(t => t.GetTimeZoneIdAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.UpdateAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTimeZoneIdByAirportCodeAsync_ReturnsExistingTimeZone()
    {
        var airport = new Airport
        {
            Id = 1,
            IataCode = "JFK",
            Latitude = 40.6,
            Longitude = -73.7,
            TimeZoneId = "America/New_York"
        };

        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.GetByCodeAsync("JFK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(airport);

        var timeApi = new Mock<ITimeApiService>();

        var service = new AirportService(repo.Object, timeApi.Object);

        var result = await service.GetTimeZoneIdByAirportCodeAsync("JFK");

        Assert.True(result.IsSuccess);
        Assert.Equal("America/New_York", result.Value);
        timeApi.Verify(t => t.GetTimeZoneIdAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.UpdateAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTimeZoneIdByAirportCodeAsync_UpdatesWhenMissing()
    {
        var airport = new Airport
        {
            Id = 2,
            IataCode = "LAX",
            Latitude = 33.9,
            Longitude = -118.4,
            TimeZoneId = null
        };

        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.GetByCodeAsync("LAX", It.IsAny<CancellationToken>()))
            .ReturnsAsync(airport);

        var timeApi = new Mock<ITimeApiService>();
        timeApi.Setup(t => t.GetTimeZoneIdAsync(airport.Latitude.Value, airport.Longitude.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync("America/Los_Angeles");

        var service = new AirportService(repo.Object, timeApi.Object);

        var result = await service.GetTimeZoneIdByAirportCodeAsync("LAX");

        Assert.True(result.IsSuccess);
        Assert.Equal("America/Los_Angeles", result.Value);
        repo.Verify(r => r.UpdateAsync(It.Is<Airport>(a => a.TimeZoneId == "America/Los_Angeles"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTimeZoneIdByAirportCodeAsync_ReturnsNull_OnTimeApiFailure()
    {
        var airport = new Airport
        {
            Id = 3,
            IataCode = "SFO",
            Latitude = 37.6,
            Longitude = -122.3,
            TimeZoneId = null
        };

        var repo = new Mock<IAirportRepository>();
        repo.Setup(r => r.GetByCodeAsync("SFO", It.IsAny<CancellationToken>()))
            .ReturnsAsync(airport);

        var timeApi = new Mock<ITimeApiService>();
        timeApi.Setup(t => t.GetTimeZoneIdAsync(airport.Latitude.Value, airport.Longitude.Value, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Exception("boom"));

        var service = new AirportService(repo.Object, timeApi.Object);

        var result = await service.GetTimeZoneIdByAirportCodeAsync("SFO");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        repo.Verify(r => r.UpdateAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
