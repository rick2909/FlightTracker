using System.Security.Claims;
using System.Text.RegularExpressions;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.Auth;
using FlightTracker.Web.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FlightTracker.Web.Pages;

public partial class Register
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private IUsernameValidationService UsernameValidationService { get; set; } = default!;

    [Inject]
    private IOptions<AuthSettings> AuthOptions { get; set; } = default!;

    private RegisterViewModel Model { get; } = new();

    private string? RegistrationError { get; set; }

    private bool EnableDevBypass => AuthOptions.Value.EnableDevBypass;

    private async Task RegisterAsync()
    {
        RegistrationError = null;
        const string registrationFailedMessage = "We couldn’t complete your registration. Please check your details.";

        var usernameValidation = await UsernameValidationService.ValidateAsync(Model.UserName.Trim());
        if (usernameValidation.IsFailure || usernameValidation.Value is null)
        {
            RegistrationError = usernameValidation.ErrorMessage ?? "Username validation failed.";
            return;
        }

        if (!usernameValidation.Value.IsValid)
        {
            RegistrationError = usernameValidation.Value.ErrorMessage ?? "Invalid username.";
            return;
        }

        var passwordRegexValidations = new (Regex pattern, string errorMessage)[]
        {
            (new Regex(@"[A-Z]"), "Password must contain at least one uppercase letter."),
            (new Regex(@"[a-z]"), "Password must contain at least one lowercase letter."),
            (new Regex(@"\d"), "Password must contain at least one digit."),
            (new Regex(@"[^A-Za-z0-9]"), "Password must contain at least one non-alphanumeric character.")
        };

        foreach (var (pattern, errorMessage) in passwordRegexValidations)
        {
            if (!pattern.IsMatch(Model.Password))
            {
                RegistrationError = errorMessage;
                return;
            }
        }

        var user = new ApplicationUser
        {
            FullName = Model.FullName.Trim(),
            UserName = Model.UserName.Trim(),
            Email = Model.Email.Trim().ToLowerInvariant()
        };

        var createResult = await UserManager.CreateAsync(user, Model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                if (string.Equals(error.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(error.Code, "DuplicateEmail", StringComparison.OrdinalIgnoreCase))
                {
                    RegistrationError = registrationFailedMessage;
                    break;
                }

                RegistrationError = error.Description;
                break;
            }

            return;
        }

        var claimResult = await UserManager.AddClaimAsync(user, new Claim("display_name", Model.FullName));
        if (!claimResult.Succeeded)
        {
            await UserManager.DeleteAsync(user);
            RegistrationError = "An unexpected error occurred during registration. Please try again.";
            return;
        }

        var userName = Uri.EscapeDataString(Model.UserName);
        NavigationManager.NavigateTo($"/login?registered=true&username={userName}", forceLoad: true);
    }
}
