using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Repositories.Interfaces;

/// <summary>
/// Repository interface for airline data operations.
/// </summary>
public interface IAirlineRepository
{
    Task<Airline?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Airline?> GetByIcaoAsync(string icaoCode, CancellationToken cancellationToken = default);
    Task<Airline?> GetByIataAsync(string iataCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Airline>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Airline>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Airline> AddAsync(Airline airline, CancellationToken cancellationToken = default);
    Task<Airline> UpdateAsync(Airline airline, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
