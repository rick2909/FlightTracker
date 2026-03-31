using System.Net.Http.Json;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;

namespace FlightTracker.Web.Api.Clients;

public class AccountApiClient(HttpClient httpClient) : IAccountApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<AccountProfileResponse?> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<AccountProfileResponse>(
            $"api/v1/users/{userId}/account",
            cancellationToken);
    }

    public async Task<AccountProfileResponse> UpdateAsync(
        int userId,
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.SendRequiredAsync<AccountProfileResponse>(
                   HttpMethod.Put,
                   $"api/v1/users/{userId}/account",
                   request,
                   cancellationToken)
               ?? throw new InvalidOperationException("Profile update returned no content.");
    }

    public async Task ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await _httpClient.SendRequiredAsync<object>(
            HttpMethod.Post,
            $"api/v1/users/{userId}/account/change-password",
            request,
            cancellationToken);
    }

    public async Task<byte[]> ExportFlightsCsvAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/users/{userId}/account/export/flights.csv",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> ExportAllJsonAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/users/{userId}/account/export/all.json",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<DeleteAccountResponse> DeleteAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.SendRequiredAsync<DeleteAccountResponse>(
                   HttpMethod.Delete,
                   $"api/v1/users/{userId}/account",
                   null,
                   cancellationToken)
               ?? throw new InvalidOperationException("Delete account returned no content.");
    }
}
