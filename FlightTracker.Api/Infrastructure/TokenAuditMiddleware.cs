using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using System.Security.Claims;

namespace FlightTracker.Api.Infrastructure;

/// <summary>
/// ASP.NET Core middleware that validates personal access tokens (PATs with the <c>ft_pat_*</c> prefix),
/// enforces required scopes, records usage, and projects a <see cref="System.Security.Claims.ClaimsPrincipal"/>
/// so downstream <c>[Authorize]</c> filters work without additional configuration.
/// </summary>
public sealed class TokenAuditMiddleware(
    RequestDelegate next,
    ILogger<TokenAuditMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<TokenAuditMiddleware> _logger = logger;

    /// <summary>Invokes the middleware for the current HTTP request.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        IPersonalAccessTokenService tokenService)
    {
        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        var token = ExtractBearerToken(authorizationHeader);

        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("ft_pat_", StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        var requiredScope = GetRequiredScope(context.Request.Method, context.Request.Path);
        var auditTarget = GetAuditTarget(context.Request.Path);
        var auditOperation = GetAuditOperation(
            context.Request.Method,
            requiredScope);
        var validationResult = await tokenService.ValidateTokenAsync(
            token,
            requiredScope,
            context.RequestAborted);

        if (validationResult.IsFailure)
        {
            _logger.LogWarning(
                "PAT validation failed. Operation={Operation} Target={Target} Error={ErrorCode}",
                auditOperation,
                auditTarget,
                validationResult.ErrorCode);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Invalid token",
                detail = validationResult.ErrorMessage ?? "Personal access token validation failed."
            });
            return;
        }

        if (validationResult.Value is null)
        {
            _logger.LogWarning(
                "PAT rejected. Operation={Operation} Target={Target}",
                auditOperation,
                auditTarget);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Unauthorized",
                detail = "Personal access token is invalid, expired, revoked, or missing required scope."
            });
            return;
        }

        context.Items["pat_token_id"] = validationResult.Value.Id;
        context.Items["pat_user_id"] = validationResult.Value.UserId;

        // Project PAT identity into ClaimsPrincipal so [Authorize] works.
        var identity = new ClaimsIdentity(
            authenticationType: "PersonalAccessToken",
            nameType: ClaimTypes.NameIdentifier,
            roleType: ClaimTypes.Role);
        identity.AddClaim(new Claim(
            ClaimTypes.NameIdentifier,
            validationResult.Value.UserId.ToString()));
        identity.AddClaim(new Claim("token_type", "pat"));
        identity.AddClaim(new Claim("pat_token_id", validationResult.Value.Id.ToString()));
        identity.AddClaim(new Claim("pat_scopes", validationResult.Value.Scopes.ToString()));
        context.User = new ClaimsPrincipal(identity);

        await tokenService.RecordUsageAsync(validationResult.Value.Id, context.RequestAborted);

        _logger.LogInformation(
            "PAT access accepted. TokenId={TokenId} UserId={UserId} Operation={Operation} Target={Target}",
            validationResult.Value.Id,
            validationResult.Value.UserId,
            auditOperation,
            auditTarget);

        await _next(context);

        _logger.LogInformation(
            "PAT access completed. TokenId={TokenId} StatusCode={StatusCode} Operation={Operation} Target={Target}",
            validationResult.Value.Id,
            context.Response.StatusCode,
            auditOperation,
            auditTarget);
    }

    private static string? ExtractBearerToken(string headerValue)
    {
        const string prefix = "Bearer ";
        if (!headerValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return headerValue[prefix.Length..].Trim();
    }

    private static PersonalAccessTokenScopes GetRequiredScope(
        string method,
        PathString path)
    {
        if (path.StartsWithSegments("/api/v1/stats") ||
            path.StartsWithSegments("/api/v1/passport"))
        {
            return PersonalAccessTokenScopes.ReadStats;
        }

        if (HttpMethods.IsGet(method))
        {
            return PersonalAccessTokenScopes.ReadFlights;
        }

        return PersonalAccessTokenScopes.WriteFlights;
    }

    private static string GetAuditOperation(
        string method,
        PersonalAccessTokenScopes requiredScope)
    {
        if (HttpMethods.IsGet(method))
        {
            return requiredScope == PersonalAccessTokenScopes.ReadStats
                ? "read-stats"
                : "read-flights";
        }

        return "write-flights";
    }

    private static string GetAuditTarget(PathString path)
    {
        if (path.StartsWithSegments("/api/v1/personal-access-tokens"))
        {
            return "personal-access-tokens";
        }

        if (path.StartsWithSegments("/api/v1/passport"))
        {
            return "passport";
        }

        if (path.StartsWithSegments("/api/v1/stats"))
        {
            return "stats";
        }

        if (path.StartsWithSegments("/api/v1/userflights"))
        {
            return "userflights";
        }

        if (path.StartsWithSegments("/api/v1/flights"))
        {
            return "flights";
        }

        if (path.StartsWithSegments("/api/v1/airports"))
        {
            return "airports";
        }

        if (path.StartsWithSegments("/api/v1/account"))
        {
            return "account";
        }

        return "unknown-api-target";
    }
}
