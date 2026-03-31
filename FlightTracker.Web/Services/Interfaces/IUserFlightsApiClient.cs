using FlightTracker.Application.Dtos;
using FlightTracker.Web.Api.Contracts;

namespace FlightTracker.Web.Services.Interfaces;

public interface IUserFlightsApiClient
{
    Task<IReadOnlyList<UserFlightDto>> GetUserFlightsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<UserFlightDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<UserFlightDto?> AddUserFlightAsync(
        int userId,
        CreateUserFlightDto request,
        CancellationToken cancellationToken = default);

    Task<UserFlightDto?> UpdateUserFlightAndScheduleAsync(
        int id,
        UpdateUserFlightDto userFlight,
        FlightScheduleUpdateDto schedule,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteUserFlightAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<UserFlightStatsDto?> GetUserFlightStatsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> HasUserFlownFlightAsync(
        int userId,
        int flightId,
        CancellationToken cancellationToken = default);

    Task<UserFlightLookupRefreshResponse?> LookupRefreshAsync(
        int id,
        CancellationToken cancellationToken = default);
}
