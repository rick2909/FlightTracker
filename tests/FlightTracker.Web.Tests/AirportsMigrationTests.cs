using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Entities;
using FlightTracker.Web.Configuration;
using FlightTracker.Web.Services.Implementation;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FlightTracker.Web.Tests;

public class AirportsMigrationTests
{
    [Fact]
    public async Task GetAllAirportsAsync_ReturnsFailure_WhenAirportsSliceDisabled()
    {
        var apiClient = new Mock<IAirportsApiClient>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AirportsSliceGateway>>();

        var gateway = CreateGateway(
            logger,
            apiClient,
            new FlightTrackerApiOptions
            {
                BaseUrl = "https://localhost:7001",
                Slices = new ApiSliceRolloutOptions
                {
                    Airports = false
                }
            });

        var result = await gateway.GetAllAirportsAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        apiClient.Verify(
            client => client.GetAirportsAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFlightsAsync_UsesApiClient_WhenAirportsSliceEnabled()
    {
        var apiClient = new Mock<IAirportsApiClient>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AirportsSliceGateway>>();

        IReadOnlyList<Airport> airports =
        [
            new Airport
            {
                Id = 7,
                Name = "John F Kennedy International",
                City = "New York",
                Country = "United States",
                IataCode = "JFK"
            }
        ];

        apiClient
            .Setup(client => client.GetAirportsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(airports);

        apiClient
            .Setup(client => client.GetFlightsAsync(
                "JFK",
                "departing",
                true,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirportFlightsResultDto
            {
                Departing =
                [
                    new AirportFlightListItemDto
                    {
                        Id = 44,
                        FlightNumber = "FT123",
                        DepartureCode = "JFK",
                        ArrivalCode = "LHR"
                    }
                ]
            });

        var gateway = CreateGateway(
            logger,
            apiClient,
            new FlightTrackerApiOptions
            {
                BaseUrl = "https://localhost:7001",
                Slices = new ApiSliceRolloutOptions
                {
                    Airports = true
                }
            });

        var result = await gateway.GetFlightsAsync(
            7,
            "departing",
            true,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Departing);
        Assert.Equal("FT123", result.Value!.Departing.Single().FlightNumber);
    }

    private static AirportsSliceGateway CreateGateway(
        Mock<ILogger<AirportsSliceGateway>> logger,
        Mock<IAirportsApiClient> apiClient,
        FlightTrackerApiOptions options)
    {
        return new AirportsSliceGateway(
            logger.Object,
            apiClient.Object,
            Options.Create(options));
    }
}
