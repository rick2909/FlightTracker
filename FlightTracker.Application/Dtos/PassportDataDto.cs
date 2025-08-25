using System.Collections.Generic;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// Aggregated dataset for the Passport page, derived from the user's flights.
/// Does not include UI identity fields (name/avatar).
/// </summary>
public record PassportDataDto
{
    // Aggregates
    public int TotalFlights { get; init; }
    public int TotalMiles { get; init; }
    public int LongestFlightMiles { get; init; }
    public int ShortestFlightMiles { get; init; }

    // Favorites/Top
    public string FavoriteAirline { get; init; } = string.Empty;
    public string FavoriteAirport { get; init; } = string.Empty;
    public string MostFlownAircraftType { get; init; } = string.Empty;
    public string FavoriteClass { get; init; } = string.Empty;

    // Collections
    public List<string> AirlinesVisited { get; init; } = new();
    public List<string> AirportsVisited { get; init; } = new();
    public List<string> CountriesVisitedIso2 { get; init; } = new();

    // Year → count
    public Dictionary<int, int> FlightsPerYear { get; init; } = new();

    // Breakdowns for charts
    public Dictionary<string, int> FlightsByAirline { get; init; } = new();
    public Dictionary<string, int> FlightsByAircraftType { get; init; } = new();

    // Map routes (past + upcoming)
    public List<MapFlightDto> Routes { get; init; } = new();
}
