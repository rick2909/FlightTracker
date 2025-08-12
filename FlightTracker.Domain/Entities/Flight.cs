using System;
using System.Collections.Generic;
using FlightTracker.Domain.Enums;

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
    /// Current status of the flight.
    /// </summary>
    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

    public DateTime DepartureTimeUtc { get; set; }
    public DateTime ArrivalTimeUtc { get; set; }

    public int DepartureAirportId { get; set; }
    public int ArrivalAirportId { get; set; }

    // Navigation properties (optional in pure domain model; included for EF convenience)
    public Airport? DepartureAirport { get; set; }
    public Airport? ArrivalAirport { get; set; }

    /// <summary>
    /// Collection of user flight experiences for this flight.
    /// </summary>
    public ICollection<UserFlight> UserFlights { get; set; } = new List<UserFlight>();

    /// <summary>
    /// Optional collection for related operational notes or events in future iterations.
    /// </summary>
    public ICollection<string> Tags { get; set; } = new List<string>();
}
