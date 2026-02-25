using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Results;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Business operations and queries related to <see cref="Airport"/> entities.
/// </summary>
public interface IAirportService
{
    Task<Result<Airport>> GetAirportByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Airport>>> GetAllAirportsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<Airport>> AddAirportAsync(
        Airport airport,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAirportAsync(
        Airport airport,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAirportAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<string?>> GetTimeZoneIdByAirportCodeAsync(
        string airportCode,
        CancellationToken cancellationToken = default);
}
