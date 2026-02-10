using System.Collections.Generic;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Domain.Entities;

/// <summary>
/// Represents an aircraft that can operate flights.
/// </summary>
public class Aircraft
{
    public int Id { get; set; }

    /// <summary>
    /// Aircraft registration (tail number), e.g., "N12345".
    /// </summary>
    public string Registration { get; set; } = string.Empty;

    /// <summary>
    /// Aircraft manufacturer.
    /// </summary>
    public AircraftManufacturer Manufacturer { get; set; }

    /// <summary>
    /// Aircraft model, e.g., "737-800", "A320".
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Year the aircraft was manufactured.
    /// </summary>
    public int? YearManufactured { get; set; }

    /// <summary>
    /// Maximum passenger capacity in standard configuration.
    /// </summary>
    public int? PassengerCapacity { get; set; }

    /// <summary>
    /// ICAO aircraft type designator, e.g., "B738", "A320".
    /// </summary>
    public string? IcaoTypeCode { get; set; }

    /// <summary>
    /// Mode S transponder code (hex), e.g., "402837". Used for aircraft lookups.
    /// </summary>
    public string? ModeS { get; set; }

    /// <summary>
    /// Optional notes about this specific aircraft.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Owning or primary operating airline for this aircraft (optional).
    /// </summary>
    public int? AirlineId { get; set; }

    /// <summary>
    /// Navigation to the airline associated with this aircraft.
    /// </summary>
    public Airline? Airline { get; set; }

    /// <summary>
    /// Collection of flights operated by this aircraft.
    /// </summary>
    public ICollection<Flight> Flights { get; set; } = new List<Flight>();
}
