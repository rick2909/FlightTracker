using System.Security.Claims;
using System.Text.Json;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class Statistics
{
    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Inject]
    private IPassportService PassportService { get; set; } = default!;

    [Inject]
    private ILogger<Statistics> Logger { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected StatsViewModel Model { get; private set; } = new();

    protected string FlightsPerYearJson { get; private set; } = "[]";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId <= 0)
            {
                return;
            }

            var statsResult = await UserFlightService.GetUserFlightStatsAsync(userId);
            var passportResult = await PassportService.GetPassportDataAsync(userId);
            var allFlightsResult = await UserFlightService.GetUserFlightsAsync(userId);

            if (statsResult.IsFailure || statsResult.Value is null
                || passportResult.IsFailure || passportResult.Value is null
                || allFlightsResult.IsFailure || allFlightsResult.Value is null)
            {
                return;
            }

            var perYear = allFlightsResult.Value
                .GroupBy(f => f.DepartureTimeUtc.Year)
                .OrderBy(g => g.Key)
                .Select(g => new StatsViewModel.FlightsPerYearPoint(g.Key, g.Count()))
                .ToList();

            Model = new StatsViewModel
            {
                UserId = userId,
                Stats = statsResult.Value,
                MapFlights = passportResult.Value.Routes,
                FlightsPerYear = perYear
            };

            FlightsPerYearJson = JsonSerializer.Serialize(perYear);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading user statistics");
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
}
