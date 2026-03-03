using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Provides live departures/arrivals for a given airport using an external provider.
/// </summary>
public interface IAirportLiveService
{
    Task<Result<IReadOnlyList<LiveFlightDto>>> GetDeparturesAsync(string airportCode, int limit = 50, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LiveFlightDto>>> GetArrivalsAsync(string airportCode, int limit = 50, CancellationToken cancellationToken = default);
}
