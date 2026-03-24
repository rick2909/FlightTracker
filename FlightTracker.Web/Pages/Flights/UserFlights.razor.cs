using System.Security.Claims;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class UserFlights
{
    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Inject]
    private IUserPreferencesService UserPreferencesService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Parameter]
    public int? UserId { get; set; }

    protected List<UserFlightDto> Flights { get; private set; } = [];

    protected DateFormat DateFormat { get; private set; } = DateFormat.YearMonthDay;

    protected TimeFormat TimeFormat { get; private set; } = TimeFormat.TwentyFourHour;

    protected string Title { get; private set; } = "My Flights";

    protected override async Task OnParametersSetAsync()
    {
        var effectiveUserId = UserId ?? await GetCurrentUserIdAsync();
        if (effectiveUserId <= 0)
        {
            return;
        }

        Title = UserId.HasValue ? $"User #{effectiveUserId} Flights" : "My Flights";

        var flightsResult = await UserFlightService.GetUserFlightsAsync(effectiveUserId);
        if (!flightsResult.IsFailure && flightsResult.Value is not null)
        {
            Flights = flightsResult.Value.OrderByDescending(f => f.DepartureTimeUtc).ToList();
        }

        var preferencesResult = await UserPreferencesService.GetOrCreateAsync(effectiveUserId);
        if (!preferencesResult.IsFailure && preferencesResult.Value is not null)
        {
            DateFormat = preferencesResult.Value.DateFormat;
            TimeFormat = preferencesResult.Value.TimeFormat;
        }
    }

    private async Task<int> GetCurrentUserIdAsync()
    {
        if (AuthenticationStateTask is null)
        {
            return 0;
        }

        var authState = await AuthenticationStateTask;
        var idStr = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var userId) ? userId : 0;
    }
}
