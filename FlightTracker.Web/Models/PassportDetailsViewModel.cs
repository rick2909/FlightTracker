using System.Collections.Generic;
using FlightTracker.Application.Dtos;

namespace YourApp.Models;

/// <summary>
/// ViewModel for Passport Details tab/section.
/// </summary>
public class PassportDetailsViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public Dictionary<string, int> FlightsByAirline { get; set; } = new();
    public Dictionary<string, int> FlightsByAircraftType { get; set; } = new();
    public List<AirlineStatsDto> AirlineStats { get; set; } = new();
   	public Dictionary<string, int> AircraftTypeStats { get; set; } = new();
}