using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers.V1;

/// <summary>
/// Manages a user's tracked flights (add, view, update, delete).
/// User-scoped routes enforce that the caller owns the resource. Requires authentication.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public class UserFlightsController(
    IUserFlightService userFlightService,
    IFlightLookupService flightLookupService,
    IFlightService flightService) : ControllerBase
{
    private readonly IUserFlightService _userFlightService = userFlightService;
    private readonly IFlightLookupService _flightLookupService = flightLookupService;
    private readonly IFlightService _flightService = flightService;

    
    /// <summary>Returns all tracked flights for the authenticated user.</summary>
    [HttpGet("users/{userId:int}/flights")]
    [ProducesResponseType(typeof(IEnumerable<UserFlightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<UserFlightDto>>> GetUserFlightsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId);
        if (access is not null)
        {
            return access;
        }

        var result = await _userFlightService.GetUserFlightsAsync(
            userId,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        return Ok(result.Value ?? Array.Empty<UserFlightDto>());
    }

    /// <summary>Returns aggregated flight statistics for the authenticated user.</summary>
    [HttpGet("users/{userId:int}/flights/stats")]
    [ProducesResponseType(typeof(UserFlightStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightStatsDto>> GetUserFlightStatsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId);
        if (access is not null)
        {
            return access;
        }

        var result = await _userFlightService.GetUserFlightStatsAsync(
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

        return Ok(result.Value);
    }

    /// <summary>Returns whether the authenticated user has a tracked flight for the given flight.</summary>
    [HttpGet("users/{userId:int}/flights/{flightId:int}/has-flown")]
    [ProducesResponseType(typeof(HasUserFlownResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HasUserFlownResponse>> HasUserFlownFlightAsync(
        int userId,
        int flightId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId);
        if (access is not null)
        {
            return access;
        }

        var result = await _userFlightService.HasUserFlownFlightAsync(
            userId,
            flightId,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        return Ok(new HasUserFlownResponse(result.Value));
    }

    /// <summary>Adds a new tracked flight for the authenticated user.</summary>
    [HttpPost("users/{userId:int}/flights")]
    [ProducesResponseType(typeof(UserFlightDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightDto>> AddUserFlightAsync(
        int userId,
        [FromBody] CreateUserFlightDto request,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId);
        if (access is not null)
        {
            return access;
        }

        var result = await _userFlightService.AddUserFlightAsync(
            userId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        return CreatedAtAction(
            nameof(GetByIdAsync)[..^"Async".Length],
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>Returns a single user-flight record by its internal identifier.</summary>
    [HttpGet("user-flights/{id:int}")]
    [ProducesResponseType(typeof(UserFlightDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var callerId = GetCallerUserId();
        if (callerId is null)
        {
            return Unauthorized();
        }

        var result = await _userFlightService.GetByIdAsync(
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

        if (result.Value.UserId != callerId.Value)
        {
            return Forbid();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Looks up updated schedule/route data for a user-flight and returns potential changes.
    /// </summary>
    [HttpPost("user-flights/{id:int}/lookup-refresh")]
    [ProducesResponseType(typeof(UserFlightLookupRefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UserFlightLookupRefreshResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightLookupRefreshResponse>> LookupRefreshAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var callerId = GetCallerUserId();
        if (callerId is null)
        {
            return Unauthorized();
        }

        var dtoResult = await _userFlightService.GetByIdAsync(id, cancellationToken);
        if (dtoResult.IsFailure)
        {
            return this.ToFailure(dtoResult);
        }

        var dto = dtoResult.Value;
        if (dto is null)
        {
            return NotFound(new UserFlightLookupRefreshResponse(
                "not_found",
                "User flight not found.",
                null));
        }

        if (dto.UserId != callerId.Value)
        {
            return Forbid();
        }

        var date = DateOnly.FromDateTime(dto.DepartureTimeUtc);
        var candidateResult = await _flightLookupService.ResolveFlightAsync(
            dto.FlightNumber,
            date,
            cancellationToken);

        if (candidateResult.IsFailure)
        {
            return this.ToFailure(candidateResult);
        }

        var candidate = candidateResult.Value;
        if (candidate is null)
        {
            return NotFound(new UserFlightLookupRefreshResponse(
                "not_found",
                "No flight found via lookup.",
                null));
        }

        var currentFlightResult = await _flightService.GetFlightByIdAsync(
            dto.FlightId,
            cancellationToken);

        if (currentFlightResult.IsFailure)
        {
            return this.ToFailure(currentFlightResult);
        }

        var currentFlight = currentFlightResult.Value;
        if (currentFlight is null)
        {
            return NotFound(new UserFlightLookupRefreshResponse(
                "not_found",
                "Current flight not found.",
                null));
        }

        if (currentFlight.HasSameScheduleAndRoute(candidate))
        {
            return Ok(new UserFlightLookupRefreshResponse(
                "no_changes",
                "No changes found.",
                null));
        }

        var depCode = candidate.DepartureAirport?.IataCode
            ?? candidate.DepartureAirport?.IcaoCode;
        var arrCode = candidate.ArrivalAirport?.IataCode
            ?? candidate.ArrivalAirport?.IcaoCode;

        var changes = new UserFlightLookupRefreshChangesResponse(
            candidate.FlightNumber,
            candidate.DepartureTimeUtc,
            candidate.ArrivalTimeUtc,
            candidate.DepartureAirportId,
            candidate.ArrivalAirportId,
            depCode,
            arrCode);

        return Ok(new UserFlightLookupRefreshResponse(
            "changes",
            "New flight data found.",
            changes));
    }

    /// <summary>Updates the user-flight and its associated flight schedule in a single request.</summary>
    [HttpPut("user-flights/{id:int}")]
    [ProducesResponseType(typeof(UserFlightDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightDto>> UpdateUserFlightAndScheduleAsync(
        int id,
        [FromBody] UpdateUserFlightAndScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var callerId = GetCallerUserId();
        if (callerId is null)
        {
            return Unauthorized();
        }

        var existing = await _userFlightService.GetByIdAsync(id, cancellationToken);
        if (existing.IsFailure)
        {
            return this.ToFailure(existing);
        }

        if (existing.Value is null)
        {
            return NotFound();
        }

        if (existing.Value.UserId != callerId.Value)
        {
            return Forbid();
        }

        var result = await _userFlightService.UpdateUserFlightAndScheduleAsync(
            id,
            request.UserFlight,
            request.Schedule,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        return Ok(result.Value);
    }
    /// <summary>Deletes a tracked user-flight record.</summary>

    [HttpDelete("user-flights/{id:int}")]
    [ProducesResponseType(typeof(DeleteUserFlightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteUserFlightResponse>> DeleteUserFlightAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var callerId = GetCallerUserId();
        if (callerId is null)
        {
            return Unauthorized();
        }

        var existing = await _userFlightService.GetByIdAsync(id, cancellationToken);
        if (existing.IsFailure)
        {
            return this.ToFailure(existing);
        }

        if (existing.Value is null)
        {
            return NotFound();
        }

        if (existing.Value.UserId != callerId.Value)
        {
            return Forbid();
        }

        var result = await _userFlightService.DeleteUserFlightAsync(
            id,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        return Ok(new DeleteUserFlightResponse(result.Value));
    }

    private int? GetCallerUserId()
    {
        var value = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User?.FindFirst("sub")?.Value;

        if (!int.TryParse(value, out var callerId))
        {
            return null;
        }

        return callerId;
    }
}
