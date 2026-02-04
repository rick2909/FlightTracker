using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;

using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Web.Controllers.Web;

public class UserFlightsController(IUserFlightService userFlightService, IFlightService flightService, IFlightLookupService flightLookupService, IMapper mapper) : Controller
{
    private const int DemoUserId = 1; // TODO: Replace with actual auth user id when wired

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
        var effectiveUserId = GetEffectiveUserId(userId);
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
            var updated = await userFlightService.UpdateUserFlightAndScheduleAsync(
                id,
                new UpdateUserFlightDto
                {
                    FlightClass = form.FlightClass,
                    SeatNumber = form.SeatNumber,
                    Notes = form.Notes,
                    DidFly = form.DidFly
                },
                new FlightScheduleUpdateDto
                {
                    FlightId = form.FlightId,
                    FlightNumber = form.FlightNumber,
                    DepartureAirportCode = form.DepartureAirportCode ?? string.Empty,
                    ArrivalAirportCode = form.ArrivalAirportCode ?? string.Empty,
                    DepartureTimeUtc = form.DepartureTimeUtc,
                    ArrivalTimeUtc = form.ArrivalTimeUtc,
                    AircraftRegistration = form.AircraftRegistration,
                    OperatingAirlineCode = form.OperatingAirlineCode
                },
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
    [HttpPost("/UserFlights/{id:int}/LoadFromApi")]
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
        {
            return NotFound(new { status = "not_found", message = "No flight found via lookup." });
        }

        var currentFlight = await flightService.GetFlightByIdAsync(dto.FlightId, cancellationToken);
    if (currentFlight is null)
        {
            return NotFound(new { status = "not_found", message = "Current flight not found." });
        }

    var noChanges = currentFlight.HasSameScheduleAndRoute(candidate);

    if (noChanges)
        {
            return Ok(new { status = "no_changes", message = "No changes found." });
        }

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

    private static int GetEffectiveUserId(int? requestedUserId)
    {
        // Replace with actual current user id from auth when available
        return (requestedUserId.HasValue && requestedUserId.Value > 0) ? requestedUserId.Value : DemoUserId;
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
