using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

public class AirportRepository(FlightTrackerDbContext db) : IAirportRepository
{
    public async Task<Airport?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Airport?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Airport>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Airports.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Airport> AddAsync(Airport airport, CancellationToken cancellationToken = default)
    {
        await db.Airports.AddAsync(airport, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return airport;
    }

    public async Task UpdateAsync(Airport airport, CancellationToken cancellationToken = default)
    {
        db.Airports.Update(airport);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Airports.FindAsync(new object?[] { id }, cancellationToken);
        if (entity == null) return;
        db.Airports.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
