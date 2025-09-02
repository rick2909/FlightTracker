using System.Collections.Generic;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// Detailed stats for the Passport details section.
/// </summary>
public record PassportDetailsDto
{
    public List<AirlineStatsDto> AirlineStats { get; init; } = new();
    public Dictionary<string, int> AircraftTypeStats { get; init; } = new();
}
