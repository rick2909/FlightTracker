using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Api.Contracts;

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

public sealed record AirlineStatsResponse(
    string AirlineName,
    string? AirlineIata,
    string? AirlineIcao,
    int Count);

public sealed record PassportDetailsResponse(
    List<AirlineStatsResponse> AirlineStats,
    Dictionary<string, int> AircraftTypeStats);
