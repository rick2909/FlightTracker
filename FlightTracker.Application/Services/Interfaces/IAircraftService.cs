using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Service interface for aircraft business operations.
/// </summary>
public interface IAircraftService
{
    Task<Result<AircraftDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AircraftDto>> GetByRegistrationAsync(string registration, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AircraftDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AircraftDto>>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Result<AircraftDto>> CreateAsync(CreateAircraftDto createDto, CancellationToken cancellationToken = default);
    Task<Result<AircraftDto>> UpdateAsync(int id, CreateAircraftDto updateDto, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<bool>> RegistrationExistsAsync(string registration, CancellationToken cancellationToken = default);
}
