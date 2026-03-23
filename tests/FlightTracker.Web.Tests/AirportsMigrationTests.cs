using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Web.Configuration;
using FlightTracker.Web.Controllers.Web;
using FlightTracker.Web.Services.Implementation;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FlightTracker.Web.Tests;

public class AirportsMigrationTests
{
    [Fact]
    public async Task GetAllAirportsAsync_UsesApplicationServices_WhenAirportsSliceDisabled()
    {
        var airportService = new Mock<IAirportService>(MockBehavior.Strict);
        var airportOverviewService = new Mock<IAirportOverviewService>(MockBehavior.Strict);
        var apiClient = new Mock<IAirportsApiClient>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AirportsSliceGateway>>();

        IReadOnlyList<Airport> airports =
        [
            new Airport
            {
                Id = 12,
                Name = "Schiphol",
                City = "Amsterdam",
                Country = "Netherlands",
                IataCode = "AMS"
            }
        ];

        airportService
            .Setup(s => s.GetAllAirportsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Airport>>.Success(airports));

        var gateway = CreateGateway(
            logger,
            airportService,
            airportOverviewService,
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

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("AMS", result.Value![0].IataCode);
        apiClient.Verify(
            client => client.GetAirportsAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFlightsAsync_UsesApiClient_WhenAirportsSliceEnabled()
    {
        var airportService = new Mock<IAirportService>(MockBehavior.Strict);
        var airportOverviewService = new Mock<IAirportOverviewService>(MockBehavior.Strict);
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
            airportService,
            airportOverviewService,
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

        airportService.Verify(
            service => service.GetAllAirportsAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        airportOverviewService.Verify(
            service => service.GetFlightsAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Flights_ReturnsNotFound_WhenGatewayCannotResolveAirport()
    {
        var logger = new Mock<ILogger<AirportsController>>();
        var gateway = new Mock<IAirportsSliceGateway>(MockBehavior.Strict);

        gateway
            .Setup(g => g.GetFlightsAsync(
                99,
                null,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AirportFlightsResultDto>.Failure(
                "Airport not found",
                "airport.not_found"));

        var controller = new AirportsController(
            logger.Object,
            gateway.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Flights(99, null, false);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    private static AirportsSliceGateway CreateGateway(
        Mock<ILogger<AirportsSliceGateway>> logger,
        Mock<IAirportService> airportService,
        Mock<IAirportOverviewService> airportOverviewService,
        Mock<IAirportsApiClient> apiClient,
        FlightTrackerApiOptions options)
    {
        return new AirportsSliceGateway(
            logger.Object,
            airportService.Object,
            airportOverviewService.Object,
            apiClient.Object,
            Options.Create(options));
    }
}
