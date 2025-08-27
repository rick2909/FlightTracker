using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Ensures airline and airports exist (created if missing) for a given flight callsign.
/// </summary>
public interface IFlightMetadataProvisionService
{
    Task EnsureAirlineAndAirportsForCallsignAsync(string callsign, CancellationToken cancellationToken = default);
}
