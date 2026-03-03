using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Provides flight data formatted for map visualization.
/// </summary>
public interface IMapFlightService
{
    Task<Result<IReadOnlyCollection<MapFlightDto>>> GetUserMapFlightsAsync(int userId, int maxPast = 20, int maxUpcoming = 10, CancellationToken cancellationToken = default);
}
