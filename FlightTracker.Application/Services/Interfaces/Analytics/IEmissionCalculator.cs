using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Interfaces.Analytics;

/// <summary>
/// Estimates CO2 emissions for a flight given distance and (optionally) aircraft data.
/// Keeps domain free of external emission factor logic.
/// </summary>
public interface IEmissionCalculator
{
    /// <summary>
    /// Estimates CO2 in kilograms for the provided flight.
    /// Requires a distanceKm value (already computed). Aircraft may be null.
    /// Implementation may use aircraft type/model heuristics or generic factors.
    /// </summary>
    double EstimateCo2Kg(Flight flight, double distanceKm, Aircraft? aircraft = null);
}
