using System;
using System.Collections.Generic;
using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Enums;

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
    public string TotalDistanceDisplay { get; init; } = string.Empty;
    public string FavoriteAirline { get; init; } = string.Empty;
    public string FavoriteAirport { get; init; } = string.Empty;
    public string MostFlownAircraftType { get; init; } = string.Empty;
    public string FavoriteClass { get; init; } = string.Empty;
    public int LongestFlightMiles { get; init; }
    public int ShortestFlightMiles { get; init; }
    public string LongestDistanceDisplay { get; init; } = string.Empty;
    public string ShortestDistanceDisplay { get; init; } = string.Empty;

    public DistanceUnit DistanceUnit { get; init; } = DistanceUnit.Miles;
    public DateFormat DateFormat { get; init; } = DateFormat.YearMonthDay;
    public TimeFormat TimeFormat { get; init; } = TimeFormat.TwentyFourHour;

    public bool ShowTotalMiles { get; init; } = true;
    public bool ShowAirlines { get; init; } = true;
    public bool ShowCountries { get; init; } = true;
    public bool ShowMapRoutes { get; init; } = true;

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
    /// Breakdown for charts: flights grouped by airline name.
    /// </summary>
    public Dictionary<string, int> FlightsByAirline { get; init; } = new();

    /// <summary>
    /// Breakdown for charts: flights grouped by aircraft type/model.
    /// </summary>
    public Dictionary<string, int> FlightsByAircraftType { get; init; } = new();

    /// <summary>
    /// Routes to render on the map (past and upcoming).
    /// </summary>
    public List<MapFlightDto> Routes { get; init; } = new();
}
