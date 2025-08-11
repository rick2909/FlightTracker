using Microsoft.AspNetCore.Identity;

namespace FlightTracker.Infrastructure.Data;

/// <summary>
/// Application Identity user leveraging integer keys.
/// Extend with profile fields as needed.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    // Additional profile properties (e.g., FirstName, LastName) can be added later.
}
