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

    public AircraftService(IAircraftRepository aircraftRepository)
    {
        _aircraftRepository = aircraftRepository;
    }

    public async Task<AircraftDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var aircraft = await _aircraftRepository.GetByIdAsync(id, cancellationToken);
        return aircraft == null ? null : MapToDto(aircraft);
    }

    public async Task<AircraftDto?> GetByRegistrationAsync(string registration, CancellationToken cancellationToken = default)
    {
        var aircraft = await _aircraftRepository.GetByRegistrationAsync(registration, cancellationToken);
        return aircraft == null ? null : MapToDto(aircraft);
    }

    public async Task<IEnumerable<AircraftDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var aircraft = await _aircraftRepository.GetAllAsync(cancellationToken);
        return aircraft.Select(MapToDto);
    }

    public async Task<IEnumerable<AircraftDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var aircraft = await _aircraftRepository.SearchAsync(searchTerm, cancellationToken);
        return aircraft.Select(MapToDto);
    }

    public async Task<AircraftDto> CreateAsync(CreateAircraftDto createDto, CancellationToken cancellationToken = default)
    {
        // Validate registration uniqueness
        if (await _aircraftRepository.RegistrationExistsAsync(createDto.Registration, cancellationToken))
        {
            throw new InvalidOperationException($"Aircraft with registration '{createDto.Registration}' already exists.");
        }

        var aircraft = new Aircraft
        {
            Registration = createDto.Registration,
            Manufacturer = createDto.Manufacturer,
            Model = createDto.Model,
            YearManufactured = createDto.YearManufactured,
            PassengerCapacity = createDto.PassengerCapacity,
            IcaoTypeCode = createDto.IcaoTypeCode,
            Notes = createDto.Notes,
            AirlineId = createDto.AirlineId
        };

        var savedAircraft = await _aircraftRepository.AddAsync(aircraft, cancellationToken);
        return MapToDto(savedAircraft);
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

        existingAircraft.Registration = updateDto.Registration;
        existingAircraft.Manufacturer = updateDto.Manufacturer;
        existingAircraft.Model = updateDto.Model;
        existingAircraft.YearManufactured = updateDto.YearManufactured;
        existingAircraft.PassengerCapacity = updateDto.PassengerCapacity;
        existingAircraft.IcaoTypeCode = updateDto.IcaoTypeCode;
        existingAircraft.Notes = updateDto.Notes;
        existingAircraft.AirlineId = updateDto.AirlineId;

        var updatedAircraft = await _aircraftRepository.UpdateAsync(existingAircraft, cancellationToken);
        return MapToDto(updatedAircraft);
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

    private static AircraftDto MapToDto(Aircraft aircraft)
    {
        return new AircraftDto
        {
            Id = aircraft.Id,
            Registration = aircraft.Registration,
            Manufacturer = aircraft.Manufacturer,
            Model = aircraft.Model,
            YearManufactured = aircraft.YearManufactured,
            PassengerCapacity = aircraft.PassengerCapacity,
            IcaoTypeCode = aircraft.IcaoTypeCode,
            Notes = aircraft.Notes,
            AirlineId = aircraft.AirlineId,
            AirlineIcaoCode = aircraft.Airline?.IcaoCode,
            AirlineIataCode = aircraft.Airline?.IataCode,
            AirlineName = aircraft.Airline?.Name
        };
    }
}
