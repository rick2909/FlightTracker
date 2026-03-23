using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Services.Implementation;

public class PersonalAccessTokenService(
    IPersonalAccessTokenRepository repository,
    IClock clock,
    IMapper mapper) : IPersonalAccessTokenService
{
    private const int TokenByteLength = 32;

    private readonly IPersonalAccessTokenRepository _repository = repository;
    private readonly IClock _clock = clock;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<CreatePersonalAccessTokenResultDto>> CreateAsync(
        int userId,
        CreatePersonalAccessTokenDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Label))
            {
                return Result<CreatePersonalAccessTokenResultDto>.Failure(
                    "Token label is required.",
                    "access_token.label.required");
            }

            if (request.Scopes == PersonalAccessTokenScopes.None)
            {
                return Result<CreatePersonalAccessTokenResultDto>.Failure(
                    "At least one scope is required.",
                    "access_token.scope.required");
            }

            if (request.ExpiresAtUtc <= _clock.UtcNow)
            {
                return Result<CreatePersonalAccessTokenResultDto>.Failure(
                    "Expiration must be in the future.",
                    "access_token.expiry.invalid");
            }

            var plainTextToken = GenerateToken();
            var tokenHash = ComputeHash(plainTextToken);

            var token = new PersonalAccessToken
            {
                UserId = userId,
                Label = request.Label.Trim(),
                TokenPrefix = plainTextToken[..12],
                TokenHash = tokenHash,
                Scopes = request.Scopes,
                ExpiresAtUtc = request.ExpiresAtUtc,
                CreatedAtUtc = _clock.UtcNow,
                UpdatedAtUtc = _clock.UtcNow
            };

            var created = await _repository.CreateAsync(token, cancellationToken);

            var result = new CreatePersonalAccessTokenResultDto
            {
                Token = _mapper.Map<PersonalAccessTokenDto>(created),
                PlainTextToken = plainTextToken
            };

            return Result<CreatePersonalAccessTokenResultDto>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<CreatePersonalAccessTokenResultDto>.Failure(
                ex.Message,
                "access_token.create.failed");
        }
    }

    public async Task<Result<IReadOnlyList<PersonalAccessTokenDto>>> ListByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await _repository.ListByUserIdAsync(userId, cancellationToken);
            var mapped = tokens
                .Select(t => _mapper.Map<PersonalAccessTokenDto>(t))
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToList();

            return Result<IReadOnlyList<PersonalAccessTokenDto>>.Success(mapped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<PersonalAccessTokenDto>>.Failure(
                ex.Message,
                "access_token.list.failed");
        }
    }

    public async Task<Result<bool>> RevokeAsync(
        int userId,
        int tokenId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _repository.GetByIdAsync(tokenId, cancellationToken);
            if (token is null || token.UserId != userId)
            {
                return Result<bool>.Failure(
                    "Access token was not found.",
                    "access_token.not_found");
            }

            if (token.RevokedAtUtc.HasValue)
            {
                return Result<bool>.Success(true);
            }

            token.RevokedAtUtc = _clock.UtcNow;
            token.UpdatedAtUtc = _clock.UtcNow;
            await _repository.UpdateAsync(token, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                ex.Message,
                "access_token.revoke.failed");
        }
    }

    public async Task<Result<PersonalAccessTokenDto?>> ValidateTokenAsync(
        string plainTextToken,
        PersonalAccessTokenScopes requiredScopes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(plainTextToken))
            {
                return Result<PersonalAccessTokenDto?>.Success(null);
            }

            var tokenHash = ComputeHash(plainTextToken);
            var token = await _repository.GetByHashAsync(tokenHash, cancellationToken);
            if (token is null)
            {
                return Result<PersonalAccessTokenDto?>.Success(null);
            }

            var now = _clock.UtcNow;
            if (token.RevokedAtUtc.HasValue || token.ExpiresAtUtc <= now)
            {
                return Result<PersonalAccessTokenDto?>.Success(null);
            }

            if ((token.Scopes & requiredScopes) != requiredScopes)
            {
                return Result<PersonalAccessTokenDto?>.Success(null);
            }

            return Result<PersonalAccessTokenDto?>.Success(
                _mapper.Map<PersonalAccessTokenDto>(token));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<PersonalAccessTokenDto?>.Failure(
                ex.Message,
                "access_token.validate.failed");
        }
    }

    public async Task<Result> RecordUsageAsync(
        int tokenId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _repository.GetByIdAsync(tokenId, cancellationToken);
            if (token is null)
            {
                return Result.Failure(
                    "Access token was not found.",
                    "access_token.not_found");
            }

            token.LastUsedAtUtc = _clock.UtcNow;
            token.UpdatedAtUtc = _clock.UtcNow;
            await _repository.UpdateAsync(token, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "access_token.record_usage.failed");
        }
    }

    private static string GenerateToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        var tokenBody = Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return $"ft_pat_{tokenBody}";
    }

    private static string ComputeHash(string plainTextToken)
    {
        var bytes = Encoding.UTF8.GetBytes(plainTextToken);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
