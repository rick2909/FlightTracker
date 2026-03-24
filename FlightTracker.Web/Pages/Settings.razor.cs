using System.Security.Claims;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.ViewModels;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class Settings
{
    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private IUserPreferencesService UserPreferencesService { get; set; } = default!;

    [Inject]
    private IPersonalAccessTokensApiClient PersonalAccessTokensApiClient { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected SettingsViewModel? Model { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId <= 0)
        {
            return;
        }

        var user = await UserManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        var preferences = await UserPreferencesService.GetOrCreateAsync(userId, default);
        if (preferences.IsFailure || preferences.Value is null)
        {
            return;
        }

        var accessTokens = await PersonalAccessTokensApiClient.ListAsync(userId);
        Model = new SettingsViewModel
        {
            FullName = user.FullName ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            ProfileVisibility = "private",
            Theme = "system",
            Preferences = new PreferencesViewModel
            {
                DistanceUnit = preferences.Value.DistanceUnit,
                TemperatureUnit = preferences.Value.TemperatureUnit,
                TimeFormat = preferences.Value.TimeFormat,
                DateFormat = preferences.Value.DateFormat,
                ProfileVisibilityLevel = preferences.Value.ProfileVisibility,
                ShowTotalMiles = preferences.Value.ShowTotalMiles,
                ShowAirlines = preferences.Value.ShowAirlines,
                ShowCountries = preferences.Value.ShowCountries,
                ShowMapRoutes = preferences.Value.ShowMapRoutes,
                EnableActivityFeed = preferences.Value.EnableActivityFeed
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
