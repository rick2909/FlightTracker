using FlightTracker.Application.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FlightTracker.Web.Components.Maps;

public partial class FlightMapSearch
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public IEnumerable<MapFlightDto> MapFlights { get; set; } = Array.Empty<MapFlightDto>();

    private string? _search;

    private int _activeIndex = -1;

    private List<string> _currentFiltered = new();

    private List<string> suggestions = new();

    private string? Search
    {
        get => _search;
        set
        {
            if (_search == value)
            {
                return;
            }

            _search = value;
            _ = InvokeAsync(async () =>
            {
                await JS.InvokeVoidAsync("flightMapFilter", _search);
                StateHasChanged();
            });
        }
    }

    private IEnumerable<string> Filtered => string.IsNullOrWhiteSpace(_search)
        ? suggestions
        : suggestions
            .Where(s => s.Contains(_search, StringComparison.OrdinalIgnoreCase))
            .Take(20);

    protected override void OnParametersSet()
    {
        suggestions = MapFlights
            .Select(f => $"{f.FlightNumber} {f.DepartureAirportCode}->{f.ArrivalAirportCode}")
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        RebuildFiltered();
    }

    private Task OnInput(ChangeEventArgs e)
    {
        Search = e.Value?.ToString();
        _activeIndex = -1;
        RebuildFiltered();
        return Task.CompletedTask;
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (!_currentFiltered.Any())
        {
            return;
        }

        if (e.Key == "ArrowDown")
        {
            _activeIndex = (_activeIndex + 1) % _currentFiltered.Count;
            StateHasChanged();
            return;
        }

        if (e.Key == "ArrowUp")
        {
            _activeIndex = _activeIndex <= 0
                ? _currentFiltered.Count - 1
                : _activeIndex - 1;
            StateHasChanged();
            return;
        }

        if (e.Key == "Enter")
        {
            if (_activeIndex >= 0 && _activeIndex < _currentFiltered.Count)
            {
                await Select(_currentFiltered[_activeIndex]);
            }
            else
            {
                await JS.InvokeVoidAsync("flightMapZoomToFiltered");
            }
        }
    }

    private void RebuildFiltered()
    {
        _currentFiltered = Filtered.ToList();
        if (_activeIndex >= _currentFiltered.Count)
        {
            _activeIndex = -1;
        }
    }

    private async Task Select(string item)
    {
        _search = item;
        try
        {
            await JS.InvokeVoidAsync("flightMapFilter", _search);
            await JS.InvokeVoidAsync("flightMapZoomToSelection", _search);
        }
        catch (JSException)
        {
        }

        _activeIndex = -1;
        RebuildFiltered();
    }

    private async Task Clear()
    {
        _search = string.Empty;
        _activeIndex = -1;
        try
        {
            await JS.InvokeVoidAsync("flightMapFilter", (string?)null);
        }
        catch (JSException)
        {
        }

        RebuildFiltered();
    }
}