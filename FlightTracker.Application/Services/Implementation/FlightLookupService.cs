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
}
