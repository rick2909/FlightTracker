using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.ViewModels;
using FlightTracker.Web.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using FlightTracker.Application.Services.Interfaces;

namespace FlightTracker.Web.Controllers
{
    public class AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOptions<AuthSettings> authOptions,
        IUsernameValidationService usernameValidationService) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly AuthSettings _authSettings = authOptions.Value;
        private readonly IUsernameValidationService _usernameValidationService = usernameValidationService;

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
        [HttpPost("/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromForm] RegisterViewModel model, CancellationToken cancellationToken = default)
        {            cancellationToken.ThrowIfCancellationRequested();
                        const string registrationFailedMessage = "We couldn’t complete your registration. Please check your details.";

            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (!ModelState.IsValid)
            {
                SetDevViewBag();
                return View(model);
            }

            // Validate username against business rules
            var usernameValidation = await _usernameValidationService.ValidateAsync(model.UserName.Trim(), cancellationToken);
            if (!usernameValidation.IsValid)
            {
                ModelState.AddModelError(nameof(RegisterViewModel.UserName), usernameValidation.ErrorMessage ?? "Invalid username.");
                SetDevViewBag();
                return View(model);
            }

            // Validate password against regex requirements
            var passwordRegexValidations = new(Regex pattern, string errorMessage)[]
            {
                (new Regex(@"[A-Z]"), "Password must contain at least one uppercase letter."),
                (new Regex(@"[a-z]"), "Password must contain at least one lowercase letter."),
                (new Regex(@"\d"), "Password must contain at least one digit."),
                (new Regex(@"[^\w]"), "Password must contain at least one non-alphanumeric character.")
            };

            foreach (var (pattern, errorMessage) in passwordRegexValidations)
            {
                if (!pattern.IsMatch(model.Password))
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.Password), errorMessage);
                }
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim().ToLowerInvariant()
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    if (string.Equals(error.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(error.Code, "DuplicateEmail", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("RegistrationFailed", registrationFailedMessage);
                        continue;
                    }
                    
                    if (string.Equals(error.Code, "InvalidUserName", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(error.Code, "InvalidUserNameCharacters", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(error.Code, "ProhibitedUserName", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(RegisterViewModel.UserName), error.Description);
                        continue;
                    }

                    ModelState.AddModelError(string.Empty, error.Description);
                }

                SetDevViewBag();
                return View(model);
            }

            await _userManager.AddClaimAsync(user, new Claim("display_name", model.FullName));

            TempData["RegistrationSuccess"] = true;
            TempData["RegisteredUserName"] = model.UserName;
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

            SetDevViewBag();

            var userName = TempData["RegisteredUserName"] as string;
            return View(new LoginViewModel
            {
                UserNameOrEmail = userName,
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
