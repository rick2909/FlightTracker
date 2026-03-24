using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FlightTracker.Web.Components.Layout;

public partial class MainLayout
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private static readonly HashSet<string> NoSidebarRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/login",
        "/register",
        "/privacy"
    };

    private bool ShowSidebar { get; set; }

    private string PageTitle => ShowSidebar ? "FlightTracker" : string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        var relative = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var path = "/" + relative.Split('?', '#')[0].Trim('/');
        if (string.Equals(path, "/", StringComparison.Ordinal))
        {
            ShowSidebar = false;
            return;
        }

        var authState = AuthenticationStateTask is null
            ? null
            : await AuthenticationStateTask;

        ShowSidebar = authState?.User?.Identity?.IsAuthenticated == true
            && !NoSidebarRoutes.Contains(path);
    }
}
