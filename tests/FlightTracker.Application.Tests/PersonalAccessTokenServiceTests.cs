using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using Moq;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class PersonalAccessTokenServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesToken_WithHashedStorageAndPlainTextResult()
    {
        var now = new DateTime(2026, 3, 23, 10, 0, 0, DateTimeKind.Utc);
        var repository = new Mock<IPersonalAccessTokenRepository>();
        repository
            .Setup(r => r.CreateAsync(It.IsAny<PersonalAccessToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalAccessToken token, CancellationToken _) =>
            {
                token.Id = 17;
                return token;
            });

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(now);

        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<PersonalAccessTokenDto>(It.IsAny<PersonalAccessToken>()))
            .Returns((PersonalAccessToken token) => new PersonalAccessTokenDto
            {
                Id = token.Id,
                UserId = token.UserId,
                Label = token.Label,
                TokenPrefix = token.TokenPrefix,
                Scopes = token.Scopes,
                ExpiresAtUtc = token.ExpiresAtUtc,
                LastUsedAtUtc = token.LastUsedAtUtc,
                RevokedAtUtc = token.RevokedAtUtc,
                CreatedAtUtc = token.CreatedAtUtc,
                UpdatedAtUtc = token.UpdatedAtUtc
            });

        var service = new PersonalAccessTokenService(repository.Object, clock.Object, mapper.Object);

        var result = await service.CreateAsync(
            42,
            new CreatePersonalAccessTokenDto
            {
                Label = "iPhone",
                Scopes = PersonalAccessTokenScopes.ReadFlights | PersonalAccessTokenScopes.ReadStats,
                ExpiresAtUtc = now.AddDays(14)
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value!.Token);
        Assert.Equal(42, result.Value.Token.UserId);
        Assert.Equal("iPhone", result.Value.Token.Label);
        Assert.StartsWith("ft_pat_", result.Value.PlainTextToken);
        Assert.Equal(result.Value.PlainTextToken[..12], result.Value.Token.TokenPrefix);

        repository.Verify(r => r.CreateAsync(
            It.Is<PersonalAccessToken>(t =>
                !string.IsNullOrWhiteSpace(t.TokenHash) &&
                !string.Equals(t.TokenHash, result.Value.PlainTextToken, StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsNull_WhenScopeIsMissing()
    {
        var now = new DateTime(2026, 3, 23, 10, 0, 0, DateTimeKind.Utc);
        var repository = new Mock<IPersonalAccessTokenRepository>();
        repository
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonalAccessToken
            {
                Id = 7,
                UserId = 42,
                Label = "test",
                TokenPrefix = "ft_pat_abcd",
                TokenHash = "ABC",
                Scopes = PersonalAccessTokenScopes.ReadFlights,
                ExpiresAtUtc = now.AddDays(1)
            });

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(now);

        var mapper = new Mock<IMapper>();
        var service = new PersonalAccessTokenService(repository.Object, clock.Object, mapper.Object);

        var result = await service.ValidateTokenAsync(
            "ft_pat_anything",
            PersonalAccessTokenScopes.WriteFlights);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task RevokeAsync_ReturnsNotFound_WhenTokenBelongsToAnotherUser()
    {
        var repository = new Mock<IPersonalAccessTokenRepository>();
        repository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonalAccessToken
            {
                Id = 5,
                UserId = 99,
                Label = "other",
                TokenPrefix = "ft_pat_other",
                TokenHash = "HASH",
                Scopes = PersonalAccessTokenScopes.ReadFlights,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(10)
            });

        var clock = new Mock<IClock>();
        var mapper = new Mock<IMapper>();
        var service = new PersonalAccessTokenService(repository.Object, clock.Object, mapper.Object);

        var result = await service.RevokeAsync(42, 5);

        Assert.True(result.IsFailure);
        Assert.Equal("access_token.not_found", result.ErrorCode);
    }
}
