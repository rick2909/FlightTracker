using Microsoft.AspNetCore.Identity;
using FlightTracker.Domain.Entities;
using System.Collections.Generic;

namespace FlightTracker.Infrastructure.Data;

/// <summary>
/// Application Identity user leveraging integer keys.
/// Extend with profile fields as needed.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    // Additional profile properties (e.g., FirstName, LastName) can be added later.
    [PersonalData]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for user's flight experiences.
    /// </summary>
    public ICollection<UserFlight> UserFlights { get; set; } = new List<UserFlight>();
}
