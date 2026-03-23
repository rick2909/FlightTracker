using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Api.Contracts;

public sealed record UpdateUserFlightAndScheduleRequest(
    UpdateUserFlightDto UserFlight,
    FlightScheduleUpdateDto Schedule);

public sealed record DeleteUserFlightResponse(
    bool Deleted);

public sealed record HasUserFlownResponse(
    bool HasFlown);
