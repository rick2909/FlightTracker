using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Service for fetching aircraft photos from airport-data.com API.
/// </summary>
public interface IAircraftPhotoService
{
    /// <summary>
    /// Searches for aircraft photos using Mode-S code and/or registration.
    /// The API searches in order: (1) Mode-S + Registry, (2) Mode-S alone, (3) Registry alone.
    /// </summary>
    /// <param name="modeSCode">6-character hex Mode-S code (e.g., "400A0B")</param>
    /// <param name="registration">Aircraft registry/tail number (e.g., "G-KKAZ")</param>
    /// <param name="maxResults">Maximum number of results (default 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing aircraft photos or error</returns>
    Task<AircraftPhotoResultDto?> GetAircraftPhotosAsync(
        string? modeSCode,
        string? registration,
        int maxResults = 1,
        CancellationToken cancellationToken = default);
}
