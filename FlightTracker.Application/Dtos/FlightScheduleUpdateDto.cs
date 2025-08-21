using System;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for updating a flight's schedule and route using codes.
/// </summary>
public record FlightScheduleUpdateDto
{
    public int FlightId { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public string DepartureAirportCode { get; init; } = string.Empty;
    public string ArrivalAirportCode { get; init; } = string.Empty;
    public DateTime DepartureTimeUtc { get; init; }
    public DateTime ArrivalTimeUtc { get; init; }
}
