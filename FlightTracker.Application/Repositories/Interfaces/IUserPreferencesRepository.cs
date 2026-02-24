using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Repositories.Interfaces;

/// <summary>
/// Repository for user preferences.
/// </summary>
public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    
    Task<UserPreferences> CreateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    
    Task<UserPreferences> UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
}
