using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Web.Configuration;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FlightTracker.Web.Services.Implementation;

public sealed class ApiBackedUserPreferencesService(
    ILogger<ApiBackedUserPreferencesService> logger,
    IUserPreferencesApiClient apiClient,
    IOptions<FlightTrackerApiOptions> apiOptions) : IUserPreferencesService
{
    private readonly ILogger<ApiBackedUserPreferencesService> _logger = logger;
    private readonly IUserPreferencesApiClient _apiClient = apiClient;
    private readonly FlightTrackerApiOptions _apiOptions = apiOptions.Value;

    public Task<Result<UserPreferencesDto?>> GetAsync(int userId, CancellationToken cancellationToken = default)
        => UseSettingsApiAsync(
            () => _apiClient.GetAsync(userId, cancellationToken),
            dto => Result<UserPreferencesDto?>.Success(dto),
            "Unable to load preferences",
            "preferences.api.get.failed");

    public Task<Result<UserPreferencesDto>> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default)
        => UseSettingsApiAsync(
            async () => await _apiClient.GetAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException("Preferences not found."),
            dto => Result<UserPreferencesDto>.Success(dto),
            "Unable to load preferences",
            "preferences.api.get_or_create.failed");

    public Task<Result<UserPreferencesDto>> UpdateAsync(int userId, UserPreferencesDto preferences, CancellationToken cancellationToken = default)
        => UseSettingsApiAsync(
            async () => await _apiClient.UpdateAsync(userId, preferences, cancellationToken)
                ?? throw new InvalidOperationException("Preferences update returned no payload."),
            dto => Result<UserPreferencesDto>.Success(dto),
            "Unable to save preferences",
            "preferences.api.update.failed");

    private async Task<Result<T>> UseSettingsApiAsync<T>(
        Func<Task<T>> apiCall,
        Func<T, Result<T>> mapSuccess,
        string errorMessage,
        string errorCode)
    {
        if (!UseSettingsApi())
        {
            return Result<T>.Failure(
                "Settings API is not configured.",
                "preferences.api.not_configured");
        }

        try
        {
            var payload = await apiCall();
            return mapSuccess(payload);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling preferences API");
            return Result<T>.Failure(errorMessage, errorCode);
        }
    }

    private bool UseSettingsApi()
    {
        return _apiOptions.Slices.Settings
            && Uri.TryCreate(_apiOptions.BaseUrl, UriKind.Absolute, out _);
    }
}
