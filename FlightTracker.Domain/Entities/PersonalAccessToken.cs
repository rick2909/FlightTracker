using FlightTracker.Domain.Enums;

namespace FlightTracker.Domain.Entities;

public class PersonalAccessToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Label { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public PersonalAccessTokenScopes Scopes { get; set; } = PersonalAccessTokenScopes.ReadFlights;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsRevoked => RevokedAtUtc.HasValue;
}
