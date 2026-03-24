using System.Text.Json;
using FlightTracker.Application.Dtos;
using Microsoft.AspNetCore.Components;

namespace FlightTracker.Web.Components.Maps;

public partial class FlightMapPanel
{
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

    protected string FlightsJson { get; private set; } = "[]";

    protected string MapClass => "flight-map__map" + (Sticky ? " flight-map__map--sticky" : string.Empty);

    protected string MapStyle => Sticky
        ? $"height:{Height};"
        : $"height:clamp({MinHeight}, {Height}, {MaxVh}); max-height:clamp({MinHeight}, {Height}, {MaxVh});";

    protected override void OnParametersSet()
    {
        var list = MapFlights.ToList();
        MapFlightCount = list.Count;
        FlightsJson = JsonSerializer.Serialize(
            list,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
    }
}
