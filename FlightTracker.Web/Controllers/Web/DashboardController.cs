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
            var userId = 2;

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
}
