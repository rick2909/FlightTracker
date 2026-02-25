namespace FlightTracker.Domain.Enums;

/// <summary>
/// Profile visibility level determining who can view a user's profile.
/// </summary>
public enum ProfileVisibilityLevel
{
    /// <summary>
    /// Only the user can see their profile and stats.
    /// </summary>
    Private = 0,
    
    /// <summary>
    /// Publicly accessible to anyone.
    /// </summary>
    Public = 1
    
    // Future: FriendsOnly = 2 (requires friend/follower feature)
}
