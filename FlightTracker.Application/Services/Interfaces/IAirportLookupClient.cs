using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Represents a client for looking up airport information by ICAO code.
/// </summary>
public interface IAirportLookupClient
{
    /// <summary>
    /// Gets airport data by ICAO code.
    /// </summary>
    /// <param name="icaoCode">Airport ICAO code (e.g., "EHAM")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Airport enrichment data with full details, or null if not found</returns>
    Task<Result<AirportEnrichmentDto>> GetAirportAsync(string icaoCode, CancellationToken cancellationToken = default);
}
