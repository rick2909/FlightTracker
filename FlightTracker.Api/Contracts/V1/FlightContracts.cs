using FlightTracker.Domain.Enums;

namespace FlightTracker.Api.Contracts.V1;

/// <summary>Core flight data returned by the API.</summary>
/// <param name="Id">Internal flight identifier.</param>
/// <param name="FlightNumber">IATA or ICAO flight number.</param>
/// <param name="Status">Current operational status of the flight.</param>
/// <param name="DepartureTimeUtc">Scheduled departure time in UTC.</param>
/// <param name="ArrivalTimeUtc">Scheduled arrival time in UTC.</param>
/// <param name="DepartureAirportId">Internal ID of the departure airport.</param>
/// <param name="ArrivalAirportId">Internal ID of the arrival airport.</param>
/// <param name="AircraftId">Internal aircraft ID, when assigned.</param>
/// <param name="OperatingAirlineId">Internal ID of the operating airline, when known.</param>
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

/// <summary>Query parameters for upcoming-flights requests.</summary>
/// <param name="FromUtc">Start of the search window (UTC). Defaults to now when omitted.</param>
/// <param name="WindowHours">Length of the search window in hours. Uses service default when omitted.</param>
public sealed record FlightsQuery(
    DateTime? FromUtc,
    int? WindowHours);

/// <summary>Query parameters for designator-based flight lookup.</summary>
/// <param name="Designator">Flight number or callsign.</param>
/// <param name="Date">Optional date filter applied to local DB results only.</param>
public sealed record FlightLookupQuery(
    string Designator,
    DateOnly? Date);

/// <summary>A quick-add candidate derived from DB and/or ADSBDB lookups.</summary>
/// <param name="FlightId">Internal flight ID when candidate exists in the DB.</param>
/// <param name="FlightNumber">Flight number/designator.</param>
/// <param name="Callsign">Resolved callsign used by ADSBDB.</param>
/// <param name="DepartureTimeUtc">Known departure time (DB candidates).</param>
/// <param name="ArrivalTimeUtc">Known arrival time (DB candidates).</param>
/// <param name="DepartureCode">Departure airport IATA/ICAO code when known.</param>
/// <param name="ArrivalCode">Arrival airport IATA/ICAO code when known.</param>
/// <param name="DepartureAirportName">Departure airport name when known.</param>
/// <param name="ArrivalAirportName">Arrival airport name when known.</param>
/// <param name="AirlineIataCode">Operating airline IATA code when known.</param>
/// <param name="AirlineIcaoCode">Operating airline ICAO code when known.</param>
/// <param name="AirlineName">Operating airline name when known.</param>
/// <param name="IsFromDatabase">True when sourced from local DB, false when from ADSBDB.</param>
public sealed record FlightLookupCandidateResponse(
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
