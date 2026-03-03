using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Infrastructure.External;

/// <summary>
/// Minimal stub of an external provider client; returns empty results for now.
/// </summary>
public class OpenSkyClient : IFlightDataProvider
{
    public Task<Result<IReadOnlyList<Flight>>> GetFlightsInBoundingBoxAsync(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Flight> empty = Array.Empty<Flight>();
        return Task.FromResult(Result<IReadOnlyList<Flight>>.Success(empty));
    }

    public Task<Result<Flight>> GetFlightByNumberAsync(string flightNumber, DateTime? departureAfterUtc = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<Flight>.Success(null));
    }
}
