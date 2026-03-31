using FlightTracker.Application.Dtos;

namespace FlightTracker.Api.Contracts.V1;

/// <summary>Aggregated passport data for a user, including totals, favourites and travel history.</summary>
/// <param name="TotalFlights">Total number of flights tracked by the user.</param>
/// <param name="TotalMiles">Total air miles accumulated.</param>
/// <param name="LongestFlightMiles">Distance of the longest single flight in miles.</param>
/// <param name="ShortestFlightMiles">Distance of the shortest single flight in miles.</param>
/// <param name="FavoriteAirline">Name of the most frequently flown airline.</param>
/// <param name="FavoriteAirport">IATA/ICAO code or name of the most visited airport.</param>
/// <param name="MostFlownAircraftType">Aircraft type description most commonly flown.</param>
/// <param name="FavoriteClass">Most frequently used cabin class.</param>
/// <param name="AirlinesVisited">Distinct airline names the user has flown with.</param>
/// <param name="AirportsVisited">Distinct airport codes the user has passed through.</param>
/// <param name="CountriesVisitedIso2">ISO-3166-1 alpha-2 country codes of countries visited.</param>
/// <param name="FlightsPerYear">Number of flights per calendar year (year → count).</param>
/// <param name="FlightsByAirline">Flights broken down by airline name (name → count).</param>
/// <param name="FlightsByAircraftType">Flights broken down by aircraft type (type → count).</param>
/// <param name="Routes">Map routes for visualisation.</param>
public sealed record PassportDataResponse(
    int TotalFlights,
    int TotalMiles,
    int LongestFlightMiles,
    int ShortestFlightMiles,
    string FavoriteAirline,
    string FavoriteAirport,
    string MostFlownAircraftType,
    string FavoriteClass,
    List<string> AirlinesVisited,
    List<string> AirportsVisited,
    List<string> CountriesVisitedIso2,
    Dictionary<int, int> FlightsPerYear,
    Dictionary<string, int> FlightsByAirline,
    Dictionary<string, int> FlightsByAircraftType,
    List<MapFlightDto> Routes);

/// <summary>Flight count statistics for a single airline.</summary>
/// <param name="AirlineName">Full airline name.</param>
/// <param name="AirlineIata">Two-letter IATA airline code, when known.</param>
/// <param name="AirlineIcao">Three-letter ICAO airline code, when known.</param>
/// <param name="Count">Number of flights operated by this airline.</param>
public sealed record AirlineStatsResponse(
    string AirlineName,
    string? AirlineIata,
    string? AirlineIcao,
    int Count);

/// <summary>Detailed passport breakdown by airline and aircraft type.</summary>
/// <param name="AirlineStats">Per-airline flight counts.</param>
/// <param name="AircraftTypeStats">Flights broken down by aircraft type (type → count).</param>
public sealed record PassportDetailsResponse(
    List<AirlineStatsResponse> AirlineStats,
    Dictionary<string, int> AircraftTypeStats);
