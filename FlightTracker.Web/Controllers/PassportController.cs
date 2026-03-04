using Microsoft.AspNetCore.Mvc;
using YourApp.Models;
using FlightTracker.Application.Services.Interfaces;
using System.Security.Claims;
using FlightTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using FlightTracker.Domain.Enums;
using FlightTracker.Application.Dtos;
using FlightTracker.Web.Formatting;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Web.Controllers;

[Route("Passport")]
public class PassportController(
    IPassportService passportService,
    UserManager<ApplicationUser> userManager,
    IUserFlightService flightService,
    IUserPreferencesService userPreferencesService,
    ILogger<PassportController> logger) : Controller
{
    private readonly IPassportService _passportService = passportService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IUserFlightService _flightService = flightService;
    private readonly IUserPreferencesService _userPreferencesService = userPreferencesService;
    private readonly ILogger<PassportController> _logger = logger;

    [HttpGet("{id?}")]
    public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
    {
        int? userId = id;
        var currentUserId = 0;

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
            currentUserId = currentId == 0 ? 0 : currentId;
        }

        var isOtherUser = currentUserId == 0 || userId!.Value != currentUserId;

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

        var shownPreferencesResult = await _userPreferencesService.GetOrCreateAsync(
            userId.Value,
            cancellationToken);

        if (shownPreferencesResult.IsFailure || shownPreferencesResult.Value is null)
        {
            return Problem(
                title: "Unable to load user preferences",
                detail: shownPreferencesResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var shownPreferences = shownPreferencesResult.Value;

        if (isOtherUser && shownPreferences.ProfileVisibility == ProfileVisibilityLevel.Private)
        {
            return Forbid();
        }

        var viewerPreferences = await ResolveViewerPreferencesAsync(
            currentUserId,
            cancellationToken);

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
            TotalMiles = shownPreferences.ShowTotalMiles ? data.TotalMiles : 0,
            TotalDistanceDisplay = shownPreferences.ShowTotalMiles
                ? PreferenceFormatter.FormatDistanceFromMiles(
                    data.TotalMiles,
                    viewerPreferences.DistanceUnit)
                : "Hidden",
            FavoriteAirline = shownPreferences.ShowAirlines
                ? data.FavoriteAirline
                : string.Empty,
            FavoriteAirport = data.FavoriteAirport,
            MostFlownAircraftType = data.MostFlownAircraftType,
            FavoriteClass = data.FavoriteClass,
            LongestFlightMiles = shownPreferences.ShowTotalMiles
                ? data.LongestFlightMiles
                : 0,
            ShortestFlightMiles = shownPreferences.ShowTotalMiles
                ? data.ShortestFlightMiles
                : 0,
            LongestDistanceDisplay = shownPreferences.ShowTotalMiles
                ? PreferenceFormatter.FormatDistanceFromMiles(
                    data.LongestFlightMiles,
                    viewerPreferences.DistanceUnit)
                : "Hidden",
            ShortestDistanceDisplay = shownPreferences.ShowTotalMiles
                ? PreferenceFormatter.FormatDistanceFromMiles(
                    data.ShortestFlightMiles,
                    viewerPreferences.DistanceUnit)
                : "Hidden",
            DistanceUnit = viewerPreferences.DistanceUnit,
            DateFormat = viewerPreferences.DateFormat,
            TimeFormat = viewerPreferences.TimeFormat,
            ShowTotalMiles = shownPreferences.ShowTotalMiles,
            ShowAirlines = shownPreferences.ShowAirlines,
            ShowCountries = shownPreferences.ShowCountries,
            ShowMapRoutes = shownPreferences.ShowMapRoutes,
            AirlinesVisited = shownPreferences.ShowAirlines
                ? data.AirlinesVisited
                : new List<string>(),
            AirportsVisited = data.AirportsVisited,
            CountriesVisitedIso2 = shownPreferences.ShowCountries
                ? data.CountriesVisitedIso2
                : new List<string>(),
            FlightsPerYear = data.FlightsPerYear,
            FlightsByAirline = shownPreferences.ShowAirlines
                ? data.FlightsByAirline
                : new Dictionary<string, int>(),
            FlightsByAircraftType = data.FlightsByAircraftType,
            Routes = shownPreferences.ShowMapRoutes
                ? data.Routes
                : new List<MapFlightDto>()
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
        var currentUserId = 0;

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
            currentUserId = currentId == 0 ? 0 : currentId;
        }

        var displayedUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (displayedUser is null)
        {
            return NotFound();
        }

        var isOtherUser = currentUserId == 0 || userId.Value != currentUserId;
        var displayName = isOtherUser
            ? (displayedUser.UserName ?? "Guest User")
            : (User?.Identity?.Name ?? displayedUser.UserName ?? "Guest User");
        string? avatarUrl = null;

        var shownPreferencesResult = await _userPreferencesService.GetOrCreateAsync(
            userId.Value,
            cancellationToken);

        if (shownPreferencesResult.IsFailure || shownPreferencesResult.Value is null)
        {
            return Problem(
                title: "Unable to load user preferences",
                detail: shownPreferencesResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var shownPreferences = shownPreferencesResult.Value;

        if (isOtherUser
            && shownPreferences.ProfileVisibility
                == ProfileVisibilityLevel.Private)
        {
            return Forbid();
        }

        var viewerPreferences = await ResolveViewerPreferencesAsync(
            currentUserId,
            cancellationToken);


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
        var userFlightsResult = await _flightService.GetUserFlightsAsync(
            userId.Value,
            cancellationToken);

        if (userFlightsResult.IsFailure || userFlightsResult.Value is null)
        {
            return Problem(
                title: "Unable to load user flights",
                detail: userFlightsResult.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var userFlights = userFlightsResult.Value;

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
            DateFormat = viewerPreferences.DateFormat,
            TimeFormat = viewerPreferences.TimeFormat,
            ShowAirlines = shownPreferences.ShowAirlines,
            FlightsByAirline = shownPreferences.ShowAirlines
                ? passportData.FlightsByAirline
                : new Dictionary<string, int>(),
            FlightsByAircraftType = passportData.FlightsByAircraftType,
            AirlineStats = shownPreferences.ShowAirlines
                ? airlineStats.AirlineStats
                : new List<AirlineStatsDto>(),
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

    private async Task<UserPreferencesDto> ResolveViewerPreferencesAsync(int currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId <= 0)
        {
            _logger.LogDebug(
                "Viewer is anonymous. Using default viewer preferences.");
            return CreateDefaultViewerPreferences();
        }

        var viewerPreferencesResult = await _userPreferencesService.GetAsync(currentUserId, cancellationToken);

        if (viewerPreferencesResult.IsFailure
            || viewerPreferencesResult.Value is null)
        {
            _logger.LogWarning(
                "Viewer preferences unavailable for user {UserId}. "
                + "Using default viewer preferences. Error: {Error}",
                currentUserId,
                viewerPreferencesResult.ErrorMessage);
            return CreateDefaultViewerPreferences();
        }

        return viewerPreferencesResult.Value;
    }

    private static UserPreferencesDto CreateDefaultViewerPreferences()
    {
        return new UserPreferencesDto
        {
            DistanceUnit = DistanceUnit.Kilometers,
            DateFormat = DateFormat.DayMonthYear,
            TimeFormat = TimeFormat.TwentyFourHour
        };
    }
}
