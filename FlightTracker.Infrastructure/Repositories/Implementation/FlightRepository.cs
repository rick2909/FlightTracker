using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

public class FlightRepository(FlightTrackerDbContext db) : IFlightRepository
{
    public async Task<Flight?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<Flight?> GetByFlightNumberAndDateAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await db.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber
                && f.DepartureTimeUtc >= start && f.DepartureTimeUtc <= end, cancellationToken);
    }

    public async Task<IReadOnlyList<Flight>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Flight> AddAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        await db.Flights.AddAsync(flight, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return flight;
    }

    public async Task UpdateAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        db.Flights.Update(flight);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Flights.FindAsync(new object?[] { id }, cancellationToken);
        if (entity == null) return;
        db.Flights.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
