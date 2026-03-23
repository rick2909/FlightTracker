using FlightTracker.Domain.Enums;

namespace FlightTracker.Web.Api.Contracts;

public sealed record PersonalAccessTokenResponse(
    int Id,
    int UserId,
    string Label,
    string TokenPrefix,
    PersonalAccessTokenScopes Scopes,
    DateTime ExpiresAtUtc,
    DateTime? LastUsedAtUtc,
    DateTime? RevokedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreatePersonalAccessTokenRequest(
    string Label,
    PersonalAccessTokenScopes Scopes,
    DateTime ExpiresAtUtc);

public sealed record CreatePersonalAccessTokenResponse(
    PersonalAccessTokenResponse Token,
    string PlainTextToken);

public sealed record RevokePersonalAccessTokenRequest(
    int TokenId);

public sealed record RevokePersonalAccessTokenResponse(
    bool Revoked);
