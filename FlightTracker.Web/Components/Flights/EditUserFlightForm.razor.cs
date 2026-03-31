using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using TimeZoneConverter;

namespace FlightTracker.Web.Components.Flights;

public partial class EditUserFlightForm
{
    [Inject]
    private IUserFlightService UserFlightService { get; set; } = default!;

    [Inject]
    private IUserFlightsApiClient UserFlightsApiClient { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    [Parameter]
    public int UserFlightId { get; set; }

    private UserFlightDto? Model;

    private FlightClass FlightClass { get; set; } = FlightClass.Economy;
    private string SeatNumber { get; set; } = string.Empty;
    private bool DidFly { get; set; } = true;
    private string? Notes { get; set; }

    private int FlightId { get; set; }
    private string FlightNumber { get; set; } = string.Empty;
    private string? DepartureAirportCode { get; set; }
    private string? ArrivalAirportCode { get; set; }
    private DateTime DepartureTimeUtc { get; set; }
    private DateTime ArrivalTimeUtc { get; set; }
    private DateTime DepartureTimeLocal { get; set; }
    private DateTime ArrivalTimeLocal { get; set; }

    private string? AircraftRegistration { get; set; }
    private string? OperatingAirlineCode { get; set; }

    private string? DepartureTimeZoneId { get; set; }
    private string? ArrivalTimeZoneId { get; set; }

    private bool CanEditFlight { get; set; }
    private string? browserTimeZoneId;
    private IEnumerable<FlightClass> flightClasses = Array.Empty<FlightClass>();

    private sealed class PendingChanges
    {
        public string? FlightNumber { get; set; }
        public DateTime? DepartureTimeUtc { get; set; }
        public DateTime? ArrivalTimeUtc { get; set; }
    }

    private readonly PendingChanges pendingChanges = new();

    private bool HasPendingFlightNumber =>
        !string.IsNullOrWhiteSpace(pendingChanges.FlightNumber)
        && !string.Equals(
            pendingChanges.FlightNumber,
            FlightNumber,
            StringComparison.OrdinalIgnoreCase);

    private bool HasPendingDepartureTime =>
        pendingChanges.DepartureTimeUtc.HasValue
        && pendingChanges.DepartureTimeUtc.Value != DepartureTimeUtc;

    private bool HasPendingArrivalTime =>
        pendingChanges.ArrivalTimeUtc.HasValue
        && pendingChanges.ArrivalTimeUtc.Value != ArrivalTimeUtc;

    private DateTime DepartureTimeForm
    {
        get => DepartureTimeLocal == default
            ? ConvertUtcToLocal(DepartureTimeUtc, DepartureTimeZoneId)
            : DepartureTimeLocal;
        set => DepartureTimeLocal = DateTime.SpecifyKind(
            value,
            DateTimeKind.Unspecified);
    }

    private DateTime ArrivalTimeForm
    {
        get => ArrivalTimeLocal == default
            ? ConvertUtcToLocal(ArrivalTimeUtc, ArrivalTimeZoneId)
            : ArrivalTimeLocal;
        set => ArrivalTimeLocal = DateTime.SpecifyKind(
            value,
            DateTimeKind.Unspecified);
    }

    protected override async Task OnInitializedAsync()
    {
        flightClasses = Enum.GetValues(typeof(FlightClass))
            .Cast<FlightClass>()
            .ToArray();

        var dtoResult = await UserFlightService.GetByIdAsync(UserFlightId);
        if (dtoResult.IsFailure)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Load failed",
                Detail = dtoResult.ErrorMessage ?? "Failed to load flight.",
                Duration = 4000
            });
            return;
        }

        var dto = dtoResult.Value;
        Model = dto;
        if (dto is null)
        {
            return;
        }

        FlightId = dto.FlightId;
        FlightClass = dto.FlightClass;
        SeatNumber = dto.SeatNumber;
        DidFly = dto.DidFly;
        Notes = dto.Notes;
        FlightNumber = dto.FlightNumber;
        DepartureAirportCode = dto.DepartureIataCode
            ?? dto.DepartureIcaoCode
            ?? dto.DepartureAirportCode;
        ArrivalAirportCode = dto.ArrivalIataCode
            ?? dto.ArrivalIcaoCode
            ?? dto.ArrivalAirportCode;
        DepartureTimeUtc = DateTime.SpecifyKind(
            dto.DepartureTimeUtc,
            DateTimeKind.Utc);
        ArrivalTimeUtc = DateTime.SpecifyKind(
            dto.ArrivalTimeUtc,
            DateTimeKind.Utc);
        DepartureTimeZoneId = dto.DepartureTimeZoneId;
        ArrivalTimeZoneId = dto.ArrivalTimeZoneId;
        DepartureTimeLocal = ConvertUtcToLocal(
            DepartureTimeUtc,
            DepartureTimeZoneId);
        ArrivalTimeLocal = ConvertUtcToLocal(
            ArrivalTimeUtc,
            ArrivalTimeZoneId);
        AircraftRegistration = dto.Aircraft?.Registration;
        OperatingAirlineCode = dto.OperatingAirlineIataCode
            ?? dto.OperatingAirlineIcaoCode;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            browserTimeZoneId = await JS.InvokeAsync<string>(
                "(function(){return Intl.DateTimeFormat().resolvedOptions().timeZone;})");
        }
        catch
        {
            browserTimeZoneId = null;
        }

        StateHasChanged();
    }

    private string FormatLocal(DateTime utc, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return "-";
        }

        try
        {
            var tz = ResolveTimeZone(timeZoneId);
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Utc),
                tz);
            var offset = tz.GetUtcOffset(local);
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var hh = Math.Abs(offset.Hours).ToString("00");
            var mm = Math.Abs(offset.Minutes).ToString("00");
            return $"{local:MMM dd, yyyy HH:mm} (UTC{sign}{hh}:{mm})";
        }
        catch
        {
            return "-";
        }
    }

    private DateTime ConvertUtcToLocal(DateTime utc, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
        }

        try
        {
            var tz = ResolveTimeZone(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Utc),
                tz);
        }
        catch
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
        }
    }

    private DateTime ConvertLocalToUtc(DateTime local, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return DateTime.SpecifyKind(local, DateTimeKind.Utc);
        }

        try
        {
            var tz = ResolveTimeZone(timeZoneId);
            return TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
                tz);
        }
        catch
        {
            return DateTime.SpecifyKind(local, DateTimeKind.Utc);
        }
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TZConvert.GetTimeZoneInfo(timeZoneId);
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
    }

    private void EnableManualFlightEdit()
    {
        CanEditFlight = true;
    }

    private async Task LoadFromApiAsync()
    {
        try
        {
            var response = await UserFlightsApiClient.LookupRefreshAsync(
                UserFlightId);

            if (response is null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Not found",
                    Detail = "No flight found via lookup.",
                    Duration = 2500
                });
                return;
            }

            switch (response.Status)
            {
                case "no_changes":
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Info,
                        Summary = "No changes",
                        Detail = "No changes found.",
                        Duration = 2500
                    });
                    break;
                case "changes":
                    pendingChanges.FlightNumber = response.Changes?.FlightNumber;
                    pendingChanges.DepartureTimeUtc = response.Changes?
                        .DepartureTimeUtc;
                    pendingChanges.ArrivalTimeUtc = response.Changes?
                        .ArrivalTimeUtc;
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Updates available",
                        Detail = "New flight data found.",
                        Duration = 2500
                    });
                    StateHasChanged();
                    break;
                default:
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Not found",
                        Detail = response.Message
                            ?? "No flight found via lookup.",
                        Duration = 2500
                    });
                    break;
            }
        }
        catch
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Lookup failed",
                Detail = "Unable to load flight data from API.",
                Duration = 3000
            });
        }
    }

    private void ApplyFlightNumber()
    {
        if (!HasPendingFlightNumber)
        {
            return;
        }

        CanEditFlight = true;
        FlightNumber = pendingChanges.FlightNumber!;
        pendingChanges.FlightNumber = null;
    }

    private void ApplyDepartureTime()
    {
        if (!HasPendingDepartureTime)
        {
            return;
        }

        CanEditFlight = true;
        DepartureTimeUtc = pendingChanges.DepartureTimeUtc!.Value;
        DepartureTimeLocal = ConvertUtcToLocal(
            DepartureTimeUtc,
            DepartureTimeZoneId);
        pendingChanges.DepartureTimeUtc = null;
    }

    private void ApplyArrivalTime()
    {
        if (!HasPendingArrivalTime)
        {
            return;
        }

        CanEditFlight = true;
        ArrivalTimeUtc = pendingChanges.ArrivalTimeUtc!.Value;
        ArrivalTimeLocal = ConvertUtcToLocal(
            ArrivalTimeUtc,
            ArrivalTimeZoneId);
        pendingChanges.ArrivalTimeUtc = null;
    }

    private async Task SaveAsync()
    {
        if (Model is null)
        {
            return;
        }

        var userFlight = new UpdateUserFlightDto
        {
            FlightClass = FlightClass,
            SeatNumber = SeatNumber,
            Notes = Notes,
            DidFly = DidFly
        };

        var departureLocal = DepartureTimeForm;
        var arrivalLocal = ArrivalTimeForm;
        if (arrivalLocal <= departureLocal
            && arrivalLocal.AddHours(30) >= departureLocal)
        {
            var nextDay = departureLocal.Date.AddDays(1);
            arrivalLocal = nextDay + arrivalLocal.TimeOfDay;
            ArrivalTimeForm = arrivalLocal;
        }

        DepartureTimeUtc = ConvertLocalToUtc(
            departureLocal,
            DepartureTimeZoneId);
        ArrivalTimeUtc = ConvertLocalToUtc(
            arrivalLocal,
            ArrivalTimeZoneId);

        var schedule = new FlightScheduleUpdateDto
        {
            FlightId = FlightId,
            FlightNumber = FlightNumber,
            DepartureAirportCode = DepartureAirportCode ?? string.Empty,
            ArrivalAirportCode = ArrivalAirportCode ?? string.Empty,
            DepartureTimeUtc = DepartureTimeUtc,
            ArrivalTimeUtc = ArrivalTimeUtc,
            AircraftRegistration = AircraftRegistration,
            OperatingAirlineCode = OperatingAirlineCode
        };

        try
        {
            var updateResult = await UserFlightService
                .UpdateUserFlightAndScheduleAsync(
                    UserFlightId,
                    userFlight,
                    schedule);

            if (updateResult.IsFailure)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Save failed",
                    Detail = updateResult.ErrorMessage
                        ?? "Failed to save changes.",
                    Duration = 4000
                });
                return;
            }

            if (updateResult.Value is null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Not found",
                    Detail = "Flight not found.",
                    Duration = 3000
                });
                return;
            }
        }
        catch (FluentValidation.ValidationException vex)
        {
            foreach (var error in vex.Errors)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validation error",
                    Detail = $"{error.PropertyName}: {error.ErrorMessage}",
                    Duration = 4000
                });
            }
            return;
        }

        Nav.NavigateTo($"/UserFlights/Details/{UserFlightId}", true);
    }

    private void Cancel()
    {
        Nav.NavigateTo($"/UserFlights/Details/{UserFlightId}", true);
    }
}