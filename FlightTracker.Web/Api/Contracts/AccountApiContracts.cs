using System.ComponentModel.DataAnnotations;

namespace FlightTracker.Web.Api.Contracts;

public sealed record AccountProfileResponse(
    int UserId,
    string FullName,
    string UserName,
    string Email);

public sealed record UpdateAccountProfileRequest(
    [property: Required, StringLength(64, MinimumLength = 3)] string FullName,
    [property: Required, StringLength(32, MinimumLength = 3)] string UserName,
    [property: Required, EmailAddress] string Email);

public sealed record ChangePasswordRequest(
    [property: Required] string CurrentPassword,
    [property: Required, StringLength(100, MinimumLength = 6)] string NewPassword,
    [property: Required] string ConfirmNewPassword);

public sealed record DeleteAccountResponse(
    int DeletedFlights,
    bool UserDeleted);
