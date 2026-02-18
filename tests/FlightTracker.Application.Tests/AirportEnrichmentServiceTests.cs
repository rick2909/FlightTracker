using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class AirportEnrichmentServiceTests
{
    [Fact]
    public async Task EnrichAirportAsync_ReturnsExistingAirport_WhenFoundInDatabase()
    {
        // Arrange
        var icaoCode = "EHAM";
        var existingAirport = new Airport
        {
            Id = 1,
            IcaoCode = "EHAM",
            IataCode = "AMS",
            Name = "Amsterdam Airport Schiphol",
            City = "Amsterdam",
            Country = "Netherlands",
            Latitude = 52.308601,
            Longitude = 4.76389
        };

        var mockRepository = new Mock<IAirportRepository>();
        mockRepository
            .Setup(r => r.GetByCodeAsync(icaoCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAirport);

        var mockLookupClient = new Mock<IAirportLookupClient>();
        var mockMapper = new Mock<IMapper>();
        mockMapper
            .Setup(m => m.Map<AirportDto>(existingAirport))
            .Returns(new AirportDto
            {
                Id = existingAirport.Id,
                IataCode = existingAirport.IataCode,
                IcaoCode = existingAirport.IcaoCode,
                Name = existingAirport.Name,
                City = existingAirport.City,
                Country = existingAirport.Country,
                Latitude = existingAirport.Latitude,
                Longitude = existingAirport.Longitude,
                TimeZoneId = existingAirport.TimeZoneId
            });

        var service = new AirportEnrichmentService(
            mockLookupClient.Object,
            mockRepository.Object,
            mockMapper.Object);

        // Act
        var result = await service.EnrichAirportAsync(icaoCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("EHAM", result.IcaoCode);
        Assert.Equal("AMS", result.IataCode);

        // Verify external API was not called
        mockLookupClient.Verify(
            c => c.GetAirportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify no new airport was added
        mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnrichAirportAsync_CreatesNewAirport_WhenNotFoundInDatabase()
    {
        // Arrange
        var icaoCode = "KJFK";
        var enrichmentDto = new AirportEnrichmentDto(
            IataCode: "JFK",
            IcaoCode: "KJFK",
            Name: "John F Kennedy International Airport",
            Municipality: "New York",
            CountryName: "United States",
            CountryIsoCode: "US",
            Latitude: 40.6398,
            Longitude: -73.7789,
            ElevationFeet: 13,
            AirportType: "large_airport"
        );

        var mockRepository = new Mock<IAirportRepository>();
        mockRepository
            .Setup(r => r.GetByCodeAsync(icaoCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airport?)null);
        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airport a, CancellationToken _) =>
            {
                a.Id = 42;
                return a;
            });

        var mockLookupClient = new Mock<IAirportLookupClient>();
        mockLookupClient
            .Setup(c => c.GetAirportAsync(icaoCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrichmentDto);

        var mockMapper = new Mock<IMapper>();
        mockMapper
            .Setup(m => m.Map<Airport>(enrichmentDto))
            .Returns(new Airport
            {
                IcaoCode = enrichmentDto.IcaoCode,
                IataCode = enrichmentDto.IataCode,
                Name = enrichmentDto.Name ?? string.Empty,
                City = enrichmentDto.Municipality ?? string.Empty,
                Country = enrichmentDto.CountryName ?? string.Empty,
                Latitude = enrichmentDto.Latitude,
                Longitude = enrichmentDto.Longitude
            });
        mockMapper
            .Setup(m => m.Map<AirportDto>(It.IsAny<Airport>()))
            .Returns((Airport a) => new AirportDto
            {
                Id = a.Id,
                IataCode = a.IataCode,
                IcaoCode = a.IcaoCode,
                Name = a.Name,
                City = a.City,
                Country = a.Country,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                TimeZoneId = a.TimeZoneId
            });

        var service = new AirportEnrichmentService(
            mockLookupClient.Object,
            mockRepository.Object,
            mockMapper.Object);

        // Act
        var result = await service.EnrichAirportAsync(icaoCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("KJFK", result.IcaoCode);
        Assert.Equal("JFK", result.IataCode);

        // Verify external API was called
        mockLookupClient.Verify(
            c => c.GetAirportAsync(icaoCode, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify new airport was added
        mockRepository.Verify(
            r => r.AddAsync(It.Is<Airport>(a =>
                a.IcaoCode == "KJFK" &&
                a.IataCode == "JFK" &&
                a.Name == "John F Kennedy International Airport"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnrichAirportAsync_ThrowsArgumentNullException_WhenIcaoCodeIsNull()
    {
        // Arrange
        var mockRepository = new Mock<IAirportRepository>();
        var mockLookupClient = new Mock<IAirportLookupClient>();
        var mockMapper = new Mock<IMapper>();

        var service = new AirportEnrichmentService(
            mockLookupClient.Object,
            mockRepository.Object,
            mockMapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.EnrichAirportAsync(null!));
    }

    [Fact]
    public async Task EnrichAirportAsync_ThrowsArgumentNullException_WhenIcaoCodeIsEmpty()
    {
        // Arrange
        var mockRepository = new Mock<IAirportRepository>();
        var mockLookupClient = new Mock<IAirportLookupClient>();
        var mockMapper = new Mock<IMapper>();

        var service = new AirportEnrichmentService(
            mockLookupClient.Object,
            mockRepository.Object,
            mockMapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.EnrichAirportAsync("   "));
    }

    [Fact]
    public async Task EnrichAirportAsync_ReturnsNull_WhenApiReturnsNull()
    {
        // Arrange
        var icaoCode = "XXXX";

        var mockRepository = new Mock<IAirportRepository>();
        mockRepository
            .Setup(r => r.GetByCodeAsync(icaoCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airport?)null);

        var mockLookupClient = new Mock<IAirportLookupClient>();
        mockLookupClient
            .Setup(c => c.GetAirportAsync(icaoCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirportEnrichmentDto?)null);

        var mockMapper = new Mock<IMapper>();

        var service = new AirportEnrichmentService(
            mockLookupClient.Object,
            mockRepository.Object,
            mockMapper.Object);

        // Act
        var result = await service.EnrichAirportAsync(icaoCode);

        // Assert
        Assert.Null(result);

        // Verify external API was called
        mockLookupClient.Verify(
            c => c.GetAirportAsync(icaoCode, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify no new airport was added
        mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnrichAirportAsync_NormalizesIcaoCodeToUpperCase()
    {
        // Arrange
        var icaoCode = "eham";
        var normalizedCode = "EHAM";

        var mockRepository = new Mock<IAirportRepository>();
        mockRepository
            .Setup(r => r.GetByCodeAsync(normalizedCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airport?)null);

        var mockLookupClient = new Mock<IAirportLookupClient>();
        mockLookupClient
            .Setup(c => c.GetAirportAsync(normalizedCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirportEnrichmentDto?)null);

        var mockMapper = new Mock<IMapper>();

        var service = new AirportEnrichmentService(
            mockLookupClient.Object,
            mockRepository.Object,
            mockMapper.Object);

        // Act
        await service.EnrichAirportAsync(icaoCode);

        // Assert
        // Verify the code was normalized to upper case
        mockRepository.Verify(
            r => r.GetByCodeAsync(normalizedCode, It.IsAny<CancellationToken>()),
            Times.Once);
        mockLookupClient.Verify(
            c => c.GetAirportAsync(normalizedCode, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnrichAirportAsync_HandlesMissingOptionalFields()
    {
        // Arrange
        var icaoCode = "TEST";
        var enrichmentDto = new AirportEnrichmentDto(
            IataCode: null,
            IcaoCode: "TEST",
            Name: null,
            Municipality: null,
            CountryName: null,
            CountryIsoCode: null,
            Latitude: null,
            Longitude: null,
            ElevationFeet: null,
            AirportType: null
        );

        var mockRepository = new Mock<IAirportRepository>();
        mockRepository
            .Setup(r => r.GetByCodeAsync(icaoCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airport?)null);
        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Airport a, CancellationToken _) =>
            {
                a.Id = 99;
                return a;
            });

        var mockLookupClient = new Mock<IAirportLookupClient>();
        mockLookupClient
            .Setup(c => c.GetAirportAsync(icaoCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrichmentDto);

        var mockMapper = new Mock<IMapper>();
        mockMapper
            .Setup(m => m.Map<Airport>(enrichmentDto))
            .Returns(new Airport
            {
                IcaoCode = "TEST",
                Name = string.Empty,
                City = string.Empty,
                Country = string.Empty
            });
        mockMapper
            .Setup(m => m.Map<AirportDto>(It.IsAny<Airport>()))
            .Returns((Airport a) => new AirportDto
            {
                Id = a.Id,
                IataCode = a.IataCode,
                IcaoCode = a.IcaoCode,
                Name = a.Name,
                City = a.City,
                Country = a.Country,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                TimeZoneId = a.TimeZoneId
            });

        var service = new AirportEnrichmentService(
            mockLookupClient.Object,
            mockRepository.Object,
            mockMapper.Object);

        // Act
        var result = await service.EnrichAirportAsync(icaoCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Equal("TEST", result.IcaoCode);

        // Verify airport was still created despite missing optional fields
        mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnrichAirportAsync_PropagatesCancellationToken()
    {
        // Arrange
        var icaoCode = "EGLL";
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var mockRepository = new Mock<IAirportRepository>();
        mockRepository
            .Setup(r => r.GetByCodeAsync(icaoCode, token))
            .ReturnsAsync((Airport?)null);

        var mockLookupClient = new Mock<IAirportLookupClient>();
        mockLookupClient
            .Setup(c => c.GetAirportAsync(icaoCode, token))
            .ReturnsAsync((AirportEnrichmentDto?)null);

        var mockMapper = new Mock<IMapper>();

        var service = new AirportEnrichmentService(
            mockLookupClient.Object,
            mockRepository.Object,
            mockMapper.Object);

        // Act
        await service.EnrichAirportAsync(icaoCode, token);

        // Assert
        // Verify cancellation token was passed through
        mockRepository.Verify(
            r => r.GetByCodeAsync(icaoCode, token),
            Times.Once);
        mockLookupClient.Verify(
            c => c.GetAirportAsync(icaoCode, token),
            Times.Once);
    }
}
