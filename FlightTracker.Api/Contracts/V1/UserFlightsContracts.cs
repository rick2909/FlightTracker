using FlightTracker.Application.Dtos;

namespace FlightTracker.Api.Contracts.V1;

public sealed record UpdateUserFlightAndScheduleRequest(
    UpdateUserFlightDto UserFlight,
    FlightScheduleUpdateDto Schedule);

public sealed record DeleteUserFlightResponse(
    bool Deleted);

public sealed record HasUserFlownResponse(
    bool HasFlown);
