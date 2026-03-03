using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// External lookup client to fetch flight route metadata (airline, origin, destination)
/// from a callsign/flight number.
/// </summary>
public interface IFlightRouteLookupClient
{
    Task<FlightRouteLookupResult?> GetFlightRouteAsync(string callsign, CancellationToken cancellationToken = default);
}

public sealed record FlightRouteLookupResult(
    string Callsign,
    FlightRouteAirline? Airline,
    FlightRouteAirport? Origin,
    FlightRouteAirport? Destination
);

public sealed record FlightRouteAirline(
    string? Name,
    string? Icao,
    string? Iata,
    string? CountryName,
    string? CountryIso,
    string? Callsign
);

public sealed record FlightRouteAirport(
    string? IataCode,
    string? IcaoCode,
    string? Name,
    string? Municipality,
    string? CountryName,
    string? CountryIsoName,
    double? Latitude,
    double? Longitude,
    int? ElevationFeet
);
