using FlightTracker.Web.Api.Contracts;

namespace FlightTracker.Web.Services.Interfaces;

public interface IAccountApiClient
{
    Task<AccountProfileResponse?> GetAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<AccountProfileResponse> UpdateAsync(
        int userId,
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportFlightsCsvAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportAllJsonAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<DeleteAccountResponse> DeleteAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
