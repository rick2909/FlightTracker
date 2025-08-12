using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

/// <summary>
/// EF Core implementation of aircraft repository.
/// </summary>
public class AircraftRepository : IAircraftRepository
{
    private readonly FlightTrackerDbContext _context;

    public AircraftRepository(FlightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Aircraft?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Aircraft
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Aircraft?> GetByRegistrationAsync(string registration, CancellationToken cancellationToken = default)
    {
        return await _context.Aircraft
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Registration == registration, cancellationToken);
    }

    public async Task<IEnumerable<Aircraft>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Aircraft
            .AsNoTracking()
            .OrderBy(a => a.Registration)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Aircraft>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();
        return await _context.Aircraft
            .AsNoTracking()
            .Where(a => a.Registration.ToLower().Contains(term) ||
                       a.Manufacturer.ToString().ToLower().Contains(term) ||
                       a.Model.ToLower().Contains(term) ||
                       (a.IcaoTypeCode != null && a.IcaoTypeCode.ToLower().Contains(term)))
            .OrderBy(a => a.Registration)
            .ToListAsync(cancellationToken);
    }

    public async Task<Aircraft> AddAsync(Aircraft aircraft, CancellationToken cancellationToken = default)
    {
        _context.Aircraft.Add(aircraft);
        await _context.SaveChangesAsync(cancellationToken);
        return aircraft;
    }

    public async Task<Aircraft> UpdateAsync(Aircraft aircraft, CancellationToken cancellationToken = default)
    {
        _context.Aircraft.Update(aircraft);
        await _context.SaveChangesAsync(cancellationToken);
        return aircraft;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var aircraft = await _context.Aircraft.FindAsync(new object[] { id }, cancellationToken);
        if (aircraft != null)
        {
            _context.Aircraft.Remove(aircraft);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Aircraft
            .AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> RegistrationExistsAsync(string registration, CancellationToken cancellationToken = default)
    {
        return await _context.Aircraft
            .AnyAsync(a => a.Registration == registration, cancellationToken);
    }
}
