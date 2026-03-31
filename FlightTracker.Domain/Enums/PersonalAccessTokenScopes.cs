namespace FlightTracker.Domain.Enums;

[Flags]
public enum PersonalAccessTokenScopes
{
    None = 0,
    ReadFlights = 1,
    WriteFlights = 2,
    ReadStats = 4
}
