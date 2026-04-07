namespace FlightTracker.Web.Api.Contracts;

public sealed record FlightLookupCandidateApiResponse(
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
    bool IsFromDatabase);
