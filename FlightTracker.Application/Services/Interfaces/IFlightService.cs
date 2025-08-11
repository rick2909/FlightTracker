using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Business operations and queries related to <see cref="Flight"/> entities.
/// </summary>
public interface IFlightService
{
    Task<Flight?> GetFlightByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns flights departing after (>=) the provided UTC moment within an optional window.
    /// </summary>
    Task<IReadOnlyList<Flight>> GetUpcomingFlightsAsync(DateTime fromUtc, TimeSpan? window = null, CancellationToken cancellationToken = default);

    Task<Flight> AddFlightAsync(Flight flight, CancellationToken cancellationToken = default);
    Task UpdateFlightAsync(Flight flight, CancellationToken cancellationToken = default);
    Task DeleteFlightAsync(int id, CancellationToken cancellationToken = default);
}
