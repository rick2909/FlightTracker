using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

public class PersonalAccessTokenRepository(
    FlightTrackerDbContext context) : IPersonalAccessTokenRepository
{
    private readonly FlightTrackerDbContext _context = context;

    public async Task<PersonalAccessToken> CreateAsync(
        PersonalAccessToken token,
        CancellationToken cancellationToken = default)
    {
        _context.PersonalAccessTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<IReadOnlyList<PersonalAccessToken>> ListByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PersonalAccessTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PersonalAccessToken?> GetByIdAsync(
        int tokenId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);
    }

    public async Task<PersonalAccessToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _context.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<PersonalAccessToken> UpdateAsync(
        PersonalAccessToken token,
        CancellationToken cancellationToken = default)
    {
        _context.PersonalAccessTokens.Update(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }
}
