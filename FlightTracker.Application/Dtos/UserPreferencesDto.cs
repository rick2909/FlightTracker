using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for user display and unit preferences.
/// </summary>
public class UserPreferencesDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    // Display & Units
    public DistanceUnit DistanceUnit { get; set; } = DistanceUnit.Miles;
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;
    public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;
    public DateFormat DateFormat { get; set; } = DateFormat.YearMonthDay;
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
