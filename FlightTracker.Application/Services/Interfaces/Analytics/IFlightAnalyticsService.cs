using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces.Analytics;

/// <summary>
/// High-level analytics service combining distance and CO2 calculation.
/// </summary>
public interface IFlightAnalyticsService
{
    /// <summary>
    /// Computes distance and emission metrics for a flight id.
    /// Returns null if flight or required coordinates unavailable.
    /// </summary>
    Task<DistanceEmissionDto?> GetDistanceAndEmissionsAsync(int flightId, CancellationToken cancellationToken = default);
}
