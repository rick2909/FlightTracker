using AutoMapper;
using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Api.Controllers.V1;

/// <summary>Provides read-only access to airport data and per-airport flight lists.</summary>
[ApiController]
[Route("api/v1/airports")]
public class AirportsController(
    IAirportService airportService,
    IAirportOverviewService airportOverviewService,
    IMapper mapper) : ControllerBase
{
    private readonly IAirportService _airportService = airportService;
    private readonly IAirportOverviewService _airportOverviewService = airportOverviewService;
    private readonly IMapper _mapper = mapper;

    /// <summary>Returns all airports known to the system.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AirportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AirportResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _airportService.GetAllAirportsAsync(cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        var airportDtos = _mapper.Map<IReadOnlyList<AirportDto>>(result.Value);
        var response = airportDtos.Select(MapAirport).ToList();

        return Ok(response);
    }

    /// <summary>Returns a single airport by its IATA or ICAO code.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(AirportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AirportResponse>> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var result = await _airportService.GetAirportByCodeAsync(
            code,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        var dto = _mapper.Map<AirportDto>(result.Value);
        return Ok(MapAirport(dto));
    }

    /// <summary>Returns departure and arrival flight lists for the given airport code.</summary>
    [HttpGet("{code}/flights")]
    [ProducesResponseType(typeof(AirportFlightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AirportFlightsResponse>> GetFlightsAsync(
        string code,
        [FromQuery] string? direction,
        [FromQuery] bool live = true,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _airportOverviewService.GetFlightsAsync(
            code,
            direction,
            live,
            limit,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        var payload = result.Value ?? new AirportFlightsResultDto();

        var departing = payload.Departing
            .Select(item => new AirportFlightListItemResponse(
                item.Id,
                item.FlightNumber,
                item.Airline,
                item.Aircraft,
                item.DepartureTimeUtc,
                item.ArrivalTimeUtc,
                item.DepartureCode,
                item.ArrivalCode))
            .ToList();

        var arriving = payload.Arriving
            .Select(item => new AirportFlightListItemResponse(
                item.Id,
                item.FlightNumber,
                item.Airline,
                item.Aircraft,
                item.DepartureTimeUtc,
                item.ArrivalTimeUtc,
                item.DepartureCode,
                item.ArrivalCode))
            .ToList();

        return Ok(new AirportFlightsResponse(departing, arriving));
    }

    /// <summary>
    /// Returns airports within optional bounds with zoom-based filtering.
    /// </summary>
    [HttpGet("browse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BrowseAsync(
        [FromQuery] double? north,
        [FromQuery] double? south,
        [FromQuery] double? east,
        [FromQuery] double? west,
        [FromQuery] int? zoom,
        CancellationToken cancellationToken = default)
    {
        int z = zoom ?? 10;
        if (z <= 3)
        {
            return Ok(Array.Empty<object>());
        }

        var result = await _airportService.GetAllAirportsAsync(cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return this.ToFailure(result);
        }

        IEnumerable<Airport> inView = result.Value
            .Where(a => a.Latitude.HasValue && a.Longitude.HasValue);

        if (north.HasValue && south.HasValue && east.HasValue && west.HasValue)
        {
            bool crossesAntimeridian = east.Value < west.Value;
            inView = inView.Where(a =>
            {
                var lat = a.Latitude!.Value;
                var lon = a.Longitude!.Value;
                var withinLat = lat <= north.Value && lat >= south.Value;
                var withinLon = crossesAntimeridian
                    ? (lon >= west.Value || lon <= east.Value)
                    : (lon >= west.Value && lon <= east.Value);
                return withinLat && withinLon;
            });
        }

        if (z is >= 4 and < 6)
        {
            inView = inView.Where(a => !string.IsNullOrWhiteSpace(a.IataCode));
        }
        else if (z is >= 6 and < 8)
        {
            inView = inView.Where(
                a => !string.IsNullOrWhiteSpace(a.IataCode)
                  || !string.IsNullOrWhiteSpace(a.City));
        }

        var response = inView.Select(a => new
        {
            id = a.Id,
            name = a.Name,
            city = a.City,
            country = a.Country,
            iata = a.IataCode,
            icao = a.IcaoCode,
            lat = a.Latitude,
            lon = a.Longitude
        });

        return Ok(response);
    }

    /// <summary>
    /// Returns departure and arrival flight lists for the given airport id.
    /// </summary>
    [HttpGet("{id:int}/flights")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFlightsByIdAsync(
        int id,
        [FromQuery] bool live = true,
        CancellationToken cancellationToken = default)
    {
        var airportsResult = await _airportService.GetAllAirportsAsync(cancellationToken);

        if (airportsResult.IsFailure || airportsResult.Value is null)
        {
            return this.ToFailure(airportsResult);
        }

        var airport = airportsResult.Value.FirstOrDefault(a => a.Id == id);
        if (airport is null)
        {
            return NotFound(new { error = "Airport not found" });
        }

        var code = airport.IataCode ?? airport.IcaoCode ?? airport.Name;

        var flightsResult = await _airportOverviewService.GetFlightsAsync(
            code,
            null,
            live,
            100,
            cancellationToken);

        if (flightsResult.IsFailure)
        {
            return this.ToFailure(flightsResult);
        }

        var payload = flightsResult.Value ?? new AirportFlightsResultDto();

        static object ShapeItem(AirportFlightListItemDto i) => new
        {
            id = i.Id,
            flightNumber = i.FlightNumber,
            airline = i.Airline,
            aircraft = i.Aircraft,
            route = $"{i.DepartureCode} -> {i.ArrivalCode}",
            departureCode = i.DepartureCode,
            arrivalCode = i.ArrivalCode,
            departureTimeUtc = i.DepartureTimeUtc,
            arrivalTimeUtc = i.ArrivalTimeUtc
        };

        return Ok(new
        {
            departing = payload.Departing.Select(ShapeItem),
            arriving = payload.Arriving.Select(ShapeItem)
        });
    }

    private static AirportResponse MapAirport(AirportDto dto)
    {
        return new AirportResponse(
            dto.Id,
            dto.Name,
            dto.City,
            dto.Country,
            dto.IataCode,
            dto.IcaoCode,
            dto.Latitude,
            dto.Longitude,
            dto.TimeZoneId);
    }
}
