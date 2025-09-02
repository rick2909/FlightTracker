using System.Collections.Generic;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Models.ViewModels;

public class FlightStatusPageViewModel
{
    public FlightStateViewModel State { get; init; } = default!;
    public IEnumerable<MapFlightDto> MapFlights { get; init; } = new List<MapFlightDto>();
}
