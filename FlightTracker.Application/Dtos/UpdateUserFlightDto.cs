using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for updating a user flight experience fields (non-schedule).
/// </summary>
public record UpdateUserFlightDto
{
    /// <summary>
    /// The seat number assigned to the user for the flight.
    /// </summary>
    public string SeatNumber { get; init; } = string.Empty;

    /// <summary>
    /// The class of the flight (e.g., Economy, Business).
    /// </summary>
    public FlightClass FlightClass { get; init; } = FlightClass.Economy;

    /// <summary>
    /// Additional notes about the flight experience.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Indicates whether the user actually flew on this flight.
    /// </summary>
    public bool DidFly { get; init; } = true;
}
