using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service implementation for airport-related business operations.
/// </summary>
public class AirportService : IAirportService
{
    private readonly IAirportRepository _airportRepository;
    private readonly ITimeApiService _timeApiService;

    public AirportService(IAirportRepository airportRepository, ITimeApiService timeApiService)
    {
        _airportRepository = airportRepository;
        _timeApiService = timeApiService;
    }

    public async Task<Result<Airport>> GetAirportByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var airport = await _airportRepository.GetByCodeAsync(
                code,
                cancellationToken);

            return Result<Airport>.Success(airport);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<Airport>.Failure(
                ex.Message,
                "airport.by_code.load_failed");
        }
    }

    public async Task<Result<IReadOnlyList<Airport>>> GetAllAirportsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var airports = await _airportRepository.GetAllAsync(
                cancellationToken);

            return Result<IReadOnlyList<Airport>>.Success(airports);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<Airport>>.Failure(
                ex.Message,
                "airport.all.load_failed");
        }
    }

    public async Task<Result<Airport>> AddAirportAsync(Airport airport, CancellationToken cancellationToken = default)
    {
        try
        {
            var added = await _airportRepository.AddAsync(
                airport,
                cancellationToken);

            return Result<Airport>.Success(added);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<Airport>.Failure(
                ex.Message,
                "airport.add.failed");
        }
    }

    public async Task<Result> UpdateAirportAsync(Airport airport, CancellationToken cancellationToken = default)
    {
        try
        {
            await _airportRepository.UpdateAsync(airport, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure(
                ex.Message,
                "airport.update.failed");
        }
    }

    public async Task<Result> DeleteAirportAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _airportRepository.DeleteAsync(id, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure(
                ex.Message,
                "airport.delete.failed");
        }
    }

    public async Task<Result<string?>> GetTimeZoneIdByAirportCodeAsync(string airportCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var airport = await _airportRepository.GetByCodeAsync(
                airportCode,
                cancellationToken);

            if (airport?.Latitude is double lat &&
                airport.Longitude is double lon &&
                airport.TimeZoneId is null)
            {
                try
                {
                    airport.TimeZoneId = await _timeApiService.GetTimeZoneIdAsync(
                        lat,
                        lon,
                        cancellationToken);

                    await _airportRepository.UpdateAsync(
                        airport,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    return Result<string?>.Success(null);
                }
            }

            return Result<string?>.Success(airport?.TimeZoneId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<string?>.Failure(
                ex.Message,
                "airport.timezone.load_failed");
        }
    }
}
