using FlightTracker.Domain.Enums;

namespace FlightTracker.Api.Contracts.V1;

/// <summary>Current display and privacy preferences for a user.</summary>
/// <param name="Id">Internal preferences record identifier.</param>
/// <param name="UserId">Owner user identifier.</param>
/// <param name="DistanceUnit">Preferred distance unit (kilometres or miles).</param>
/// <param name="TemperatureUnit">Preferred temperature unit (Celsius or Fahrenheit).</param>
/// <param name="TimeFormat">Preferred time format (12h or 24h).</param>
/// <param name="DateFormat">Preferred date display format.</param>
/// <param name="ProfileVisibility">Who can view this user's public profile.</param>
/// <param name="ShowTotalMiles">Whether to display total air miles on the profile.</param>
/// <param name="ShowAirlines">Whether to display airline history on the profile.</param>
/// <param name="ShowCountries">Whether to display visited countries on the profile.</param>
/// <param name="ShowMapRoutes">Whether to show a route map on the profile.</param>
/// <param name="EnableActivityFeed">Whether the user's activity appears in public feeds.</param>
/// <param name="CreatedAtUtc">Record creation timestamp in UTC.</param>
/// <param name="UpdatedAtUtc">Last-modified timestamp in UTC.</param>
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

/// <summary>Request body for updating a user's display and privacy preferences.</summary>
/// <param name="DistanceUnit">Preferred distance unit.</param>
/// <param name="TemperatureUnit">Preferred temperature unit.</param>
/// <param name="TimeFormat">Preferred time format.</param>
/// <param name="DateFormat">Preferred date format.</param>
/// <param name="ProfileVisibility">Profile visibility level.</param>
/// <param name="ShowTotalMiles">Whether to show total miles on the profile.</param>
/// <param name="ShowAirlines">Whether to show airline history on the profile.</param>
/// <param name="ShowCountries">Whether to show visited countries on the profile.</param>
/// <param name="ShowMapRoutes">Whether to show a route map on the profile.</param>
/// <param name="EnableActivityFeed">Whether activity should appear in public feeds.</param>
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
