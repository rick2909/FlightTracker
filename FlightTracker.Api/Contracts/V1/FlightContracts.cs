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
