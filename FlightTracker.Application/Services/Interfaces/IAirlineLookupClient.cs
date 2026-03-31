using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// External lookup client to fetch airline metadata by IATA/ICAO code.
/// </summary>
public interface IAirlineLookupClient
{
    Task<AirlineLookupResult?> GetAirlineByCodeAsync(
        string airlineCode,
        CancellationToken cancellationToken = default);
}

public sealed record AirlineLookupResult(
    string? Name,
    string? Icao,
    string? Iata,
    string? Country,
    string? CountryIso,
    string? Callsign
);
