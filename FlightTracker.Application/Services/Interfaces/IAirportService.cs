using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Business operations and queries related to <see cref="Airport"/> entities.
/// </summary>
public interface IAirportService
{
    Task<Airport?> GetAirportByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Airport>> GetAllAirportsAsync(CancellationToken cancellationToken = default);
    Task<Airport> AddAirportAsync(Airport airport, CancellationToken cancellationToken = default);
    Task UpdateAirportAsync(Airport airport, CancellationToken cancellationToken = default);
    Task DeleteAirportAsync(int id, CancellationToken cancellationToken = default);
}
