namespace FlightTracker.Application.Dtos;

/// <summary>
/// Aggregated count of flights grouped by airline.
/// </summary>
public record AirlineStatsDto
{
    public string AirlineName { get; init; } = string.Empty;
    public string? AirlineIata { get; init; }
    public string? AirlineIcao { get; init; }
    public int Count { get; init; }
}
