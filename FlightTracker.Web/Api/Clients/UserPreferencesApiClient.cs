using FlightTracker.Application.Dtos;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;

namespace FlightTracker.Web.Api.Clients;

public sealed class UserPreferencesApiClient(HttpClient httpClient)
    : IUserPreferencesApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<UserPreferencesDto?> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.ReadRequiredAsync<UserPreferencesResponse>(
            $"/api/v1/preferences/users/{userId}",
            cancellationToken);

        return response is null ? null : Map(response);
    }

    public async Task<UserPreferencesDto?> UpdateAsync(
        int userId,
        UserPreferencesDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendRequiredAsync<UserPreferencesResponse>(
            HttpMethod.Put,
            $"/api/v1/preferences/users/{userId}",
            new UpdateUserPreferencesRequest(
                request.DistanceUnit,
                request.TemperatureUnit,
                request.TimeFormat,
                request.DateFormat,
                request.ProfileVisibility,
                request.ShowTotalMiles,
                request.ShowAirlines,
                request.ShowCountries,
                request.ShowMapRoutes,
                request.EnableActivityFeed),
            cancellationToken);

        return response is null ? null : Map(response);
    }

    private static UserPreferencesDto Map(UserPreferencesResponse response)
    {
        return new UserPreferencesDto
        {
            Id = response.Id,
            UserId = response.UserId,
            DistanceUnit = response.DistanceUnit,
            TemperatureUnit = response.TemperatureUnit,
            TimeFormat = response.TimeFormat,
            DateFormat = response.DateFormat,
            ProfileVisibility = response.ProfileVisibility,
            ShowTotalMiles = response.ShowTotalMiles,
            ShowAirlines = response.ShowAirlines,
            ShowCountries = response.ShowCountries,
            ShowMapRoutes = response.ShowMapRoutes,
            EnableActivityFeed = response.EnableActivityFeed,
            CreatedAtUtc = response.CreatedAtUtc,
            UpdatedAtUtc = response.UpdatedAtUtc
        };
    }
}
