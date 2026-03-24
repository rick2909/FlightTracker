using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.Auth;
using FlightTracker.Web.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FlightTracker.Web.Pages;

public partial class Login
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;

    [Inject]
    private IOptions<AuthSettings> AuthOptions { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery(Name = "registered")]
    public bool Registered { get; set; }

    [SupplyParameterFromQuery(Name = "username")]
    public string? UserName { get; set; }

    private LoginViewModel Model { get; } = new();

    private string? ErrorMessage { get; set; }

    private bool RegistrationSuccess => Registered;

    private bool EnableDevBypass => AuthOptions.Value.EnableDevBypass;

    protected override void OnParametersSet()
    {
        Model.ReturnUrl = ReturnUrl;
        if (!string.IsNullOrWhiteSpace(UserName))
        {
            Model.UserNameOrEmail = UserName;
        }
    }

    private async Task LoginAsync()
    {
        ErrorMessage = null;

        var user = await UserManager.FindByNameAsync(Model.UserNameOrEmail);
        user ??= await UserManager.FindByEmailAsync(Model.UserNameOrEmail);

        if (user is null)
        {
            ErrorMessage = "Invalid credentials.";
            return;
        }

        var signInResult = await SignInManager.PasswordSignInAsync(
            user,
            Model.Password,
            Model.RememberMe,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            ErrorMessage = "Invalid credentials.";
            return;
        }

        if (IsLocalReturnUrl(Model.ReturnUrl))
        {
            NavigationManager.NavigateTo(Model.ReturnUrl!, forceLoad: true);
            return;
        }

        NavigationManager.NavigateTo("/Dashboard", forceLoad: true);
    }

    private static bool IsLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        return returnUrl.StartsWith("/", StringComparison.Ordinal)
            && !returnUrl.StartsWith("//", StringComparison.Ordinal);
    }
}
