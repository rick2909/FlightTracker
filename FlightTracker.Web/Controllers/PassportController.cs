using Microsoft.AspNetCore.Mvc;
using YourApp.Models;
using FlightTracker.Application.Services.Interfaces;
using System.Security.Claims;
using FlightTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using FlightTracker.Domain.Enums;
using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Controllers;

[Route("Passport")]
public class PassportController : Controller
{
    private readonly IPassportService _passportService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserFlightService _flightService;

    public PassportController(IPassportService passportService, UserManager<ApplicationUser> userManager, IUserFlightService flightService)
    {
        _passportService = passportService;
        _userManager = userManager;
        _flightService = flightService;
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
        int? currentUserId = null;

        if (userId is null)
        {
            if (!TryGetCurrentUserId(out var currentId, out var challengeResult))
            {
                return challengeResult!;
            }

            currentUserId = currentId;
            userId = currentId;
        }
        else
        {
            TryGetCurrentUserId(out var currentId, out _);
            currentUserId = currentId == 0 ? null : currentId;
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

        var dataResult = await _passportService.GetPassportDataAsync(
            userId!.Value,
            cancellationToken);

        if (dataResult.IsFailure || dataResult.Value is null)
        {
            return Problem(
                title: "Unable to load passport data",
                detail: dataResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var data = dataResult.Value;

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

    [HttpGet("Details")]
    [HttpGet("{id?}/Details")]
    public async Task<IActionResult> Details(
        int? id,
        string? q,
        FlightClass? @class,
        bool? didFly,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Resolve user (prefer route id, else current user)
        int? userId = id;
        int? currentUserId = null;

        if (userId is null)
        {
            if (!TryGetCurrentUserId(out var currentId, out var challengeResult))
            {
                return challengeResult!;
            }

            currentUserId = currentId;
            userId = currentId;
        }
        else
        {
            TryGetCurrentUserId(out var currentId, out _);
            currentUserId = currentId == 0 ? null : currentId;
        }

        var displayedUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (displayedUser is null)
        {
            return NotFound();
        }

        var isOtherUser = currentUserId is null || userId.Value != currentUserId.Value;
        var displayName = isOtherUser
            ? (displayedUser.UserName ?? "Guest User")
            : (User?.Identity?.Name ?? displayedUser.UserName ?? "Guest User");
        string? avatarUrl = null;


        var passportDataResult = await _passportService.GetPassportDataAsync(
            userId.Value,
            cancellationToken);

        if (passportDataResult.IsFailure || passportDataResult.Value is null)
        {
            return Problem(
                title: "Unable to load passport data",
                detail: passportDataResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var airlineStatsResult = await _passportService.GetPassportDetailsAsync(
            userId.Value,
            cancellationToken);

        if (airlineStatsResult.IsFailure || airlineStatsResult.Value is null)
        {
            return Problem(
                title: "Unable to load passport details",
                detail: airlineStatsResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var passportData = passportDataResult.Value;
        var airlineStats = airlineStatsResult.Value;
        var userFlights = await _flightService.GetUserFlightsAsync(userId.Value, cancellationToken);

        userFlights = ApplyFilters(userFlights, q, @class, didFly, fromUtc, toUtc);
        var pageItems = Paginate(userFlights, ref page, ref pageSize, out var totalCount, out var totalPages);

        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalCount"] = totalCount;
        ViewData["TotalPages"] = totalPages;

        var vm = new PassportDetailsViewModel
        {
            UserName = displayName,
            AvatarUrl = avatarUrl,
            FlightsByAirline = passportData.FlightsByAirline,
            FlightsByAircraftType = passportData.FlightsByAircraftType,
            AirlineStats = airlineStats.AirlineStats,
            AircraftTypeStats = airlineStats.AircraftTypeStats,
            UserFlights = pageItems
        };

        return View(vm);
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

    private static IEnumerable<UserFlightDto> ApplyFilters(
        IEnumerable<UserFlightDto> flights,
        string? q,
        FlightClass? @class,
        bool? didFly,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            flights = flights.Where(f => f.FlightNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
        }
        if (@class.HasValue)
        {
            var cls = @class.Value;
            flights = flights.Where(f => f.FlightClass == cls);
        }
        if (didFly.HasValue)
        {
            var flag = didFly.Value;
            flights = flights.Where(f => f.DidFly == flag);
        }
        if (fromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(fromUtc.Value.Date, DateTimeKind.Utc);
            flights = flights.Where(f => f.DepartureTimeUtc >= from);
        }
        if (toUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(toUtc.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            flights = flights.Where(f => f.DepartureTimeUtc <= to);
        }
        return flights;
    }

    private static List<UserFlightDto> Paginate(
        IEnumerable<UserFlightDto> flights,
        ref int page,
        ref int pageSize,
        out int totalCount,
        out int totalPages)
    {
        var ordered = flights.OrderByDescending(f => f.DepartureTimeUtc);
        totalCount = ordered.Count();
        if (pageSize <= 0) pageSize = 20;
        if (page <= 0) page = 1;
        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page > totalPages && totalPages > 0) page = totalPages;
        return ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
