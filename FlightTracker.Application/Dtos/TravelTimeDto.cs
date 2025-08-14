using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for user flight statistics.
/// </summary>
public record TravelTimeDto
{
    public int TotalTravelTimeInMinutes { get; init; }
    public FlightClass FlightClass { get; init; }
}
