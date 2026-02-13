using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
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
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            SetDevViewBag();

            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpGet("/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            SetDevViewBag();

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
                SetDevViewBag();
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
                SetDevViewBag();
                return View(model);
            }

            var signInResult = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                SetDevViewBag();
                return View(model);
            }

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
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private void SetDevViewBag()
        {
            if (!_authSettings.EnableDevBypass )
            {
                return;
            }

            ViewBag.DevBypassEnabled = true;
            ViewBag.DevUserName = _authSettings.DevUserName;
            ViewBag.DevEmail = _authSettings.DevEmail;
        }
    }
}
