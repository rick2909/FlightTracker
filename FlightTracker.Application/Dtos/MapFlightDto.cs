using System;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO used for map rendering (simplified flight path representation).
/// </summary>
public record MapFlightDto
{
    public int FlightId { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public DateTime DepartureTimeUtc { get; init; }
    public DateTime ArrivalTimeUtc { get; init; }
    public string DepartureAirportCode { get; init; } = string.Empty;
    public string ArrivalAirportCode { get; init; } = string.Empty;
    public double? DepartureLat { get; init; }
    public double? DepartureLon { get; init; }
    public double? ArrivalLat { get; init; }
    public double? ArrivalLon { get; init; }
    public bool IsUpcoming { get; init; }
}
