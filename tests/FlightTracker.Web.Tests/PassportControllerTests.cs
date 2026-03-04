using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Enums;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YourApp.Models;

namespace FlightTracker.Web.Tests;

public class PassportControllerTests
{
    [Fact]
    public async Task Index_ReturnsForbid_WhenOtherUserProfileIsPrivate()
    {
        var passportService = new Mock<IPassportService>(MockBehavior.Strict);
        var flightService = new Mock<IUserFlightService>(MockBehavior.Strict);
        var preferencesService = new Mock<IUserPreferencesService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<PassportController>>();
        var userManager = CreateUserManager();

        userManager
            .Setup(m => m.FindByIdAsync("2"))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 2,
                UserName = "private-user"
            });

        preferencesService
            .Setup(s => s.GetOrCreateAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserPreferencesDto>.Success(new UserPreferencesDto
            {
                UserId = 2,
                ProfileVisibility = ProfileVisibilityLevel.Private,
                ShowTotalMiles = true,
                ShowAirlines = true,
                ShowCountries = true,
                ShowMapRoutes = true
            }));

        preferencesService
            .Setup(s => s.GetAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserPreferencesDto?>.Success(null));

        var controller = new PassportController(
            passportService.Object,
            userManager.Object,
            flightService.Object,
            preferencesService.Object,
            logger.Object);

        SetAuthenticatedUser(controller, userId: 1, userName: "viewer");

        var result = await controller.Index(2, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        passportService.Verify(
            s => s.GetPassportDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_MasksFields_WhenVisibilityFlagsAreDisabled()
    {
        var passportService = new Mock<IPassportService>();
        var flightService = new Mock<IUserFlightService>(MockBehavior.Strict);
        var preferencesService = new Mock<IUserPreferencesService>();
        var logger = new Mock<ILogger<PassportController>>();
        var userManager = CreateUserManager();

        userManager
            .Setup(m => m.FindByIdAsync("10"))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 10,
                UserName = "owner"
            });

        preferencesService
            .Setup(s => s.GetOrCreateAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserPreferencesDto>.Success(new UserPreferencesDto
            {
                UserId = 10,
                ProfileVisibility = ProfileVisibilityLevel.Public,
                DistanceUnit = DistanceUnit.Kilometers,
                DateFormat = DateFormat.MonthDayYear,
                TimeFormat = TimeFormat.TwelveHour,
                ShowTotalMiles = false,
                ShowAirlines = false,
                ShowCountries = false,
                ShowMapRoutes = false
            }));

        preferencesService
            .Setup(s => s.GetAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserPreferencesDto?>.Success(new UserPreferencesDto
            {
                UserId = 10,
                DistanceUnit = DistanceUnit.NauticalMiles,
                DateFormat = DateFormat.DayMonthYear,
                TimeFormat = TimeFormat.TwelveHour
            }));

        passportService
            .Setup(s => s.GetPassportDataAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PassportDataDto>.Success(new PassportDataDto
            {
                TotalFlights = 5,
                TotalMiles = 1240,
                FavoriteAirline = "FT",
                FavoriteAirport = "JFK",
                MostFlownAircraftType = "A320",
                FavoriteClass = "Economy",
                LongestFlightMiles = 900,
                ShortestFlightMiles = 150,
                AirlinesVisited = new List<string> { "FT" },
                AirportsVisited = new List<string> { "JFK", "LHR" },
                CountriesVisitedIso2 = new List<string> { "us", "gb" },
                FlightsPerYear = new Dictionary<int, int> { { 2025, 5 } },
                FlightsByAirline = new Dictionary<string, int> { { "FT", 5 } },
                FlightsByAircraftType = new Dictionary<string, int> { { "A320", 5 } },
                Routes = new List<MapFlightDto>
                {
                    new()
                    {
                        FlightNumber = "FT100",
                        DepartureLat = 40.64,
                        DepartureLon = -73.78,
                        ArrivalLat = 51.47,
                        ArrivalLon = -0.45
                    }
                }
            }));

        var controller = new PassportController(
            passportService.Object,
            userManager.Object,
            flightService.Object,
            preferencesService.Object,
            logger.Object);

        SetAuthenticatedUser(controller, userId: 10, userName: "owner");

        var result = await controller.Index(null, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PassportViewModel>(view.Model);

        Assert.Equal(5, model.TotalFlights);
        Assert.Equal(0, model.TotalMiles);
        Assert.Equal("Hidden", model.TotalDistanceDisplay);
        Assert.Equal(string.Empty, model.FavoriteAirline);
        Assert.Empty(model.AirlinesVisited);
        Assert.Empty(model.CountriesVisitedIso2);
        Assert.Empty(model.FlightsByAirline);
        Assert.Empty(model.Routes);

        Assert.False(model.ShowTotalMiles);
        Assert.False(model.ShowAirlines);
        Assert.False(model.ShowCountries);
        Assert.False(model.ShowMapRoutes);
        Assert.Equal(DistanceUnit.NauticalMiles, model.DistanceUnit);
        Assert.Equal(DateFormat.DayMonthYear, model.DateFormat);
        Assert.Equal(TimeFormat.TwelveHour, model.TimeFormat);
    }

    [Fact]
    public async Task Index_UsesMetricDefaults_WhenViewerNotLoggedIn()
    {
        var passportService = new Mock<IPassportService>();
        var flightService = new Mock<IUserFlightService>(MockBehavior.Strict);
        var preferencesService = new Mock<IUserPreferencesService>();
        var logger = new Mock<ILogger<PassportController>>();
        var userManager = CreateUserManager();

        userManager
            .Setup(m => m.FindByIdAsync("99"))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 99,
                UserName = "public-user"
            });

        preferencesService
            .Setup(s => s.GetOrCreateAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserPreferencesDto>.Success(new UserPreferencesDto
            {
                UserId = 99,
                ProfileVisibility = ProfileVisibilityLevel.Public,
                ShowTotalMiles = true,
                ShowAirlines = true,
                ShowCountries = true,
                ShowMapRoutes = true
            }));

        passportService
            .Setup(s => s.GetPassportDataAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PassportDataDto>.Success(new PassportDataDto
            {
                TotalFlights = 1,
                TotalMiles = 100
            }));

        var controller = new PassportController(
            passportService.Object,
            userManager.Object,
            flightService.Object,
            preferencesService.Object,
            logger.Object);

        var result = await controller.Index(99, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PassportViewModel>(view.Model);

        Assert.Equal(DistanceUnit.Kilometers, model.DistanceUnit);
        Assert.Equal("161 km", model.TotalDistanceDisplay);
    }

    [Fact]
    public async Task Index_UsesMetricDefaults_WhenViewerPreferencesMissing()
    {
        var passportService = new Mock<IPassportService>();
        var flightService = new Mock<IUserFlightService>(MockBehavior.Strict);
        var preferencesService = new Mock<IUserPreferencesService>();
        var logger = new Mock<ILogger<PassportController>>();
        var userManager = CreateUserManager();

        userManager
            .Setup(m => m.FindByIdAsync("2"))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 2,
                UserName = "public-user"
            });

        preferencesService
            .Setup(s => s.GetOrCreateAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserPreferencesDto>.Success(new UserPreferencesDto
            {
                UserId = 2,
                ProfileVisibility = ProfileVisibilityLevel.Public,
                ShowTotalMiles = true,
                ShowAirlines = true,
                ShowCountries = true,
                ShowMapRoutes = true
            }));

        preferencesService
            .Setup(s => s.GetAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserPreferencesDto?>.Success(null));

        passportService
            .Setup(s => s.GetPassportDataAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PassportDataDto>.Success(new PassportDataDto
            {
                TotalFlights = 1,
                TotalMiles = 100
            }));

        var controller = new PassportController(
            passportService.Object,
            userManager.Object,
            flightService.Object,
            preferencesService.Object,
            logger.Object);

        SetAuthenticatedUser(controller, userId: 1, userName: "viewer");

        var result = await controller.Index(2, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PassportViewModel>(view.Model);

        Assert.Equal(DistanceUnit.Kilometers, model.DistanceUnit);
        Assert.Equal("161 km", model.TotalDistanceDisplay);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var normalizer = new UpperInvariantLookupNormalizer();
        var errorDescriber = new IdentityErrorDescriber();
        var serviceProvider = new Mock<IServiceProvider>();
        var logger = NullLogger<UserManager<ApplicationUser>>.Instance;

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            options,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            normalizer,
            errorDescriber,
            serviceProvider.Object,
            logger);
    }

    private static void SetAuthenticatedUser(
        Controller controller,
        int userId,
        string userName)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }
}