using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Repositories.Interfaces;

/// <summary>
/// Repository interface for aircraft data operations.
/// </summary>
public interface IAircraftRepository
{
    Task<Aircraft?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Aircraft?> GetByRegistrationAsync(string registration, CancellationToken cancellationToken = default);
    Task<IEnumerable<Aircraft>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Aircraft>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Aircraft> AddAsync(Aircraft aircraft, CancellationToken cancellationToken = default);
    Task<Aircraft> UpdateAsync(Aircraft aircraft, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> RegistrationExistsAsync(string registration, CancellationToken cancellationToken = default);
}
