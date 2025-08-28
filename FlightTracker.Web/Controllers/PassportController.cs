using Microsoft.AspNetCore.Mvc;
using YourApp.Models;
using FlightTracker.Application.Services.Interfaces;
using System.Security.Claims;
using FlightTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace FlightTracker.Web.Controllers;

[Route("Passport")]
public class PassportController : Controller
{
    private readonly IPassportService _passportService;
     private readonly UserManager<ApplicationUser> _userManager;

    public PassportController(IPassportService passportService, UserManager<ApplicationUser> userManager)
    {
        _passportService = passportService;
        _userManager = userManager;
    }
    [HttpGet("{id?}")]
    public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
    {
        // TODO make this an optional parameter that a user can block this and or later only can share it (temporary with an outher user).
        // Choose target user id:
        // - If id provided: use it.
        // - Else if authenticated: use current user id.
        // - Else: redirect back (or Dashboard) and render nothing.
        int? userId = id;
        int? currentUserId = 1;

        if (User?.Identity?.IsAuthenticated == true)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idStr, out var parsed))
            {
                currentUserId = parsed;
            }
        }

        if (userId is null)
        {
            if (currentUserId is null)
            {
                var referer = Request.Headers["Referer"].FirstOrDefault();
                // Only allow safe local redirects; otherwise go to Dashboard
                if (!string.IsNullOrWhiteSpace(referer) && Url.IsLocalUrl(referer))
                {
                    return Redirect(referer);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            userId = currentUserId;
        }

        var isOtherUser = currentUserId is null || userId!.Value != currentUserId.Value;

        var displayedUser = await _userManager.FindByIdAsync(userId!.Value.ToString());

        if (displayedUser is null)
        {
            return NotFound();
        }

        // Prefer DB user info when viewing another user's passport.
        var displayName = isOtherUser
            ? (displayedUser.UserName ?? "Guest User")
            : (User?.Identity?.Name ?? displayedUser.UserName ?? "Guest User");

        var avatarUrl = "";
        // var avatarUrl = isOtherUser
        //     ? (displayedUser.AvatarUrl ?? displayedUser.PictureUrl)
        //     : (User?.Claims?.FirstOrDefault(c => c.Type == "picture")?.Value
        //        ?? displayedUser.AvatarUrl ?? displayedUser.PictureUrl);

        if (displayedUser is null)
        {
            return NotFound();
        }

        var data = await _passportService.GetPassportDataAsync(userId!.Value, cancellationToken);

        var model = new PassportViewModel
        {
            UserName = displayName,
            AvatarUrl = avatarUrl,
            TotalFlights = data.TotalFlights,
            TotalMiles = data.TotalMiles,
            FavoriteAirline = data.FavoriteAirline,
            FavoriteAirport = data.FavoriteAirport,
            MostFlownAircraftType = data.MostFlownAircraftType,
            FavoriteClass = data.FavoriteClass,
            LongestFlightMiles = data.LongestFlightMiles,
            ShortestFlightMiles = data.ShortestFlightMiles,
            AirlinesVisited = data.AirlinesVisited,
            AirportsVisited = data.AirportsVisited,
            CountriesVisitedIso2 = data.CountriesVisitedIso2,
            FlightsPerYear = data.FlightsPerYear,
            FlightsByAirline = data.FlightsByAirline,
            FlightsByAircraftType = data.FlightsByAircraftType,
            Routes = data.Routes
        };
        return View(model);
    }
}
