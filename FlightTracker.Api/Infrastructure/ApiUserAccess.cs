using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Infrastructure;

internal static class ApiUserAccess
{
    public static ActionResult? EnsureRouteUser(
        this ControllerBase controller,
        int routeUserId,
        bool allowPersonalAccessToken = true)
    {
        if (controller.User?.Identity?.IsAuthenticated != true)
        {
            return controller.Unauthorized();
        }

        if (!TryReadUserId(controller.User, out var callerUserId))
        {
            return controller.Unauthorized();
        }

        if (!allowPersonalAccessToken && IsPersonalAccessTokenPrincipal(controller.User))
        {
            return controller.Forbid();
        }

        if (callerUserId != routeUserId)
        {
            return controller.Forbid();
        }

        return null;
    }

    private static bool TryReadUserId(ClaimsPrincipal principal, out int userId)
    {
        userId = 0;

        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return int.TryParse(value, out userId);
    }

    private static bool IsPersonalAccessTokenPrincipal(ClaimsPrincipal principal)
    {
        var tokenType = principal.FindFirstValue("token_type");
        return string.Equals(tokenType, "pat", StringComparison.OrdinalIgnoreCase);
    }
}
