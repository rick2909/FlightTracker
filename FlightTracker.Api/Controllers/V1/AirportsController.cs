using AutoMapper;
using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
