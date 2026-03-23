using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Services.Interfaces;

public interface IUserPreferencesApiClient
{
    Task<UserPreferencesDto?> GetAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<UserPreferencesDto?> UpdateAsync(
        int userId,
        UserPreferencesDto request,
        CancellationToken cancellationToken = default);
}
