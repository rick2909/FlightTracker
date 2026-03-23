using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

public sealed class CreatePersonalAccessTokenDto
{
    public string Label { get; set; } = string.Empty;

    public PersonalAccessTokenScopes Scopes { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
