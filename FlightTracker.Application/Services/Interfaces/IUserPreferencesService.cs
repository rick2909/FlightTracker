using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Service for managing user display and unit preferences.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets user preferences if present; does not create defaults.
    /// </summary>
    Task<Result<UserPreferencesDto?>> GetAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user preferences. Creates default preferences if none exist.
    /// </summary>
    Task<Result<UserPreferencesDto>> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates user preferences.
    /// </summary>
    Task<Result<UserPreferencesDto>> UpdateAsync(int userId, UserPreferencesDto preferences, CancellationToken cancellationToken = default);
}
