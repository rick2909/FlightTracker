namespace FlightTracker.Domain.Enums;

/// <summary>
/// Represents the current status of a flight.
/// </summary>
public enum FlightStatus
{
    Scheduled = 1,
    Delayed = 2,
    Boarding = 3,
    Departed = 4,
    InFlight = 5,
    Landed = 6,
    Cancelled = 7,
    Diverted = 8
}
