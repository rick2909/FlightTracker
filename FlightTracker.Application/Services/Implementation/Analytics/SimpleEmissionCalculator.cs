using FlightTracker.Application.Services.Interfaces.Analytics;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation.Analytics;

/// <summary>
/// Very simple emission calculator using generic emission factors.
/// This is deliberately minimal and can be replaced with a more
/// sophisticated implementation (e.g., factoring seat class, load).
/// </summary>
public class SimpleEmissionCalculator : IEmissionCalculator
{
    // Generic average emission factor (kg CO2 per passenger-km) for mixed fleet economy baseline.
    // Source placeholder: typical 0.09 - 0.12 kg; using 0.11 kg here.
    private const double GenericFactorKgPerPaxKm = 0.11;

    // Adjustment multipliers per broad aircraft size category (naive heuristic)
    private const double WideBodyMultiplier = 1.05; // Slightly higher due to weight but more seats -> near neutral
    private const double NarrowBodyMultiplier = 1.00;
    private const double RegionalMultiplier = 1.20; // Less efficient per seat

    public double EstimateCo2Kg(Flight flight, double distanceKm, Aircraft? aircraft = null)
    {
        if (distanceKm <= 0) return 0;

        var factor = GenericFactorKgPerPaxKm;

        if (aircraft != null)
        {
            // Very rough categorization by ICAO type or model keywords
            var model = (aircraft.Model ?? string.Empty).ToUpperInvariant();
            var code = (aircraft.IcaoTypeCode ?? string.Empty).ToUpperInvariant();
            if (model.Contains("ATR") || code.StartsWith("AT") || code.StartsWith("DH") || model.Contains("Q400"))
            {
                factor *= RegionalMultiplier; // turboprops per seat often higher when low load
            }
            else if (model.Contains("350") || model.Contains("777") || model.Contains("787") || model.Contains("330") || model.Contains("340") || model.Contains("767") || model.Contains("380"))
            {
                factor *= WideBodyMultiplier;
            }
            else
            {
                factor *= NarrowBodyMultiplier;
            }
        }

        // NOTE: This treats one passenger. For user stats, multiply by their flights flown.
        var co2 = distanceKm * factor;
        return System.Math.Round(co2, 2);
    }
}
