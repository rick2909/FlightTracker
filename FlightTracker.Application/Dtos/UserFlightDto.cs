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

    // Airline details
    public int? OperatingAirlineId { get; init; }
    public string? OperatingAirlineIcaoCode { get; init; }
    public string? OperatingAirlineIataCode { get; init; }
    public string? OperatingAirlineName { get; init; }

    // Airport details
    public string DepartureAirportCode { get; init; } = string.Empty;
    public string? DepartureIataCode { get; init; }
    public string? DepartureIcaoCode { get; init; }
    public string DepartureAirportName { get; init; } = string.Empty;
    public string DepartureCity { get; init; } = string.Empty;
    public string ArrivalAirportCode { get; init; } = string.Empty;
    public string? ArrivalIataCode { get; init; }
    public string? ArrivalIcaoCode { get; init; }
    public string ArrivalAirportName { get; init; } = string.Empty;
    public string ArrivalCity { get; init; } = string.Empty;
    public string? DepartureTimeZoneId { get; init; }
    public string? ArrivalTimeZoneId { get; init; }

    // Aircraft details (optional)
    public AircraftDto? Aircraft { get; init; }
}
