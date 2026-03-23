using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/preferences/users/{userId:int}")]
public class PreferencesController(
    IUserPreferencesService userPreferencesService) : ControllerBase
{
    private readonly IUserPreferencesService _userPreferencesService = userPreferencesService;

    [HttpGet]
    [ProducesResponseType(typeof(UserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserPreferencesResponse>> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId);
        if (access is not null)
        {
            return access;
        }

        var result = await _userPreferencesService.GetOrCreateAsync(
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

        return Ok(MapPreferences(result.Value));
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserPreferencesResponse>> UpdateAsync(
        int userId,
        [FromBody] UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId);
        if (access is not null)
        {
            return access;
        }

        var dto = new UserPreferencesDto
        {
            UserId = userId,
            DistanceUnit = request.DistanceUnit,
            TemperatureUnit = request.TemperatureUnit,
            TimeFormat = request.TimeFormat,
            DateFormat = request.DateFormat,
            ProfileVisibility = request.ProfileVisibility,
            ShowTotalMiles = request.ShowTotalMiles,
            ShowAirlines = request.ShowAirlines,
            ShowCountries = request.ShowCountries,
            ShowMapRoutes = request.ShowMapRoutes,
            EnableActivityFeed = request.EnableActivityFeed
        };

        var result = await _userPreferencesService.UpdateAsync(
            userId,
            dto,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        return Ok(MapPreferences(result.Value));
    }

    private static UserPreferencesResponse MapPreferences(
        UserPreferencesDto dto)
    {
        return new UserPreferencesResponse(
            dto.Id,
            dto.UserId,
            dto.DistanceUnit,
            dto.TemperatureUnit,
            dto.TimeFormat,
            dto.DateFormat,
            dto.ProfileVisibility,
            dto.ShowTotalMiles,
            dto.ShowAirlines,
            dto.ShowCountries,
            dto.ShowMapRoutes,
            dto.EnableActivityFeed,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc);
    }
}
