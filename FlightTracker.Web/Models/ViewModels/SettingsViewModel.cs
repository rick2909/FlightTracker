using System.ComponentModel.DataAnnotations;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Web.Models.ViewModels;

public class SettingsViewModel
{
    // Profile
    [Required]
    [Display(Name = "Full Name")]
    [StringLength(64, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Preferences
    [RegularExpression("^(public|private)$")]
    public string ProfileVisibility { get; set; } = "private";

    [RegularExpression("^(light|dark|system)$")]
    public string Theme { get; set; } = "system";

    public PreferencesViewModel Preferences => new() { ProfileVisibility = ProfileVisibility, Theme = Theme };
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class PreferencesViewModel
{
    [RegularExpression("^(public|private)$")]
    public string ProfileVisibility { get; set; } = "private";

    [RegularExpression("^(light|dark|system)$")]
    public string Theme { get; set; } = "system";

    // Display & Units
    public DistanceUnit DistanceUnit { get; set; } = DistanceUnit.Miles;

    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;

    public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;

    public DateFormat DateFormat { get; set; } = DateFormat.YearMonthDay;
    
    // Privacy & Sharing
    public ProfileVisibilityLevel ProfileVisibilityLevel { get; set; } = ProfileVisibilityLevel.Private;
    
    public bool ShowTotalMiles { get; set; } = true;
    
    public bool ShowAirlines { get; set; } = true;
    
    public bool ShowCountries { get; set; } = true;
    
    public bool ShowMapRoutes { get; set; } = true;
    
    public bool EnableActivityFeed { get; set; } = false;
}
