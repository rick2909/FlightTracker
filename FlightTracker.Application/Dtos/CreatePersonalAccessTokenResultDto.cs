namespace FlightTracker.Application.Dtos;

public sealed class CreatePersonalAccessTokenResultDto
{
    public PersonalAccessTokenDto Token { get; set; } = new();

    public string PlainTextToken { get; set; } = string.Empty;
}
