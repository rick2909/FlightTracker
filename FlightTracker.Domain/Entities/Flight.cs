using System;
using System.Collections.Generic;

namespace FlightTracker.Domain.Entities;

/// <summary>
/// Represents a scheduled or in-progress flight.
/// </summary>
public class Flight
{
    public int Id { get; set; }

    /// <summary>
    /// Airline flight designator (e.g., "AA123").
    /// </summary>
    public string FlightNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status text (e.g., Scheduled, Boarding, Departed, Landed, Cancelled, Delayed).
    /// (Can be replaced later by a strongly-typed enum if desired.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    public DateTime DepartureTimeUtc { get; set; }
    public DateTime ArrivalTimeUtc { get; set; }

    public int DepartureAirportId { get; set; }
    public int ArrivalAirportId { get; set; }

    // Navigation properties (optional in pure domain model; included for EF convenience)
    public Airport? DepartureAirport { get; set; }
    public Airport? ArrivalAirport { get; set; }

    /// <summary>
    /// Optional collection for related operational notes or events in future iterations.
    /// </summary>
    public ICollection<string> Tags { get; set; } = new List<string>();
}
