using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FlightTracker.Application.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using AutoMapper;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserFlightService _userFlightService;
    private readonly IUsernameValidationService _usernameValidationService;
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly IMapper _mapper;

    public SettingsController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserFlightService userFlightService,
        IUsernameValidationService usernameValidationService,
        IUserPreferencesService userPreferencesService,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userFlightService = userFlightService;
        _usernameValidationService = usernameValidationService;
        _userPreferencesService = userPreferencesService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Challenge();
        }

        var preferences = await _userPreferencesService.GetOrCreateAsync(userId, default);

        var vm = new SettingsViewModel
        {
            FullName = user.FullName ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            ProfileVisibility = Request.Cookies["ft_profile_visibility"] ?? "private",
            Theme = Request.Cookies["ft_theme"] ?? "system"
        };
        
        // Set display & units from database
        vm.Preferences.DistanceUnit = preferences.DistanceUnit;
        vm.Preferences.TemperatureUnit = preferences.TemperatureUnit;
        vm.Preferences.TimeFormat = preferences.TimeFormat;
        vm.Preferences.DateFormat = preferences.DateFormat;
        
        // Set privacy & sharing from database
        vm.Preferences.ProfileVisibilityLevel = preferences.ProfileVisibility;
        vm.Preferences.ShowTotalMiles = preferences.ShowTotalMiles;
        vm.Preferences.ShowAirlines = preferences.ShowAirlines;
        vm.Preferences.ShowCountries = preferences.ShowCountries;
        vm.Preferences.ShowMapRoutes = preferences.ShowMapRoutes;
        vm.Preferences.EnableActivityFeed = preferences.EnableActivityFeed;
        
        ViewData["Title"] = "Settings";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, errors });
        }

        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Challenge();
        }

        if (!string.Equals(user.FullName, model.FullName, StringComparison.Ordinal))
        {
            user.FullName = model.FullName;
            // Update display_name claim
            var displayNameClaim = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "display_name");
            if (displayNameClaim != null)
            {
                var removeClaim = await _userManager.RemoveClaimAsync(user, displayNameClaim);
                if (!removeClaim.Succeeded)
                {
                    var errors = removeClaim.Errors.Select(e => e.Description).ToList();
                    return Json(new { success = false, errors });
                }
            }

            var addClaim = await _userManager.AddClaimAsync(
                user,
                new Claim("display_name", model.FullName));
            if (!addClaim.Succeeded)
            {
                var errors = addClaim.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, errors });
            }
        }

        var trimmedUserName = model.UserName.Trim();
        if (!string.Equals(user.UserName, trimmedUserName, StringComparison.Ordinal))
        {
            // Validate username against business rules
            var usernameValidation = await _usernameValidationService.ValidateAsync(trimmedUserName);
            if (!usernameValidation.IsValid)
            {
                return Json(new { success = false, errors = new[] { usernameValidation.ErrorMessage ?? "Invalid username." } });
            }

            var setName = await _userManager.SetUserNameAsync(user, trimmedUserName);
            if (!setName.Succeeded)
            {
                var errors = setName.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, errors });
            }
        }

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmail = await _userManager.SetEmailAsync(user, model.Email);
            if (!setEmail.Succeeded)
            {
                var errors = setEmail.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, errors });
            }
            else
            {
                // If you require confirmation, you'd generate token & send email. For now mark confirmed to keep demo simple.
                user.EmailConfirmed = true;
            }
        }

        var updateUser = await _userManager.UpdateAsync(user);
        if (!updateUser.Succeeded)
        {
            var errors = updateUser.Errors.Select(e => e.Description).ToList();
            return Json(new { success = false, errors });
        }

        // Refresh sign-in to update cookie with new claims
        await _signInManager.RefreshSignInAsync(user);

        TempData["Status"] = "Profile updated";
        return Json(new { success = true, displayName = user.FullName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, errors });
        }
        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Challenge();
        }

        // Validate password against regex requirements
        var passwordRegexValidations = new(Regex pattern, string errorMessage)[]
        {
            (new Regex(@"[A-Z]"), "Password must contain at least one uppercase letter."),
            (new Regex(@"[a-z]"), "Password must contain at least one lowercase letter."),
            (new Regex(@"\d"), "Password must contain at least one digit."),
            (new Regex(@"[^A-Za-z0-9]"), "Password must contain at least one non-alphanumeric character.")
        };

        foreach (var (pattern, errorMessage) in passwordRegexValidations)
        {
            if (!pattern.IsMatch(model.NewPassword))
            {
                ModelState.AddModelError(nameof(ChangePasswordViewModel.NewPassword), errorMessage);
            }
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, errors });
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Json(new { success = false, errors });
        }
        
        TempData["Status"] = "Password changed";
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePreferences([FromForm] PreferencesViewModel model, [FromServices] IWebHostEnvironment env, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, errors });
        }

        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }

        // Persist simple preferences in cookies (1 year)
        var opts = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
            Secure = !env.IsDevelopment()
        };
        if (!string.IsNullOrWhiteSpace(model.Theme)) Response.Cookies.Append("ft_theme", model.Theme, opts);
        if (!string.IsNullOrWhiteSpace(model.ProfileVisibility)) Response.Cookies.Append("ft_profile_visibility", model.ProfileVisibility, opts);

        // Persist display & units preferences to database
        var preferencesDto = _mapper.Map<UserPreferencesDto>(model);
        preferencesDto.UserId = userId;
        
        await _userPreferencesService.UpdateAsync(userId, preferencesDto, cancellationToken);

        TempData["Status"] = "Preferences saved";
        return Json(new { success = true });
    }

    private async Task<IActionResult> ReturnSettingsWithErrors()
    {
        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Challenge();
        }

        var vm = new SettingsViewModel
        {
            FullName = user.FullName ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            ProfileVisibility = Request.Cookies["ft_profile_visibility"] ?? "private",
            Theme = Request.Cookies["ft_theme"] ?? "system"
        };
        ViewData["Title"] = "Settings";
        return View("Index", vm);
    }

    // ===== Export =====
    [HttpGet("/Settings/Export/Flights.csv")]
    public async Task<IActionResult> ExportFlightsCsv(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }

        var flightsResult = await _userFlightService.GetUserFlightsAsync(
            userId,
            cancellationToken);

        if (flightsResult.IsFailure || flightsResult.Value is null)
        {
            return Problem(
                title: "Unable to export flights",
                detail: flightsResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var flights = flightsResult.Value;

        var sb = new StringBuilder();
        // Hint Excel about the separator to avoid locale issues (e.g., semicolon locales)
        sb.AppendLine("sep=,");
        sb.AppendLine("FlightNumber,DepartureTimeUtc,ArrivalTimeUtc,DepartureAirport,ArrivalAirport,FlightClass,SeatNumber,DidFly,BookedOnUtc,Notes");

        foreach (var f in flights)
        {
            static string Esc(string? v)
                => v is null ? string.Empty : "\"" + v.Replace("\"", "\"\"") + "\"";

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

        // Prepend UTF-8 BOM so Excel detects encoding and respects separator directive
        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return File(bytes, "text/csv; charset=utf-8", fileDownloadName: "user-flights.csv");
    }

    [HttpGet("/Settings/Export/All.json")]
    public async Task<IActionResult> ExportAllJson(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Challenge();
        }
        var flightsResult = await _userFlightService.GetUserFlightsAsync(
            userId,
            cancellationToken);

        if (flightsResult.IsFailure || flightsResult.Value is null)
        {
            return Problem(
                title: "Unable to export data",
                detail: flightsResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var flights = flightsResult.Value;

        // Collect preferences from cookies (client-side persistence in this demo)
        var theme = Request.Cookies["ft_theme"] ?? "system";
        var visibility = Request.Cookies["ft_profile_visibility"] ?? "private";

        var shaped = new
        {
            profile = new
            {
                userName = user?.UserName ?? "demo",
                email = user?.Email ?? "demo@example.com",
                preferences = new { theme, profileVisibility = visibility }
            },
            flights = flights.Select(f => new
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
        return File(json, "application/json; charset=utf-8", fileDownloadName: "user-profile-export.json");
    }

    // ===== Danger zone: delete all user flights (demo only) =====
    [HttpPost("/Settings/DeleteProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProfile(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }
        // Delete user flights first
        var flightsResult = await _userFlightService.GetUserFlightsAsync(
            userId,
            cancellationToken);

        if (flightsResult.IsFailure || flightsResult.Value is null)
        {
            return Problem(
                title: "Unable to delete profile",
                detail: flightsResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var flights = flightsResult.Value;
        var deletedFlights = 0;
        foreach (var f in flights)
        {
            var deleteResult = await _userFlightService.DeleteUserFlightAsync(
                f.Id,
                cancellationToken);

            if (deleteResult.IsSuccess && deleteResult.Value)
            {
                deletedFlights++;
            }
        }

        // Then delete the user profile
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var userDeleted = false;
        if (user != null)
        {
            var res = await _userManager.DeleteAsync(user);
            userDeleted = res.Succeeded;
        }

        return Json(new { deletedFlights, userDeleted });
    }

    private bool TryGetCurrentUserId(out int userId, out IActionResult? challengeResult)
    {
        userId = 0;
        challengeResult = null;

        if (User?.Identity?.IsAuthenticated != true)
        {
            challengeResult = Challenge();
            return false;
        }

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out userId))
        {
            challengeResult = Challenge();
            return false;
        }

        return true;
    }
}
