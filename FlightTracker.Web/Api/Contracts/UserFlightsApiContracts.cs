using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Api.Contracts;

public sealed record UpdateUserFlightAndScheduleRequest(
    UpdateUserFlightDto UserFlight,
    FlightScheduleUpdateDto Schedule);

public sealed record DeleteUserFlightResponse(
    bool Deleted);

public sealed record HasUserFlownResponse(
    bool HasFlown);

public sealed record UserFlightLookupRefreshChangesResponse(
    string? FlightNumber,
    DateTime? DepartureTimeUtc,
    DateTime? ArrivalTimeUtc,
    int? DepartureAirportId,
    int? ArrivalAirportId,
    string? DepartureAirportCode,
    string? ArrivalAirportCode);

public sealed record UserFlightLookupRefreshResponse(
    string Status,
    string? Message,
    UserFlightLookupRefreshChangesResponse? Changes);
