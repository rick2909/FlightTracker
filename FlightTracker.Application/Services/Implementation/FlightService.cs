using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service implementation for flight-related business operations.
/// </summary>
public class FlightService(IFlightRepository flightRepository, ILogger<FlightService> logger) : IFlightService
{
    private readonly IFlightRepository _flightRepository = flightRepository;
    private readonly ILogger<FlightService> _logger = logger;

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
            _logger.LogError(ex, "Error loading flight with ID {FlightId}", id);
            return Result<Flight>.Failure(
            "Unable to load the requested flight.",
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
            _logger.LogError(ex, "Error loading upcoming flights from {FromUtc} with window {Window}", fromUtc, window);
            return Result<IReadOnlyList<Flight>>.Failure(
            "Unable to load upcoming flights.",
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
            _logger.LogError(ex, "Error adding flight with number {FlightNumber}", flight.FlightNumber);
            return Result<Flight>.Failure(
            "Unable to add the flight.",
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
            _logger.LogError(ex, "Error updating flight with ID {FlightId}", flight.Id);
            return Result.Failure(
            "Unable to update the flight.",
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
            _logger.LogError(ex, "Error deleting flight with ID {FlightId}", id);
            return Result.Failure(
                "Unable to delete the flight.",
                "flight.delete.failed");
        }
    }
}
