using System.Threading;
using System.Threading.Tasks;
// using already present above
using FlightTracker.Application.Dtos;
using FlightTracker.Web.Models;
using FlightTracker.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

using FlightTracker.Application.Services.Interfaces;

namespace FlightTracker.Web.Controllers.Web;

public class UserFlightsController(IUserFlightService userFlightService, IFlightService flightService, IFlightLookupService flightLookupService) : Controller
{
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
        var vm = new EditUserFlightViewModel
        {
            UserFlightId = dto.Id,
            FlightId = dto.FlightId,
            FlightClass = dto.FlightClass,
            SeatNumber = dto.SeatNumber,
            DidFly = dto.DidFly,
            Notes = dto.Notes,
            FlightNumber = dto.FlightNumber,
            DepartureAirportCode = dto.DepartureIataCode ?? dto.DepartureIcaoCode ?? dto.DepartureAirportCode,
            ArrivalAirportCode = dto.ArrivalIataCode ?? dto.ArrivalIcaoCode ?? dto.ArrivalAirportCode,
            DepartureTimeUtc = dto.DepartureTimeUtc,
            ArrivalTimeUtc = dto.ArrivalTimeUtc
        };
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
            var vm = new EditUserFlightViewModel
            {
                UserFlightId = current.Id,
                FlightId = current.FlightId,
                FlightClass = current.FlightClass,
                SeatNumber = current.SeatNumber,
                DidFly = current.DidFly,
                Notes = current.Notes,
                FlightNumber = current.FlightNumber,
                DepartureAirportCode = current.DepartureIataCode ?? current.DepartureIcaoCode ?? current.DepartureAirportCode,
                ArrivalAirportCode = current.ArrivalIataCode ?? current.ArrivalIcaoCode ?? current.ArrivalAirportCode,
                DepartureTimeUtc = current.DepartureTimeUtc,
                ArrivalTimeUtc = current.ArrivalTimeUtc
            };
            return View(vm);
        }
        // Update flight details (manual changes)
        var flight = await flightService.GetFlightByIdAsync(form.FlightId, cancellationToken);
        if (flight is null)
        {
            return NotFound();
        }

        // Map mutable flight fields
        flight.FlightNumber = form.FlightNumber;
        if (!string.IsNullOrWhiteSpace(form.DepartureAirportCode))
        {
            // TODO: Resolve code to airport entity via IAirportService if necessary
        }
        if (!string.IsNullOrWhiteSpace(form.ArrivalAirportCode))
        {
        }
        flight.DepartureTimeUtc = form.DepartureTimeUtc;
        flight.ArrivalTimeUtc = form.ArrivalTimeUtc;
        await flightService.UpdateFlightAsync(flight, cancellationToken);

        // Update user-flight fields
        var update = new CreateUserFlightDto
        {
            FlightId = form.FlightId,
            FlightClass = form.FlightClass,
            SeatNumber = form.SeatNumber,
            Notes = form.Notes,
            DidFly = form.DidFly
        };
        var updated = await userFlightService.UpdateUserFlightAsync(id, update, cancellationToken);
        if (updated == null)
        {
            return NotFound();
        }

        // Redirect to Details after successful update
    return RedirectToAction("Details", new { id });
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

        // Current behavior: simulate external not found until providers are integrated
        var simulateExternalNotFound = true;
        if (simulateExternalNotFound)
        {
            return NotFound(new { status = "not_found", message = "No flight found via lookup (not implemented)." });
        }

        // Future: try lookup based on flight number and departure date
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

        var noChanges = string.Equals(candidate.FlightNumber, currentFlight.FlightNumber, StringComparison.OrdinalIgnoreCase)
                        && candidate.DepartureTimeUtc == currentFlight.DepartureTimeUtc
                        && candidate.ArrivalTimeUtc == currentFlight.ArrivalTimeUtc
                        && candidate.DepartureAirportId == currentFlight.DepartureAirportId
                        && candidate.ArrivalAirportId == currentFlight.ArrivalAirportId;

        if (noChanges)
        {
            return Ok(new { status = "no_changes", message = "No changes found." });
        }

        // Return minimal delta payload for now (no DB update yet)
        return Ok(new
        {
            status = "changes",
            changes = new
            {
                flightNumber = candidate.FlightNumber,
                departureTimeUtc = candidate.DepartureTimeUtc,
                arrivalTimeUtc = candidate.ArrivalTimeUtc,
                departureAirportId = candidate.DepartureAirportId,
                arrivalAirportId = candidate.ArrivalAirportId
            }
        });
    }
}
