using System;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Models.ViewModels;

/// <summary>
/// View model describing the user's current travel context for a single flight,
/// used by the FlightStateSheet component and Status page.
/// </summary>
public class FlightStateViewModel
{
    public enum TravelState
    {
        PreFlight,
        AtAirport,
        PostFlight
    }

    public TravelState State { get; set; }

    /// <summary>
    /// The user flight (flattened) for quick labels.
    /// </summary>
    public UserFlightDto Flight { get; set; } = default!;

    /// <summary>
    /// Additional schedule/airport details pulled from Flight entity.
    /// </summary>
    public string? DepartureTerminal { get; set; }
    public string? DepartureGate { get; set; }
    public string? ArrivalTerminal { get; set; }
    public string? ArrivalGate { get; set; }
    public DateTime? BoardingStartUtc { get; set; }
    public DateTime? BoardingEndUtc { get; set; }

    /// <summary>
    /// Optional single-route map data for Status page.
    /// </summary>
    public MapFlightDto? Route { get; set; }

    /// <summary>
    /// Friendly countdown label (e.g., "Gate Departure in 1h 54m").
    /// </summary>
    public string CountdownLabel { get; set; } = string.Empty;

    /// <summary>
    /// Ticks remaining to the next milestone. Used for auto-refresh if desired.
    /// </summary>
    public TimeSpan? TimeToNext { get; set; }

    /// <summary>
    /// Optional note line (e.g., inbound aircraft has arrived).
    /// </summary>
    public string? Note { get; set; }
}
