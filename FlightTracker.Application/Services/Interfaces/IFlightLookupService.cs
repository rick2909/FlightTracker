using System;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Resolves a Flight by flight number and date, first checking the local DB
/// and later (future) falling back to external providers.
/// </summary>
public interface IFlightLookupService
{
    Task<Flight?> ResolveFlightAsync(string flightNumber, DateOnly date,
        CancellationToken cancellationToken = default);
}
