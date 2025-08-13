using System.Collections.Generic;

namespace FlightTracker.Domain.Entities;

/// <summary>
/// Represents an airport (IATA / ICAO code, etc.).
/// </summary>
public class Airport
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // e.g., "JFK"
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Latitude in decimal degrees (WGS84). Optional until data populated.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude in decimal degrees (WGS84). Optional until data populated.
    /// </summary>
    public double? Longitude { get; set; }

    // Navigation collections
    public ICollection<Flight> DepartingFlights { get; set; } = new List<Flight>();
    public ICollection<Flight> ArrivingFlights { get; set; } = new List<Flight>();
}
