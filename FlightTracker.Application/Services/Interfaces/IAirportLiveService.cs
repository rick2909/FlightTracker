using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Provides live departures/arrivals for a given airport using an external provider.
/// </summary>
public interface IAirportLiveService
{
    Task<IReadOnlyList<LiveFlightDto>> GetDeparturesAsync(string airportCode, int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LiveFlightDto>> GetArrivalsAsync(string airportCode, int limit = 50, CancellationToken cancellationToken = default);
}
