using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service for managing user display and unit preferences.
/// </summary>
public class UserPreferencesService(
    IUserPreferencesRepository repository,
    IMapper mapper) : IUserPreferencesService
{
    private readonly IUserPreferencesRepository _repository = repository;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<UserPreferencesDto>> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _repository.GetByUserIdAsync(userId, cancellationToken);

            if (existing != null)
            {
                return Result<UserPreferencesDto>.Success(_mapper.Map<UserPreferencesDto>(existing));
            }

            var newPreferences = new UserPreferences
            {
                UserId = userId
            };

            var created = await _repository.CreateAsync(
                newPreferences,
                cancellationToken);

            return Result<UserPreferencesDto>.Success(_mapper.Map<UserPreferencesDto>(created));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<UserPreferencesDto>.Failure(
                ex.Message,
                "user_preferences.get_or_create.failed");
        }
    }

    public async Task<Result<UserPreferencesDto>> UpdateAsync(
        int userId,
        UserPreferencesDto preferences,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _repository.GetByUserIdAsync(
                userId,
                cancellationToken);

            if (existing == null)
            {
                var newPreferences = _mapper.Map<UserPreferences>(preferences);
                newPreferences.UserId = userId;

                var created = await _repository.CreateAsync(
                    newPreferences,
                    cancellationToken);

                return Result<UserPreferencesDto>.Success(_mapper.Map<UserPreferencesDto>(created));
            }

            _mapper.Map(preferences, existing);

            var updated = await _repository.UpdateAsync(existing, cancellationToken);
            return Result<UserPreferencesDto>.Success(_mapper.Map<UserPreferencesDto>(updated));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<UserPreferencesDto>.Failure(
                ex.Message,
                "user_preferences.update.failed");
        }
    }
}
