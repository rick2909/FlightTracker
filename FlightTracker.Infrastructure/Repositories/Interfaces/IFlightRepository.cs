using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Infrastructure.Repositories.Interfaces;

public interface IFlightRepository
{
    Task<Flight?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Flight>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Flight> AddAsync(Flight flight, CancellationToken cancellationToken = default);
    Task UpdateAsync(Flight flight, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
