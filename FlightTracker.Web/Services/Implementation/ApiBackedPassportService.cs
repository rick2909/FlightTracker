using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Web.Configuration;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FlightTracker.Web.Services.Implementation;

public sealed class ApiBackedPassportService(
    ILogger<ApiBackedPassportService> logger,
    IPassportApiClient apiClient,
    IOptions<FlightTrackerApiOptions> apiOptions) : IPassportService
{
    private readonly ILogger<ApiBackedPassportService> _logger = logger;
    private readonly IPassportApiClient _apiClient = apiClient;
    private readonly FlightTrackerApiOptions _apiOptions = apiOptions.Value;

    public Task<Result<PassportDataDto>> GetPassportDataAsync(int userId, CancellationToken cancellationToken = default)
        => UsePassportApiAsync(
            () => _apiClient.GetPassportDataAsync(userId, cancellationToken),
            "Unable to load passport data",
            "passport.api.data.failed");

    public Task<Result<PassportDetailsDto>> GetPassportDetailsAsync(int userId, CancellationToken cancellationToken = default)
        => UsePassportApiAsync(
            () => _apiClient.GetPassportDetailsAsync(userId, cancellationToken),
            "Unable to load passport details",
            "passport.api.details.failed");

    private async Task<Result<T>> UsePassportApiAsync<T>(
        Func<Task<T?>> apiCall,
        string errorMessage,
        string errorCode)
    {
        if (!UsePassportApi())
        {
            return Result<T>.Failure(
                "Passport API is not configured.",
                "passport.api.not_configured");
        }

        try
        {
            var payload = await apiCall();
            return Result<T>.Success(payload);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling passport API");
            return Result<T>.Failure(errorMessage, errorCode);
        }
    }

    private bool UsePassportApi()
    {
        return _apiOptions.Slices.Passport
            && Uri.TryCreate(_apiOptions.BaseUrl, UriKind.Absolute, out _);
    }
}
