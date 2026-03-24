using System.Security.Claims;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using FlightTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using YourApp.Models;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class PassportDetails
{
    [Inject]
    private IPassportService PassportService { get; set; } = default!;

    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Inject]
    private IUserPreferencesService UserPreferencesService { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Parameter]
    public int? Id { get; set; }

    protected PassportDetailsViewModel? Model { get; private set; }

    protected override async Task OnParametersSetAsync()
    {
        var currentUserId = await GetCurrentUserIdAsync();
        var userId = Id ?? currentUserId;
        if (userId <= 0)
        {
            return;
        }

        var displayedUser = await UserManager.FindByIdAsync(userId.ToString());
        if (displayedUser is null)
        {
            return;
        }

        var shownPreferencesResult = await UserPreferencesService.GetOrCreateAsync(userId, default);
        if (shownPreferencesResult.IsFailure || shownPreferencesResult.Value is null)
        {
            return;
        }

        var shownPreferences = shownPreferencesResult.Value;
        if (currentUserId != userId && shownPreferences.ProfileVisibility == ProfileVisibilityLevel.Private)
        {
            return;
        }

        var passportDataResult = await PassportService.GetPassportDataAsync(userId, default);
        var detailsResult = await PassportService.GetPassportDetailsAsync(userId, default);
        var flightsResult = await UserFlightService.GetUserFlightsAsync(userId, default);

        if (passportDataResult.IsFailure || passportDataResult.Value is null
            || detailsResult.IsFailure || detailsResult.Value is null
            || flightsResult.IsFailure || flightsResult.Value is null)
        {
            return;
        }

        var passportData = passportDataResult.Value;
        var details = detailsResult.Value;

        Model = new PassportDetailsViewModel
        {
            UserName = displayedUser.UserName ?? "Guest User",
            DateFormat = shownPreferences.DateFormat,
            TimeFormat = shownPreferences.TimeFormat,
            ShowAirlines = shownPreferences.ShowAirlines,
            FlightsByAirline = shownPreferences.ShowAirlines ? passportData.FlightsByAirline : new Dictionary<string, int>(),
            FlightsByAircraftType = passportData.FlightsByAircraftType,
            AirlineStats = shownPreferences.ShowAirlines ? details.AirlineStats : [],
            AircraftTypeStats = details.AircraftTypeStats,
            UserFlights = flightsResult.Value.OrderByDescending(f => f.DepartureTimeUtc).ToList()
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
