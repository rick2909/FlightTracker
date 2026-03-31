using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Web.Services.Interfaces;

public interface IPersonalAccessTokensApiClient
{
    Task<IReadOnlyList<PersonalAccessTokenDto>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<CreatePersonalAccessTokenResultDto?> CreateAsync(
        int userId,
        string label,
        PersonalAccessTokenScopes scopes,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        int userId,
        int tokenId,
        CancellationToken cancellationToken = default);
}
