using FlightTracker.Domain.Enums;

namespace FlightTracker.Domain.Entities;

/// <summary>
/// User display and unit preferences.
/// Stored per user; defaults set on first access.
/// </summary>
public class UserPreferences
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to ApplicationUser (Identity).
    /// One-to-one relationship.
    /// </summary>
    public int UserId { get; set; }

    // Display & Units
    public DistanceUnit DistanceUnit { get; set; } = DistanceUnit.Miles;

    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;

    public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;

    public DateFormat DateFormat { get; set; } = DateFormat.YearMonthDay;

    // Audit
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}