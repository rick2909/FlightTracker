using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class UserFlightServiceTests
{
    [Fact]
    public async Task AddUserFlightAsync_UsesExistingFlight_WhenFlightIdProvided()
    {
        var flight = new Flight { Id = 10 };
        var createDto = new CreateUserFlightDto
        {
            FlightId = 10,
            FlightClass = FlightClass.Economy,
            SeatNumber = "12A",
            DidFly = true
        };

        var userFlightRepository = new Mock<IUserFlightRepository>();
        userFlightRepository
            .Setup(r => r.HasUserFlownFlightAsync(5, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userFlightRepository
            .Setup(r => r.AddAsync(It.IsAny<UserFlight>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFlight uf, CancellationToken _) =>
            {
                uf.Id = 99;
                return uf;
            });
        userFlightRepository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserFlight { Id = 99, UserId = 5, FlightId = 10, Flight = flight });

        var flightRepository = new Mock<IFlightRepository>();
        flightRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flight);

        var flightService = new Mock<IFlightService>();
        var mapper = CreateMapper();

        var service = CreateService(
            userFlightRepository.Object,
            flightRepository.Object,
            flightService.Object,
            mapper);

        await service.AddUserFlightAsync(5, createDto);

        flightRepository.Verify(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        flightService.Verify(s => s.AddFlightAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddUserFlightAsync_CreatesFlight_WhenNotProvided()
    {
        var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
        var createDto = new CreateUserFlightDto
        {
            FlightId = 0,
            FlightNumber = "FT123",
            DepartureAirportCode = "JFK",
            ArrivalAirportCode = "LAX",
            DepartureTimeUtc = baseTime,
            ArrivalTimeUtc = baseTime.AddHours(5),
            FlightClass = FlightClass.Business,
            SeatNumber = "2C",
            DidFly = true
        };

        var userFlightRepository = new Mock<IUserFlightRepository>();
        userFlightRepository
            .Setup(r => r.HasUserFlownFlightAsync(7, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userFlightRepository
            .Setup(r => r.AddAsync(It.IsAny<UserFlight>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFlight uf, CancellationToken _) =>
            {
                uf.Id = 100;
                return uf;
            });
        userFlightRepository
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserFlight { Id = 100, UserId = 7, FlightId = 77, Flight = new Flight { Id = 77 } });

        var airportService = new Mock<IAirportService>();
        airportService
            .Setup(s => s.GetAirportByCodeAsync("JFK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Airport>.Success(new Airport { Id = 1, IataCode = "JFK" }));
        airportService
            .Setup(s => s.GetAirportByCodeAsync("LAX", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Airport>.Success(new Airport { Id = 2, IataCode = "LAX" }));
        airportService
            .Setup(s => s.GetTimeZoneIdByAirportCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string?>.Success(null));

        var metadata = new Mock<IFlightMetadataProvisionService>();
        metadata
            .Setup(s => s.EnsureAirlineAndAirportsForCallsignAsync("FT123", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var flightService = new Mock<IFlightService>();
        flightService
            .Setup(s => s.AddFlightAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Flight>.Success(new Flight { Id = 77 }));

        var mapper = CreateMapper();

        var service = CreateService(
            userFlightRepository.Object,
            new Mock<IFlightRepository>().Object,
            flightService.Object,
            mapper,
            airportService.Object,
            metadata.Object);

        await service.AddUserFlightAsync(7, createDto);

        flightService.Verify(s => s.AddFlightAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddUserFlightAsync_UsesExternalAirline_WhenNotInLocalRepository()
    {
        var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
        var createDto = new CreateUserFlightDto
        {
            FlightId = 0,
            FlightNumber = "MSI123",
            OperatingAirlineCode = "MSI",
            DepartureAirportCode = "JFK",
            ArrivalAirportCode = "LAX",
            DepartureTimeUtc = baseTime,
            ArrivalTimeUtc = baseTime.AddHours(5),
            FlightClass = FlightClass.Business,
            SeatNumber = "2C",
            DidFly = true
        };

        var userFlightRepository = new Mock<IUserFlightRepository>();
        userFlightRepository
            .Setup(r => r.HasUserFlownFlightAsync(7, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userFlightRepository
            .Setup(r => r.AddAsync(It.IsAny<UserFlight>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFlight uf, CancellationToken _) =>
            {
                uf.Id = 100;
                return uf;
            });
        userFlightRepository
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserFlight { Id = 100, UserId = 7, FlightId = 77, Flight = new Flight { Id = 77 } });

        var airportService = new Mock<IAirportService>();
        airportService
            .Setup(s => s.GetAirportByCodeAsync("JFK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Airport>.Success(new Airport { Id = 1, IataCode = "JFK" }));
        airportService
            .Setup(s => s.GetAirportByCodeAsync("LAX", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Airport>.Success(new Airport { Id = 2, IataCode = "LAX" }));
        airportService
            .Setup(s => s.GetTimeZoneIdByAirportCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string?>.Success(null));

        var metadata = new Mock<IFlightMetadataProvisionService>();
        metadata
            .Setup(s => s.EnsureAirlineAndAirportsForCallsignAsync("MSI123", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var airlineRepository = new Mock<IAirlineRepository>();
        airlineRepository
            .Setup(r => r.GetByIataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airline?)null);
        airlineRepository
            .Setup(r => r.GetByIcaoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airline?)null);
        airlineRepository
            .Setup(r => r.AddAsync(It.IsAny<Airline>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airline a, CancellationToken _) =>
            {
                a.Id = 55;
                return a;
            });

        var airlineLookupClient = new Mock<IAirlineLookupClient>();
        airlineLookupClient
            .Setup(c => c.GetAirlineByCodeAsync("MSI", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirlineLookupResult(
                Name: "Motor Sich",
                Icao: "MSI",
                Iata: "M9",
                Country: "Ukraine",
                CountryIso: "UA",
                Callsign: "MOTOR SICH"));

        Flight? createdFlight = null;
        var flightService = new Mock<IFlightService>();
        flightService
            .Setup(s => s.AddFlightAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Flight flight, CancellationToken _) =>
            {
                createdFlight = flight;
                return Result<Flight>.Success(new Flight { Id = 77 });
            });

        var mapper = CreateMapper();

        var service = CreateService(
            userFlightRepository.Object,
            new Mock<IFlightRepository>().Object,
            flightService.Object,
            mapper,
            airportService.Object,
            metadata.Object,
            null,
            airlineRepository.Object,
            airlineLookupClient.Object);

        await service.AddUserFlightAsync(7, createDto);

        Assert.NotNull(createdFlight);
        Assert.Equal(55, createdFlight!.OperatingAirlineId);
        airlineRepository.Verify(r => r.AddAsync(It.IsAny<Airline>(), It.IsAny<CancellationToken>()), Times.Once);
        airlineLookupClient.Verify(c => c.GetAirlineByCodeAsync("MSI", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddUserFlightAsync_ReturnsFailure_WhenFlightAlreadyRecorded()
    {
        var createDto = new CreateUserFlightDto
        {
            FlightId = 10,
            FlightClass = FlightClass.Economy,
            SeatNumber = "12A",
            DidFly = true
        };

        var userFlightRepository = new Mock<IUserFlightRepository>();
        userFlightRepository
            .Setup(r => r.HasUserFlownFlightAsync(5, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var flightRepository = new Mock<IFlightRepository>();
        flightRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Flight { Id = 10 });

        var service = CreateService(
            userFlightRepository.Object,
            flightRepository.Object,
            new Mock<IFlightService>().Object,
            CreateMapper());

        var result = await service.AddUserFlightAsync(5, createDto);

        Assert.True(result.IsFailure);
        Assert.Equal("userflight.add.failed", result.ErrorCode);
        Assert.Contains("already recorded flight 10", result.ErrorMessage);
        userFlightRepository.Verify(
            r => r.AddAsync(It.IsAny<UserFlight>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddUserFlightAsync_ReturnsFailure_WhenProvidedFlightDoesNotExist()
    {
        var createDto = new CreateUserFlightDto
        {
            FlightId = 77,
            FlightClass = FlightClass.Economy,
            SeatNumber = "14C",
            DidFly = true
        };

        var flightRepository = new Mock<IFlightRepository>();
        flightRepository
            .Setup(r => r.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Flight?)null);

        var service = CreateService(
            new Mock<IUserFlightRepository>().Object,
            flightRepository.Object,
            new Mock<IFlightService>().Object,
            CreateMapper());

        var result = await service.AddUserFlightAsync(5, createDto);

        Assert.True(result.IsFailure);
        Assert.Equal("userflight.add.failed", result.ErrorCode);
        Assert.Contains("Flight with ID 77 not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateUserFlightAsync_ReturnsNull_WhenRecordDoesNotExist()
    {
        var repository = new Mock<IUserFlightRepository>();
        repository
            .Setup(r => r.GetByIdAsync(88, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFlight?)null);

        var service = CreateService(
            repository.Object,
            new Mock<IFlightRepository>().Object,
            new Mock<IFlightService>().Object,
            CreateMapper());

        var result = await service.UpdateUserFlightAsync(
            88,
            new CreateUserFlightDto
            {
                FlightClass = FlightClass.Business,
                SeatNumber = "2A",
                DidFly = true
            });

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        repository.Verify(
            r => r.UpdateAsync(It.IsAny<UserFlight>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserFlightAsync_UpdatesExistingRecord()
    {
        var existing = new UserFlight
        {
            Id = 12,
            UserId = 3,
            FlightId = 44,
            FlightClass = FlightClass.Economy,
            SeatNumber = "18B",
            Notes = "old",
            DidFly = true,
            Flight = new Flight { Id = 44 }
        };

        var repository = new Mock<IUserFlightRepository>();
        repository
            .Setup(r => r.GetByIdAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository
            .Setup(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var service = CreateService(
            repository.Object,
            new Mock<IFlightRepository>().Object,
            new Mock<IFlightService>().Object,
            CreateMapper());

        var result = await service.UpdateUserFlightAsync(
            12,
            new CreateUserFlightDto
            {
                FlightClass = FlightClass.First,
                SeatNumber = "1A",
                Notes = "upgraded",
                DidFly = false
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(FlightClass.First, existing.FlightClass);
        Assert.Equal("1A", existing.SeatNumber);
        Assert.Equal("upgraded", existing.Notes);
        Assert.False(existing.DidFly);
        repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserFlightAsync_ReturnsFalse_WhenRecordDoesNotExist()
    {
        var repository = new Mock<IUserFlightRepository>();
        repository
            .Setup(r => r.GetByIdAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFlight?)null);

        var service = CreateService(repository.Object);

        var result = await service.DeleteUserFlightAsync(25);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
        repository.Verify(r => r.DeleteAsync(25, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HasUserFlownFlightAsync_ReturnsRepositoryValue()
    {
        var repository = new Mock<IUserFlightRepository>();
        repository
            .Setup(r => r.HasUserFlownFlightAsync(7, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(repository.Object);

        var result = await service.HasUserFlownFlightAsync(7, 30);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task GetUserFlightStatsAsync_ComputesTotals_AndIgnoresNoShows()
    {
        var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
        var jfk = new Airport { Id = 1, Country = "United States" };
        var lax = new Airport { Id = 2, Country = "United States" };
        var lhr = new Airport { Id = 3, Country = "United Kingdom" };

        var flights = new List<UserFlight>
        {
            new()
            {
                DidFly = true,
                FlightClass = FlightClass.Economy,
                Flight = new Flight
                {
                    DepartureTimeUtc = baseTime,
                    ArrivalTimeUtc = baseTime.AddHours(6),
                    DepartureAirport = jfk,
                    ArrivalAirport = lax
                }
            },
            new()
            {
                DidFly = true,
                FlightClass = FlightClass.Business,
                Flight = new Flight
                {
                    DepartureTimeUtc = baseTime.AddHours(1),
                    ArrivalTimeUtc = baseTime.AddHours(3),
                    DepartureAirport = lax,
                    ArrivalAirport = lhr
                }
            },
            new()
            {
                DidFly = false,
                FlightClass = FlightClass.First,
                Flight = new Flight
                {
                    DepartureTimeUtc = baseTime,
                    ArrivalTimeUtc = baseTime.AddHours(2),
                    DepartureAirport = jfk,
                    ArrivalAirport = lhr
                }
            }
        };

        var repo = new Mock<IUserFlightRepository>();
        repo.Setup(r => r.GetUserFlightsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flights);

        var service = CreateService(repo.Object);

        var statsResult = await service.GetUserFlightStatsAsync(42);

        Assert.True(statsResult.IsSuccess);
        Assert.NotNull(statsResult.Value);

        var stats = statsResult.Value!;

        Assert.Equal(42, stats.UserId);
        Assert.Equal(2, stats.TotalFlights);
        Assert.Equal(1, stats.EconomyFlights);
        Assert.Equal(1, stats.BusinessFlights);
        Assert.Equal(0, stats.FirstClassFlights);
        Assert.Equal(3, stats.UniqueAirports);
        Assert.Equal(2, stats.UniqueCountries);
        Assert.Equal(480, stats.TotalTravelTimeInMinutes);
        Assert.Contains(stats.TravelTimes, t => t.FlightClass == FlightClass.Economy && t.TotalTravelTimeInMinutes == 360);
        Assert.Contains(stats.TravelTimes, t => t.FlightClass == FlightClass.Business && t.TotalTravelTimeInMinutes == 120);
    }

    [Fact]
    public async Task GetUserFlightStatsAsync_ReturnsZeros_WhenNoFlights()
    {
        var repo = new Mock<IUserFlightRepository>();
        repo.Setup(r => r.GetUserFlightsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFlight>());

        var service = CreateService(repo.Object);

        var statsResult = await service.GetUserFlightStatsAsync(7);

        Assert.True(statsResult.IsSuccess);
        Assert.NotNull(statsResult.Value);

        var stats = statsResult.Value!;

        Assert.Equal(0, stats.TotalFlights);
        Assert.Equal(0, stats.UniqueAirports);
        Assert.Equal(0, stats.UniqueCountries);
        Assert.Equal(0, stats.TotalTravelTimeInMinutes);
    }

    [Fact]
    public async Task GetUserFlightStatsAsync_HandlesMissingFlightData()
    {
        var flights = new List<UserFlight>
        {
            new() { DidFly = true, FlightClass = FlightClass.Economy, Flight = null },
            new()
            {
                DidFly = true,
                FlightClass = FlightClass.Business,
                Flight = new Flight
                {
                    DepartureAirport = null,
                    ArrivalAirport = null
                }
            }
        };

        var repo = new Mock<IUserFlightRepository>();
        repo.Setup(r => r.GetUserFlightsAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flights);

        var service = CreateService(repo.Object);

        var statsResult = await service.GetUserFlightStatsAsync(9);

        Assert.True(statsResult.IsSuccess);
        Assert.NotNull(statsResult.Value);

        var stats = statsResult.Value!;

        Assert.Equal(2, stats.TotalFlights);
        Assert.Equal(0, stats.UniqueAirports);
        Assert.Equal(0, stats.UniqueCountries);
        Assert.Equal(0, stats.TotalTravelTimeInMinutes);
    }

    private static UserFlightService CreateService(IUserFlightRepository userFlightRepository)
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var airportService = CreateDefaultAirportServiceMock();

        return new UserFlightService(
            userFlightRepository,
            new Mock<IFlightRepository>().Object,
            airportService.Object,
            new Mock<IFlightService>().Object,
            new Mock<IFlightMetadataProvisionService>().Object,
            new Mock<IAirlineRepository>().Object,
            new Mock<IAirlineLookupClient>().Object,
            new Mock<IAircraftRepository>().Object,
            new Mock<IAircraftLookupClient>().Object,
            new Mock<IAirportEnrichmentService>().Object,
            new Mock<IMapper>().Object,
            clock.Object);
    }

    private static UserFlightService CreateService(
        IUserFlightRepository userFlightRepository,
        IFlightRepository flightRepository,
        IFlightService flightService,
        IMapper mapper,
        IAirportService? airportService = null,
        IFlightMetadataProvisionService? metadataProvisionService = null,
        IClock? clock = null,
        IAirlineRepository? airlineRepository = null,
        IAirlineLookupClient? airlineLookupClient = null)
    {
        if (clock is null)
        {
            var defaultClock = new Mock<IClock>();
            defaultClock.SetupGet(c => c.UtcNow)
                .Returns(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            clock = defaultClock.Object;
        }

        var resolvedAirportService = airportService
            ?? CreateDefaultAirportServiceMock().Object;

        return new UserFlightService(
            userFlightRepository,
            flightRepository,
            resolvedAirportService,
            flightService,
            metadataProvisionService ?? new Mock<IFlightMetadataProvisionService>().Object,
            airlineRepository ?? new Mock<IAirlineRepository>().Object,
            airlineLookupClient ?? new Mock<IAirlineLookupClient>().Object,
            new Mock<IAircraftRepository>().Object,
            new Mock<IAircraftLookupClient>().Object,
            new Mock<IAirportEnrichmentService>().Object,
            mapper,
            clock);
    }

    private static Mock<IAirportService> CreateDefaultAirportServiceMock()
    {
        var airportService = new Mock<IAirportService>();
        airportService
            .Setup(s => s.GetAirportByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Airport>.Success(null));
        airportService
            .Setup(s => s.GetTimeZoneIdByAirportCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string?>.Success(null));

        return airportService;
    }

    private static IMapper CreateMapper()
    {
        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<UserFlightDto>(It.IsAny<UserFlight>(), It.IsAny<Action<IMappingOperationOptions>>()))
            .Returns<UserFlight, Action<IMappingOperationOptions>>((uf, _) => new UserFlightDto
            {
                Id = uf.Id,
                UserId = uf.UserId,
                FlightId = uf.FlightId
            });

        return mapper.Object;
    }
}
