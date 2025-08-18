using System;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

public class FlightLookupService(IFlightRepository flights) : IFlightLookupService
{
    public async Task<Flight?> ResolveFlightAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flightNumber)) return null;
        // First: check local DB
        var local = await flights.GetByFlightNumberAndDateAsync(flightNumber.Trim().ToUpperInvariant(), date, cancellationToken);
        if (local != null) return local;

        // TODO: External API lookup (OpenSky/FR24) via abstraction when available
        return null;
    }

    public Task<IReadOnlyList<Flight>> SearchByFlightNumberAsync(string flightNumber, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        return flights.SearchByFlightNumberAsync(flightNumber, date, cancellationToken);
    }

    public Task<IReadOnlyList<Flight>> SearchByRouteAsync(string? departure, string? arrival, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        return flights.SearchByRouteAsync(departure, arrival, date, cancellationToken);
    }
}
