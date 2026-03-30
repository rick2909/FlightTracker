using System.Security.Claims;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Formatting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using YourApp.Models;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class Passport
{
    [Inject]
    private IPassportService PassportService { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private IUserPreferencesService UserPreferencesService { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Parameter]
    public int? Id { get; set; }

    protected PassportViewModel? Model { get; private set; }

    protected string Initials { get; private set; } = "GU";

    protected int UniqueRoutesCount =>
        Model?.Routes
            .Select(r => $"{r.DepartureAirportCode}->{r.ArrivalAirportCode}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() ?? 0;

    protected int UpcomingRoutesCount =>
        Model?.Routes.Count(r => r.IsUpcoming) ?? 0;

    protected int FlightsThisYearCount
    {
        get
        {
            if (Model?.FlightsPerYear is null)
            {
                return 0;
            }

            return Model.FlightsPerYear.TryGetValue(DateTime.UtcNow.Year, out var count)
                ? count
                : 0;
        }
    }

    private bool _chartsInitialized;

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
        var isOwner = currentUserId == userId;

        if (!isOwner && shownPreferences.ProfileVisibility == ProfileVisibilityLevel.Private)
        {
            return;
        }

        var dataResult = await PassportService.GetPassportDataAsync(userId, default);
        if (dataResult.IsFailure || dataResult.Value is null)
        {
            return;
        }

        var data = dataResult.Value;
        var displayName = displayedUser.UserName ?? "Guest User";
        var showTotalMiles = isOwner || shownPreferences.ShowTotalMiles;
        var showAirlines = isOwner || shownPreferences.ShowAirlines;
        var showCountries = isOwner || shownPreferences.ShowCountries;
        var showMapRoutes = isOwner || shownPreferences.ShowMapRoutes;

        Model = new PassportViewModel
        {
            UserName = displayName,
            TotalFlights = data.TotalFlights,
            TotalMiles = showTotalMiles ? data.TotalMiles : 0,
            TotalDistanceDisplay = showTotalMiles
                ? PreferenceFormatter.FormatDistanceFromMiles(data.TotalMiles, shownPreferences.DistanceUnit)
                : "Hidden",
            FavoriteAirline = showAirlines ? data.FavoriteAirline : string.Empty,
            FavoriteAirport = data.FavoriteAirport,
            MostFlownAircraftType = data.MostFlownAircraftType,
            FavoriteClass = data.FavoriteClass,
            LongestFlightMiles = showTotalMiles ? data.LongestFlightMiles : 0,
            ShortestFlightMiles = showTotalMiles ? data.ShortestFlightMiles : 0,
            LongestDistanceDisplay = showTotalMiles
                ? PreferenceFormatter.FormatDistanceFromMiles(data.LongestFlightMiles, shownPreferences.DistanceUnit)
                : "Hidden",
            ShortestDistanceDisplay = showTotalMiles
                ? PreferenceFormatter.FormatDistanceFromMiles(data.ShortestFlightMiles, shownPreferences.DistanceUnit)
                : "Hidden",
            DistanceUnit = shownPreferences.DistanceUnit,
            DateFormat = shownPreferences.DateFormat,
            TimeFormat = shownPreferences.TimeFormat,
            ShowTotalMiles = showTotalMiles,
            ShowAirlines = showAirlines,
            ShowCountries = showCountries,
            ShowMapRoutes = showMapRoutes,
            AirlinesVisited = showAirlines ? data.AirlinesVisited : [],
            AirportsVisited = data.AirportsVisited,
            CountriesVisitedIso2 = showCountries ? data.CountriesVisitedIso2 : [],
            FlightsPerYear = data.FlightsPerYear,
            FlightsByAirline = showAirlines ? data.FlightsByAirline : new Dictionary<string, int>(),
            FlightsByAircraftType = data.FlightsByAircraftType,
            Routes = showMapRoutes ? data.Routes : []
        };

        Initials = BuildInitials(displayName);
        _chartsInitialized = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Model is null || _chartsInitialized)
        {
            return;
        }

        try
        {
            await JsRuntime.InvokeVoidAsync(
                "Passport.initPassportChart",
                Model.FlightsPerYear);

            if (Model.ShowAirlines)
            {
                await JsRuntime.InvokeVoidAsync(
                    "Passport.initPie",
                    "passport-airlines-pie",
                    Model.FlightsByAirline,
                    "Airlines");
            }

            await JsRuntime.InvokeVoidAsync(
                "Passport.initPie",
                "passport-aircraft-pie",
                Model.FlightsByAircraftType,
                "Aircraft");

            _chartsInitialized = true;
        }
        catch (JSDisconnectedException)
        {
            // Ignore transient disconnects during navigation/teardown.
        }
        catch (TaskCanceledException)
        {
            // Ignore canceled invocations during rapid route changes.
        }
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

    private static string BuildInitials(string name)
    {
        var parts = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "GU";
        }

        if (parts.Length == 1)
        {
            return new string(parts[0].Take(2).Select(char.ToUpperInvariant).ToArray());
        }

        return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[1][0]));
    }

    private static string BuildFlagClass(string? iso2Code)
    {
        var flagCode = (iso2Code ?? string.Empty).ToLowerInvariant();
        return "fi fi-" + flagCode;
    }

    private static string BuildAvatarFallbackStyle(string? avatarUrl)
    {
        var displayValue = string.IsNullOrWhiteSpace(avatarUrl)
            ? "inline-flex"
            : "none";

        return $"display:{displayValue};align-items:center;justify-content:center;width:100%;height:100%;font-weight:600;";
    }
}
