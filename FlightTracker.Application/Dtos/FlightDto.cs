using System;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// Lightweight flight listing DTO.
/// </summary>
public record FlightDto
{
    public int Id { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public FlightStatus Status { get; init; }
    public DateTime DepartureTimeUtc { get; init; }
    public DateTime ArrivalTimeUtc { get; init; }
    public int DepartureAirportId { get; init; }
    public int ArrivalAirportId { get; init; }
    public int? AircraftId { get; init; }
    public int? OperatingAirlineId { get; init; }
}
