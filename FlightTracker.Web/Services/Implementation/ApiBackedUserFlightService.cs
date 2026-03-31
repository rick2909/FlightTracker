using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using FlightTracker.Web.Configuration;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FlightTracker.Web.Services.Implementation;

public sealed class ApiBackedUserFlightService(
    ILogger<ApiBackedUserFlightService> logger,
    IUserFlightsApiClient apiClient,
    IOptions<FlightTrackerApiOptions> apiOptions) : IUserFlightService
{
    private readonly ILogger<ApiBackedUserFlightService> _logger = logger;
    private readonly IUserFlightsApiClient _apiClient = apiClient;
    private readonly FlightTrackerApiOptions _apiOptions = apiOptions.Value;

    public Task<Result<IEnumerable<UserFlightDto>>> GetUserFlightsAsync(int userId, CancellationToken cancellationToken = default)
        => UseFlightsApiAsync<IReadOnlyList<UserFlightDto>, IEnumerable<UserFlightDto>>(
            () => _apiClient.GetUserFlightsAsync(userId, cancellationToken),
            flights => Result<IEnumerable<UserFlightDto>>.Success(flights),
            "Unable to load flights",
            "userflight.api.list.failed");

    public Task<Result<UserFlightDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => UseFlightsApiAsync<UserFlightDto?, UserFlightDto>(
            () => _apiClient.GetByIdAsync(id, cancellationToken),
            dto => Result<UserFlightDto>.Success(dto),
            "Unable to load flight",
            "userflight.api.by_id.failed");

    public Task<Result<IEnumerable<UserFlightDto>>> GetUserFlightsByClassAsync(int userId, FlightClass flightClass, CancellationToken cancellationToken = default)
        => UseFlightsApiAsync<IReadOnlyList<UserFlightDto>, IEnumerable<UserFlightDto>>(
            () => _apiClient.GetUserFlightsAsync(userId, cancellationToken),
            flights => Result<IEnumerable<UserFlightDto>>.Success(flights.Where(f => f.FlightClass == flightClass)),
            "Unable to load flights",
            "userflight.api.list_by_class.failed");

    public Task<Result<UserFlightDto>> AddUserFlightAsync(int userId, CreateUserFlightDto createDto, CancellationToken cancellationToken = default)
        => UseFlightsApiAsync<UserFlightDto?, UserFlightDto>(
            () => _apiClient.AddUserFlightAsync(userId, createDto, cancellationToken),
            dto => Result<UserFlightDto>.Success(dto),
            "Unable to save flight",
            "userflight.api.add.failed");

    public Task<Result<UserFlightDto>> UpdateUserFlightAsync(int id, CreateUserFlightDto updateDto, CancellationToken cancellationToken = default)
        => Result<UserFlightDto>.Failure(
            "Legacy update endpoint is not supported in API mode.",
            "userflight.api.legacy_update.unsupported").AsTask();

    public Task<Result<UserFlightDto>> UpdateUserFlightAndScheduleAsync(int id, UpdateUserFlightDto userFlight, FlightScheduleUpdateDto schedule, CancellationToken cancellationToken = default)
        => UseFlightsApiAsync<UserFlightDto?, UserFlightDto>(
            () => _apiClient.UpdateUserFlightAndScheduleAsync(id, userFlight, schedule, cancellationToken),
            dto => Result<UserFlightDto>.Success(dto),
            "Unable to update flight",
            "userflight.api.update.failed");

    public Task<Result<bool>> DeleteUserFlightAsync(int id, CancellationToken cancellationToken = default)
        => UseFlightsApiAsync<bool, bool>(
            () => _apiClient.DeleteUserFlightAsync(id, cancellationToken),
            deleted => Result<bool>.Success(deleted),
            "Unable to delete flight",
            "userflight.api.delete.failed");

    public Task<Result<UserFlightStatsDto>> GetUserFlightStatsAsync(int userId, CancellationToken cancellationToken = default)
        => UseFlightsApiAsync<UserFlightStatsDto?, UserFlightStatsDto>(
            () => _apiClient.GetUserFlightStatsAsync(userId, cancellationToken),
            stats => Result<UserFlightStatsDto>.Success(stats),
            "Unable to load flight statistics",
            "userflight.api.stats.failed");

    public Task<Result<bool>> HasUserFlownFlightAsync(int userId, int flightId, CancellationToken cancellationToken = default)
        => UseFlightsApiAsync<bool, bool>(
            () => _apiClient.HasUserFlownFlightAsync(userId, flightId, cancellationToken),
            hasFlown => Result<bool>.Success(hasFlown),
            "Unable to check user flight history",
            "userflight.api.hasflown.failed");

    private async Task<Result<TResult>> UseFlightsApiAsync<TApi, TResult>(
        Func<Task<TApi>> apiCall,
        Func<TApi, Result<TResult>> mapSuccess,
        string errorMessage,
        string errorCode)
    {
        if (!UseFlightsApi())
        {
            return Result<TResult>.Failure(
                "Flights API is not configured.",
                "userflight.api.not_configured");
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
            _logger.LogError(ex, "Error calling flights API");
            return Result<TResult>.Failure(errorMessage, errorCode);
        }
    }

    private bool UseFlightsApi()
    {
        return _apiOptions.Slices.Flights
            && Uri.TryCreate(_apiOptions.BaseUrl, UriKind.Absolute, out _);
    }
}

internal static class ResultTaskExtensions
{
    public static Task<Result<T>> AsTask<T>(this Result<T> result)
    {
        return Task.FromResult(result);
    }
}
