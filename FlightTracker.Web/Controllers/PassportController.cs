using Microsoft.AspNetCore.Mvc;
using YourApp.Models;
using FlightTracker.Application.Services.Interfaces;
using System.Security.Claims;

namespace FlightTracker.Web.Controllers;

[Route("Passport")]
public class PassportController : Controller
{
    private readonly IPassportService _passportService;

    public PassportController(IPassportService passportService)
    {
        _passportService = passportService;
    }
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Resolve current user id; fall back to demo user id = 0 (service can decide default) if unauthenticated
        int userId = 0;
        if (User?.Identity?.IsAuthenticated == true)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idStr, out var parsed)) userId = parsed;
        }

        var data = await _passportService.GetPassportDataAsync(userId, cancellationToken);

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
