using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

public sealed class PersonalAccessTokenDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Label { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public PersonalAccessTokenScopes Scopes { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
