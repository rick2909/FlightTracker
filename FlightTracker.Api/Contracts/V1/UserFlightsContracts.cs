using FlightTracker.Application.Dtos;

namespace FlightTracker.Api.Contracts.V1;

/// <summary>
/// Request body for updating a user-flight record and its associated schedule in one operation.
/// </summary>
/// <param name="UserFlight">Updated user-flight fields (class, seat, notes, etc.).</param>
/// <param name="Schedule">Updated flight schedule fields (times, airports, etc.).</param>
public sealed record UpdateUserFlightAndScheduleRequest(
    UpdateUserFlightDto UserFlight,
    FlightScheduleUpdateDto Schedule);

/// <summary>Result of a user-flight deletion request.</summary>
/// <param name="Deleted"><see langword="true"/> if the record was successfully deleted.</param>
public sealed record DeleteUserFlightResponse(
    bool Deleted);

/// <summary>Indicates whether a user has a tracked flight for a given flight.</summary>
/// <param name="HasFlown"><see langword="true"/> if at least one user-flight record exists.</param>
public sealed record HasUserFlownResponse(
    bool HasFlown);

/// <summary>Change payload returned when flight lookup detects schedule or route updates.</summary>
public sealed record UserFlightLookupRefreshChangesResponse(
    string? FlightNumber,
    DateTime? DepartureTimeUtc,
    DateTime? ArrivalTimeUtc,
    int? DepartureAirportId,
    int? ArrivalAirportId,
    string? DepartureAirportCode,
    string? ArrivalAirportCode);

/// <summary>Response of a user-flight lookup refresh operation.</summary>
public sealed record UserFlightLookupRefreshResponse(
    string Status,
    string? Message,
    UserFlightLookupRefreshChangesResponse? Changes);
