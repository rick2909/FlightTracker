using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Service interface for aircraft business operations.
/// </summary>
public interface IAircraftService
{
    Task<AircraftDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AircraftDto?> GetByRegistrationAsync(string registration, CancellationToken cancellationToken = default);
    Task<IEnumerable<AircraftDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AircraftDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<AircraftDto> CreateAsync(CreateAircraftDto createDto, CancellationToken cancellationToken = default);
    Task<AircraftDto?> UpdateAsync(int id, CreateAircraftDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> RegistrationExistsAsync(string registration, CancellationToken cancellationToken = default);
}
