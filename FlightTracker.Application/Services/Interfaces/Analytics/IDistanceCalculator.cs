using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Interfaces.Analytics;

/// <summary>
/// Calculates great-circle distances between two airports.
/// Pure function contract; implementation can cache or optimize.
/// </summary>
public interface IDistanceCalculator
{
    /// <summary>
    /// Computes great-circle distance in kilometers (Haversine) between
    /// two airports using their latitude/longitude.
    /// Returns null if coordinates missing.
    /// </summary>
    double? CalculateGreatCircleKm(Airport from, Airport to);
}
