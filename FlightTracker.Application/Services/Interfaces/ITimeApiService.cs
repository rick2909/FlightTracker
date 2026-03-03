using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Results;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Abstraction for resolving time zone information by coordinates.
/// Implemented in Infrastructure via an external API client.
/// </summary>
public interface ITimeApiService
{
    /// <summary>
    /// Returns a time zone ID (IANA) for the given latitude/longitude,
    /// or null if it cannot be determined.
    /// </summary>
    Task<Result<string?>> GetTimeZoneIdAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
