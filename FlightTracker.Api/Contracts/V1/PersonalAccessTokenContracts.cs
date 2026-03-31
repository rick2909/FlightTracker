using FlightTracker.Domain.Enums;

namespace FlightTracker.Api.Contracts.V1;

/// <summary>Metadata for an existing personal access token (never includes the raw token value).</summary>
/// <param name="Id">Internal token identifier.</param>
/// <param name="UserId">Owner user identifier.</param>
/// <param name="Label">Human-readable label assigned at creation.</param>
/// <param name="TokenPrefix">First visible characters of the token (e.g. <c>ft_pat_abc…</c>).</param>
/// <param name="Scopes">Bitfield of granted permission scopes.</param>
/// <param name="ExpiresAtUtc">Expiry timestamp in UTC.</param>
/// <param name="LastUsedAtUtc">Most recent successful use timestamp, when available.</param>
/// <param name="RevokedAtUtc">Revocation timestamp in UTC, when revoked.</param>
/// <param name="CreatedAtUtc">Creation timestamp in UTC.</param>
/// <param name="UpdatedAtUtc">Last-modified timestamp in UTC.</param>
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

/// <summary>Request body for creating a new personal access token.</summary>
/// <param name="Label">Descriptive label to identify the token.</param>
/// <param name="Scopes">Permission scopes to grant (combine flags as needed).</param>
/// <param name="ExpiresAtUtc">Desired expiry date and time in UTC.</param>
public sealed record CreatePersonalAccessTokenRequest(
    string Label,
    PersonalAccessTokenScopes Scopes,
    DateTime ExpiresAtUtc);

/// <summary>
/// Response after successfully creating a personal access token.
/// The <see cref="PlainTextToken"/> is shown exactly once and cannot be retrieved again.
/// </summary>
/// <param name="Token">Token metadata.</param>
/// <param name="PlainTextToken">Full raw token value — store it securely; it is not retrievable later.</param>
public sealed record CreatePersonalAccessTokenResponse(
    PersonalAccessTokenResponse Token,
    string PlainTextToken);

/// <summary>Request body for revoking a personal access token.</summary>
/// <param name="TokenId">Identifier of the token to revoke.</param>
public sealed record RevokePersonalAccessTokenRequest(
    int TokenId);

/// <summary>Result of a token revocation request.</summary>
/// <param name="Revoked"><see langword="true"/> if the token was successfully revoked.</param>
public sealed record RevokePersonalAccessTokenResponse(
    bool Revoked);
