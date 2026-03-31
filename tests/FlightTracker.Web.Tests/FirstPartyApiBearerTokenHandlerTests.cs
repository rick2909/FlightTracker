using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Web.Api.Clients;
using FlightTracker.Web.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FlightTracker.Web.Tests;

public class FirstPartyApiBearerTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsBearerToken_ForAuthenticatedUser()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "42")],
            "cookie"));

        var handler = new FirstPartyApiBearerTokenHandler(
            new HttpContextAccessor { HttpContext = context },
            Options.Create(new FlightTrackerApiOptions
            {
                FirstPartyAuth = new FirstPartyApiAuthOptions
                {
                    Issuer = "flighttracker-web",
                    Audience = "flighttracker-firstparty",
                    SigningKey = "dev-only-replace-this-with-a-long-random-secret-key-32-plus",
                    TokenLifetimeMinutes = 15
                }
            }))
        {
            InnerHandler = new CaptureHandler()
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost")
        };

        using var response = await client.GetAsync("/api/v1/preferences/users/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var capture = (CaptureHandler)handler.InnerHandler;
        Assert.NotNull(capture.LastRequest);
        Assert.NotNull(capture.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", capture.LastRequest.Headers.Authorization!.Scheme);
        Assert.False(string.IsNullOrWhiteSpace(capture.LastRequest.Headers.Authorization!.Parameter));

        var token = capture.LastRequest.Headers.Authorization!.Parameter!;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("flighttracker-web", jwt.Issuer);
        Assert.Contains("flighttracker-firstparty", jwt.Audiences);
        Assert.Equal("42", jwt.Subject);
    }

    [Fact]
    public async Task SendAsync_DoesNotAddBearerToken_WhenUnauthenticated()
    {
        var context = new DefaultHttpContext();

        var handler = new FirstPartyApiBearerTokenHandler(
            new HttpContextAccessor { HttpContext = context },
            Options.Create(new FlightTrackerApiOptions
            {
                FirstPartyAuth = new FirstPartyApiAuthOptions
                {
                    Issuer = "flighttracker-web",
                    Audience = "flighttracker-firstparty",
                    SigningKey = "dev-only-replace-this-with-a-long-random-secret-key-32-plus"
                }
            }))
        {
            InnerHandler = new CaptureHandler()
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost")
        };

        _ = await client.GetAsync("/api/v1/airports");

        var capture = (CaptureHandler)handler.InnerHandler;
        Assert.NotNull(capture.LastRequest);
        Assert.Null(capture.LastRequest!.Headers.Authorization);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }
}
