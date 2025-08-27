using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Web.Controllers;

public class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public SettingsController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
    var user = await _userManager.FindByIdAsync("1");

        var vm = new SettingsViewModel
        {
            UserName = user?.UserName ?? "demo",
            Email = user?.Email ?? "demo@example.com",
            ProfileVisibility = Request.Cookies["ft_profile_visibility"] ?? "private",
            Theme = Request.Cookies["ft_theme"] ?? "system"
        };
        ViewData["Title"] = "Settings";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Settings";
            return View("Index", model);
        }
    var user = await _userManager.FindByIdAsync("1");
        if (user == null)
        {
            // No auth configured yet: accept values but don't persist to DB
            TempData["Status"] = "Profile updated (session only)";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(user.UserName, model.UserName, StringComparison.Ordinal))
        {
            var setName = await _userManager.SetUserNameAsync(user, model.UserName);
            if (!setName.Succeeded)
            {
                foreach (var e in setName.Errors) ModelState.AddModelError("UserName", e.Description);
            }
        }

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmail = await _userManager.SetEmailAsync(user, model.Email);
            if (!setEmail.Succeeded)
            {
                foreach (var e in setEmail.Errors) ModelState.AddModelError("Email", e.Description);
            }
            else
            {
                // If you require confirmation, you'd generate token & send email. For now mark confirmed to keep demo simple.
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Settings";
            return View("Index", model);
        }

        TempData["Status"] = "Profile updated";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnSettingsWithErrors();
        }
    var user = await _userManager.FindByIdAsync("1");
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Password change requires sign-in.");
            return await ReturnSettingsWithErrors();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return await ReturnSettingsWithErrors();
        }
        TempData["Status"] = "Password changed";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePreferences([FromForm] PreferencesViewModel model)
    {
        // Persist simple preferences in cookies (1 year)
        var opts = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax };
        if (!string.IsNullOrWhiteSpace(model.Theme)) Response.Cookies.Append("ft_theme", model.Theme, opts);
        if (!string.IsNullOrWhiteSpace(model.ProfileVisibility)) Response.Cookies.Append("ft_profile_visibility", model.ProfileVisibility, opts);

        TempData["Status"] = "Preferences saved";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReturnSettingsWithErrors()
    {
    var user = await _userManager.FindByIdAsync("1");
        if (user == null)
        {
            var vmAnon = new SettingsViewModel
            {
                UserName = "demo",
                Email = "demo@example.com",
                ProfileVisibility = Request.Cookies["ft_profile_visibility"] ?? "private",
                Theme = Request.Cookies["ft_theme"] ?? "system"
            };
            ViewData["Title"] = "Settings";
            return View("Index", vmAnon);
        }
    var vm = new SettingsViewModel
        {
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            ProfileVisibility = Request.Cookies["ft_profile_visibility"] ?? "private",
            Theme = Request.Cookies["ft_theme"] ?? "system"
        };
        ViewData["Title"] = "Settings";
        return View("Index", vm);
    }
}
