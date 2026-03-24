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
        var validationResult = await tokenService.ValidateTokenAsync(
            token,
            requiredScope,
            context.RequestAborted);

        if (validationResult.IsFailure)
        {
            _logger.LogWarning(
                "PAT validation failed for {Method} {Path}. Error: {ErrorCode}",
                context.Request.Method,
                context.Request.Path,
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
                "PAT rejected for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

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
            "PAT access accepted. TokenId={TokenId} UserId={UserId} Method={Method} Path={Path}",
            validationResult.Value.Id,
            validationResult.Value.UserId,
            context.Request.Method,
            context.Request.Path);

        await _next(context);

        _logger.LogInformation(
            "PAT access completed. TokenId={TokenId} StatusCode={StatusCode} Method={Method} Path={Path}",
            validationResult.Value.Id,
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path);
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
}
