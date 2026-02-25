using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class UserPreferencesServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_ReturnsExistingPreferences_WhenPresent()
    {
        var existing = new UserPreferences
        {
            Id = 10,
            UserId = 42,
            DistanceUnit = DistanceUnit.Kilometers,
            TemperatureUnit = TemperatureUnit.Fahrenheit,
            TimeFormat = TimeFormat.TwelveHour,
            DateFormat = DateFormat.MonthDayYear,
            ProfileVisibility = ProfileVisibilityLevel.Public,
            ShowTotalMiles = false,
            ShowAirlines = false,
            ShowCountries = true,
            ShowMapRoutes = false,
            EnableActivityFeed = true,
            CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var repository = new Mock<IUserPreferencesRepository>();
        repository
            .Setup(r => r.GetByUserIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var mapper = CreateMapper();
        var service = new UserPreferencesService(repository.Object, mapper.Object);

        var result = await service.GetOrCreateAsync(42);

        Assert.Equal(10, result.Id);
        Assert.Equal(42, result.UserId);
        Assert.Equal(DistanceUnit.Kilometers, result.DistanceUnit);
        Assert.Equal(TemperatureUnit.Fahrenheit, result.TemperatureUnit);
        Assert.Equal(TimeFormat.TwelveHour, result.TimeFormat);
        Assert.Equal(DateFormat.MonthDayYear, result.DateFormat);
        Assert.Equal(ProfileVisibilityLevel.Public, result.ProfileVisibility);
        Assert.False(result.ShowTotalMiles);
        Assert.False(result.ShowAirlines);
        Assert.True(result.ShowCountries);
        Assert.False(result.ShowMapRoutes);
        Assert.True(result.EnableActivityFeed);

        repository.Verify(r => r.CreateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesDefaults_WhenMissing()
    {
        var before = DateTime.UtcNow;
        UserPreferences? createdInput = null;

        var repository = new Mock<IUserPreferencesRepository>();
        repository
            .Setup(r => r.GetByUserIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences preferences, CancellationToken _) =>
            {
                createdInput = preferences;
                preferences.Id = 77;
                return preferences;
            });

        var mapper = CreateMapper();
        var service = new UserPreferencesService(repository.Object, mapper.Object);

        var result = await service.GetOrCreateAsync(7);
        var after = DateTime.UtcNow;

        Assert.NotNull(createdInput);
        Assert.Equal(7, createdInput!.UserId);
        Assert.Equal(DistanceUnit.Miles, createdInput.DistanceUnit);
        Assert.Equal(TemperatureUnit.Celsius, createdInput.TemperatureUnit);
        Assert.Equal(TimeFormat.TwentyFourHour, createdInput.TimeFormat);
        Assert.Equal(DateFormat.YearMonthDay, createdInput.DateFormat);
        Assert.Equal(ProfileVisibilityLevel.Private, createdInput.ProfileVisibility);
        Assert.True(createdInput.ShowTotalMiles);
        Assert.True(createdInput.ShowAirlines);
        Assert.True(createdInput.ShowCountries);
        Assert.True(createdInput.ShowMapRoutes);
        Assert.False(createdInput.EnableActivityFeed);
        Assert.InRange(createdInput.CreatedAtUtc, before, after);
        Assert.InRange(createdInput.UpdatedAtUtc, before, after);

        Assert.Equal(77, result.Id);
        Assert.Equal(7, result.UserId);

        repository.Verify(r => r.CreateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExisting_AndPersistsAllFields()
    {
        var initialUpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existing = new UserPreferences
        {
            Id = 15,
            UserId = 9,
            DistanceUnit = DistanceUnit.Miles,
            TemperatureUnit = TemperatureUnit.Celsius,
            TimeFormat = TimeFormat.TwentyFourHour,
            DateFormat = DateFormat.YearMonthDay,
            ProfileVisibility = ProfileVisibilityLevel.Private,
            ShowTotalMiles = true,
            ShowAirlines = true,
            ShowCountries = true,
            ShowMapRoutes = true,
            EnableActivityFeed = false,
            CreatedAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = initialUpdatedAt
        };

        var update = new UserPreferencesDto
        {
            UserId = 9,
            DistanceUnit = DistanceUnit.NauticalMiles,
            TemperatureUnit = TemperatureUnit.Fahrenheit,
            TimeFormat = TimeFormat.TwelveHour,
            DateFormat = DateFormat.DayMonthYear,
            ProfileVisibility = ProfileVisibilityLevel.Public,
            ShowTotalMiles = false,
            ShowAirlines = false,
            ShowCountries = false,
            ShowMapRoutes = false,
            EnableActivityFeed = true
        };

        UserPreferences? persisted = null;

        var repository = new Mock<IUserPreferencesRepository>();
        repository
            .Setup(r => r.GetByUserIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences preferences, CancellationToken _) =>
            {
                persisted = preferences;
                return preferences;
            });

        var mapper = CreateMapper();
        var service = new UserPreferencesService(repository.Object, mapper.Object);

        var result = await service.UpdateAsync(9, update);

        Assert.NotNull(persisted);
        Assert.Equal(15, persisted!.Id);
        Assert.Equal(9, persisted.UserId);
        Assert.Equal(DistanceUnit.NauticalMiles, persisted.DistanceUnit);
        Assert.Equal(TemperatureUnit.Fahrenheit, persisted.TemperatureUnit);
        Assert.Equal(TimeFormat.TwelveHour, persisted.TimeFormat);
        Assert.Equal(DateFormat.DayMonthYear, persisted.DateFormat);
        Assert.Equal(ProfileVisibilityLevel.Public, persisted.ProfileVisibility);
        Assert.False(persisted.ShowTotalMiles);
        Assert.False(persisted.ShowAirlines);
        Assert.False(persisted.ShowCountries);
        Assert.False(persisted.ShowMapRoutes);
        Assert.True(persisted.EnableActivityFeed);
        Assert.True(persisted.UpdatedAtUtc > initialUpdatedAt);

        Assert.Equal(15, result.Id);
        Assert.Equal(9, result.UserId);
        Assert.Equal(DistanceUnit.NauticalMiles, result.DistanceUnit);
        Assert.True(result.EnableActivityFeed);

        repository.Verify(r => r.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.CreateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Creates_WhenPreferencesMissing()
    {
        var input = new UserPreferencesDto
        {
            DistanceUnit = DistanceUnit.Kilometers,
            TemperatureUnit = TemperatureUnit.Fahrenheit,
            TimeFormat = TimeFormat.TwelveHour,
            DateFormat = DateFormat.MonthDayYear,
            ProfileVisibility = ProfileVisibilityLevel.Public,
            ShowTotalMiles = false,
            ShowAirlines = true,
            ShowCountries = false,
            ShowMapRoutes = true,
            EnableActivityFeed = true
        };

        UserPreferences? created = null;

        var repository = new Mock<IUserPreferencesRepository>();
        repository
            .Setup(r => r.GetByUserIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences preferences, CancellationToken _) =>
            {
                created = preferences;
                preferences.Id = 500;
                return preferences;
            });

        var mapper = CreateMapper();
        var service = new UserPreferencesService(repository.Object, mapper.Object);

        var result = await service.UpdateAsync(99, input);

        Assert.NotNull(created);
        Assert.Equal(99, created!.UserId);
        Assert.Equal(DistanceUnit.Kilometers, created.DistanceUnit);
        Assert.Equal(TemperatureUnit.Fahrenheit, created.TemperatureUnit);
        Assert.Equal(TimeFormat.TwelveHour, created.TimeFormat);
        Assert.Equal(DateFormat.MonthDayYear, created.DateFormat);
        Assert.Equal(ProfileVisibilityLevel.Public, created.ProfileVisibility);
        Assert.False(created.ShowTotalMiles);
        Assert.True(created.ShowAirlines);
        Assert.False(created.ShowCountries);
        Assert.True(created.ShowMapRoutes);
        Assert.True(created.EnableActivityFeed);

        Assert.Equal(500, result.Id);
        Assert.Equal(99, result.UserId);

        repository.Verify(r => r.CreateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IMapper> CreateMapper()
    {
        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<UserPreferencesDto>(It.IsAny<UserPreferences>()))
            .Returns((UserPreferences source) => ToDto(source));
        mapper
            .Setup(m => m.Map<UserPreferences>(It.IsAny<UserPreferencesDto>()))
            .Returns((UserPreferencesDto source) => ToEntity(source));

        return mapper;
    }

    private static UserPreferencesDto ToDto(UserPreferences source)
    {
        return new UserPreferencesDto
        {
            Id = source.Id,
            UserId = source.UserId,
            DistanceUnit = source.DistanceUnit,
            TemperatureUnit = source.TemperatureUnit,
            TimeFormat = source.TimeFormat,
            DateFormat = source.DateFormat,
            ProfileVisibility = source.ProfileVisibility,
            ShowTotalMiles = source.ShowTotalMiles,
            ShowAirlines = source.ShowAirlines,
            ShowCountries = source.ShowCountries,
            ShowMapRoutes = source.ShowMapRoutes,
            EnableActivityFeed = source.EnableActivityFeed,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
    }

    private static UserPreferences ToEntity(UserPreferencesDto source)
    {
        return new UserPreferences
        {
            Id = source.Id,
            UserId = source.UserId,
            DistanceUnit = source.DistanceUnit,
            TemperatureUnit = source.TemperatureUnit,
            TimeFormat = source.TimeFormat,
            DateFormat = source.DateFormat,
            ProfileVisibility = source.ProfileVisibility,
            ShowTotalMiles = source.ShowTotalMiles,
            ShowAirlines = source.ShowAirlines,
            ShowCountries = source.ShowCountries,
            ShowMapRoutes = source.ShowMapRoutes,
            EnableActivityFeed = source.EnableActivityFeed,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
    }
}
