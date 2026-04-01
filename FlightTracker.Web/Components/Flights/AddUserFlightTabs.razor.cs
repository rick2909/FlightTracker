using System.ComponentModel.DataAnnotations;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using TimeZoneConverter;

namespace FlightTracker.Web.Components.Flights;

public partial class AddUserFlightTabs
{
    [Parameter]
    public CreateUserFlightDto? Value { get; set; }

    [Parameter]
    public bool PreferManualTab { get; set; }

    [Parameter]
    public EventCallback<CreateUserFlightDto> OnSave { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    public IFlightsApiClient FlightsApiClient { get; set; } = default!;

    [Inject]
    public IFlightLookupService Lookup { get; set; } = default!;

    [Inject]
    public IAirportsApiClient AirportsApiClient { get; set; } = default!;

    private EditForm? editForm;

    private sealed class ValidationModel
    {
        [Required, MinLength(2)]
        public string? SeatNumber { get; set; }

        [Required, MinLength(3)]
        public string? FlightNumber { get; set; }

        [Required, MinLength(3), MaxLength(4)]
        public string? DepartureAirportCode { get; set; }

        [Required, MinLength(3), MaxLength(4)]
        public string? ArrivalAirportCode { get; set; }

        [Required]
        public DateTime? DepartureTimeLocal { get; set; }

        [Required]
        public DateTime? ArrivalTimeLocal { get; set; }

        public string? AircraftRegistration { get; set; }
        public string? OperatingAirlineCode { get; set; }
    }

    private ValidationModel validation = new();

    private int flightId;
    private FlightClass flightClass = FlightClass.Economy;
    private string? notes;

    private int _tabIndex;
    private string? numberSearchNumber;
    private DateTime? numberSearchDate;
    private List<FlightLookupCandidateApiResponse> numberResults = new();

    private string? routeDep;
    private string? routeArr;
    private DateTime? routeDate;
    private bool routeNoDateMatches;
    private List<Flight> routeResults = new();

    private CreateUserFlightDto? _lastValue;
    private IReadOnlyList<Airport>? _airports;

    private readonly FlightClass[] _classes = Enum.GetValues<FlightClass>();

    private bool HasTimes => validation.DepartureTimeLocal.HasValue
        && validation.ArrivalTimeLocal.HasValue;

    private bool HasRouteAndNumber =>
        !string.IsNullOrWhiteSpace(validation.FlightNumber)
        && !string.IsNullOrWhiteSpace(validation.DepartureAirportCode)
        && !string.IsNullOrWhiteSpace(validation.ArrivalAirportCode);

    private bool CanSave => !string.IsNullOrWhiteSpace(validation.SeatNumber)
        && HasRouteAndNumber
        && HasTimes;

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(Value, _lastValue))
        {
            return;
        }

        _lastValue = Value;

        if (Value is null)
        {
            flightId = 0;
            validation = new ValidationModel();
            flightClass = FlightClass.Economy;
            notes = null;
            _tabIndex = PreferManualTab ? 2 : 0;
            return;
        }

        flightId = Value.FlightId;
        validation.SeatNumber = Value.SeatNumber;
        flightClass = Value.FlightClass;
        notes = Value.Notes;
        validation.FlightNumber = Value.FlightNumber;
        validation.DepartureAirportCode = Value.DepartureAirportCode;
        validation.ArrivalAirportCode = Value.ArrivalAirportCode;
        validation.DepartureTimeLocal = await ConvertUtcToLocalAsync(
            Value.DepartureTimeUtc,
            Value.DepartureAirportCode);
        validation.ArrivalTimeLocal = await ConvertUtcToLocalAsync(
            Value.ArrivalTimeUtc,
            Value.ArrivalAirportCode);
        validation.AircraftRegistration = Value.AircraftRegistration;
        validation.OperatingAirlineCode = Value.OperatingAirlineCode;

        _tabIndex = PreferManualTab ? 2 : 0;
    }

    private async Task SearchByNumber()
    {
        numberResults.Clear();

        if (string.IsNullOrWhiteSpace(numberSearchNumber))
        {
            return;
        }

        DateOnly? dateOnly = numberSearchDate.HasValue
            ? DateOnly.FromDateTime(numberSearchDate.Value.Date)
            : null;

        var results = await FlightsApiClient.LookupByDesignatorAsync(
            numberSearchNumber,
            dateOnly);

        numberResults = results.ToList();
    }

    private async Task SearchByRoute()
    {
        routeNoDateMatches = false;
        routeResults.Clear();

        if (string.IsNullOrWhiteSpace(routeDep)
            && string.IsNullOrWhiteSpace(routeArr))
        {
            return;
        }

        IReadOnlyList<Flight> results;

        if (routeDate.HasValue)
        {
            var dateOnly = DateOnly.FromDateTime(routeDate.Value.Date);
            var resultsWrapper = await Lookup.SearchByRouteAsync(
                routeDep,
                routeArr,
                dateOnly);
            results = resultsWrapper.IsSuccess
                ? resultsWrapper.Value ?? Array.Empty<Flight>()
                : Array.Empty<Flight>();

            if (results.Count == 0)
            {
                routeNoDateMatches = true;
                var fallbackWrapper = await Lookup.SearchByRouteAsync(
                    routeDep,
                    routeArr,
                    null);
                results = fallbackWrapper.IsSuccess
                    ? fallbackWrapper.Value ?? Array.Empty<Flight>()
                    : Array.Empty<Flight>();
            }
        }
        else
        {
            var resultsWrapper = await Lookup.SearchByRouteAsync(
                routeDep,
                routeArr,
                null);
            results = resultsWrapper.IsSuccess
                ? resultsWrapper.Value ?? Array.Empty<Flight>()
                : Array.Empty<Flight>();
        }

        routeResults = results.ToList();
    }

    private async Task SelectFlightFullAsync(Flight flight)
    {
        flightId = flight.Id;
        validation.FlightNumber = flight.FlightNumber;
        validation.DepartureAirportCode = flight.DepartureAirport?.IataCode
            ?? flight.DepartureAirport?.IcaoCode;
        validation.ArrivalAirportCode = flight.ArrivalAirport?.IataCode
            ?? flight.ArrivalAirport?.IcaoCode;
        validation.DepartureTimeLocal = await ConvertUtcToLocalAsync(
            flight.DepartureTimeUtc,
            validation.DepartureAirportCode);
        validation.ArrivalTimeLocal = await ConvertUtcToLocalAsync(
            flight.ArrivalTimeUtc,
            validation.ArrivalAirportCode);
        _tabIndex = 2;
        StateHasChanged();
    }

    private Task SelectFlightNoDateAsync(Flight flight)
    {
        flightId = flight.Id;
        validation.FlightNumber = flight.FlightNumber;
        validation.DepartureAirportCode = flight.DepartureAirport?.IataCode
            ?? flight.DepartureAirport?.IcaoCode;
        validation.ArrivalAirportCode = flight.ArrivalAirport?.IataCode
            ?? flight.ArrivalAirport?.IcaoCode;
        validation.DepartureTimeLocal = null;
        validation.ArrivalTimeLocal = null;
        _tabIndex = 2;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task SelectLookupCandidateFullAsync(FlightLookupCandidateApiResponse candidate)
    {
        flightId = candidate.FlightId ?? 0;
        validation.FlightNumber = candidate.FlightNumber;
        validation.DepartureAirportCode = candidate.DepartureCode;
        validation.ArrivalAirportCode = candidate.ArrivalCode;
        validation.OperatingAirlineCode = candidate.AirlineIcaoCode
            ?? candidate.AirlineIataCode;

        validation.DepartureTimeLocal = await ConvertUtcToLocalAsync(
            candidate.DepartureTimeUtc,
            validation.DepartureAirportCode);
        validation.ArrivalTimeLocal = await ConvertUtcToLocalAsync(
            candidate.ArrivalTimeUtc,
            validation.ArrivalAirportCode);

        _tabIndex = 2;
        StateHasChanged();
    }

    private Task SelectLookupCandidateNoDateAsync(FlightLookupCandidateApiResponse candidate)
    {
        flightId = candidate.FlightId ?? 0;
        validation.FlightNumber = candidate.FlightNumber;
        validation.DepartureAirportCode = candidate.DepartureCode;
        validation.ArrivalAirportCode = candidate.ArrivalCode;
        validation.DepartureTimeLocal = null;
        validation.ArrivalTimeLocal = null;
        validation.OperatingAirlineCode = candidate.AirlineIcaoCode
            ?? candidate.AirlineIataCode;

        _tabIndex = 2;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task HandleSave()
    {
        if (!CanSave || !OnSave.HasDelegate)
        {
            return;
        }

        var adjustedArrivalLocal = EnsureArrivalAfterDepartureLocal(
            validation.DepartureTimeLocal,
            validation.ArrivalTimeLocal);

        if (adjustedArrivalLocal != validation.ArrivalTimeLocal)
        {
            validation.ArrivalTimeLocal = adjustedArrivalLocal;
        }

        var depUtc = await ConvertLocalToUtcAsync(
            validation.DepartureTimeLocal,
            validation.DepartureAirportCode);
        var arrUtc = await ConvertLocalToUtcAsync(
            adjustedArrivalLocal,
            validation.ArrivalAirportCode);

        var dto = new CreateUserFlightDto
        {
            FlightId = flightId,
            FlightNumber = validation.FlightNumber,
            FlightDate = depUtc.HasValue
                ? DateOnly.FromDateTime(depUtc.Value.Date)
                : null,
            FlightClass = flightClass,
            SeatNumber = validation.SeatNumber!,
            Notes = notes,
            DepartureAirportCode = validation.DepartureAirportCode,
            ArrivalAirportCode = validation.ArrivalAirportCode,
            DepartureTimeUtc = depUtc,
            ArrivalTimeUtc = arrUtc,
            AircraftRegistration = validation.AircraftRegistration,
            OperatingAirlineCode = validation.OperatingAirlineCode
        };

        await OnSave.InvokeAsync(dto);
    }

    private async Task OnSaveClicked()
    {
        var ctx = editForm?.EditContext;
        if (ctx is not null && ctx.Validate())
        {
            await HandleSave();
        }
    }

    private async Task OnNumberKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchByNumber();
        }
    }

    private async Task OnRouteKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchByRoute();
        }
    }

    private void OnDepartureTimeChanged(DateTime? departureTimeLocal)
    {
        validation.DepartureTimeLocal = departureTimeLocal;
        validation.ArrivalTimeLocal = departureTimeLocal;
    }

    private async Task OnCancelClicked()
    {
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }
    }

    private async Task<DateTime?> ConvertUtcToLocalAsync(
        DateTime? utc,
        string? airportCode)
    {
        if (!utc.HasValue)
        {
            return null;
        }

        var tzId = await GetTimeZoneIdAsync(airportCode);
        if (string.IsNullOrWhiteSpace(tzId))
        {
            return DateTime.SpecifyKind(utc.Value, DateTimeKind.Unspecified);
        }

        try
        {
            var tz = ResolveTimeZone(tzId);
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc),
                tz);
        }
        catch
        {
            return DateTime.SpecifyKind(utc.Value, DateTimeKind.Unspecified);
        }
    }

    private async Task<DateTime?> ConvertLocalToUtcAsync(
        DateTime? local,
        string? airportCode)
    {
        if (!local.HasValue)
        {
            return null;
        }

        var tzId = await GetTimeZoneIdAsync(airportCode);
        if (string.IsNullOrWhiteSpace(tzId))
        {
            return DateTime.SpecifyKind(local.Value, DateTimeKind.Utc);
        }

        try
        {
            var tz = ResolveTimeZone(tzId);
            return TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(local.Value, DateTimeKind.Unspecified),
                tz);
        }
        catch
        {
            return DateTime.SpecifyKind(local.Value, DateTimeKind.Utc);
        }
    }

    private async Task<string?> GetTimeZoneIdAsync(string? airportCode)
    {
        if (string.IsNullOrWhiteSpace(airportCode))
        {
            return null;
        }

        _airports ??= await AirportsApiClient.GetAirportsAsync();
        var normalizedCode = airportCode.Trim();

        var airport = _airports.FirstOrDefault(a =>
            string.Equals(a.IataCode, normalizedCode,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(a.IcaoCode, normalizedCode,
                StringComparison.OrdinalIgnoreCase));

        return airport?.TimeZoneId;
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

    private static DateTime? EnsureArrivalAfterDepartureLocal(
        DateTime? departureLocal,
        DateTime? arrivalLocal)
    {
        if (!departureLocal.HasValue || !arrivalLocal.HasValue)
        {
            return arrivalLocal;
        }

        if (arrivalLocal.Value <= departureLocal.Value
            && arrivalLocal.Value.AddHours(30) >= departureLocal.Value)
        {
            var nextDay = departureLocal.Value.Date.AddDays(1);
            return nextDay + arrivalLocal.Value.TimeOfDay;
        }

        return arrivalLocal;
    }
}
