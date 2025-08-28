using System;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// Lightweight DTO for live/real-time flight data from external providers.
/// Does not use internal IDs; uses codes and names for safe presentation.
/// </summary>
public record LiveFlightDto
{
    public string FlightNumber { get; init; } = string.Empty;
    public FlightStatus? Status { get; init; }

    public string? AirlineName { get; init; }
    public string? AirlineIata { get; init; }
    public string? AirlineIcao { get; init; }

    public string? DepartureIata { get; init; }
    public string? DepartureIcao { get; init; }
    public DateTime? DepartureScheduledUtc { get; init; }
    public DateTime? DepartureActualUtc { get; init; }

    public string? ArrivalIata { get; init; }
    public string? ArrivalIcao { get; init; }
    public DateTime? ArrivalScheduledUtc { get; init; }
    public DateTime? ArrivalActualUtc { get; init; }

    public string? AircraftRegistration { get; init; }
    public string? AircraftIcaoType { get; init; }
}
