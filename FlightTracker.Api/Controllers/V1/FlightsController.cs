using AutoMapper;
using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers.V1;

/// <summary>Provides read-only access to flight schedule data.</summary>
[ApiController]
[Route("api/v1/flights")]
public class FlightsController(
    IFlightService flightService,
    IFlightLookupService flightLookupService,
    IMapper mapper) : ControllerBase
{
    private readonly IFlightService _flightService = flightService;
    private readonly IFlightLookupService _flightLookupService = flightLookupService;
    private readonly IMapper _mapper = mapper;

    /// <summary>Returns a single flight by its internal identifier.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FlightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FlightResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _flightService.GetFlightByIdAsync(
            id,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        var dto = _mapper.Map<FlightDto>(result.Value);
        return Ok(MapFlight(dto));
    }

    /// <summary>Returns scheduled flights within an optional time window starting from <paramref name="query"/>.</summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(IEnumerable<FlightResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<FlightResponse>>> GetUpcomingAsync(
        [FromQuery] FlightsQuery query,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = query.FromUtc ?? DateTime.UtcNow;
        TimeSpan? window = query.WindowHours.HasValue
            ? TimeSpan.FromHours(query.WindowHours.Value)
            : null;

        var result = await _flightService.GetUpcomingFlightsAsync(
            fromUtc,
            window,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        var flightDtos = _mapper.Map<IReadOnlyList<FlightDto>>(result.Value);
        var response = flightDtos.Select(MapFlight).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Returns quick-add candidates for a flight number or callsign.
    /// Date only filters flights already present in the local database.
    /// </summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IEnumerable<FlightLookupCandidateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<FlightLookupCandidateResponse>>> LookupByDesignatorAsync(
        [FromQuery] FlightLookupQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Designator))
        {
            return BadRequest("designator is required.");
        }

        var result = await _flightLookupService.SearchCandidatesByDesignatorAsync(
            query.Designator,
            query.Date,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        var response = (result.Value ?? Array.Empty<FlightLookupCandidateDto>())
            .Select(candidate => new FlightLookupCandidateResponse(
                candidate.FlightId,
                candidate.FlightNumber,
                candidate.Callsign,
                candidate.DepartureTimeUtc,
                candidate.ArrivalTimeUtc,
                candidate.DepartureCode,
                candidate.ArrivalCode,
                candidate.DepartureAirportName,
                candidate.ArrivalAirportName,
                candidate.AirlineIataCode,
                candidate.AirlineIcaoCode,
                candidate.AirlineName,
                candidate.IsFromDatabase))
            .ToList();

        return Ok(response);
    }

    private static FlightResponse MapFlight(FlightDto dto)
    {
        return new FlightResponse(
            dto.Id,
            dto.FlightNumber,
            dto.Status,
            dto.DepartureTimeUtc,
            dto.ArrivalTimeUtc,
            dto.DepartureAirportId,
            dto.ArrivalAirportId,
            dto.AircraftId,
            dto.OperatingAirlineId);
    }
}
