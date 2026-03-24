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

    protected string MapClass => "flight-map__map" + (Sticky ? " flight-map__map--sticky" : string.Empty);

    protected string MapStyle => Sticky
        ? $"height:{Height};"
        : $"height:clamp({MinHeight}, {Height}, {MaxVh}); max-height:clamp({MinHeight}, {Height}, {MaxVh});";

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
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync(
                "flightMapInitOrReload",
                MapElementId,
                MapDataElementId);
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
}
