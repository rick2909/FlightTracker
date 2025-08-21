using System;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Web.Models;

public class EditUserFlightViewModel
{
    // Route/ids
    public int UserFlightId { get; set; }
    public int FlightId { get; set; }

    // User flight fields
    public FlightClass FlightClass { get; set; } = FlightClass.Economy;
    public string SeatNumber { get; set; } = string.Empty;
    public bool DidFly { get; set; } = true;
    public string? Notes { get; set; }

    // Flight fields (manual edit)
    public string FlightNumber { get; set; } = string.Empty;
    public string? DepartureAirportCode { get; set; }
    public string? ArrivalAirportCode { get; set; }
    public DateTime DepartureTimeUtc { get; set; }
    public DateTime ArrivalTimeUtc { get; set; }
}
