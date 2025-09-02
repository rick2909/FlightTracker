using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Web.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet("/register")]
        public IActionResult Register()
        {
            // For now, redirect to dashboard as a placeholder
            TempData["Message"] = "Registration functionality coming soon! For now, you can explore the demo dashboard.";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            // For now, redirect to dashboard as a placeholder
            TempData["Message"] = "Login functionality coming soon! For now, you can explore the demo dashboard.";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
