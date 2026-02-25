using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Provides aggregated Passport data for a user.
/// </summary>
public interface IPassportService
{
    Task<Result<PassportDataDto>> GetPassportDataAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Computes grouped stats for a user's flown flights.
    /// </summary>
    Task<Result<PassportDetailsDto>> GetPassportDetailsAsync(int userId, CancellationToken cancellationToken = default);
}
