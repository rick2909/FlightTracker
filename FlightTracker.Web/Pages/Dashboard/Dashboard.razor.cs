using System.Security.Claims;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class Dashboard
{
    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Inject]
    private IPassportService PassportService { get; set; } = default!;

    [Inject]
    private ILogger<Dashboard> Logger { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected DashboardViewModel Model { get; private set; } = new();

    protected FlightStateViewModel? State { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (!await TryGetCurrentUserIdAsync())
            {
                return;
            }

            var userId = Model.UserId;
            var statsResult = await UserFlightService.GetUserFlightStatsAsync(userId);
            var recentFlightsResult = await UserFlightService.GetUserFlightsAsync(userId);
            var passportResult = await PassportService.GetPassportDataAsync(userId);

            if (statsResult.IsFailure || statsResult.Value is null
                || recentFlightsResult.IsFailure || recentFlightsResult.Value is null
                || passportResult.IsFailure || passportResult.Value is null)
            {
                Model.ErrorMessage = "Unable to load dashboard data. Please try again.";
                return;
            }

            Model = new DashboardViewModel
            {
                UserId = userId,
                Stats = statsResult.Value,
                RecentFlights = recentFlightsResult.Value.Take(5).ToList(),
                MapFlights = passportResult.Value.Routes
            };

            State = await BuildCurrentStateAsync(userId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading dashboard for user");
            Model.ErrorMessage = "Unable to load dashboard data. Please try again.";
        }
    }

    private async Task<bool> TryGetCurrentUserIdAsync()
    {
        if (AuthenticationStateTask is null)
        {
            return false;
        }

        var state = await AuthenticationStateTask;
        var id = state.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(id, out var userId))
        {
            return false;
        }

        Model.UserId = userId;
        return true;
    }

    private async Task<FlightStateViewModel> BuildCurrentStateAsync(int userId)
    {
        var flightsResult = await UserFlightService.GetUserFlightsAsync(userId);
        if (flightsResult.IsFailure || flightsResult.Value is null)
        {
            throw new InvalidOperationException(flightsResult.ErrorMessage ?? "Unable to load user flights.");
        }

        var flights = flightsResult.Value.OrderBy(f => f.DepartureTimeUtc).ToList();
        var now = DateTime.UtcNow;
        var next = flights.FirstOrDefault(f => f.DepartureTimeUtc >= now);
        var prev = flights.LastOrDefault(f => f.ArrivalTimeUtc <= now) ?? flights.LastOrDefault();
        var chosen = next ?? prev ?? flights.FirstOrDefault();
        if (chosen is null)
        {
            return new FlightStateViewModel
            {
                State = FlightStateViewModel.TravelState.PreFlight,
                Flight = new UserFlightDto { FlightNumber = "-", DepartureAirportCode = "-", ArrivalAirportCode = "-" },
                CountdownLabel = "No flights scheduled"
            };
        }

        var state = new FlightStateViewModel
        {
            Flight = chosen
        };

        if (next is not null)
        {
            var untilDep = chosen.DepartureTimeUtc - now;
            state.State = untilDep > TimeSpan.FromHours(2)
                ? FlightStateViewModel.TravelState.PreFlight
                : FlightStateViewModel.TravelState.AtAirport;
            state.CountdownLabel = $"Gate Departure in {FormatDuration(untilDep)}";
            state.Note = "Inbound aircraft has arrived";
        }
        else
        {
            state.State = FlightStateViewModel.TravelState.PostFlight;
            var sinceArr = now - chosen.ArrivalTimeUtc;
            state.CountdownLabel = $"Arrived {FormatDuration(sinceArr)} ago";
        }

        var passportResult = await PassportService.GetPassportDataAsync(userId);
        if (!passportResult.IsFailure && passportResult.Value is not null)
        {
            var match = passportResult.Value.Routes.FirstOrDefault(r => r.FlightId == chosen.FlightId);
            if (match is not null)
            {
                state.Route = match;
            }
        }

        return state;
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero)
            ts = -ts;

        var totalDays = (int)ts.TotalDays;

        if (totalDays >= 30)
        {
            var months = totalDays / 30;
            var remainingDays = totalDays % 30;
            return remainingDays > 0
                ? $"{months}mo {remainingDays}d"
                : $"{months}mo";
        }

        if (totalDays >= 7)
        {
            var weeks = totalDays / 7;
            var remainingDays = totalDays % 7;
            return remainingDays > 0
                ? $"{weeks}w {remainingDays}d"
                : $"{weeks}w";
        }

        if (totalDays >= 1)
        {
            var hours = ts.Hours;
            return hours > 0
                ? $"{totalDays}d {hours}h"
                : $"{totalDays}d";
        }

        if (ts.TotalHours >= 1)
        {
            return ts.Minutes > 0
                ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                : $"{(int)ts.TotalHours}h";
        }

        return $"{ts.Minutes}m";
    }
}
