using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FlightTracker.Application.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FlightTracker.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserFlightService _userFlightService;

    public SettingsController(UserManager<ApplicationUser> userManager, IUserFlightService userFlightService)
    {
        _userManager = userManager;
        _userFlightService = userFlightService;
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

        var vm = new SettingsViewModel
        {
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            ProfileVisibility = Request.Cookies["ft_profile_visibility"] ?? "private",
            Theme = Request.Cookies["ft_theme"] ?? "system"
        };
        ViewData["Title"] = "Settings";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Settings";
            return View("Index", model);
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

        if (!string.Equals(user.UserName, model.UserName, StringComparison.Ordinal))
        {
            var setName = await _userManager.SetUserNameAsync(user, model.UserName);
            if (!setName.Succeeded)
            {
                foreach (var e in setName.Errors) ModelState.AddModelError("UserName", e.Description);
            }
        }

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmail = await _userManager.SetEmailAsync(user, model.Email);
            if (!setEmail.Succeeded)
            {
                foreach (var e in setEmail.Errors) ModelState.AddModelError("Email", e.Description);
            }
            else
            {
                // If you require confirmation, you'd generate token & send email. For now mark confirmed to keep demo simple.
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Settings";
            return View("Index", model);
        }

        TempData["Status"] = "Profile updated";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnSettingsWithErrors();
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

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return await ReturnSettingsWithErrors();
        }
        TempData["Status"] = "Password changed";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePreferences([FromForm] PreferencesViewModel model)
    {
        // Persist simple preferences in cookies (1 year)
        var opts = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax };
        if (!string.IsNullOrWhiteSpace(model.Theme)) Response.Cookies.Append("ft_theme", model.Theme, opts);
        if (!string.IsNullOrWhiteSpace(model.ProfileVisibility)) Response.Cookies.Append("ft_profile_visibility", model.ProfileVisibility, opts);

        TempData["Status"] = "Preferences saved";
        return RedirectToAction(nameof(Index));
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

        var flights = await _userFlightService.GetUserFlightsAsync(userId, cancellationToken);

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
        var flights = await _userFlightService.GetUserFlightsAsync(userId, cancellationToken);

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
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteProfile(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId, out var challengeResult))
        {
            return challengeResult!;
        }
        // Delete user flights first
        var flights = await _userFlightService.GetUserFlightsAsync(userId, cancellationToken);
        var deletedFlights = 0;
        foreach (var f in flights)
        {
            if (await _userFlightService.DeleteUserFlightAsync(f.Id, cancellationToken))
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
