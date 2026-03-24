using System.ComponentModel.DataAnnotations;

namespace FlightTracker.Api.Contracts.V1;

/// <summary>Current account profile data for a user.</summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="FullName">Display name for the account.</param>
/// <param name="UserName">Unique username for sign-in and profile display.</param>
/// <param name="Email">Primary email address for the account.</param>
public sealed record AccountProfileResponse(
    int UserId,
    string FullName,
    string UserName,
    string Email);

/// <summary>Request body for updating the account profile.</summary>
/// <param name="FullName">New display name.</param>
/// <param name="UserName">New username.</param>
/// <param name="Email">New email address.</param>
public sealed record UpdateAccountProfileRequest(
    [property: Required, StringLength(64, MinimumLength = 3)] string FullName,
    [property: Required, StringLength(32, MinimumLength = 3)] string UserName,
    [property: Required, EmailAddress] string Email);

/// <summary>Request body for changing the current account password.</summary>
/// <param name="CurrentPassword">Current password for verification.</param>
/// <param name="NewPassword">Replacement password.</param>
/// <param name="ConfirmNewPassword">Confirmation of the replacement password.</param>
public sealed record ChangePasswordRequest(
    [property: Required] string CurrentPassword,
    [property: Required, StringLength(100, MinimumLength = 6)] string NewPassword,
    [property: Required] string ConfirmNewPassword);

/// <summary>Response payload for profile deletion.</summary>
/// <param name="DeletedFlights">Number of deleted user-flight records.</param>
/// <param name="UserDeleted">Whether the account record was deleted successfully.</param>
public sealed record DeleteAccountResponse(
    int DeletedFlights,
    bool UserDeleted);
