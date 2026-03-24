using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class UserFlightDetails
{
    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Inject]
    private IUserPreferencesService UserPreferencesService { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    protected UserFlightDto? Flight { get; private set; }

    protected DateFormat DateFormat { get; private set; } = DateFormat.YearMonthDay;

    protected TimeFormat TimeFormat { get; private set; } = TimeFormat.TwentyFourHour;

    protected override async Task OnParametersSetAsync()
    {
        var dtoResult = await UserFlightService.GetByIdAsync(Id);
        if (dtoResult.IsFailure || dtoResult.Value is null)
        {
            return;
        }

        Flight = dtoResult.Value;

        var preferencesResult = await UserPreferencesService.GetOrCreateAsync(Flight.UserId);
        if (!preferencesResult.IsFailure && preferencesResult.Value is not null)
        {
            DateFormat = preferencesResult.Value.DateFormat;
            TimeFormat = preferencesResult.Value.TimeFormat;
        }
    }
}
