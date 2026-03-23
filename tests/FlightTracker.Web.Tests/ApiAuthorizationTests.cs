using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Controllers.V1;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FlightTracker.Web.Tests;

public class ApiAuthorizationTests
{
    [Fact]
    public async Task Preferences_GetAsync_ReturnsUnauthorized_WhenUnauthenticated()
    {
        var service = new Mock<IUserPreferencesService>(MockBehavior.Strict);
        var controller = new PreferencesController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetAsync(42);

        Assert.IsType<UnauthorizedResult>(result.Result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Preferences_GetAsync_ReturnsForbid_WhenRouteUserMismatchesPrincipal()
    {
        var service = new Mock<IUserPreferencesService>(MockBehavior.Strict);
        var controller = new PreferencesController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = BuildHttpContextWithUser(userId: 7)
            }
        };

        var result = await controller.GetAsync(42);

        Assert.IsType<ForbidResult>(result.Result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PersonalAccessTokens_CreateAsync_ReturnsForbid_WhenPrincipalIsPat()
    {
        var service = new Mock<IPersonalAccessTokenService>(MockBehavior.Strict);
        var controller = new PersonalAccessTokensController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = BuildHttpContextWithUser(userId: 42, tokenType: "pat")
            }
        };

        var result = await controller.CreateAsync(
            42,
            new CreatePersonalAccessTokenRequest(
                "mobile",
                FlightTracker.Domain.Enums.PersonalAccessTokenScopes.ReadFlights,
                DateTime.UtcNow.AddDays(7)));

        Assert.IsType<ForbidResult>(result.Result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UserFlights_GetByIdAsync_ReturnsForbid_WhenFlightBelongsToAnotherUser()
    {
        var service = new Mock<IUserFlightService>();
        service
            .Setup(s => s.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserFlightDto>.Success(new UserFlightDto
            {
                Id = 11,
                UserId = 99
            }));

        var controller = new UserFlightsController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = BuildHttpContextWithUser(userId: 42)
            }
        };

        var result = await controller.GetByIdAsync(11);

        Assert.IsType<ForbidResult>(result.Result);
    }

    private static DefaultHttpContext BuildHttpContextWithUser(
        int userId,
        string? tokenType = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(tokenType))
        {
            claims.Add(new Claim("token_type", tokenType));
        }

        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext
        {
            User = principal
        };
    }
}
