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

    /// <summary>
    /// The aircraft operating this flight (optional).
    /// </summary>
    public int? AircraftId { get; set; }

    /// <summary>
    /// Operating airline (parsed from flight number or manually assigned). Optional for now.
    /// </summary>
    public int? OperatingAirlineId { get; set; }

    /// <summary>
    /// Departure terminal identifier (e.g., T1, 2, A). Optional.
    /// </summary>
    public string? DepartureTerminal { get; set; }

    /// <summary>
    /// Departure gate identifier (e.g., A12). Optional.
    /// </summary>
    public string? DepartureGate { get; set; }

    /// <summary>
    /// Arrival terminal identifier. Optional.
    /// </summary>
    public string? ArrivalTerminal { get; set; }

    /// <summary>
    /// Arrival gate identifier. Optional.
    /// </summary>
    public string? ArrivalGate { get; set; }

    /// <summary>
    /// Boarding start time (UTC). Optional.
    /// </summary>
    public DateTime? BoardingStartUtc { get; set; }

    /// <summary>
    /// Boarding end / gate close time (UTC). Optional.
    /// </summary>
    public DateTime? BoardingEndUtc { get; set; }

    // Navigation properties (optional in pure domain model; included for EF convenience)
    public Airport? DepartureAirport { get; set; }
    public Airport? ArrivalAirport { get; set; }
    public Aircraft? Aircraft { get; set; }
    public Airline? OperatingAirline { get; set; }

    /// <summary>
    /// Collection of user flight experiences for this flight.
    /// </summary>
    public ICollection<UserFlight> UserFlights { get; set; } = new List<UserFlight>();

    /// <summary>
    /// Optional collection for related operational notes or events in future iterations.
    /// </summary>
    public ICollection<string> Tags { get; set; } = new List<string>();
}
