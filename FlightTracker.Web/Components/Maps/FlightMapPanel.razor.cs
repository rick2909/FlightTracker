using System.Text.Json;
using FlightTracker.Application.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FlightTracker.Web.Components.Maps;

public partial class FlightMapPanel
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter]
    public IEnumerable<MapFlightDto> MapFlights { get; set; } = Array.Empty<MapFlightDto>();

    [Parameter]
    public string Height { get; set; } = "50vh";

    [Parameter]
    public bool Sticky { get; set; }

    [Parameter]
    public string MinHeight { get; set; } = "420px";

    [Parameter]
    public string MaxVh { get; set; } = "85vh";

    protected int MapFlightCount { get; private set; }

    protected string MapElementId { get; } = $"flightMap-{Guid.NewGuid():N}";

    protected string MapDataElementId { get; } = $"flightMapData-{Guid.NewGuid():N}";

    protected MarkupString FlightsJsonMarkup { get; private set; } = new("[]");

    private bool _mapInitialized;

    private int _mapInitAttempts;

    private const int MaxMapInitAttempts = 25;

    protected string MapClass => "flight-map__map" + (Sticky ? " flight-map__map--sticky" : string.Empty);

    protected string MapStyle => Sticky
        ? $"width:100%;min-height:{MinHeight};height:{Height};"
        : $"width:100%;min-height:{MinHeight};height:{Height};max-height:{MaxVh};";

    protected override void OnParametersSet()
    {
        var list = MapFlights.ToList();
        MapFlightCount = list.Count;
        var flightsJson = JsonSerializer.Serialize(
            list,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        FlightsJsonMarkup = new MarkupString(flightsJson);

        _mapInitialized = false;
        _mapInitAttempts = 0;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_mapInitialized)
        {
            return;
        }

        try
        {
            var initialized = await JsRuntime.InvokeAsync<bool>(
                "flightMapInitOrReload",
                MapElementId,
                MapDataElementId);

            if (initialized)
            {
                _mapInitialized = true;
            }
            else
            {
                await RetryMapInitAsync();
            }
        }
        catch (JSException)
        {
            await RetryMapInitAsync();
        }
        catch (InvalidOperationException)
        {
            await RetryMapInitAsync();
        }
        catch (JSDisconnectedException)
        {
            // Ignore transient disconnects during navigation/teardown.
        }
        catch (TaskCanceledException)
        {
            // Ignore canceled invocations during rapid route changes.
        }
    }

    private async Task RetryMapInitAsync()
    {
        _mapInitAttempts++;
        if (_mapInitAttempts >= MaxMapInitAttempts)
        {
            return;
        }

        await Task.Delay(200);
        await InvokeAsync(StateHasChanged);
    }
}
