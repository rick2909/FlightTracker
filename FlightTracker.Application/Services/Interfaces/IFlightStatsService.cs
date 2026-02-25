using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Provides grouped flight statistics for a user's passport views.
/// </summary>
public interface IFlightStatsService
{
    /// <summary>
    /// Computes grouped airline and aircraft-type stats
    /// for a user's flown flights.
    /// </summary>
    Task<PassportDetailsDto> GetPassportDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
