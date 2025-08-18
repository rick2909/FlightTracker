using System.Collections.Generic;

namespace FlightTracker.Domain.Entities;

/// <summary>
/// Represents an airport (IATA / ICAO code, etc.).
/// </summary>
public class Airport
{
    public int Id { get; set; }
    /// <summary>
    /// Optional IATA 3-letter code (e.g., JFK). Prefer using specific
    /// properties over the legacy Code where possible.
    /// </summary>
    public string? IataCode { get; set; }

    /// <summary>
    /// Optional ICAO 4-letter code (e.g., KJFK).
    /// </summary>
    public string? IcaoCode { get; set; }
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
