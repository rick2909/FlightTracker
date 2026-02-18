using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Service for enriching airports with additional data from external sources
/// and persisting them to the database if they don't already exist.
/// </summary>
public interface IAirportEnrichmentService
{
    /// <summary>
    /// Fetches airport data from AirportDB by ICAO code and stores it in the
    /// database if it doesn't already exist. Returns the stored or newly created airport DTO.
    /// </summary>
    /// <param name="icaoCode">Airport ICAO code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Airport DTO if found or created, null otherwise</returns>
    Task<AirportDto?> EnrichAirportAsync(
        string icaoCode,
        CancellationToken cancellationToken = default);
}
