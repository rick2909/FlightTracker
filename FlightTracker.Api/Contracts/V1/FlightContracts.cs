using FlightTracker.Domain.Enums;

namespace FlightTracker.Api.Contracts.V1;

public sealed record FlightResponse(
    int Id,
    string FlightNumber,
    FlightStatus Status,
    DateTime DepartureTimeUtc,
    DateTime ArrivalTimeUtc,
    int DepartureAirportId,
    int ArrivalAirportId,
    int? AircraftId,
    int? OperatingAirlineId);

public sealed record FlightsQuery(
    DateTime? FromUtc,
    int? WindowHours);
