using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Repositories.Interfaces;
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

    public async Task<Flight?> GetFlightByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _flightRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<Flight>> GetUpcomingFlightsAsync(DateTime fromUtc, TimeSpan? window = null, CancellationToken cancellationToken = default)
    {
        var allFlights = await _flightRepository.GetAllAsync(cancellationToken);

        var query = allFlights.Where(f => f.DepartureTimeUtc >= fromUtc);

        if (window.HasValue)
        {
            var endTime = fromUtc.Add(window.Value);
            query = query.Where(f => f.DepartureTimeUtc <= endTime);
        }

        return query.OrderBy(f => f.DepartureTimeUtc).ToList();
    }

    public async Task<Flight> AddFlightAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        return await _flightRepository.AddAsync(flight, cancellationToken);
    }

    public async Task UpdateFlightAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        await _flightRepository.UpdateAsync(flight, cancellationToken);
    }

    public async Task DeleteFlightAsync(int id, CancellationToken cancellationToken = default)
    {
        await _flightRepository.DeleteAsync(id, cancellationToken);
    }
}
