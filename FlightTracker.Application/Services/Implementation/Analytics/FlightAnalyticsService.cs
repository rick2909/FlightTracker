using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces.Analytics;

namespace FlightTracker.Application.Services.Implementation.Analytics;

/// <summary>
/// Orchestrates distance + emission calculations for flights.
/// </summary>
public class FlightAnalyticsService : IFlightAnalyticsService
{
    private readonly IFlightRepository _flightRepository;
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly IEmissionCalculator _emissionCalculator;

    public FlightAnalyticsService(
        IFlightRepository flightRepository,
        IDistanceCalculator distanceCalculator,
        IEmissionCalculator emissionCalculator)
    {
        _flightRepository = flightRepository;
        _distanceCalculator = distanceCalculator;
        _emissionCalculator = emissionCalculator;
    }

    public async Task<DistanceEmissionDto?> GetDistanceAndEmissionsAsync(int flightId, CancellationToken cancellationToken = default)
    {
        var flight = await _flightRepository.GetByIdAsync(flightId, cancellationToken);
        if (flight == null || flight.DepartureAirport == null || flight.ArrivalAirport == null)
            return null;

        var distanceKm = _distanceCalculator.CalculateGreatCircleKm(flight.DepartureAirport, flight.ArrivalAirport);
        if (distanceKm == null)
            return null; // Missing coordinates

        // Adjust distance with simple surcharge for routing/wind (e.g., +5%)
        var adjustedDistance = System.Math.Round(distanceKm.Value * 1.05, 1);
        var co2 = _emissionCalculator.EstimateCo2Kg(flight, adjustedDistance, flight.Aircraft);

        return new DistanceEmissionDto(
            GreatCircleDistanceKm: distanceKm.Value,
            AdjustedDistanceKm: adjustedDistance,
            EstimatedCo2Kg: co2,
            MethodologyVersion: "v1-haversine-heuristic"
        );
    }
}
