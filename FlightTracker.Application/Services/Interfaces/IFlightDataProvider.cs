using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Abstraction over external live flight data providers (e.g., OpenSky, FR24).
/// Implementations should handle rate limiting and transient faults internally.
/// </summary>
public interface IFlightDataProvider
{
    /// <summary>
    /// Returns flights within a geographic bounding box.
    /// </summary>
    Task<IReadOnlyList<Flight>> GetFlightsInBoundingBoxAsync(
        double minLatitude,
        double minLongitude,
        double maxLatitude,
        double maxLongitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a flight by designator and optional earliest departure time (UTC).
    /// </summary>
    Task<Flight?> GetFlightByNumberAsync(
        string flightNumber,
        DateTime? departureAfterUtc = null,
        CancellationToken cancellationToken = default);
}
