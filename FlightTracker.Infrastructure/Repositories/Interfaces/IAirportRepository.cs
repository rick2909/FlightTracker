using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Infrastructure.Repositories.Interfaces;

public interface IAirportRepository
{
    Task<Airport?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Airport?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Airport>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Airport> AddAsync(Airport airport, CancellationToken cancellationToken = default);
    Task UpdateAsync(Airport airport, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
