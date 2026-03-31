using System.Security.Claims;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class Status
{
    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Inject]
    private IPassportService PassportService { get; set; } = default!;

    [Inject]
    private ILogger<Status> Logger { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected FlightStatusPageViewModel Model { get; private set; } = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId <= 0)
            {
                return;
            }

            var state = await BuildCurrentStateAsync(userId);
            IEnumerable<MapFlightDto> mapFlights;
            if (state.Route is not null)
            {
                mapFlights = new[] { state.Route };
            }
            else
            {
                var passportResult = await PassportService.GetPassportDataAsync(userId);
                mapFlights = passportResult.IsFailure || passportResult.Value is null
                    ? Array.Empty<MapFlightDto>()
                    : passportResult.Value.Routes;
            }

            Model = new FlightStatusPageViewModel
            {
                State = state,
                MapFlights = mapFlights
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading status view");
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

        var result = new FlightStateViewModel
        {
            Flight = chosen
        };

        if (next is not null)
        {
            var untilDep = chosen.DepartureTimeUtc - now;
            result.State = untilDep > TimeSpan.FromHours(2)
                ? FlightStateViewModel.TravelState.PreFlight
                : FlightStateViewModel.TravelState.AtAirport;
            result.CountdownLabel = $"Gate Departure in {FormatDuration(untilDep)}";
            result.Note = "Inbound aircraft has arrived";
        }
        else
        {
            result.State = FlightStateViewModel.TravelState.PostFlight;
            result.CountdownLabel = $"Arrived {FormatDuration(now - chosen.ArrivalTimeUtc)} ago";
        }

        var passportResult = await PassportService.GetPassportDataAsync(userId);
        if (!passportResult.IsFailure && passportResult.Value is not null)
        {
            var match = passportResult.Value.Routes.FirstOrDefault(r => r.FlightId == chosen.FlightId);
            if (match is not null)
            {
                result.Route = match;
            }
        }

        return result;
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
