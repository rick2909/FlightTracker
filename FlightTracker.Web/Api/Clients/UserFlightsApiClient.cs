using FlightTracker.Application.Dtos;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;

namespace FlightTracker.Web.Api.Clients;

public sealed class UserFlightsApiClient(HttpClient httpClient)
    : IUserFlightsApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<UserFlightDto>> GetUserFlightsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.ReadRequiredAsync<List<UserFlightDto>>(
            $"/api/v1/users/{userId}/flights",
            cancellationToken);

        return response ?? new List<UserFlightDto>();
    }

    public Task<UserFlightDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.ReadRequiredAsync<UserFlightDto>(
            $"/api/v1/user-flights/{id}",
            cancellationToken);
    }

    public Task<UserFlightDto?> AddUserFlightAsync(
        int userId,
        CreateUserFlightDto request,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.SendRequiredAsync<UserFlightDto>(
            HttpMethod.Post,
            $"/api/v1/users/{userId}/flights",
            request,
            cancellationToken);
    }

    public Task<UserFlightDto?> UpdateUserFlightAndScheduleAsync(
        int id,
        UpdateUserFlightDto userFlight,
        FlightScheduleUpdateDto schedule,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.SendRequiredAsync<UserFlightDto>(
            HttpMethod.Put,
            $"/api/v1/user-flights/{id}",
            new UpdateUserFlightAndScheduleRequest(userFlight, schedule),
            cancellationToken);
    }

    public async Task<bool> DeleteUserFlightAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendRequiredAsync<DeleteUserFlightResponse>(
            HttpMethod.Delete,
            $"/api/v1/user-flights/{id}",
            cancellationToken: cancellationToken);

        return response?.Deleted ?? false;
    }

    public Task<UserFlightStatsDto?> GetUserFlightStatsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.ReadRequiredAsync<UserFlightStatsDto>(
            $"/api/v1/users/{userId}/flights/stats",
            cancellationToken);
    }

    public async Task<bool> HasUserFlownFlightAsync(
        int userId,
        int flightId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.ReadRequiredAsync<HasUserFlownResponse>(
            $"/api/v1/users/{userId}/flights/{flightId}/has-flown",
            cancellationToken);

        return response?.HasFlown ?? false;
    }
}
