using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service implementation for aircraft business operations.
/// </summary>
public class AircraftService : IAircraftService
{
    private readonly IAircraftRepository _aircraftRepository;
    private readonly IMapper _mapper;

    public AircraftService(IAircraftRepository aircraftRepository, IMapper mapper)
    {
        _aircraftRepository = aircraftRepository;
        _mapper = mapper;
    }

    public async Task<AircraftDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var aircraft = await _aircraftRepository.GetByIdAsync(id, cancellationToken);
        return aircraft == null ? null : _mapper.Map<AircraftDto>(aircraft);
    }

    public async Task<AircraftDto?> GetByRegistrationAsync(string registration, CancellationToken cancellationToken = default)
    {
        var aircraft = await _aircraftRepository.GetByRegistrationAsync(registration, cancellationToken);
        return aircraft == null ? null : _mapper.Map<AircraftDto>(aircraft);
    }

    public async Task<IEnumerable<AircraftDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var aircraft = await _aircraftRepository.GetAllAsync(cancellationToken);
        return aircraft.Select(a => _mapper.Map<AircraftDto>(a));
    }

    public async Task<IEnumerable<AircraftDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var aircraft = await _aircraftRepository.SearchAsync(searchTerm, cancellationToken);
        return aircraft.Select(a => _mapper.Map<AircraftDto>(a));
    }

    public async Task<AircraftDto> CreateAsync(CreateAircraftDto createDto, CancellationToken cancellationToken = default)
    {
        // Validate registration uniqueness
        if (await _aircraftRepository.RegistrationExistsAsync(createDto.Registration, cancellationToken))
        {
            throw new InvalidOperationException($"Aircraft with registration '{createDto.Registration}' already exists.");
        }

        var aircraft = _mapper.Map<Aircraft>(createDto);

        var savedAircraft = await _aircraftRepository.AddAsync(aircraft, cancellationToken);
        return _mapper.Map<AircraftDto>(savedAircraft);
    }

    public async Task<AircraftDto?> UpdateAsync(int id, CreateAircraftDto updateDto, CancellationToken cancellationToken = default)
    {
        var existingAircraft = await _aircraftRepository.GetByIdAsync(id, cancellationToken);
        if (existingAircraft == null)
        {
            return null;
        }

        // Check if registration is being changed and if the new registration already exists
        if (existingAircraft.Registration != updateDto.Registration &&
            await _aircraftRepository.RegistrationExistsAsync(updateDto.Registration, cancellationToken))
        {
            throw new InvalidOperationException($"Aircraft with registration '{updateDto.Registration}' already exists.");
        }

        _mapper.Map(updateDto, existingAircraft);

        var updatedAircraft = await _aircraftRepository.UpdateAsync(existingAircraft, cancellationToken);
        return _mapper.Map<AircraftDto>(updatedAircraft);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _aircraftRepository.ExistsAsync(id, cancellationToken))
        {
            return false;
        }

        await _aircraftRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _aircraftRepository.ExistsAsync(id, cancellationToken);
    }

    public async Task<bool> RegistrationExistsAsync(string registration, CancellationToken cancellationToken = default)
    {
        return await _aircraftRepository.RegistrationExistsAsync(registration, cancellationToken);
    }

    
}
