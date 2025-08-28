using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Provides aggregated Passport data for a user.
/// </summary>
public interface IPassportService
{
    Task<PassportDataDto> GetPassportDataAsync(int userId, CancellationToken cancellationToken = default);
}
