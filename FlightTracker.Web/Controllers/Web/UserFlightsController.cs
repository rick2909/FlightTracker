using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Web.Controllers.Web;

public class UserFlightsController(IUserFlightService userFlightService) : Controller
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
        return View(dto);
    }
}
