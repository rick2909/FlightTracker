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
