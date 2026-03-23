using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Services.Interfaces;

public interface IPersonalAccessTokenService
{
    Task<Result<CreatePersonalAccessTokenResultDto>> CreateAsync(
        int userId,
        CreatePersonalAccessTokenDto request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PersonalAccessTokenDto>>> ListByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RevokeAsync(
        int userId,
        int tokenId,
        CancellationToken cancellationToken = default);

    Task<Result<PersonalAccessTokenDto?>> ValidateTokenAsync(
        string plainTextToken,
        PersonalAccessTokenScopes requiredScopes,
        CancellationToken cancellationToken = default);

    Task<Result> RecordUsageAsync(
        int tokenId,
        CancellationToken cancellationToken = default);
}
