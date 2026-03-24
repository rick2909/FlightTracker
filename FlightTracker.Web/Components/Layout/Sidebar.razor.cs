using System.Security.Claims;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace FlightTracker.Web.Components.Layout;

public partial class Sidebar : IDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IAccountApiClient AccountApiClient { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private string DisplayName { get; set; } = "Guest User";

    private string Initials { get; set; } = "GU";

    private string? ProfileImageUrl { get; set; }

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (AuthenticationStateTask is null)
        {
            return;
        }

        var state = await AuthenticationStateTask;
        var user = state.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            DisplayName = "Guest User";
            Initials = "GU";
            ProfileImageUrl = null;
            return;
        }

        DisplayName = user.FindFirstValue("display_name")
            ?? user.Identity?.Name
            ?? "User";
        Initials = BuildInitials(DisplayName);

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var parsedUserId))
        {
            ProfileImageUrl = null;
            return;
        }

        var profile = await AccountApiClient.GetAsync(parsedUserId);
        if (profile is not null)
        {
            DisplayName = string.IsNullOrWhiteSpace(profile.FullName)
                ? profile.UserName
                : profile.FullName;
            Initials = BuildInitials(DisplayName);
        }

        ProfileImageUrl = null;
    }

    private string ActiveClass(string prefix)
    {
        var current = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Split('?', '#')[0].Trim('/');
        return current.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? "is-active"
            : string.Empty;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }

    private static string BuildInitials(string name)
    {
        var parts = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "GU";
        }

        if (parts.Length == 1)
        {
            return new string(parts[0].Take(2).Select(char.ToUpperInvariant).ToArray());
        }

        return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[1][0]));
    }
}
