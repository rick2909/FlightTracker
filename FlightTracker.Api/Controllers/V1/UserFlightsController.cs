using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers.V1;

[ApiController]
[Route("api/v1")]
public class UserFlightsController(
    IUserFlightService userFlightService) : ControllerBase
{
    private readonly IUserFlightService _userFlightService = userFlightService;

    [HttpGet("users/{userId:int}/flights")]
    [ProducesResponseType(typeof(IEnumerable<UserFlightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<UserFlightDto>>> GetUserFlightsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _userFlightService.GetUserFlightsAsync(
            userId,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        return Ok(result.Value ?? Array.Empty<UserFlightDto>());
    }

    [HttpGet("users/{userId:int}/flights/stats")]
    [ProducesResponseType(typeof(UserFlightStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightStatsDto>> GetUserFlightStatsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
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

    [HttpGet("users/{userId:int}/flights/{flightId:int}/has-flown")]
    [ProducesResponseType(typeof(HasUserFlownResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HasUserFlownResponse>> HasUserFlownFlightAsync(
        int userId,
        int flightId,
        CancellationToken cancellationToken = default)
    {
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

    [HttpPost("users/{userId:int}/flights")]
    [ProducesResponseType(typeof(UserFlightDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightDto>> AddUserFlightAsync(
        int userId,
        [FromBody] CreateUserFlightDto request,
        CancellationToken cancellationToken = default)
    {
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
            nameof(GetByIdAsync),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpGet("user-flights/{id:int}")]
    [ProducesResponseType(typeof(UserFlightDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
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

        return Ok(result.Value);
    }

    [HttpPut("user-flights/{id:int}")]
    [ProducesResponseType(typeof(UserFlightDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserFlightDto>> UpdateUserFlightAndScheduleAsync(
        int id,
        [FromBody] UpdateUserFlightAndScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
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

    [HttpDelete("user-flights/{id:int}")]
    [ProducesResponseType(typeof(DeleteUserFlightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteUserFlightResponse>> DeleteUserFlightAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _userFlightService.DeleteUserFlightAsync(
            id,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        return Ok(new DeleteUserFlightResponse(result.Value));
    }
}
