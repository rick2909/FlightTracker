using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using FlightTracker.Web.Components.Cards;
using FlightTracker.Web.Components.Flights;
using FlightTracker.Web.Formatting;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using Radzen;

namespace FlightTracker.Web.Components;

public partial class UserFlightsList
{
    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public IEnumerable<UserFlightDto>? Flights { get; set; }

    [Parameter]
    public bool ShowFilters { get; set; } = true;

    [Parameter]
    public DateFormat DateFormat { get; set; } = DateFormat.YearMonthDay;

    [Parameter]
    public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;

    [Parameter]
    public EventCallback OnFlightsChanged { get; set; }

    private string DatePattern => PreferenceFormatter.GetDatePattern(DateFormat);

    private string? filterFlightNumber;

    private FlightClass? filterClass;

    private bool? filterDidFly;

    private DateTime? filterFromUtc;

    private DateTime? filterToUtc;

    private sealed record Option<T>(string Text, T? Value);

    private readonly IEnumerable<Option<FlightClass>> flightClassOptions = new[]
    {
        new Option<FlightClass>("Economy", FlightClass.Economy),
        new Option<FlightClass>("Premium Economy", FlightClass.PremiumEconomy),
        new Option<FlightClass>("Business", FlightClass.Business),
        new Option<FlightClass>("First", FlightClass.First)
    };

    private readonly IEnumerable<Option<bool>> didFlyOptions = new[]
    {
        new Option<bool>("Completed", true),
        new Option<bool>("No Show", false)
    };

    private IEnumerable<UserFlightDto> FlightsOrdered =>
        FilteredFlights()
            .OrderByDescending(f => f.DepartureTimeUtc);

    protected override void OnInitialized()
    {
        var uri = new Uri(Nav.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("q", out var q)
            && !string.IsNullOrWhiteSpace(q))
        {
            filterFlightNumber = q;
        }

        if (query.TryGetValue("class", out var cls)
            && Enum.TryParse<FlightClass>(cls, out var parsedClass))
        {
            filterClass = parsedClass;
        }

        if (query.TryGetValue("didFly", out var df)
            && bool.TryParse(df, out var parsedDidFly))
        {
            filterDidFly = parsedDidFly;
        }

        if (query.TryGetValue("fromUtc", out var from)
            && DateTime.TryParse(from, out var fromDt))
        {
            filterFromUtc = DateTime.SpecifyKind(fromDt.Date, DateTimeKind.Utc);
        }

        if (query.TryGetValue("toUtc", out var to)
            && DateTime.TryParse(to, out var toDt))
        {
            filterToUtc = DateTime.SpecifyKind(toDt.Date, DateTimeKind.Utc);
        }
    }

    private IEnumerable<UserFlightDto> FilteredFlights()
    {
        var source = Flights ?? Enumerable.Empty<UserFlightDto>();

        if (!string.IsNullOrWhiteSpace(filterFlightNumber))
        {
            var term = filterFlightNumber.Trim();
            source = source.Where(f =>
                f.FlightNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (filterClass.HasValue)
        {
            var cls = filterClass.Value;
            source = source.Where(f => f.FlightClass == cls);
        }

        if (filterDidFly.HasValue)
        {
            var flag = filterDidFly.Value;
            source = source.Where(f => f.DidFly == flag);
        }

        if (filterFromUtc.HasValue)
        {
            var from = filterFromUtc.Value.Date;
            source = source.Where(f => f.DepartureTimeUtc >= from);
        }

        if (filterToUtc.HasValue)
        {
            var to = filterToUtc.Value.Date.AddDays(1).AddTicks(-1);
            source = source.Where(f => f.DepartureTimeUtc <= to);
        }

        return source;
    }

    private async Task ClearFiltersAsync()
    {
        filterFlightNumber = null;
        filterClass = null;
        filterDidFly = null;
        filterFromUtc = null;
        filterToUtc = null;
        await UpdateUrlAsync();
    }

    private Task OnFiltersChangedAsync()
    {
        return UpdateUrlAsync();
    }

    private async Task UpdateUrlAsync()
    {
        var uri = new Uri(Nav.Uri);
        var basePath = uri.GetLeftPart(UriPartial.Path);

        var qs = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(filterFlightNumber))
        {
            qs["q"] = filterFlightNumber;
        }

        if (filterClass.HasValue)
        {
            qs["class"] = filterClass.Value.ToString();
        }

        if (filterDidFly.HasValue)
        {
            qs["didFly"] = filterDidFly.Value.ToString();
        }

        if (filterFromUtc.HasValue)
        {
            qs["fromUtc"] = filterFromUtc.Value.ToString("yyyy-MM-dd");
        }

        if (filterToUtc.HasValue)
        {
            qs["toUtc"] = filterToUtc.Value.ToString("yyyy-MM-dd");
        }

        var newUrl = QueryHelpers.AddQueryString(basePath, qs);
        await JS.InvokeVoidAsync("FlightTracker.replaceUrl", newUrl);
    }

    private async Task HandleDeleteAsync(UserFlightDto flight)
    {
        var dialogResult = await DialogService.OpenAsync<UserFlightDeleteDialog>(
            "Manage Flight Record",
            new Dictionary<string, object?>
            {
                [nameof(UserFlightDeleteDialog.FlightNumber)] = flight.FlightNumber
            },
            new DialogOptions
            {
                Width = "560px",
                ShowClose = false,
                CloseDialogOnEsc = true
            });

        var action = dialogResult as string;
        if (string.IsNullOrWhiteSpace(action)
            || string.Equals(
                action,
                UserFlightDeleteDialog.CancelAction,
                StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(
                action,
                UserFlightDeleteDialog.DeleteAction,
                StringComparison.Ordinal))
        {
            await DeleteFlightAsync(flight.Id);
            return;
        }

        if (string.Equals(
                action,
                UserFlightDeleteDialog.MarkDidNotFlyAction,
                StringComparison.Ordinal))
        {
            await MarkDidNotFlyAsync(flight);
        }
    }

    private async Task DeleteFlightAsync(int userFlightId)
    {
        var result = await UserFlightService.DeleteUserFlightAsync(userFlightId);
        if (result.IsFailure || !result.Value)
        {
            NotificationService.Notify(
                NotificationSeverity.Error,
                "Delete failed",
                "The flight could not be deleted.");
            return;
        }

        NotificationService.Notify(
            NotificationSeverity.Success,
            "Flight deleted",
            "The flight record was permanently deleted.");

        await OnFlightsChanged.InvokeAsync();
    }

    private async Task MarkDidNotFlyAsync(UserFlightDto flight)
    {
        var userFlightUpdate = new UpdateUserFlightDto
        {
            SeatNumber = flight.SeatNumber,
            FlightClass = flight.FlightClass,
            Notes = flight.Notes,
            DidFly = false
        };

        var scheduleUpdate = new FlightScheduleUpdateDto
        {
            FlightId = flight.FlightId,
            FlightNumber = flight.FlightNumber,
            DepartureAirportCode = flight.DepartureAirportCode,
            ArrivalAirportCode = flight.ArrivalAirportCode,
            DepartureTimeUtc = flight.DepartureTimeUtc,
            ArrivalTimeUtc = flight.ArrivalTimeUtc,
            AircraftRegistration = flight.Aircraft?.Registration,
            OperatingAirlineCode = flight.OperatingAirlineIataCode
                ?? flight.OperatingAirlineIcaoCode
        };

        var result = await UserFlightService.UpdateUserFlightAndScheduleAsync(
            flight.Id,
            userFlightUpdate,
            scheduleUpdate);

        if (result.IsFailure || result.Value is null)
        {
            NotificationService.Notify(
                NotificationSeverity.Error,
                "Update failed",
                "The flight could not be marked as not flown.");
            return;
        }

        NotificationService.Notify(
            NotificationSeverity.Success,
            "Flight updated",
            "Marked as not flown (DidFly = false).");

        await OnFlightsChanged.InvokeAsync();
    }
}