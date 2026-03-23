using FlightTracker.Domain.Enums;

namespace FlightTracker.Api.Contracts.V1;

public sealed record UserPreferencesResponse(
    int Id,
    int UserId,
    DistanceUnit DistanceUnit,
    TemperatureUnit TemperatureUnit,
    TimeFormat TimeFormat,
    DateFormat DateFormat,
    ProfileVisibilityLevel ProfileVisibility,
    bool ShowTotalMiles,
    bool ShowAirlines,
    bool ShowCountries,
    bool ShowMapRoutes,
    bool EnableActivityFeed,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record UpdateUserPreferencesRequest(
    DistanceUnit DistanceUnit,
    TemperatureUnit TemperatureUnit,
    TimeFormat TimeFormat,
    DateFormat DateFormat,
    ProfileVisibilityLevel ProfileVisibility,
    bool ShowTotalMiles,
    bool ShowAirlines,
    bool ShowCountries,
    bool ShowMapRoutes,
    bool EnableActivityFeed);
