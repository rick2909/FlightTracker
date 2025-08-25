using Microsoft.AspNetCore.Mvc;
using YourApp.Models;
using FlightTracker.Application.Services.Interfaces;
using System.Security.Claims;
using System.Linq;

namespace FlightTracker.Web.Controllers;

[Route("Passport")]
public class PassportController : Controller
{
    private readonly IPassportService _passportService;

    public PassportController(IPassportService passportService)
    {
        _passportService = passportService;
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
        if (userId is null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var parsed)) userId = parsed;
            }
            else
            {
                var referer = Request.Headers["Referer"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToAction("Index", "Dashboard");
            }
        }

        var data = await _passportService.GetPassportDataAsync(userId!.Value, cancellationToken);

        var model = new PassportViewModel
        {
            UserName = User?.Identity?.Name ?? "Guest User",
            AvatarUrl = User?.Claims?.FirstOrDefault(c => c.Type == "picture")?.Value,
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
            Routes = data.Routes
        };
        return View(model);
    }
}
