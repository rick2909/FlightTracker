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

    public PreferencesViewModel Preferences { get; set; } = new();

    public List<PersonalAccessTokenViewModel> PersonalAccessTokens { get; set; } = [];

    public string? CreatedAccessTokenValue { get; set; }
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

public class PersonalAccessTokenViewModel
{
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public PersonalAccessTokenScopes Scopes { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
}

public class CreatePersonalAccessTokenViewModel
{
    [Required]
    [StringLength(128, MinimumLength = 3)]
    public string Label { get; set; } = string.Empty;

    public bool ScopeReadFlights { get; set; } = true;

    public bool ScopeWriteFlights { get; set; }

    public bool ScopeReadStats { get; set; }

    [Range(1, 365)]
    public int ExpiresInDays { get; set; } = 30;
}
