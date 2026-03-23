using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Enums;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;

namespace FlightTracker.Web.Api.Clients;

public sealed class PersonalAccessTokensApiClient(HttpClient httpClient)
    : IPersonalAccessTokensApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<PersonalAccessTokenDto>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.ReadRequiredAsync<List<PersonalAccessTokenResponse>>(
            $"/api/v1/users/{userId}/access-tokens",
            cancellationToken);

        return response?.Select(Map).ToArray()
            ?? Array.Empty<PersonalAccessTokenDto>();
    }

    public async Task<CreatePersonalAccessTokenResultDto?> CreateAsync(
        int userId,
        string label,
        PersonalAccessTokenScopes scopes,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendRequiredAsync<CreatePersonalAccessTokenResponse>(
            HttpMethod.Post,
            $"/api/v1/users/{userId}/access-tokens",
            new CreatePersonalAccessTokenRequest(label, scopes, expiresAtUtc),
            cancellationToken);

        if (response is null)
        {
            return null;
        }

        return new CreatePersonalAccessTokenResultDto
        {
            Token = Map(response.Token),
            PlainTextToken = response.PlainTextToken
        };
    }

    public async Task<bool> RevokeAsync(
        int userId,
        int tokenId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendRequiredAsync<RevokePersonalAccessTokenResponse>(
            HttpMethod.Post,
            $"/api/v1/users/{userId}/access-tokens/revoke",
            new RevokePersonalAccessTokenRequest(tokenId),
            cancellationToken);

        return response?.Revoked ?? false;
    }

    private static PersonalAccessTokenDto Map(PersonalAccessTokenResponse response)
    {
        return new PersonalAccessTokenDto
        {
            Id = response.Id,
            UserId = response.UserId,
            Label = response.Label,
            TokenPrefix = response.TokenPrefix,
            Scopes = response.Scopes,
            ExpiresAtUtc = response.ExpiresAtUtc,
            LastUsedAtUtc = response.LastUsedAtUtc,
            RevokedAtUtc = response.RevokedAtUtc,
            CreatedAtUtc = response.CreatedAtUtc,
            UpdatedAtUtc = response.UpdatedAtUtc
        };
    }
}
