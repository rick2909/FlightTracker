using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Represents a client for looking up aircraft information by registration or Mode S code.
/// </summary>
public interface IAircraftLookupClient
{
    /// <summary>
    /// Gets aircraft data by registration or Mode S code.
    /// </summary>
    /// <param name="registrationOrModeS">Aircraft registration (e.g., "N123AA") or Mode S code (hex)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aircraft enrichment data with full details, or null if not found</returns>
    Task<Result<AircraftEnrichmentDto>> GetAircraftAsync(string registrationOrModeS, CancellationToken cancellationToken = default);
}
