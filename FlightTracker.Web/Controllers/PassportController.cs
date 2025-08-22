using Microsoft.AspNetCore.Mvc;
using YourApp.Models;

namespace FlightTracker.Web.Controllers;

[Route("Passport")]
public class PassportController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = PassportMockData.Get();
        return View(model);
    }
}
