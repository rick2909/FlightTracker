using System.Security.Claims;
using FlightTracker.Web.Models.ViewModels;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class Settings
{
    [Inject]
    private IAccountApiClient AccountApiClient { get; set; } = default!;

    [Inject]
    private IUserPreferencesApiClient UserPreferencesApiClient { get; set; } = default!;

    [Inject]
    private IPersonalAccessTokensApiClient PersonalAccessTokensApiClient { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected int UserId { get; private set; }

    protected SettingsViewModel? Model { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        UserId = await GetCurrentUserIdAsync();
        if (UserId <= 0)
        {
            return;
        }

        var profile = await AccountApiClient.GetAsync(UserId);
        if (profile is null)
        {
            return;
        }

        var preferences = await UserPreferencesApiClient.GetAsync(UserId);
        if (preferences is null)
        {
            return;
        }

        var accessTokens = await PersonalAccessTokensApiClient.ListAsync(UserId);
        var requestCookies = HttpContextAccessor.HttpContext?.Request.Cookies;
        Model = new SettingsViewModel
        {
            FullName = profile.FullName,
            UserName = profile.UserName,
            Email = profile.Email,
            ProfileVisibility = requestCookies?["ft_profile_visibility"] ?? "private",
            Theme = requestCookies?["ft_theme"] ?? "system",
            Preferences = new PreferencesViewModel
            {
                DistanceUnit = preferences.DistanceUnit,
                TemperatureUnit = preferences.TemperatureUnit,
                TimeFormat = preferences.TimeFormat,
                DateFormat = preferences.DateFormat,
                ProfileVisibilityLevel = preferences.ProfileVisibility,
                ShowTotalMiles = preferences.ShowTotalMiles,
                ShowAirlines = preferences.ShowAirlines,
                ShowCountries = preferences.ShowCountries,
                ShowMapRoutes = preferences.ShowMapRoutes,
                EnableActivityFeed = preferences.EnableActivityFeed
            },
            PersonalAccessTokens = accessTokens.Select(token => new PersonalAccessTokenViewModel
            {
                Id = token.Id,
                Label = token.Label,
                TokenPrefix = token.TokenPrefix,
                Scopes = token.Scopes,
                ExpiresAtUtc = token.ExpiresAtUtc,
                LastUsedAtUtc = token.LastUsedAtUtc,
                RevokedAtUtc = token.RevokedAtUtc
            }).ToList()
        };
    }

    private async Task<int> GetCurrentUserIdAsync()
    {
        if (AuthenticationStateTask is null)
        {
            return 0;
        }

        var state = await AuthenticationStateTask;
        var idStr = state.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var userId) ? userId : 0;
    }
}
