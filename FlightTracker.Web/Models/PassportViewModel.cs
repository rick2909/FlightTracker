using System;
using System.Collections.Generic;
using FlightTracker.Application.Dtos;

namespace YourApp.Models;

/// <summary>
/// ViewModel for the Passport screen (Flighty-style).
/// Reuses MapFlightDto for map routes.
/// </summary>
public class PassportViewModel
{
    // Profile
    public string UserName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }

    // Aggregate stats
    public int TotalFlights { get; init; }
    public int TotalMiles { get; init; }
    public string FavoriteAirline { get; init; } = string.Empty;
    public int LongestFlightMiles { get; init; }
    public int ShortestFlightMiles { get; init; }

    // Collections
    public List<string> AirlinesVisited { get; init; } = new();
    public List<string> AirportsVisited { get; init; } = new();
    /// <summary>
    /// ISO-3166-1 alpha-2 country codes (lowercase preferred, e.g., "us").
    /// </summary>
    public List<string> CountriesVisitedIso2 { get; init; } = new();

    /// <summary>
    /// Year → number of flights.
    /// </summary>
    public Dictionary<int, int> FlightsPerYear { get; init; } = new();

    /// <summary>
    /// Routes to render on the map (past and upcoming).
    /// </summary>
    public List<MapFlightDto> Routes { get; init; } = new();
}
