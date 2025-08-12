using FlightTracker.Domain.Enums;
using System;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for user flight experience data transfer.
/// </summary>
public record UserFlightDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public int FlightId { get; init; }
    public FlightClass FlightClass { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public DateTime BookedOnUtc { get; init; }
    public string? Notes { get; init; }
    public bool DidFly { get; init; }
    
    // Flight details (flattened for easier display)
    public string FlightNumber { get; init; } = string.Empty;
    public FlightStatus FlightStatus { get; init; }
    public DateTime DepartureTimeUtc { get; init; }
    public DateTime ArrivalTimeUtc { get; init; }
    
    // Airport details
    public string DepartureAirportCode { get; init; } = string.Empty;
    public string DepartureAirportName { get; init; } = string.Empty;
    public string DepartureCity { get; init; } = string.Empty;
    public string ArrivalAirportCode { get; init; } = string.Empty;
    public string ArrivalAirportName { get; init; } = string.Empty;
    public string ArrivalCity { get; init; } = string.Empty;
    
    // Aircraft details (optional)
    public string? AircraftRegistration { get; init; }
    public string? AircraftModel { get; init; }
    public AircraftManufacturer? AircraftManufacturer { get; init; }
}
