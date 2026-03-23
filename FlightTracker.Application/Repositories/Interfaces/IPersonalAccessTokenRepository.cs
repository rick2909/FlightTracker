using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Repositories.Interfaces;

public interface IPersonalAccessTokenRepository
{
    Task<PersonalAccessToken> CreateAsync(
        PersonalAccessToken token,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalAccessToken>> ListByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<PersonalAccessToken?> GetByIdAsync(
        int tokenId,
        CancellationToken cancellationToken = default);

    Task<PersonalAccessToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<PersonalAccessToken> UpdateAsync(
        PersonalAccessToken token,
        CancellationToken cancellationToken = default);
}
