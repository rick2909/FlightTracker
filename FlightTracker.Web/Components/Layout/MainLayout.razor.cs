using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Globalization;

namespace FlightTracker.Web.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

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

    private string _currentPath = "/";

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        var relative = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var path = "/" + relative.Split('?', '#')[0].Trim('/');
        _currentPath = path;
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync(
            "flightTracker.setDocumentTitle",
            BuildDocumentTitle(_currentPath));
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var relative = NavigationManager.ToBaseRelativePath(args.Location);
        _currentPath = "/" + relative.Split('?', '#')[0].Trim('/');
    }

    private static string BuildDocumentTitle(string path)
    {
        var segments = path
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(static segment => !int.TryParse(segment, out _))
            .ToArray();

        var page = segments.Length == 0
            ? "Home"
            : string.Join(" ", segments.Select(ToTitleCase));

        return $"FlightTracker - {page}";
    }

    private static string ToTitleCase(string value)
    {
        var normalized = value
            .Replace('-', ' ')
            .Replace('_', ' ');

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }
}
