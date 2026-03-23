using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FlightTracker.Web.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FlightTracker.Web.Api.Clients;

public sealed class FirstPartyApiBearerTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FlightTrackerApiOptions> apiOptions) : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly FlightTrackerApiOptions _apiOptions = apiOptions.Value;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var token = CreateTokenForCurrentUser();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    private string? CreateTokenForCurrentUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out _))
        {
            return null;
        }

        var authOptions = _apiOptions.FirstPartyAuth;
        if (string.IsNullOrWhiteSpace(authOptions.SigningKey) ||
            string.IsNullOrWhiteSpace(authOptions.Issuer) ||
            string.IsNullOrWhiteSpace(authOptions.Audience))
        {
            return null;
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(authOptions.SigningKey));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userIdClaim),
            new(JwtRegisteredClaimNames.Sub, userIdClaim),
            new("token_type", "first_party")
        };

        var token = new JwtSecurityToken(
            issuer: authOptions.Issuer,
            audience: authOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(authOptions.TokenLifetimeMinutes),
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
