using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service for managing user display and unit preferences.
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly IUserPreferencesRepository _repository;
    private readonly IMapper _mapper;

    public UserPreferencesService(
        IUserPreferencesRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<UserPreferencesDto> GetOrCreateAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByUserIdAsync(userId, cancellationToken);
        
        if (existing != null)
        {
            return _mapper.Map<UserPreferencesDto>(existing);
        }

        // Create default preferences
        var newPreferences = new UserPreferences
        {
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(newPreferences, cancellationToken);
        return _mapper.Map<UserPreferencesDto>(created);
    }

    public async Task<UserPreferencesDto> UpdateAsync(
        int userId,
        UserPreferencesDto preferences,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByUserIdAsync(userId, cancellationToken);
        
        if (existing == null)
        {
            // Create if doesn't exist
            var newPreferences = _mapper.Map<UserPreferences>(preferences);
            newPreferences.UserId = userId;
            newPreferences.CreatedAtUtc = DateTime.UtcNow;
            newPreferences.UpdatedAtUtc = DateTime.UtcNow;
            
            var created = await _repository.CreateAsync(newPreferences, cancellationToken);
            return _mapper.Map<UserPreferencesDto>(created);
        }

        // Update existing
        existing.DistanceUnit = preferences.DistanceUnit;
        existing.TemperatureUnit = preferences.TemperatureUnit;
        existing.TimeFormat = preferences.TimeFormat;
        existing.DateFormat = preferences.DateFormat;
        existing.ProfileVisibility = preferences.ProfileVisibility;
        existing.ShowTotalMiles = preferences.ShowTotalMiles;
        existing.ShowAirlines = preferences.ShowAirlines;
        existing.ShowCountries = preferences.ShowCountries;
        existing.ShowMapRoutes = preferences.ShowMapRoutes;
        existing.EnableActivityFeed = preferences.EnableActivityFeed;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing, cancellationToken);
        return _mapper.Map<UserPreferencesDto>(updated);
    }
}
