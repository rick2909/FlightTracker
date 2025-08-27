using System.Collections.Generic;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// Result DTO containing departing and arriving flight lists for an airport.
/// </summary>
public sealed record AirportFlightsResultDto
{
    public IEnumerable<AirportFlightListItemDto> Departing { get; init; } = new List<AirportFlightListItemDto>();
    public IEnumerable<AirportFlightListItemDto> Arriving { get; init; } = new List<AirportFlightListItemDto>();
}
