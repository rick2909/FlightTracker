using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service implementation for flight-related business operations.
/// </summary>
public class FlightService : IFlightService
{
    private readonly IFlightRepository _flightRepository;

    public FlightService(IFlightRepository flightRepository)
    {
        _flightRepository = flightRepository;
    }

    public async Task<Result<Flight>> GetFlightByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var flight = await _flightRepository.GetByIdAsync(
                id,
                cancellationToken);

            return Result<Flight>.Success(flight);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<Flight>.Failure(
                ex.Message,
                "flight.by_id.load_failed");
        }
    }

    public async Task<Result<IReadOnlyList<Flight>>> GetUpcomingFlightsAsync(DateTime fromUtc, TimeSpan? window = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var allFlights = await _flightRepository.GetAllAsync(cancellationToken);

            var query = allFlights.Where(f => f.DepartureTimeUtc >= fromUtc);

            if (window.HasValue)
            {
                var endTime = fromUtc.Add(window.Value);
                query = query.Where(f => f.DepartureTimeUtc <= endTime);
            }

            var flights = query.OrderBy(f => f.DepartureTimeUtc).ToList();
            return Result<IReadOnlyList<Flight>>.Success(flights);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<Flight>>.Failure(
                ex.Message,
                "flight.upcoming.load_failed");
        }
    }

    public async Task<Result<Flight>> AddFlightAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _flightRepository.AddAsync(
                flight,
                cancellationToken);

            return Result<Flight>.Success(created);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<Flight>.Failure(
                ex.Message,
                "flight.add.failed");
        }
    }

    public async Task<Result> UpdateFlightAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        try
        {
            await _flightRepository.UpdateAsync(flight, cancellationToken);
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
                "flight.update.failed");
        }
    }

    public async Task<Result> DeleteFlightAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _flightRepository.DeleteAsync(id, cancellationToken);
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
                "flight.delete.failed");
        }
    }
}
