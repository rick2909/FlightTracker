using System;
using FlightTracker.Application.Services.Interfaces;

namespace FlightTracker.Infrastructure.Time;

/// <summary>
/// Production clock implementation using system UTC time.
/// </summary>
public class UtcClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
