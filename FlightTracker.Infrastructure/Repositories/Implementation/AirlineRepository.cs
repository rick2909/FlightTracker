using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

public class AirlineRepository(FlightTrackerDbContext db) : IAirlineRepository
{
    public async Task<Airline?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await db.Airlines.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Airline?> GetByIcaoAsync(string icaoCode, CancellationToken cancellationToken = default)
    {
        var norm = (icaoCode ?? string.Empty).Trim().ToUpperInvariant();
        return await db.Airlines.AsNoTracking().FirstOrDefaultAsync(a => a.IcaoCode.ToUpper() == norm, cancellationToken);
    }

    public async Task<Airline?> GetByIataAsync(string iataCode, CancellationToken cancellationToken = default)
    {
        var norm = (iataCode ?? string.Empty).Trim().ToUpperInvariant();
        return await db.Airlines.AsNoTracking().FirstOrDefaultAsync(a => a.IataCode != null && a.IataCode.ToUpper() == norm, cancellationToken);
    }

    public async Task<IEnumerable<Airline>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.Airlines.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IEnumerable<Airline>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var q = (searchTerm ?? string.Empty).Trim();
        var upper = q.ToUpperInvariant();
        return await db.Airlines.AsNoTracking()
            .Where(a => a.Name.Contains(q) || a.IcaoCode.ToUpper() == upper || (a.IataCode != null && a.IataCode.ToUpper() == upper))
            .ToListAsync(cancellationToken);
    }

    public async Task<Airline> AddAsync(Airline airline, CancellationToken cancellationToken = default)
    {
        await db.Airlines.AddAsync(airline, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return airline;
    }

    public async Task<Airline> UpdateAsync(Airline airline, CancellationToken cancellationToken = default)
    {
        db.Airlines.Update(airline);
        await db.SaveChangesAsync(cancellationToken);
        return airline;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Airlines.FindAsync(new object?[] { id }, cancellationToken);
        if (entity == null) return;
        db.Airlines.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => await db.Airlines.AsNoTracking().AnyAsync(a => a.Id == id, cancellationToken);
}
