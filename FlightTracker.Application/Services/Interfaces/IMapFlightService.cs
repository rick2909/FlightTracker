using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Provides flight data formatted for map visualization.
/// </summary>
public interface IMapFlightService
{
    Task<IReadOnlyCollection<MapFlightDto>> GetUserMapFlightsAsync(int userId, int maxPast = 20, int maxUpcoming = 10, CancellationToken cancellationToken = default);
}
