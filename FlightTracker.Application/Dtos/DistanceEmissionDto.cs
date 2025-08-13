namespace FlightTracker.Application.Dtos;

/// <summary>
/// Represents computed distance and emission metrics for a flight segment.
/// </summary>
public record DistanceEmissionDto(
    double GreatCircleDistanceKm,
    double AdjustedDistanceKm,
    double EstimatedCo2Kg,
    string MethodologyVersion
);
