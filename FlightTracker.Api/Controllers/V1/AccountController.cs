using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers.V1;

/// <summary>Manages authenticated account profile operations that are not part of domain slices.</summary>
[ApiController]
[Authorize]
[Route("api/v1/users/{userId:int}/account")]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    IUserFlightService userFlightService,
    IUserPreferencesService userPreferencesService,
    IUsernameValidationService usernameValidationService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IUserFlightService _userFlightService = userFlightService;
    private readonly IUserPreferencesService _userPreferencesService = userPreferencesService;
    private readonly IUsernameValidationService _usernameValidationService = usernameValidationService;

    /// <summary>Returns the current account profile for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AccountProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountProfileResponse>> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Ok(Map(user));
    }

    /// <summary>Updates the account profile for the authenticated user.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(AccountProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountProfileResponse>> UpdateAsync(
        int userId,
        [FromBody] UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(user.FullName, request.FullName, StringComparison.Ordinal))
        {
            user.FullName = request.FullName.Trim();
        }

        var trimmedUserName = request.UserName.Trim();
        if (!string.Equals(user.UserName, trimmedUserName, StringComparison.Ordinal))
        {
            var usernameValidation = await _usernameValidationService.ValidateAsync(trimmedUserName, cancellationToken);
            if (usernameValidation.IsFailure || usernameValidation.Value is null)
            {
                return ValidationProblem(CreateValidationDetails(new Dictionary<string, string[]>
                {
                    [nameof(request.UserName)] = [usernameValidation.ErrorMessage ?? "Username validation failed."]
                }));
            }

            if (!usernameValidation.Value.IsValid)
            {
                return ValidationProblem(CreateValidationDetails(new Dictionary<string, string[]>
                {
                    [nameof(request.UserName)] = [usernameValidation.Value.ErrorMessage ?? "Invalid username."]
                }));
            }

            var setName = await _userManager.SetUserNameAsync(user, trimmedUserName);
            if (!setName.Succeeded)
            {
                return ValidationProblem(CreateValidationDetails(ToErrors(setName.Errors)));
            }
        }

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmail = await _userManager.SetEmailAsync(user, request.Email.Trim());
            if (!setEmail.Succeeded)
            {
                return ValidationProblem(CreateValidationDetails(ToErrors(setEmail.Errors)));
            }

            user.EmailConfirmed = true;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ValidationProblem(CreateValidationDetails(ToErrors(updateResult.Errors)));
        }

        return Ok(Map(user));
    }

    /// <summary>Changes the current password for the authenticated user.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangePasswordAsync(
        int userId,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return ValidationProblem(CreateValidationDetails(new Dictionary<string, string[]>
            {
                [nameof(request.ConfirmNewPassword)] = ["Password and confirmation password do not match."]
            }));
        }

        var passwordRegexValidations = new (Regex Pattern, string ErrorMessage)[]
        {
            (new Regex(@"[A-Z]"), "Password must contain at least one uppercase letter."),
            (new Regex(@"[a-z]"), "Password must contain at least one lowercase letter."),
            (new Regex(@"\d"), "Password must contain at least one digit."),
            (new Regex(@"[^A-Za-z0-9]"), "Password must contain at least one non-alphanumeric character.")
        };

        foreach (var (pattern, errorMessage) in passwordRegexValidations)
        {
            if (!pattern.IsMatch(request.NewPassword))
            {
                return ValidationProblem(CreateValidationDetails(new Dictionary<string, string[]>
                {
                    [nameof(request.NewPassword)] = [errorMessage]
                }));
            }
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            return ValidationProblem(CreateValidationDetails(ToErrors(result.Errors)));
        }

        return NoContent();
    }

    /// <summary>Exports all recorded flights for the authenticated user as CSV.</summary>
    [HttpGet("export/flights.csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportFlightsCsvAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        var flightsResult = await _userFlightService.GetUserFlightsAsync(userId, cancellationToken);
        if (flightsResult.IsFailure || flightsResult.Value is null)
        {
            return this.ToFailure(flightsResult);
        }

        var sb = new StringBuilder();
        sb.AppendLine("sep=,");
        sb.AppendLine("FlightNumber,DepartureTimeUtc,ArrivalTimeUtc,DepartureAirport,ArrivalAirport,FlightClass,SeatNumber,DidFly,BookedOnUtc,Notes");

        foreach (var f in flightsResult.Value)
        {
            static string Esc(string? value)
                => value is null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

            sb.Append(Esc(f.FlightNumber)); sb.Append(',');
            sb.Append(Esc(f.DepartureTimeUtc.ToString("o"))); sb.Append(',');
            sb.Append(Esc(f.ArrivalTimeUtc.ToString("o"))); sb.Append(',');
            sb.Append(Esc(f.DepartureIataCode ?? f.DepartureIcaoCode ?? f.DepartureAirportCode)); sb.Append(',');
            sb.Append(Esc(f.ArrivalIataCode ?? f.ArrivalIcaoCode ?? f.ArrivalAirportCode)); sb.Append(',');
            sb.Append(Esc(f.FlightClass.ToString())); sb.Append(',');
            sb.Append(Esc(f.SeatNumber)); sb.Append(',');
            sb.Append(f.DidFly ? "true" : "false"); sb.Append(',');
            sb.Append(Esc(f.BookedOnUtc.ToString("o"))); sb.Append(',');
            sb.Append(Esc(f.Notes));
            sb.AppendLine();
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return File(bytes, "text/csv; charset=utf-8", "user-flights.csv");
    }

    /// <summary>Exports the authenticated user's profile and flights as JSON.</summary>
    [HttpGet("export/all.json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAllJsonAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var flightsResult = await _userFlightService.GetUserFlightsAsync(userId, cancellationToken);
        if (flightsResult.IsFailure || flightsResult.Value is null)
        {
            return this.ToFailure(flightsResult);
        }

        var preferencesResult = await _userPreferencesService.GetOrCreateAsync(userId, cancellationToken);
        if (preferencesResult.IsFailure || preferencesResult.Value is null)
        {
            return this.ToFailure(preferencesResult);
        }

        var shaped = new
        {
            profile = new
            {
                fullName = user.FullName,
                userName = user.UserName,
                email = user.Email,
                preferences = new
                {
                    distanceUnit = preferencesResult.Value.DistanceUnit,
                    temperatureUnit = preferencesResult.Value.TemperatureUnit,
                    timeFormat = preferencesResult.Value.TimeFormat,
                    dateFormat = preferencesResult.Value.DateFormat,
                    profileVisibility = preferencesResult.Value.ProfileVisibility,
                    showTotalMiles = preferencesResult.Value.ShowTotalMiles,
                    showAirlines = preferencesResult.Value.ShowAirlines,
                    showCountries = preferencesResult.Value.ShowCountries,
                    showMapRoutes = preferencesResult.Value.ShowMapRoutes,
                    enableActivityFeed = preferencesResult.Value.EnableActivityFeed
                }
            },
            flights = flightsResult.Value.Select(f => new
            {
                flightNumber = f.FlightNumber,
                status = f.FlightStatus.ToString(),
                departureTimeUtc = f.DepartureTimeUtc,
                arrivalTimeUtc = f.ArrivalTimeUtc,
                departure = new
                {
                    code = f.DepartureIataCode ?? f.DepartureIcaoCode ?? f.DepartureAirportCode,
                    name = f.DepartureAirportName,
                    city = f.DepartureCity,
                    timeZoneId = f.DepartureTimeZoneId
                },
                arrival = new
                {
                    code = f.ArrivalIataCode ?? f.ArrivalIcaoCode ?? f.ArrivalAirportCode,
                    name = f.ArrivalAirportName,
                    city = f.ArrivalCity,
                    timeZoneId = f.ArrivalTimeZoneId
                },
                airline = new
                {
                    name = f.OperatingAirlineName,
                    iata = f.OperatingAirlineIataCode,
                    icao = f.OperatingAirlineIcaoCode
                },
                flightClass = f.FlightClass.ToString(),
                seatNumber = f.SeatNumber,
                didFly = f.DidFly,
                bookedOnUtc = f.BookedOnUtc,
                notes = f.Notes
            })
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(shaped, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return File(json, "application/json; charset=utf-8", "user-profile-export.json");
    }

    /// <summary>Deletes the authenticated user's profile and all recorded flights.</summary>
    [HttpDelete]
    [ProducesResponseType(typeof(DeleteAccountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeleteAccountResponse>> DeleteAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        var flightsResult = await _userFlightService.GetUserFlightsAsync(userId, cancellationToken);
        if (flightsResult.IsFailure || flightsResult.Value is null)
        {
            return this.ToFailure(flightsResult);
        }

        var deletedFlights = 0;
        foreach (var flight in flightsResult.Value)
        {
            var deleteResult = await _userFlightService.DeleteUserFlightAsync(flight.Id, cancellationToken);
            if (deleteResult.IsSuccess && deleteResult.Value)
            {
                deletedFlights++;
            }
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        var userDeleted = false;
        if (user is not null)
        {
            var deleteResult = await _userManager.DeleteAsync(user);
            userDeleted = deleteResult.Succeeded;
        }

        return Ok(new DeleteAccountResponse(deletedFlights, userDeleted));
    }

    private static AccountProfileResponse Map(ApplicationUser user)
    {
        return new AccountProfileResponse(
            user.Id,
            user.FullName,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty);
    }

    private static IDictionary<string, string[]> ToErrors(IEnumerable<IdentityError> errors)
    {
        return errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.Code) ? string.Empty : error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());
    }

    private static ValidationProblemDetails CreateValidationDetails(
        IDictionary<string, string[]> errors)
    {
        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest
        };
    }
}
