using FlightTracker.Domain.Enums;
using System;

namespace FlightTracker.Domain.Entities;

/// <summary>
/// Represents a user's specific flight experience, tracking which user flew on which flight
/// with details like seat assignment and class of service.
/// </summary>
public class UserFlight
{
    public int Id { get; set; }

    /// <summary>
    /// Reference to the user who took this flight.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Reference to the flight that was taken.
    /// </summary>
    public int FlightId { get; set; }

    /// <summary>
    /// The class of service the user flew in.
    /// </summary>
    public FlightClass FlightClass { get; set; } = FlightClass.Economy;

    /// <summary>
    /// The seat assignment (e.g., "12A", "1B", "34F").
    /// </summary>
    public string SeatNumber { get; set; } = string.Empty;

    /// <summary>
    /// When the user booked or recorded this flight experience.
    /// </summary>
    public DateTime BookedOnUtc { get; set; }

    /// <summary>
    /// Optional notes about the user's experience on this flight.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the user actually flew (true) or was a no-show/cancelled (false).
    /// </summary>
    public bool DidFly { get; set; } = true;

    // Navigation properties
    public Flight? Flight { get; set; }
}
