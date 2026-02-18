using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;

using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FlightTracker.Web.Controllers.Web;

[Authorize]
public class UserFlightsController(
    IUserFlightService userFlightService,
    IFlightService flightService,
    IFlightLookupService flightLookupService,
    IAircraftPhotoService aircraftPhotoService,
    IMapper mapper) : Controller
{
    [HttpGet("/UserFlights/{userId:int?}")]
    public async Task<IActionResult> Index(
        int? userId,
        string? q,
        FlightClass? @class,
        bool? didFly,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetEffectiveUserId(userId, out var effectiveUserId, out var challengeResult))
        {
            return challengeResult!;
        }
        var flights = await userFlightService.GetUserFlightsAsync(effectiveUserId, cancellationToken);
        ViewData["RequestedUserId"] = userId;

        flights = ApplyFilters(flights, q, @class, didFly, fromUtc, toUtc);
        var pageItems = Paginate(flights, ref page, ref pageSize, out var totalCount, out var totalPages);

        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalCount"] = totalCount;
        ViewData["TotalPages"] = totalPages;

        return View(pageItems);
    }

    [HttpGet("/UserFlights/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var dto = await userFlightService.GetByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound();
        }
        return View(dto);
    }

    [HttpGet("/UserFlights/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var dto = await userFlightService.GetByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound();
        }
        var vm = mapper.Map<EditUserFlightViewModel>(dto);
        return View(vm);
    }

    [HttpPost("/UserFlights/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] EditUserFlightViewModel form, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            // Reload current values for display when validation fails
            var current = await userFlightService.GetByIdAsync(id, cancellationToken);
            if (current == null)
            {
                return NotFound();
            }
            // map back into vm
            var vm = mapper.Map<EditUserFlightViewModel>(current);
            return View(vm);
        }
        try
        {
            var userFlightDto = mapper.Map<UpdateUserFlightDto>(form);
            var scheduleDto = mapper.Map<FlightScheduleUpdateDto>(form);

            var updated = await userFlightService.UpdateUserFlightAndScheduleAsync(
                id,
                userFlightDto,
                scheduleDto,
                cancellationToken);
            if (updated == null)
            {
                return NotFound();
            }

            // Redirect to Details after successful update
            return RedirectToAction("Details", new { id });
        }
        catch (FluentValidation.ValidationException vex)
        {
            foreach (var error in vex.Errors)
            {
                // Map validator property names to our form fields
                var key = error.PropertyName;
                ModelState.AddModelError(key, error.ErrorMessage);
            }

            // Return the view with the same form values so validation messages render
            return View(form);
        }
    }

    // Placeholder API endpoint to load/refresh flight details from external providers.
    // Will call lookup/services later; for now returns current UserFlightDto.
    [HttpGet("/UserFlights/{id:int}/LoadFromApi")]
    public async Task<IActionResult> LoadFromApi(int id, CancellationToken cancellationToken = default)
    {
        var dto = await userFlightService.GetByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound(new { status = "not_found", message = "User flight not found." });
        }

        // Try lookup based on flight number and departure date
        var date = DateOnly.FromDateTime(dto.DepartureTimeUtc);
        var candidate = await flightLookupService.ResolveFlightAsync(dto.FlightNumber, date, cancellationToken);

        if (candidate is null)
            return NotFound(new { status = "not_found", message = "No flight found via lookup." });

        var currentFlight = await flightService.GetFlightByIdAsync(dto.FlightId, cancellationToken);

        if (currentFlight is null)
            return NotFound(new { status = "not_found", message = "Current flight not found." });

        var noChanges = currentFlight.HasSameScheduleAndRoute(candidate);

        if (noChanges)
            return Ok(new { status = "no_changes", message = "No changes found." });

        // Return minimal delta payload for now (no DB update yet)
        var depCode = candidate.DepartureAirport?.IataCode ?? candidate.DepartureAirport?.IcaoCode;
        var arrCode = candidate.ArrivalAirport?.IataCode ?? candidate.ArrivalAirport?.IcaoCode;
        return Ok(new
        {
            status = "changes",
            changes = new
            {
                flightNumber = candidate.FlightNumber,
                departureTimeUtc = candidate.DepartureTimeUtc,
                arrivalTimeUtc = candidate.ArrivalTimeUtc,
                departureAirportId = candidate.DepartureAirportId,
                arrivalAirportId = candidate.ArrivalAirportId,
                departureAirportCode = depCode,
                arrivalAirportCode = arrCode
            }
        });
    }

    /// <summary>
    /// API endpoint to fetch aircraft photos via the backend service (avoids CORS issues).
    /// </summary>
    [HttpGet("/api/aircraft-photos")]
    public async Task<IActionResult> GetAircraftPhoto(string? modeSCode, string? registration, int maxResults = 1, CancellationToken cancellationToken = default)
    {
        const int MinResults = 1;
        const int MaxResults = 5;

        if (string.IsNullOrWhiteSpace(modeSCode) && string.IsNullOrWhiteSpace(registration))
        {
            return BadRequest(new { error = "Either modeSCode or registration must be provided" });
        }

        if (maxResults < MinResults || maxResults > MaxResults)
        {
            return BadRequest(new { error = $"maxResults must be between {MinResults} and {MaxResults}." });
        }

        var result = await aircraftPhotoService.GetAircraftPhotosAsync(modeSCode, registration, maxResults, cancellationToken);
        if (result == null)
        {
            return NotFound(new { error = "No photos found" });
        }

        return Ok(result);
    }

    private bool TryGetEffectiveUserId(int? requestedUserId, out int userId, out IActionResult? challengeResult)
    {
        userId = 0;
        challengeResult = null;

        if (requestedUserId.HasValue && requestedUserId.Value > 0)
        {
            userId = requestedUserId.Value;
            return true;
        }

        return TryGetCurrentUserId(out userId, out challengeResult);
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
