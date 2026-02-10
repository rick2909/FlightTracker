using Microsoft.AspNetCore.Mvc;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Dtos;
using FlightTracker.Web.Models.ViewModels;

namespace FlightTracker.Web.Controllers.Web;

/// <summary>
/// Main dashboard controller for authenticated users.
/// Shows user's flight history, statistics, and quick actions.
/// </summary>
public class DashboardController(
    ILogger<DashboardController> logger,
    IUserFlightService userFlightService,
    IFlightService flightService,
IAirportService airportService,
IMapFlightService mapFlightService) : Controller
{
    private readonly ILogger<DashboardController> _logger = logger;
    private readonly IUserFlightService _userFlightService = userFlightService;
    private readonly IFlightService _flightService = flightService;
    private readonly IAirportService _airportService = airportService;
    private readonly IMapFlightService _mapFlightService = mapFlightService;

    /// <summary>
    /// Main dashboard view showing user's flight overview.
    /// </summary>
    /// <returns>Dashboard view with user flight data</returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            // TODO: Get actual user ID from authentication context
            // For now, using a hardcoded user ID (1 = admin user from seed data)
            var userId = 1;

            // Get user flight statistics
            var stats = await _userFlightService.GetUserFlightStatsAsync(userId);

            // Get recent user flights (last 5)
            var recentFlights = (await _userFlightService.GetUserFlightsAsync(userId))
                .Take(5)
                .ToList();

            // Create view model
            var mapFlights = await _mapFlightService.GetUserMapFlightsAsync(userId);
            var viewModel = new DashboardViewModel
            {
                Stats = stats,
                RecentFlights = recentFlights,
                UserId = userId,
                MapFlights = mapFlights
            };

            // Compute state sheet model for the next flight (if any) for the dashboard card
            var state = await BuildCurrentStateAsync(userId);
            ViewBag.FlightState = state; // pass to view as optional card

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user");

            // Return view with empty data in case of error
            var emptyViewModel = new DashboardViewModel
            {
                Stats = new UserFlightStatsDto { UserId = 1 },
                RecentFlights = new List<UserFlightDto>(),
                UserId = 1,
                ErrorMessage = "Unable to load dashboard data. Please try again."
            };

            return View(emptyViewModel);
        }
    }

    /// <summary>
    /// Shows user flight statistics and analytics.
    /// </summary>
    /// <returns>Statistics view</returns>
    public async Task<IActionResult> Statistics()
    {
        try
        {
            // TODO: Get actual user ID from authentication context
            var userId = 2;

            var stats = await _userFlightService.GetUserFlightStatsAsync(userId);
            var mapFlights = await _mapFlightService.GetUserMapFlightsAsync(userId, maxPast: 500, maxUpcoming: 50);

            // Aggregate flights per year from user's flights
            var allFlights = await _userFlightService.GetUserFlightsAsync(userId);
            var perYear = allFlights
                .GroupBy(f => f.DepartureTimeUtc.Year)
                .OrderBy(g => g.Key)
                .Select(g => new FlightTracker.Web.Models.ViewModels.StatsViewModel.FlightsPerYearPoint(g.Key, g.Count()))
                .ToList();

            var vm = new FlightTracker.Web.Models.ViewModels.StatsViewModel
            {
                UserId = userId,
                Stats = stats,
                MapFlights = mapFlights,
                FlightsPerYear = perYear
            };

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user statistics");
            var vm = new FlightTracker.Web.Models.ViewModels.StatsViewModel
            {
                UserId = 1,
                Stats = new UserFlightStatsDto { UserId = 1 },
                MapFlights = Array.Empty<MapFlightDto>(),
                FlightsPerYear = Array.Empty<FlightTracker.Web.Models.ViewModels.StatsViewModel.FlightsPerYearPoint>()
            };
            return View(vm);
        }
    }

    /// <summary>
    /// Full-page status view accessible from nav or a header button; shows map + state sheet.
    /// </summary>
    public async Task<IActionResult> Status()
    {
        try
        {
            var userId = 1;
            var state = await BuildCurrentStateAsync(userId);
            // Prefer single selected route if available; fall back to wider set
            IEnumerable<MapFlightDto> mapFlights;
            if (state.Route is not null)
            {
                mapFlights = new[] { state.Route };
            }
            else
            {
                mapFlights = await _mapFlightService.GetUserMapFlightsAsync(userId, maxPast: 10, maxUpcoming: 5);
            }
            var vm = new FlightTracker.Web.Models.ViewModels.FlightStatusPageViewModel
            {
                State = state,
                MapFlights = mapFlights
            };
            return View("Status", vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading status view");
            // Fallback: redirect to dashboard
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<FlightTracker.Web.Models.ViewModels.FlightStateViewModel> BuildCurrentStateAsync(int userId)
    {
        var flights = (await _userFlightService.GetUserFlightsAsync(userId)).OrderBy(f => f.DepartureTimeUtc).ToList();
        var now = DateTime.UtcNow;
        // Define next upcoming; if none, use most recent past
        var next = flights.FirstOrDefault(f => f.DepartureTimeUtc >= now);
        var prev = flights.LastOrDefault(f => f.ArrivalTimeUtc <= now) ?? flights.LastOrDefault();
        var chosen = next ?? prev ?? flights.FirstOrDefault();
        if (chosen is null)
        {
            // Empty placeholder state
            return new FlightTracker.Web.Models.ViewModels.FlightStateViewModel
            {
                State = FlightTracker.Web.Models.ViewModels.FlightStateViewModel.TravelState.PreFlight,
                Flight = new UserFlightDto { FlightNumber = "-", DepartureAirportCode = "-", ArrivalAirportCode = "-" },
                CountdownLabel = "No flights scheduled"
            };
        }

        // Estimate gates/terminals via detail lookup if available from Flight entity
        var flightEntity = await _flightService.GetFlightByIdAsync(chosen.FlightId);
        var state = new FlightTracker.Web.Models.ViewModels.FlightStateViewModel
        {
            Flight = chosen,
            DepartureTerminal = flightEntity?.DepartureTerminal,
            DepartureGate = flightEntity?.DepartureGate,
            ArrivalTerminal = flightEntity?.ArrivalTerminal,
            ArrivalGate = flightEntity?.ArrivalGate,
            BoardingStartUtc = flightEntity?.BoardingStartUtc,
            BoardingEndUtc = flightEntity?.BoardingEndUtc,
        };

        // Decide which state bucket we're in
        if (next is not null)
        {
            var untilDep = chosen.DepartureTimeUtc - now;
            state.State = untilDep > TimeSpan.FromHours(2)
                ? FlightTracker.Web.Models.ViewModels.FlightStateViewModel.TravelState.PreFlight
                : FlightTracker.Web.Models.ViewModels.FlightStateViewModel.TravelState.AtAirport;
            state.CountdownLabel = $"Gate Departure in {FormatDuration(untilDep)}";
            // Cute sample note; later could come from live provider
            state.Note = "Inbound aircraft has arrived";
        }
        else
        {
            state.State = FlightTracker.Web.Models.ViewModels.FlightStateViewModel.TravelState.PostFlight;
            var sinceArr = now - chosen.ArrivalTimeUtc;
            state.CountdownLabel = $"Arrived {FormatDuration(sinceArr)} ago";
        }

        // Provide single-route for status map zoom if available from map service (includes coordinates)
        try
        {
            var allRoutes = await _mapFlightService.GetUserMapFlightsAsync(userId, maxPast: 1000, maxUpcoming: 1000);
            var match = allRoutes.FirstOrDefault(r => r.FlightId == chosen.FlightId);
            if (match is not null)
            {
                state.Route = match;
            }
        }
        catch { }

        return state;
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero) ts = -ts;
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }
        return $"{ts.Minutes}m";
    }
}
