namespace FlightTracker.Application.Dtos;

/// <summary>
/// Candidate flight returned by designator lookup for quick-add flows.
/// </summary>
public sealed record FlightLookupCandidateDto(
    int? FlightId,
    string FlightNumber,
    string Callsign,
    DateTime? DepartureTimeUtc,
    DateTime? ArrivalTimeUtc,
    string? DepartureCode,
    string? ArrivalCode,
    string? DepartureAirportName,
    string? ArrivalAirportName,
    string? AirlineIataCode,
    string? AirlineIcaoCode,
    string? AirlineName,
    bool IsFromDatabase
);
