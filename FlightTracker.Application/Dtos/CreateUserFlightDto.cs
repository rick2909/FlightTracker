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
    public bool DidFly { get; init; } = true;

    // Optional fields to allow creating a new Flight atomically when FlightId is not provided
    public string? DepartureAirportCode { get; init; }
    public string? ArrivalAirportCode { get; init; }
    public DateTime? DepartureTimeUtc { get; init; }
    public DateTime? ArrivalTimeUtc { get; init; }

    // Aircraft fields
    /// <summary>
    /// Aircraft registration (tail number). If provided and exists, fills all aircraft details and links to flight.
    /// If provided but doesn't exist, creates a new aircraft record.
    /// </summary>
    public string? AircraftRegistration { get; init; }

    /// <summary>
    /// Optional operating airline IATA or ICAO code. Auto-filled from flight number if not provided.
    /// Can be manually overridden.
    /// </summary>
    public string? OperatingAirlineCode { get; init; }
}
