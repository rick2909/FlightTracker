using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

/// <summary>
/// Repository for user preferences.
/// </summary>
public class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly FlightTrackerDbContext _context;

    public UserPreferencesRepository(FlightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferences?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<UserPreferences> CreateAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        _context.UserPreferences.Add(preferences);
        await _context.SaveChangesAsync(cancellationToken);
        return preferences;
    }

    public async Task<UserPreferences> UpdateAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        _context.UserPreferences.Update(preferences);
        await _context.SaveChangesAsync(cancellationToken);
        return preferences;
    }
}