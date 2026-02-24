using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Service for managing user display and unit preferences.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets user preferences. Creates default preferences if none exist.
    /// </summary>
    Task<UserPreferencesDto> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates user preferences.
    /// </summary>
    Task<UserPreferencesDto> UpdateAsync(int userId, UserPreferencesDto preferences, CancellationToken cancellationToken = default);
}
