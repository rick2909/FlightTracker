using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Models.ViewModels;

/// <summary>
/// View model for the Passport / Stats page.
/// Includes user stats, map flights for routes, and chart series.
/// </summary>
public class StatsViewModel
{
    public int UserId { get; set; }
    public UserFlightStatsDto Stats { get; set; } = new();
    public IEnumerable<MapFlightDto> MapFlights { get; set; } = Array.Empty<MapFlightDto>();

    /// <summary>
    /// Aggregated flights per year for charting.
    /// </summary>
    public IReadOnlyList<FlightsPerYearPoint> FlightsPerYear { get; set; } = Array.Empty<FlightsPerYearPoint>();

    public record FlightsPerYearPoint(int Year, int Count);
}
