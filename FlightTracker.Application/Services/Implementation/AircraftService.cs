using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
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

    public async Task<Result<AircraftDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var aircraft = await _aircraftRepository.GetByIdAsync(id, cancellationToken);
            var dto = aircraft == null ? null : _mapper.Map<AircraftDto>(aircraft);
            return Result<AircraftDto>.Success(dto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<AircraftDto>.Failure(ex.Message, "aircraft.by_id.load_failed");
        }
    }

    public async Task<Result<AircraftDto>> GetByRegistrationAsync(string registration, CancellationToken cancellationToken = default)
    {
        try
        {
            var aircraft = await _aircraftRepository.GetByRegistrationAsync(registration, cancellationToken);
            var dto = aircraft == null ? null : _mapper.Map<AircraftDto>(aircraft);
            return Result<AircraftDto>.Success(dto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<AircraftDto>.Failure(ex.Message, "aircraft.by_registration.load_failed");
        }
    }

    public async Task<Result<IEnumerable<AircraftDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var aircraft = await _aircraftRepository.GetAllAsync(cancellationToken);
            var result = aircraft.Select(a => _mapper.Map<AircraftDto>(a));
            return Result<IEnumerable<AircraftDto>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<AircraftDto>>.Failure(ex.Message, "aircraft.list.load_failed");
        }
    }

    public async Task<Result<IEnumerable<AircraftDto>>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            var aircraft = await _aircraftRepository.SearchAsync(searchTerm, cancellationToken);
            var result = aircraft.Select(a => _mapper.Map<AircraftDto>(a));
            return Result<IEnumerable<AircraftDto>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<AircraftDto>>.Failure(ex.Message, "aircraft.search.failed");
        }
    }

    public async Task<Result<AircraftDto>> CreateAsync(CreateAircraftDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _aircraftRepository.RegistrationExistsAsync(createDto.Registration, cancellationToken))
            {
                return Result<AircraftDto>.Failure(
                    $"Aircraft with registration '{createDto.Registration}' already exists.",
                    "aircraft.registration.duplicate");
            }

            var aircraft = _mapper.Map<Aircraft>(createDto);

            var savedAircraft = await _aircraftRepository.AddAsync(aircraft, cancellationToken);
            return Result<AircraftDto>.Success(_mapper.Map<AircraftDto>(savedAircraft));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<AircraftDto>.Failure(ex.Message, "aircraft.create.failed");
        }
    }

    public async Task<Result<AircraftDto>> UpdateAsync(int id, CreateAircraftDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingAircraft = await _aircraftRepository.GetByIdAsync(id, cancellationToken);
            if (existingAircraft == null)
            {
                return Result<AircraftDto>.Success(null);
            }

            if (existingAircraft.Registration != updateDto.Registration &&
                await _aircraftRepository.RegistrationExistsAsync(updateDto.Registration, cancellationToken))
            {
                return Result<AircraftDto>.Failure(
                    $"Aircraft with registration '{updateDto.Registration}' already exists.",
                    "aircraft.registration.duplicate");
            }

            _mapper.Map(updateDto, existingAircraft);

            var updatedAircraft = await _aircraftRepository.UpdateAsync(existingAircraft, cancellationToken);
            return Result<AircraftDto>.Success(_mapper.Map<AircraftDto>(updatedAircraft));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<AircraftDto>.Failure(ex.Message, "aircraft.update.failed");
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _aircraftRepository.ExistsAsync(id, cancellationToken))
            {
                return Result<bool>.Success(false);
            }

            await _aircraftRepository.DeleteAsync(id, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, "aircraft.delete.failed");
        }
    }

    public async Task<Result<bool>> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _aircraftRepository.ExistsAsync(id, cancellationToken);
            return Result<bool>.Success(exists);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, "aircraft.exists.check_failed");
        }
    }

    public async Task<Result<bool>> RegistrationExistsAsync(string registration, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _aircraftRepository.RegistrationExistsAsync(registration, cancellationToken);
            return Result<bool>.Success(exists);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, "aircraft.registration_exists.check_failed");
        }
    }


}
