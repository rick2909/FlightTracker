using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for updating a user flight experience fields (non-schedule).
/// </summary>
public record UpdateUserFlightDto
{
    public FlightClass FlightClass { get; init; } = FlightClass.Economy;
    public string SeatNumber { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool DidFly { get; init; } = true;
}
