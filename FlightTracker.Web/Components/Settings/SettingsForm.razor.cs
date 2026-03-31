using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Enums;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Models.ViewModels;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace FlightTracker.Web.Components.Settings;

public partial class SettingsForm
{
    private class ProfileModel
    {
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class PasswordModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    private record Option<T>(string Text, T Value);

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private ILogger<SettingsForm> Logger { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IAccountApiClient AccountApiClient { get; set; } = default!;

    [Inject]
    private IUserPreferencesApiClient UserPreferencesApiClient { get; set; } = default!;

    [Inject]
    private IPersonalAccessTokensApiClient PersonalAccessTokensApiClient { get; set; } = default!;

    [Parameter]
    public int UserId { get; set; }

    [Parameter]
    public string FullName { get; set; } = string.Empty;

    [Parameter]
    public string UserName { get; set; } = string.Empty;

    [Parameter]
    public string Email { get; set; } = string.Empty;

    [Parameter]
    public string ProfileVisibility { get; set; } = "private";

    [Parameter]
    public string Theme { get; set; } = "system";

    [Parameter]
    public DistanceUnit DistanceUnit { get; set; } = DistanceUnit.Miles;

    [Parameter]
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;

    [Parameter]
    public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;

    [Parameter]
    public DateFormat DateFormat { get; set; } = DateFormat.YearMonthDay;

    [Parameter]
    public ProfileVisibilityLevel ProfileVisibilityLevel { get; set; } = ProfileVisibilityLevel.Private;

    [Parameter]
    public bool ShowTotalMiles { get; set; } = true;

    [Parameter]
    public bool ShowAirlines { get; set; } = true;

    [Parameter]
    public bool ShowCountries { get; set; } = true;

    [Parameter]
    public bool ShowMapRoutes { get; set; } = true;

    [Parameter]
    public bool EnableActivityFeed { get; set; }

    [Parameter]
    public IEnumerable<PersonalAccessTokenViewModel>? PersonalAccessTokens { get; set; }

    private readonly ProfileModel profile = new();
    private PasswordModel pwd = new();
    private string visibility = "private";
    private string theme = "system";
    private DistanceUnit distanceUnit = DistanceUnit.Miles;
    private TemperatureUnit temperatureUnit = TemperatureUnit.Celsius;
    private TimeFormat timeFormat = TimeFormat.TwentyFourHour;
    private DateFormat dateFormat = DateFormat.YearMonthDay;
    private ProfileVisibilityLevel profileVisibilityLevel = ProfileVisibilityLevel.Private;
    private bool showTotalMiles = true;
    private bool showAirlines = true;
    private bool showCountries = true;
    private bool showMapRoutes = true;
    private bool enableActivityFeed;
    private string tokenLabel = string.Empty;
    private int tokenExpiresInDays = 30;
    private bool scopeReadFlights = true;
    private bool scopeWriteFlights;
    private bool scopeReadStats;
    private List<PersonalAccessTokenViewModel> accessTokens = [];
    private string? createdTokenValue;

    private readonly IReadOnlyList<string> themeOptions = ["system", "light", "dark"];
    private readonly IReadOnlyList<Option<DistanceUnit>> distanceUnitOptions =
    [
        new("Miles (mi)", DistanceUnit.Miles),
        new("Kilometers (km)", DistanceUnit.Kilometers),
        new("Nautical Miles (NM)", DistanceUnit.NauticalMiles)
    ];
    private readonly IReadOnlyList<Option<TemperatureUnit>> temperatureUnitOptions =
    [
        new("Celsius (°C)", TemperatureUnit.Celsius),
        new("Fahrenheit (°F)", TemperatureUnit.Fahrenheit)
    ];
    private readonly IReadOnlyList<Option<TimeFormat>> timeFormatOptions =
    [
        new("24-hour (14:30)", TimeFormat.TwentyFourHour),
        new("12-hour (2:30 PM)", TimeFormat.TwelveHour)
    ];
    private readonly IReadOnlyList<Option<DateFormat>> dateFormatOptions =
    [
        new("DD/MM/YYYY (25/12/2025)", DateFormat.DayMonthYear),
        new("MM/DD/YYYY (12/25/2025)", DateFormat.MonthDayYear),
        new("YYYY-MM-DD (2025-12-25)", DateFormat.YearMonthDay)
    ];

    protected override void OnParametersSet()
    {
        profile.FullName = FullName ?? string.Empty;
        profile.UserName = UserName ?? string.Empty;
        profile.Email = Email ?? string.Empty;
        visibility = string.IsNullOrWhiteSpace(ProfileVisibility) ? "private" : ProfileVisibility;
        theme = string.IsNullOrWhiteSpace(Theme) ? "system" : Theme;
        distanceUnit = DistanceUnit;
        temperatureUnit = TemperatureUnit;
        timeFormat = TimeFormat;
        dateFormat = DateFormat;
        profileVisibilityLevel = ProfileVisibilityLevel;
        showTotalMiles = ShowTotalMiles;
        showAirlines = ShowAirlines;
        showCountries = ShowCountries;
        showMapRoutes = ShowMapRoutes;
        enableActivityFeed = EnableActivityFeed;
        accessTokens = PersonalAccessTokens?.ToList() ?? [];
    }

    private async Task SaveProfileAsync()
    {
        try
        {
            await AccountApiClient.UpdateAsync(
                UserId,
                new UpdateAccountProfileRequest(
                    profile.FullName.Trim(),
                    profile.UserName.Trim(),
                    profile.Email.Trim()));

            await NotifyAsync("Profile updated successfully", NotificationSeverity.Success);
            await Task.Delay(300);
            await JS.InvokeVoidAsync("location.reload");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update profile for user {UserId}", UserId);
            await NotifyAsync(GetErrorMessage(ex, "Failed to update profile"), NotificationSeverity.Error);
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (pwd.NewPassword != pwd.ConfirmNewPassword)
        {
            await NotifyAsync("New passwords do not match", NotificationSeverity.Warning);
            return;
        }

        try
        {
            await AccountApiClient.ChangePasswordAsync(
                UserId,
                new ChangePasswordRequest(
                    pwd.CurrentPassword,
                    pwd.NewPassword,
                    pwd.ConfirmNewPassword));

            pwd = new PasswordModel();
            await NotifyAsync("Password changed successfully", NotificationSeverity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to change password for user {UserId}", UserId);
            await NotifyAsync(GetErrorMessage(ex, "Failed to change password"), NotificationSeverity.Error);
        }
    }

    private async Task SavePreferencesAsync()
    {
        visibility = profileVisibilityLevel == ProfileVisibilityLevel.Public ? "public" : "private";

        try
        {
            var request = new UserPreferencesDto
            {
                UserId = UserId,
                DistanceUnit = distanceUnit,
                TemperatureUnit = temperatureUnit,
                TimeFormat = timeFormat,
                DateFormat = dateFormat,
                ProfileVisibility = profileVisibilityLevel,
                ShowTotalMiles = showTotalMiles,
                ShowAirlines = showAirlines,
                ShowCountries = showCountries,
                ShowMapRoutes = showMapRoutes,
                EnableActivityFeed = enableActivityFeed
            };

            await UserPreferencesApiClient.UpdateAsync(UserId, request);
            await JS.InvokeVoidAsync("FlightTracker.setCookie", "ft_theme", theme, 365);
            await JS.InvokeVoidAsync("FlightTracker.setCookie", "ft_profile_visibility", visibility, 365);
            await ApplyThemeAsync(theme);
            await NotifyAsync("Preferences saved successfully", NotificationSeverity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update preferences for user {UserId}", UserId);
            await NotifyAsync(GetErrorMessage(ex, "Failed to save preferences"), NotificationSeverity.Error);
        }
    }

    private async Task CreateAccessTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(tokenLabel))
        {
            await NotifyAsync("Token label is required", NotificationSeverity.Warning);
            return;
        }

        var scopes = PersonalAccessTokenScopes.None;
        if (scopeReadFlights)
        {
            scopes |= PersonalAccessTokenScopes.ReadFlights;
        }

        if (scopeWriteFlights)
        {
            scopes |= PersonalAccessTokenScopes.WriteFlights;
        }

        if (scopeReadStats)
        {
            scopes |= PersonalAccessTokenScopes.ReadStats;
        }

        if (scopes == PersonalAccessTokenScopes.None)
        {
            await NotifyAsync("Select at least one scope", NotificationSeverity.Warning);
            return;
        }

        try
        {
            var created = await PersonalAccessTokensApiClient.CreateAsync(
                UserId,
                tokenLabel.Trim(),
                scopes,
                DateTime.UtcNow.AddDays(tokenExpiresInDays));

            if (created is null)
            {
                await NotifyAsync("Failed to create token", NotificationSeverity.Error);
                return;
            }

            createdTokenValue = created.PlainTextToken;
            accessTokens.Insert(0, MapToken(created.Token));
            tokenLabel = string.Empty;
            tokenExpiresInDays = 30;
            scopeReadFlights = true;
            scopeWriteFlights = false;
            scopeReadStats = false;
            await NotifyAsync("Access token created. Copy it now; it will not be shown again.", NotificationSeverity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create PAT for user {UserId}", UserId);
            await NotifyAsync(GetErrorMessage(ex, "Failed to create token"), NotificationSeverity.Error);
        }
    }

    private async Task RefreshAccessTokensAsync()
    {
        try
        {
            var tokens = await PersonalAccessTokensApiClient.ListAsync(UserId);
            accessTokens = tokens.Select(MapToken).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh PAT list for user {UserId}", UserId);
            await NotifyAsync(GetErrorMessage(ex, "Failed to load tokens"), NotificationSeverity.Error);
        }
    }

    private async Task RevokeAccessTokenAsync(int tokenId)
    {
        try
        {
            var revoked = await PersonalAccessTokensApiClient.RevokeAsync(UserId, tokenId);
            if (!revoked)
            {
                await NotifyAsync("Failed to revoke token", NotificationSeverity.Error);
                return;
            }

            await RefreshAccessTokensAsync();
            await NotifyAsync("Access token revoked", NotificationSeverity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to revoke PAT {TokenId} for user {UserId}", tokenId, UserId);
            await NotifyAsync(GetErrorMessage(ex, "Failed to revoke token"), NotificationSeverity.Error);
        }
    }

    private async Task ExportCsv()
    {
        await DownloadAsync(
            () => AccountApiClient.ExportFlightsCsvAsync(UserId),
            "user-flights.csv",
            "text/csv; charset=utf-8");
    }

    private async Task ExportAllJson()
    {
        await DownloadAsync(
            () => AccountApiClient.ExportAllJsonAsync(UserId),
            "user-profile-export.json",
            "application/json; charset=utf-8");
    }

    private async Task DownloadAsync(
        Func<Task<byte[]>> download,
        string fileName,
        string contentType)
    {
        try
        {
            var bytes = await download();
            await JS.InvokeVoidAsync(
                "FlightTracker.downloadBase64",
                fileName,
                contentType,
                Convert.ToBase64String(bytes));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to download {FileName} for user {UserId}", fileName, UserId);
            await NotifyAsync("Failed to start download", NotificationSeverity.Error);
        }
    }

    private async Task ConfirmDeleteProfileAsync()
    {
        var ok = await DialogService.Confirm(
            "Delete your profile and all recorded flights? This cannot be undone.",
            "Confirm deletion",
            new ConfirmOptions
            {
                OkButtonText = "Delete",
                CancelButtonText = "Cancel",
                ShowClose = false
            });

        if (ok != true)
        {
            return;
        }

        try
        {
            var result = await AccountApiClient.DeleteAsync(UserId);
            if (!result.UserDeleted)
            {
                await NotifyAsync("Failed to delete profile", NotificationSeverity.Error);
                return;
            }

            await NotifyAsync($"Profile deleted. Removed {result.DeletedFlights} recorded flights.", NotificationSeverity.Success);
            NavigationManager.NavigateTo("/logout", true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete profile for user {UserId}", UserId);
            await NotifyAsync(GetErrorMessage(ex, "Failed to delete profile"), NotificationSeverity.Error);
        }
    }

    private Task ApplyThemeAsync(string value)
    {
        return JS.InvokeVoidAsync("FlightTracker.applyTheme", value).AsTask();
    }

    private Task NotifyAsync(string message, NotificationSeverity severity)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Summary = message,
            Severity = severity,
            Duration = 2500
        });

        return Task.CompletedTask;
    }

    private static PersonalAccessTokenViewModel MapToken(PersonalAccessTokenDto token)
    {
        return new PersonalAccessTokenViewModel
        {
            Id = token.Id,
            Label = token.Label,
            TokenPrefix = token.TokenPrefix,
            Scopes = token.Scopes,
            ExpiresAtUtc = token.ExpiresAtUtc,
            LastUsedAtUtc = token.LastUsedAtUtc,
            RevokedAtUtc = token.RevokedAtUtc
        };
    }

    private static string GetErrorMessage(Exception ex, string fallback)
    {
        return string.IsNullOrWhiteSpace(ex.Message)
            ? fallback
            : ex.Message;
    }
}
