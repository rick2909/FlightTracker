using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.ViewModels;
using FlightTracker.Web.Models.Auth;
using Microsoft.AspNetCore.Authorization;

namespace FlightTracker.Web.Controllers
{
    public class AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOptions<AuthSettings> authOptions) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly AuthSettings _authSettings = authOptions.Value;

        [AllowAnonymous]
        [HttpGet("/register")]
        public IActionResult Register()
        {
            TempData["Message"] = "Registration functionality coming soon! Ask admin to create an account for you in the meantime.";
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        [HttpGet("/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (_authSettings.EnableDevBypass)
            {
                ViewBag.DevBypassEnabled = _authSettings.EnableDevBypass;
                ViewBag.DevUserName = _authSettings.DevUserName;
                ViewBag.DevEmail = _authSettings.DevEmail;
            }

            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [AllowAnonymous]
        [HttpPost("/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] LoginViewModel model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                if (_authSettings.EnableDevBypass)
                {
                    ViewBag.DevBypassEnabled = _authSettings.EnableDevBypass;
                    ViewBag.DevUserName = _authSettings.DevUserName;
                    ViewBag.DevEmail = _authSettings.DevEmail;
                }
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.UserNameOrEmail);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.UserNameOrEmail);
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                if (_authSettings.EnableDevBypass)
                {
                    ViewBag.DevBypassEnabled = _authSettings.EnableDevBypass;
                    ViewBag.DevUserName = _authSettings.DevUserName;
                    ViewBag.DevEmail = _authSettings.DevEmail;
                }
                return View(model);
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                if (_authSettings.EnableDevBypass)
                {
                    ViewBag.DevBypassEnabled = _authSettings.EnableDevBypass;
                    ViewBag.DevUserName = _authSettings.DevUserName;
                    ViewBag.DevEmail = _authSettings.DevEmail;
                }
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                });

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        [HttpPost("/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
