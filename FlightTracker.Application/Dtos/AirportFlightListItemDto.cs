using System;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// Lightweight item for airport flight listings shown on the Airports page.
/// Route string can be composed in UI from DepartureCode and ArrivalCode.
/// </summary>
public sealed record AirportFlightListItemDto
{
    public int? Id { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public string? Airline { get; init; }
    public string? Aircraft { get; init; }
    public string? DepartureTimeUtc { get; init; }
    public string? ArrivalTimeUtc { get; init; }
    public string? DepartureCode { get; init; }
    public string? ArrivalCode { get; init; }
}
