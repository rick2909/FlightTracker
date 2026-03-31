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

    [Inject]
    private IAircraftPhotoService AircraftPhotoService { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    protected UserFlightDto? Flight { get; private set; }

    protected DateFormat DateFormat { get; private set; } = DateFormat.YearMonthDay;

    protected TimeFormat TimeFormat { get; private set; } = TimeFormat.TwentyFourHour;

    protected MapFlightDto? MapFlight { get; private set; }

    protected AircraftPhotoDto? AircraftPhoto { get; private set; }

    protected bool PhotoLoading { get; private set; }

    protected override async Task OnParametersSetAsync()
    {
        Flight = null;
        MapFlight = null;
        AircraftPhoto = null;
        PhotoLoading = false;

        var dtoResult = await UserFlightService.GetByIdAsync(Id);
        if (dtoResult.IsFailure || dtoResult.Value is null)
        {
            return;
        }

        Flight = dtoResult.Value;

        var preferencesResult = await UserPreferencesService.GetOrCreateAsync(
            Flight.UserId);
        if (!preferencesResult.IsFailure && preferencesResult.Value is not null)
        {
            DateFormat = preferencesResult.Value.DateFormat;
            TimeFormat = preferencesResult.Value.TimeFormat;
        }

        if (Flight.DepartureLat.HasValue && Flight.DepartureLon.HasValue
            && Flight.ArrivalLat.HasValue && Flight.ArrivalLon.HasValue)
        {
            MapFlight = new MapFlightDto
            {
                FlightId = Flight.FlightId,
                FlightNumber = Flight.FlightNumber,
                DepartureTimeUtc = Flight.DepartureTimeUtc,
                ArrivalTimeUtc = Flight.ArrivalTimeUtc,
                DepartureAirportCode = Flight.DepartureIataCode
                    ?? Flight.DepartureIcaoCode
                    ?? Flight.DepartureAirportCode,
                ArrivalAirportCode = Flight.ArrivalIataCode
                    ?? Flight.ArrivalIcaoCode
                    ?? Flight.ArrivalAirportCode,
                DepartureLat = Flight.DepartureLat,
                DepartureLon = Flight.DepartureLon,
                ArrivalLat = Flight.ArrivalLat,
                ArrivalLon = Flight.ArrivalLon,
                IsUpcoming = Flight.DepartureTimeUtc >= DateTime.UtcNow
            };
        }

        if (Flight.Aircraft is not null)
        {
            PhotoLoading = true;
            _ = LoadAircraftPhotoAsync(Flight.Aircraft);
        }
    }

    private async Task LoadAircraftPhotoAsync(AircraftDto aircraft)
    {
        try
        {
            var result = await AircraftPhotoService.GetAircraftPhotosAsync(
                aircraft.ModeS,
                aircraft.Registration,
                maxResults: 1);

            if (result?.IsSuccess == true && result.Data.Count > 0)
            {
                AircraftPhoto = result.Data[0];
            }
        }
        catch
        {
            // Photo loading is best-effort; silently ignore errors.
        }
        finally
        {
            PhotoLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
