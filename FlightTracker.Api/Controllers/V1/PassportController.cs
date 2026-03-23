using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/passport/users/{userId:int}")]
public class PassportController(
    IPassportService passportService) : ControllerBase
{
    private readonly IPassportService _passportService = passportService;

    [HttpGet]
    [ProducesResponseType(typeof(PassportDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PassportDataResponse>> GetPassportDataAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId);
        if (access is not null)
        {
            return access;
        }

        var result = await _passportService.GetPassportDataAsync(
            userId,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        var dto = result.Value;

        return Ok(new PassportDataResponse(
            dto.TotalFlights,
            dto.TotalMiles,
            dto.LongestFlightMiles,
            dto.ShortestFlightMiles,
            dto.FavoriteAirline,
            dto.FavoriteAirport,
            dto.MostFlownAircraftType,
            dto.FavoriteClass,
            dto.AirlinesVisited,
            dto.AirportsVisited,
            dto.CountriesVisitedIso2,
            dto.FlightsPerYear,
            dto.FlightsByAirline,
            dto.FlightsByAircraftType,
            dto.Routes));
    }

    [HttpGet("details")]
    [ProducesResponseType(typeof(PassportDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PassportDetailsResponse>> GetPassportDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId);
        if (access is not null)
        {
            return access;
        }

        var result = await _passportService.GetPassportDetailsAsync(
            userId,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        var details = result.Value;

        var airlineStats = details.AirlineStats
            .Select(stat => new AirlineStatsResponse(
                stat.AirlineName,
                stat.AirlineIata,
                stat.AirlineIcao,
                stat.Count))
            .ToList();

        return Ok(new PassportDetailsResponse(
            airlineStats,
            details.AircraftTypeStats));
    }
}
