using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/stats/users/{userId:int}")]
public class StatsController(
    IFlightStatsService flightStatsService) : ControllerBase
{
    private readonly IFlightStatsService _flightStatsService = flightStatsService;

    [HttpGet("passport-details")]
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

        var result = await _flightStatsService.GetPassportDetailsAsync(
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
