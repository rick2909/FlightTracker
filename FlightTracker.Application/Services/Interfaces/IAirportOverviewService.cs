using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Use-case service to provide flights overview for an airport (departures/arrivals),
/// merging DB and live sources as requested.
/// </summary>
public interface IAirportOverviewService
{
    /// <summary>
    /// Gets departing/arriving flights for an airport code (IATA/ICAO/name fallback).
    /// dir: "departing" | "arriving" | null for both.
    /// live: when true, include live datasets and merge with DB.
    /// limit: maximum number per list.
    /// </summary>
    Task<AirportFlightsResultDto> GetFlightsAsync(
        string code,
        string? dir,
        bool live,
        int limit,
        CancellationToken cancellationToken = default);
}
