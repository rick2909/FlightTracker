using FlightTracker.Api.Contracts.V1;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/users/{userId:int}/access-tokens")]
public class PersonalAccessTokensController(
    IPersonalAccessTokenService personalAccessTokenService) : ControllerBase
{
    private readonly IPersonalAccessTokenService _personalAccessTokenService =
        personalAccessTokenService;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PersonalAccessTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PersonalAccessTokenResponse>>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        var result = await _personalAccessTokenService.ListByUserIdAsync(
            userId,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        var payload = result.Value?
            .Select(Map)
            .ToArray()
            ?? Array.Empty<PersonalAccessTokenResponse>();

        return Ok(payload);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatePersonalAccessTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreatePersonalAccessTokenResponse>> CreateAsync(
        int userId,
        [FromBody] CreatePersonalAccessTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        var result = await _personalAccessTokenService.CreateAsync(
            userId,
            new CreatePersonalAccessTokenDto
            {
                Label = request.Label,
                Scopes = request.Scopes,
                ExpiresAtUtc = request.ExpiresAtUtc
            },
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        return CreatedAtAction(
            nameof(ListAsync),
            new { userId },
            new CreatePersonalAccessTokenResponse(
                Map(result.Value.Token),
                result.Value.PlainTextToken));
    }

    [HttpPost("revoke")]
    [ProducesResponseType(typeof(RevokePersonalAccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RevokePersonalAccessTokenResponse>> RevokeAsync(
        int userId,
        [FromBody] RevokePersonalAccessTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = this.EnsureRouteUser(userId, allowPersonalAccessToken: false);
        if (access is not null)
        {
            return access;
        }

        var result = await _personalAccessTokenService.RevokeAsync(
            userId,
            request.TokenId,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToFailure(result);
        }

        return Ok(new RevokePersonalAccessTokenResponse(result.Value));
    }

    private static PersonalAccessTokenResponse Map(PersonalAccessTokenDto dto)
    {
        return new PersonalAccessTokenResponse(
            dto.Id,
            dto.UserId,
            dto.Label,
            dto.TokenPrefix,
            dto.Scopes,
            dto.ExpiresAtUtc,
            dto.LastUsedAtUtc,
            dto.RevokedAtUtc,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc);
    }
}
