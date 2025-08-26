using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Web.Controllers.Web;

/// <summary>
/// Airports overview page with a map and flight listings for a selected airport.
/// </summary>
[Route("[controller]")]
public class AirportsController(ILogger<AirportsController> logger,
    IAirportService airportService,
    IFlightRepository flightRepository) : Controller
{
    private readonly ILogger<AirportsController> _logger = logger;
    private readonly IAirportService _airportService = airportService;
    private readonly IFlightRepository _flightRepository = flightRepository;

    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Returns airports within the current map bounds, filtered by an inferred size threshold based on zoom.
    /// When zoomed out (zoom <= 3), returns none.
    /// </summary>
    [HttpGet("Browse")]
    public async Task<IActionResult> Browse(double north, double south, double east, double west, int zoom)
    {
        try
        {
            // Basic zoom threshold logic
            if (zoom <= 3)
            {
                return Json(Array.Empty<object>());
            }

            var airports = await _airportService.GetAllAirportsAsync(HttpContext.RequestAborted);

            // Filter by bounds when provided and coordinates exist
            bool crossesAntimeridian = east < west;
            bool hasBounds = !double.IsNaN(north) && !double.IsNaN(south) && !double.IsNaN(east) && !double.IsNaN(west);

            IEnumerable<Airport> inView = airports.Where(a => a.Latitude.HasValue && a.Longitude.HasValue);

            if (hasBounds)
            {
                inView = inView.Where(a =>
                {
                    var lat = a.Latitude!.Value;
                    var lon = a.Longitude!.Value;
                    var withinLat = lat <= north && lat >= south;
                    var withinLon = crossesAntimeridian
                        ? (lon >= west || lon <= east)
                        : (lon >= west && lon <= east);
                    return withinLat && withinLon;
                });
            }

            // Infer size by simple heuristic:
            // - Prefer airports with IATA code at lower zooms (likely larger/commercial)
            // - At higher zooms include all
            IEnumerable<Airport> filtered = inView;
            if (zoom is >= 4 and < 6)
            {
                filtered = filtered.Where(a => !string.IsNullOrWhiteSpace(a.IataCode));
            }
            else if (zoom is >= 6 and < 8)
            {
                // keep most, but still prefer those with IATA or city populated
                filtered = filtered.Where(a => !string.IsNullOrWhiteSpace(a.IataCode) || !string.IsNullOrWhiteSpace(a.City));
            }
            // zoom >= 8 -> show all in view

            var result = filtered.Select(a => new
            {
                id = a.Id,
                name = a.Name,
                city = a.City,
                country = a.Country,
                iata = a.IataCode,
                icao = a.IcaoCode,
                lat = a.Latitude,
                lon = a.Longitude
            });

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing airports");
            return StatusCode(500, new { error = "Failed to load airports" });
        }
    }

    /// <summary>
    /// Returns flights for an airport (both departing and arriving).
    /// </summary>
    [HttpGet("{id:int}/Flights")]
    public async Task<IActionResult> Flights(int id, string? dir = null)
    {
        try
        {
            var airports = await _airportService.GetAllAirportsAsync(HttpContext.RequestAborted);
            var airport = airports.FirstOrDefault(a => a.Id == id);
            if (airport is null)
            {
                return NotFound(new { error = "Airport not found" });
            }

            // Prefer IATA code for search, else ICAO, else name as fallback
            var code = airport.IataCode ?? airport.IcaoCode ?? airport.Name;

            // Fetch by route using repository to include nav properties
            var allDeparting = await _flightRepository.SearchByRouteAsync(code, null, null, HttpContext.RequestAborted);
            var allArriving = await _flightRepository.SearchByRouteAsync(null, code, null, HttpContext.RequestAborted);

            // Map to lightweight JSON
            var departing = allDeparting
                .OrderByDescending(f => f.DepartureTimeUtc)
                .Take(100)
                .Select(f => new
                {
                    id = f.Id,
                    flightNumber = f.FlightNumber,
                    route = $"{(f.DepartureAirport?.IataCode ?? f.DepartureAirport?.IcaoCode ?? f.DepartureAirport?.Name)} → {(f.ArrivalAirport?.IataCode ?? f.ArrivalAirport?.IcaoCode ?? f.ArrivalAirport?.Name)}",
                    airline = f.OperatingAirline?.Name,
                    aircraft = f.Aircraft != null ? (string.IsNullOrWhiteSpace(f.Aircraft.Registration) ? f.Aircraft.Model : $"{f.Aircraft.Model} • {f.Aircraft.Registration}") : null,
                    departureTimeUtc = f.DepartureTimeUtc.ToString("o", CultureInfo.InvariantCulture),
                    arrivalTimeUtc = f.ArrivalTimeUtc.ToString("o", CultureInfo.InvariantCulture)
                });

            var arriving = allArriving
                .OrderByDescending(f => f.DepartureTimeUtc)
                .Take(100)
                .Select(f => new
                {
                    id = f.Id,
                    flightNumber = f.FlightNumber,
                    route = $"{(f.DepartureAirport?.IataCode ?? f.DepartureAirport?.IcaoCode ?? f.DepartureAirport?.Name)} → {(f.ArrivalAirport?.IataCode ?? f.ArrivalAirport?.IcaoCode ?? f.ArrivalAirport?.Name)}",
                    airline = f.OperatingAirline?.Name,
                    aircraft = f.Aircraft != null ? (string.IsNullOrWhiteSpace(f.Aircraft.Registration) ? f.Aircraft.Model : $"{f.Aircraft.Model} • {f.Aircraft.Registration}") : null,
                    departureTimeUtc = f.DepartureTimeUtc.ToString("o", CultureInfo.InvariantCulture),
                    arrivalTimeUtc = f.ArrivalTimeUtc.ToString("o", CultureInfo.InvariantCulture)
                });

            object payload = dir?.ToLowerInvariant() switch
            {
                "departing" => new { departing },
                "arriving" => new { arriving },
                _ => new { departing, arriving }
            };

            return Json(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading flights for airport {AirportId}", id);
            return StatusCode(500, new { error = "Failed to load flights" });
        }
    }
}
