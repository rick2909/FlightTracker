using FlightTracker.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Infrastructure;

internal static class ApiResultMapper
{
    public static ActionResult ToFailure(
        this ControllerBase controller,
        Result result)
    {
        return controller.Problem(
            title: "Request failed",
            detail: result.ErrorMessage,
            statusCode: MapStatusCode(result.ErrorCode),
            type: result.ErrorCode);
    }

    private static int MapStatusCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return StatusCodes.Status500InternalServerError;
        }

        if (errorCode.Contains("not_found", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status404NotFound;
        }

        return StatusCodes.Status500InternalServerError;
    }
}
