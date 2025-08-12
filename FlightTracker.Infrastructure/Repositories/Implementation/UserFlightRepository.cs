using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

/// <summary>
/// Repository implementation for managing user flight experiences.
/// </summary>
public class UserFlightRepository : IUserFlightRepository
{
    private readonly FlightTrackerDbContext _context;

    public UserFlightRepository(FlightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserFlight>> GetUserFlightsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFlights
            .AsNoTracking()
            .Where(uf => uf.UserId == userId)
            .Include(uf => uf.Flight)
                .ThenInclude(f => f!.DepartureAirport)
            .Include(uf => uf.Flight)
                .ThenInclude(f => f!.ArrivalAirport)
            .OrderByDescending(uf => uf.BookedOnUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserFlight?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.UserFlights
            .Include(uf => uf.Flight)
                .ThenInclude(f => f!.DepartureAirport)
            .Include(uf => uf.Flight)
                .ThenInclude(f => f!.ArrivalAirport)
            .FirstOrDefaultAsync(uf => uf.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<UserFlight>> GetUserFlightsByClassAsync(int userId, FlightClass flightClass, CancellationToken cancellationToken = default)
    {
        return await _context.UserFlights
            .AsNoTracking()
            .Where(uf => uf.UserId == userId && uf.FlightClass == flightClass)
            .Include(uf => uf.Flight)
                .ThenInclude(f => f!.DepartureAirport)
            .Include(uf => uf.Flight)
                .ThenInclude(f => f!.ArrivalAirport)
            .OrderByDescending(uf => uf.BookedOnUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserFlight>> GetFlightPassengersAsync(int flightId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFlights
            .AsNoTracking()
            .Where(uf => uf.FlightId == flightId)
            .Include(uf => uf.Flight)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserFlight> AddAsync(UserFlight userFlight, CancellationToken cancellationToken = default)
    {
        _context.UserFlights.Add(userFlight);
        await _context.SaveChangesAsync(cancellationToken);
        return userFlight;
    }

    public async Task<UserFlight> UpdateAsync(UserFlight userFlight, CancellationToken cancellationToken = default)
    {
        _context.UserFlights.Update(userFlight);
        await _context.SaveChangesAsync(cancellationToken);
        return userFlight;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var userFlight = await _context.UserFlights.FindAsync(new object[] { id }, cancellationToken);
        if (userFlight != null)
        {
            _context.UserFlights.Remove(userFlight);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUserFlightCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFlights
            .AsNoTracking()
            .CountAsync(uf => uf.UserId == userId && uf.DidFly, cancellationToken);
    }

    public async Task<bool> HasUserFlownFlightAsync(int userId, int flightId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFlights
            .AsNoTracking()
            .AnyAsync(uf => uf.UserId == userId && uf.FlightId == flightId, cancellationToken);
    }
}
