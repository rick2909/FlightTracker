using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Repositories.Interfaces;

public interface IFlightRepository
{
    Task<Flight?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Flight?> GetByFlightNumberAndDateAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken = default);
    /// <summary>
    /// Searches flights by exact flight number with optional date filter (UTC day).
    /// </summary>
    Task<IReadOnlyList<Flight>> SearchByFlightNumberAsync(string flightNumber, DateOnly? date = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches flights by route. Departure/arrival can be airport code or city substring. Date filters by departure day (UTC).
    /// </summary>
    Task<IReadOnlyList<Flight>> SearchByRouteAsync(string? departure, string? arrival, DateOnly? date = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Flight>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Flight> AddAsync(Flight flight, CancellationToken cancellationToken = default);
    Task UpdateAsync(Flight flight, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
