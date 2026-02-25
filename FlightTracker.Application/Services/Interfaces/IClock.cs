using System;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Abstraction for current UTC time.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
