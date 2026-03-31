using Microsoft.AspNetCore.Components;

namespace FlightTracker.Web.Pages;

public partial class LoginRedirect
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var current = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var returnUrl = "/" + current;
        NavigationManager.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }
}
