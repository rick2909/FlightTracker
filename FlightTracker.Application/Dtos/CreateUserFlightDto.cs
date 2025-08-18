using FlightTracker.Domain.Enums;
using System;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for creating or updating a user flight experience.
/// </summary>
public record CreateUserFlightDto
{
    public int FlightId { get; init; }
    public string? FlightNumber { get; init; }
    public DateOnly? FlightDate { get; init; }
    public FlightClass FlightClass { get; init; } = FlightClass.Economy;
    public string SeatNumber { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
