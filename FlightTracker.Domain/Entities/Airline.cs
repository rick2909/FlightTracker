namespace FlightTracker.Domain.Entities;

/// <summary>
/// Represents an airline / operator.
/// </summary>
public class Airline
{
    public int Id { get; set; }
    /// <summary>IATA 2-letter code (may be null for some operators).</summary>
    public string? IataCode { get; set; }
    /// <summary>ICAO 3-letter code (preferred unique code).</summary>
    public string IcaoCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool Active { get; set; } = true;

    // Navigation
    public ICollection<Flight> Flights { get; set; } = new List<Flight>();
    public ICollection<Aircraft> Aircraft { get; set; } = new List<Aircraft>();
}
