using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Services.Interfaces;

public interface IPassportApiClient
{
    Task<PassportDataDto?> GetPassportDataAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<PassportDetailsDto?> GetPassportDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
