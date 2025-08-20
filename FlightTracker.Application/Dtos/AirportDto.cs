namespace FlightTracker.Application.Dtos;

/// <summary>
/// Airport DTO used for lookups and listings.
/// </summary>
public record AirportDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? IataCode { get; init; }
    public string? IcaoCode { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? TimeZoneId { get; init; }
}
