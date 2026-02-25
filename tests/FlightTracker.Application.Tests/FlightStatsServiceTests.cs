using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Domain.Entities;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class FlightStatsServiceTests
{
    [Fact]
    public async Task GetPassportDetailsAsync_GroupsAirlinesAndAircraftTypes_ForFlownFlightsOnly()
    {
        var repo = new Mock<IUserFlightRepository>();
        repo.Setup(r => r.GetUserFlightsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildFlights());

        var sut = new FlightStatsService(repo.Object);

        var result = await sut.GetPassportDetailsAsync(42);

        var airline = Assert.Single(result.AirlineStats);
        Assert.Equal("CONTOSO AIR", airline.AirlineName);
        Assert.Equal("CT", airline.AirlineIata);
        Assert.Equal("COS", airline.AirlineIcao);
        Assert.Equal(2, airline.Count);

        Assert.Equal(2, result.AircraftTypeStats.Count);
        Assert.Equal(1, result.AircraftTypeStats["A320"]);
        Assert.Equal(1, result.AircraftTypeStats["B738"]);
    }

    [Fact]
    public async Task GetPassportDetailsAsync_ReturnsEmpty_WhenNoFlownFlights()
    {
        var repo = new Mock<IUserFlightRepository>();
        repo.Setup(r => r.GetUserFlightsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFlight>
            {
                new()
                {
                    UserId = 7,
                    FlightId = 1,
                    DidFly = false,
                    Flight = new Flight()
                }
            });

        var sut = new FlightStatsService(repo.Object);

        var result = await sut.GetPassportDetailsAsync(7);

        Assert.Empty(result.AirlineStats);
        Assert.Empty(result.AircraftTypeStats);
    }

    [Fact]
    public async Task GetPassportDetailsAsync_PropagatesCancellationToken()
    {
        var expectedToken = new CancellationTokenSource().Token;
        var repo = new Mock<IUserFlightRepository>();

        repo.Setup(r => r.GetUserFlightsAsync(5, expectedToken))
            .ReturnsAsync(new List<UserFlight>());

        var sut = new FlightStatsService(repo.Object);

        await sut.GetPassportDetailsAsync(5, expectedToken);

        repo.Verify(r => r.GetUserFlightsAsync(5, expectedToken), Times.Once);
    }

    private static IEnumerable<UserFlight> BuildFlights()
    {
        var airline = new Airline
        {
            Name = " Contoso Air ",
            IataCode = " ct ",
            IcaoCode = " cos "
        };

        return new List<UserFlight>
        {
            new()
            {
                UserId = 42,
                FlightId = 1,
                DidFly = true,
                Flight = new Flight
                {
                    OperatingAirline = airline,
                    Aircraft = new Aircraft { Model = "A320" }
                }
            },
            new()
            {
                UserId = 42,
                FlightId = 2,
                DidFly = true,
                Flight = new Flight
                {
                    OperatingAirline = new Airline
                    {
                        Name = "contoso air",
                        IataCode = "CT",
                        IcaoCode = "COS"
                    },
                    Aircraft = new Aircraft { IcaoTypeCode = "B738" }
                }
            },
            new()
            {
                UserId = 42,
                FlightId = 3,
                DidFly = false,
                Flight = new Flight
                {
                    OperatingAirline = new Airline
                    {
                        Name = "Other Air",
                        IataCode = "OA",
                        IcaoCode = "OTH"
                    },
                    Aircraft = new Aircraft { Model = "A220" }
                }
            }
        };
    }
}
