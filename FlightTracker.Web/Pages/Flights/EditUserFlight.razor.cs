using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace FlightTracker.Web.Pages;

[Authorize]
public partial class EditUserFlight
{
    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    protected string FlightNumber { get; private set; } = "-";

    protected string DepartureCode { get; private set; } = "-";

    protected string ArrivalCode { get; private set; } = "-";

    protected string TimeSummary { get; private set; } = "-";

    protected string RouteSummary => $"{DepartureCode} → {ArrivalCode}";

    protected override async Task OnParametersSetAsync()
    {
        var dtoResult = await UserFlightService.GetByIdAsync(Id);
        if (dtoResult.IsFailure || dtoResult.Value is null)
        {
            return;
        }

        var model = dtoResult.Value;
        FlightNumber = model.FlightNumber;
        DepartureCode = model.DepartureAirportCode;
        ArrivalCode = model.ArrivalAirportCode;
        TimeSummary = $"{model.DepartureTimeUtc:u} → {model.ArrivalTimeUtc:u}";
    }
}
